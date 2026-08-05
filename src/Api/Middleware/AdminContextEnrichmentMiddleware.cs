using System.IdentityModel.Tokens.Jwt;
using Serilog.Context;

namespace KartAdminService.Api.Middleware;

/// <summary>
/// requirement-spec.md's Observability NFR row: "every log/trace carries adminId/entityId
/// alongside the mandatory traceId/service/level fields." adminId comes from the validated
/// JWT's `sub` claim (present on every authenticated request); entityId, where the route has
/// one, is pushed by each route's own {productId}/{categoryId}/{couponCode}/{userId}/{sku}/
/// {grantId} route value — this middleware pushes whichever one the current route defines,
/// under one common `entityId` log property, so a Tempo trace/Loki log line always uses the
/// same field name regardless of which /admin/* sub-surface handled the request.
/// </summary>
public sealed class AdminContextEnrichmentMiddleware
{
    private static readonly string[] EntityIdRouteKeys =
    [
        "productId",
        "categoryId",
        "couponCode",
        "userId",
        "sku",
        "grantId",
    ];

    private readonly RequestDelegate _next;

    public AdminContextEnrichmentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var adminIdScope = PushAdminId(context);
        using var entityIdScope = PushEntityId(context);
        await _next(context);
    }

    private static IDisposable? PushAdminId(HttpContext context)
    {
        var adminId = context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return adminId is null ? null : LogContext.PushProperty("adminId", adminId);
    }

    private static IDisposable? PushEntityId(HttpContext context)
    {
        foreach (var key in EntityIdRouteKeys)
        {
            if (context.Request.RouteValues.TryGetValue(key, out var value) && value is not null)
            {
                return LogContext.PushProperty("entityId", value);
            }
        }

        return null;
    }
}
