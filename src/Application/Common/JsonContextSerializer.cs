using System.Text.Json;

namespace KartAdminService.Application.Common;

/// <summary>
/// Serializes a feature's request shape into admin_actions.context (database-design.md: "optional
/// richer audit detail beyond the three fields the AdminActionPerformed event actually carries").
/// One place so every handler gets the same JSON conventions (camelCase, no nulls omitted vs
/// included consistently) instead of each slice hand-rolling its own serialization call.
/// </summary>
public static class JsonContextSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}
