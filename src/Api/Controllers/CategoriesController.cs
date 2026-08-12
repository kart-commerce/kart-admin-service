using Kart.Shared.Observability;
using KartAdminService.Api.Common;
using KartAdminService.Api.Security;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.CreateCategory;
using KartAdminService.Application.Features.MoveCategory;
using KartAdminService.Application.Features.ReorderCategory;
using KartAdminService.Application.Features.UpdateCategory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KartAdminService.Api.Controllers;

/// <summary>api-contract.yaml /admin/categories* (ADM-7, ADM-8, ADM-9, ADM-10) — catalog-management category, proxies Category Service's own write API. Every action here belongs to the "Category & Attribute Management (Admin)" flow (KartFlowContext.Push mirrors ProductsController's own convention).</summary>
[ApiController]
[Route("v1/admin/categories")]
[Authorize(Policy = AuthenticationExtensions.AdminPolicy)]
public sealed class CategoriesController : AdminControllerBase
{
    private const string FlowName = "CategoryAttributeManagementAdmin";

    private readonly ISender _sender;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ISender sender, ILogger<CategoriesController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> CreateCategory(
        [FromBody] CategoryWriteRequest category,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: create category received (parentId {ParentId})", "AdminCategoriesControllerReceived", category.ParentId);

        var result = await _sender.Send(new CreateCategoryCommand(ActingPrincipalId, category, idempotencyKey), cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Created(string.Empty, r));
    }

    [HttpPut("{categoryId}")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> UpdateCategory(
        [FromRoute] string categoryId,
        [FromBody] CategoryWriteRequest category,
        [FromHeader(Name = "If-Match")] string ifMatch,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: update category {CategoryId} received", "AdminCategoriesControllerReceived", categoryId);

        var result = await _sender.Send(new UpdateCategoryCommand(ActingPrincipalId, categoryId, category, ifMatch, idempotencyKey), cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }

    [HttpPost("{categoryId}/reorder")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> ReorderCategory(
        [FromRoute] string categoryId,
        [FromBody] ReorderCategoryRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: reorder category {CategoryId} received (displayOrder {DisplayOrder})", "AdminCategoriesControllerReceived", categoryId, request.DisplayOrder);

        var result = await _sender.Send(new ReorderCategoryCommand(ActingPrincipalId, categoryId, request.DisplayOrder, idempotencyKey), cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }

    [HttpPost("{categoryId}/move")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> MoveCategory(
        [FromRoute] string categoryId,
        [FromBody] MoveCategoryRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: move category {CategoryId} received", "AdminCategoriesControllerReceived", categoryId);

        var result = await _sender.Send(new MoveCategoryCommand(ActingPrincipalId, categoryId, request.NewParentId, idempotencyKey), cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }
}

/// <summary>api-contract.yaml reorderCategory requestBody shape.</summary>
public sealed record ReorderCategoryRequest(int DisplayOrder);

/// <summary>api-contract.yaml moveCategory requestBody shape. NewParentId null moves the node to become a root category.</summary>
public sealed record MoveCategoryRequest(string? NewParentId);
