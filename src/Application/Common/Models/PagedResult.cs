namespace KartAdminService.Application.Common.Models;

/// <summary>Shared paging envelope for GET /admin/permission-grants and GET /admin/actions — one consistent list-response shape (api-standards.md's consistent-response-model rule).</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
