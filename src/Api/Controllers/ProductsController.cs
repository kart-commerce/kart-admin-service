using KartAdminService.Api.Common;
using KartAdminService.Api.Security;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.CreateProduct;
using KartAdminService.Application.Features.DeactivateProduct;
using KartAdminService.Application.Features.UpdateProduct;
using Kart.Shared.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KartAdminService.Api.Controllers;

/// <summary>api-contract.yaml /admin/products* (ADM-4, ADM-5, ADM-6) — catalog-management category, proxies Product Service's own write API.</summary>
[ApiController]
[Route("v1/admin/products")]
[Authorize(Policy = AuthenticationExtensions.AdminPolicy)]
public sealed class ProductsController : AdminControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ISender sender, ILogger<ProductsController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> CreateProduct(
        [FromBody] ProductWriteRequest product,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push("ProductCatalogManagementAdmin");
        _logger.LogInformation("Stage {Stage}: create product received for sku {Sku}", "AdminProductsControllerReceived", product.Sku);

        var command = new CreateProductCommand(ActingPrincipalId, product, idempotencyKey);
        _logger.LogInformation("Stage {Stage}: dispatching CreateProductCommand for sku {Sku}", "CreateProductCommandDispatched", product.Sku);
        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Created(string.Empty, r));
    }

    [HttpPut("{productId}")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> UpdateProduct(
        [FromRoute] string productId,
        [FromBody] ProductWriteRequest product,
        [FromHeader(Name = "If-Match")] string ifMatch,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push("ProductCatalogManagementAdmin");
        _logger.LogInformation("Stage {Stage}: update product {ProductId} received", "AdminProductsControllerReceived", productId);

        var command = new UpdateProductCommand(ActingPrincipalId, productId, product, ifMatch, idempotencyKey);
        _logger.LogInformation("Stage {Stage}: dispatching UpdateProductCommand for product {ProductId}", "UpdateProductCommandDispatched", productId);
        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }

    [HttpPost("{productId}/deactivate")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> DeactivateProduct(
        [FromRoute] string productId,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push("ProductCatalogManagementAdmin");
        _logger.LogInformation("Stage {Stage}: deactivate product {ProductId} received", "AdminProductsControllerReceived", productId);

        var command = new DeactivateProductCommand(ActingPrincipalId, productId, idempotencyKey);
        _logger.LogInformation("Stage {Stage}: dispatching DeactivateProductCommand for product {ProductId}", "DeactivateProductCommandDispatched", productId);
        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }
}
