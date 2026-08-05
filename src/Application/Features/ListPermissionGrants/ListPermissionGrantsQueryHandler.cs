using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.ListPermissionGrants;

public sealed class ListPermissionGrantsQueryHandler : IRequestHandler<ListPermissionGrantsQuery, Result<PagedResult<PermissionGrantDto>>>
{
    private readonly IPermissionGrantRepository _repository;

    public ListPermissionGrantsQueryHandler(IPermissionGrantRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<PermissionGrantDto>>> Handle(ListPermissionGrantsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.ListAsync(
            request.PrincipalId,
            request.Category,
            request.IncludeRevoked,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(PermissionGrantDto.FromDomain).ToList();
        return Result.Success(new PagedResult<PermissionGrantDto>(dtos, request.Page, request.PageSize, total));
    }
}
