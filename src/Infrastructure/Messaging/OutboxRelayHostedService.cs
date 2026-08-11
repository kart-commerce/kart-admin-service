using System.Text;
using System.Text.Json;
using Kart.Shared.Messaging;
using Kart.Shared.Observability;
using KartAdminService.Domain.Actions;
using KartAdminService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace KartAdminService.Infrastructure.Messaging;

/// <summary>
/// Relays admin_actions rows to admin.exchange (design-decisions.md, "Audit Trail Publication
/// Atomicity" — Transactional Outbox). Re-declares the manifest's topology idempotently on every
/// (re)connect. Admin owns no consumer queues of its own — Analytics binds its own queue+DLQ to
/// admin.exchange. Connects lazily with its own retry loop so a RabbitMQ outage at boot degrades
/// publish latency, never crashes the Api process — the durable admin_actions row itself is never
/// at risk (event-contract.md's Retry-Tier Justification: "the durable audit record does not
/// depend on this RabbitMQ delivery succeeding").
/// </summary>
public sealed class OutboxRelayHostedService : BackgroundService
{
    private const string AdminActionPerformedEventType = "AdminActionPerformed";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(10);
    private const int BatchSize = 50;

    // Each completed flow's instrumentation pass adds its own action-name -> Flow entry here;
    // action names not present in this map (coupons/users/inventory/permission-management, as of
    // this comment) aren't tagged with a Flow yet, pending their own flow's pass, per the
    // platform-wide standard's per-flow rollout.
    private static readonly Dictionary<string, string> ActionFlowNames = new()
    {
        [KartAdminService.Domain.Common.ActionNames.ProductCreate] = "ProductCatalogManagementAdmin",
        [KartAdminService.Domain.Common.ActionNames.ProductUpdate] = "ProductCatalogManagementAdmin",
        [KartAdminService.Domain.Common.ActionNames.ProductDeactivate] = "ProductCatalogManagementAdmin",
        [KartAdminService.Domain.Common.ActionNames.CategoryCreate] = "CategoryAttributeManagementAdmin",
        [KartAdminService.Domain.Common.ActionNames.CategoryUpdate] = "CategoryAttributeManagementAdmin",
        [KartAdminService.Domain.Common.ActionNames.CategoryReorder] = "CategoryAttributeManagementAdmin",
        [KartAdminService.Domain.Common.ActionNames.CategoryMove] = "CategoryAttributeManagementAdmin",
        [KartAdminService.Domain.Common.ActionNames.AttributeCreate] = "CategoryAttributeManagementAdmin",
        [KartAdminService.Domain.Common.ActionNames.AttributeUpdate] = "CategoryAttributeManagementAdmin",
        [KartAdminService.Domain.Common.ActionNames.AttributeDeprecate] = "CategoryAttributeManagementAdmin",
    };

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionFactory _connectionFactory;
    private readonly MessageBusManifest _manifest;
    private readonly ILogger<OutboxRelayHostedService> _logger;

    public OutboxRelayHostedService(
        IServiceScopeFactory scopeFactory,
        IConnectionFactory connectionFactory,
        MessageBusManifest manifest,
        ILogger<OutboxRelayHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionFactory = connectionFactory;
        _manifest = manifest;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var channel = connection.CreateModel();
                RabbitMqTopologyProvisioner.Declare(channel, _manifest);

                await RunRelayLoopAsync(channel, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stage {Stage}: admin action outbox relay lost its RabbitMQ connection; reconnecting in {Delay}.", "RabbitMqPublishRetryScheduled", ReconnectDelay);
                await Task.Delay(ReconnectDelay, stoppingToken);
            }
        }
    }

    private async Task RunRelayLoopAsync(IModel channel, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RelayPendingBatchAsync(channel, stoppingToken);
            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task RelayPendingBatchAsync(IModel channel, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        var pending = await dbContext.AdminActions
            .Where(a => a.PublishedAt == null)
            .OrderBy(a => a.PerformedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var action in pending)
        {
            PublishOne(channel, action);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void PublishOne(IModel channel, AdminAction action)
    {
        using var flowScope = ActionFlowNames.TryGetValue(action.Action, out var flowName)
            ? KartFlowContext.Push(flowName)
            : null;

        var payload = new AdminActionPerformedPayload(action.AdminId, action.Action, action.EntityId);
        var body = JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);

        var exchange = _manifest.ExchangeFor(AdminActionPerformedEventType);
        var routingKey = _manifest.RoutingKeyFor(AdminActionPerformedEventType);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = action.ActionId.ToString();
        properties.ContentType = "application/json";

        using var activity = RabbitMqTraceContext.StartPublishActivityFromStoredTraceParent(exchange, routingKey, action.TraceParent, properties);

        channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        action.MarkPublished(DateTimeOffset.UtcNow);

        _logger.LogInformation(
            "Stage {Stage}: admin action {ActionId} ({Action}) published to {Exchange}/{RoutingKey}",
            "AdminOutboxEventPublished",
            action.ActionId,
            action.Action,
            exchange,
            routingKey);
    }
}
