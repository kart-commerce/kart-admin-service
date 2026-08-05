using KartAdminService.Application.Common.Interfaces;

namespace KartAdminService.IntegrationTests;

public sealed class FakeCurrentPrincipal : ICurrentPrincipal
{
    public FakeCurrentPrincipal(string principalId)
    {
        PrincipalId = principalId;
    }

    public string PrincipalId { get; }
}
