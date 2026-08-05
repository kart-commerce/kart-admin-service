using KartAdminService.Domain.Actions;
using KartAdminService.Domain.Common;

namespace KartAdminService.Application.Common.Models;

/// <summary>api-contract.yaml AdminActionResult schema.</summary>
public sealed record AdminActionResultDto(
    Guid ActionId,
    string AdminId,
    string Category,
    string Action,
    string EntityId,
    string? Context,
    DateTimeOffset PerformedAt,
    DateTimeOffset? PublishedAt)
{
    public static AdminActionResultDto FromDomain(AdminAction action) => new(
        action.ActionId,
        action.AdminId,
        action.Category.ToWireValue(),
        action.Action,
        action.EntityId,
        action.Context,
        action.PerformedAt,
        action.PublishedAt);
}
