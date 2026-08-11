using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.ResolveOrderFulfillmentException;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.ResolveOrderFulfillmentException;

public sealed class ResolveOrderFulfillmentExceptionCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IOrderServiceClient> _client = new();
    private readonly ResolveOrderFulfillmentExceptionCommandHandler _handler;

    public ResolveOrderFulfillmentExceptionCommandHandlerTests()
    {
        _handler = new ResolveOrderFulfillmentExceptionCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_ResolvesWithRetry()
    {
        var orderId = Guid.NewGuid();
        GrantIsLiveFor(PermissionCategory.OrderManagement);
        _client.Setup(c => c.ResolveFulfillmentExceptionAsync(orderId, "retry", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new ResolveOrderFulfillmentExceptionCommand(ActingPrincipalId, orderId, "retry", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(ActionNames.OrderFulfillmentExceptionResolve);
    }
}
