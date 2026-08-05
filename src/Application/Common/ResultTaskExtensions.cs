using Kart.Shared.Domain;

namespace KartAdminService.Application.Common;

/// <summary>
/// AdminActionExecutor always needs a Func&lt;CancellationToken, Task&lt;Result&lt;string&gt;&gt;&gt;
/// (the resulting EntityId). For update/deactivate/lock/unlock/replenish/reorder/move-style calls,
/// the EntityId is already known from the route (unlike create, where the downstream client hands
/// back a newly-minted id) — this adapts a plain Task&lt;Result&gt; downstream call into that shape
/// without every handler re-writing the same three-line if/else.
/// </summary>
public static class ResultTaskExtensions
{
    public static async Task<Result<string>> WithKnownEntityId(this Task<Result> callTask, string entityId)
    {
        var result = await callTask;
        return result.IsSuccess ? Result.Success(entityId) : Result.Failure<string>(result.Error);
    }
}
