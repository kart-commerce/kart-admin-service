using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.ListAdminActions;

public sealed class ListAdminActionsQueryHandler : IRequestHandler<ListAdminActionsQuery, Result<PagedResult<AdminActionResultDto>>>
{
    private readonly IAdminActionRepository _repository;

    public ListAdminActionsQueryHandler(IAdminActionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<AdminActionResultDto>>> Handle(ListAdminActionsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.ListAsync(
            request.AdminId,
            request.Category,
            request.From,
            request.To,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(AdminActionResultDto.FromDomain).ToList();
        return Result.Success(new PagedResult<AdminActionResultDto>(dtos, request.Page, request.PageSize, total));
    }
}
