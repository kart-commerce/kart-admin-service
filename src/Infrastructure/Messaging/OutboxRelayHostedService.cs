using System.Text;
using System.Text.Json;
using Kart.Shared.Messaging;
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
                _logger.LogError(ex, "Admin action outbox relay lost its RabbitMQ connection; reconnecting in {Delay}.", ReconnectDelay);
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
        var payload = new AdminActionPerformedPayload(action.AdminId, action.Action, action.EntityId);
        var body = JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = action.ActionId.ToString();
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: _manifest.ExchangeFor(AdminActionPerformedEventType),
            routingKey: _manifest.RoutingKeyFor(AdminActionPerformedEventType),
            basicProperties: properties,
            body: body);

        action.MarkPublished(DateTimeOffset.UtcNow);
    }
}
