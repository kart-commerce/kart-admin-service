using Kart.Shared.Observability;
using KartAdminService.Api.Common;
using KartAdminService.Api.Security;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.CreateAttribute;
using KartAdminService.Application.Features.DeprecateAttribute;
using KartAdminService.Application.Features.UpdateAttribute;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KartAdminService.Api.Controllers;

/// <summary>
/// api-contract.yaml /admin/attributes* — catalog-management category, proxies Category Service's
/// own Attribute write API. Added for the "Category &amp; Attribute Management (Admin)" flow.
/// </summary>
[ApiController]
[Route("v1/admin/attributes")]
[Authorize(Policy = AuthenticationExtensions.AdminPolicy)]
public sealed class AttributesController : AdminControllerBase
{
    private const string FlowName = "CategoryAttributeManagementAdmin";

    private readonly ISender _sender;
    private readonly ILogger<AttributesController> _logger;

    public AttributesController(ISender sender, ILogger<AttributesController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> CreateAttribute(
        [FromBody] AttributeWriteRequest attribute,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: create attribute received (categoryId {CategoryId})", "AdminAttributesControllerReceived", attribute.CategoryId);

        var command = new CreateAttributeCommand(ActingPrincipalId, attribute, idempotencyKey);
        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Created(string.Empty, r));
    }

    [HttpPut("{attributeId}")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> UpdateAttribute(
        [FromRoute] string attributeId,
        [FromBody] AttributeUpdateRequest attribute,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: update attribute {AttributeId} received", "AdminAttributesControllerReceived", attributeId);

        var command = new UpdateAttributeCommand(ActingPrincipalId, attributeId, attribute, idempotencyKey);
        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }

    [HttpDelete("{attributeId}")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> DeprecateAttribute(
        [FromRoute] string attributeId,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: deprecate attribute {AttributeId} received", "AdminAttributesControllerReceived", attributeId);

        var command = new DeprecateAttributeCommand(ActingPrincipalId, attributeId, idempotencyKey);
        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }
}
