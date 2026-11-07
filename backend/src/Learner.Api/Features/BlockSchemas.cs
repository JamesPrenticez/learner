using System.Text.Json;

namespace Learner.Api.Features;

/// <summary>
/// Validation schema per content-block type. The server stores block payloads as opaque JSON and only
/// knows the shape here, so a new block type = one line in <see cref="Schemas"/> (plus a renderer in the frontend registry).
/// </summary>
public static class BlockSchemas
{
    enum Kind { Text, Number }

    record Field(string Name, Kind Kind, bool Required = false, string[]? OneOf = null, bool HttpUrl = false);

    static Field Text(string name, bool required = false) => new(name, Kind.Text, required);
    static Field Id(string name) => new(name, Kind.Number, true);

    static readonly Dictionary<string, Field[]> Schemas = new()
    {
        ["richtext"] = [Text("markdown", true)],
        ["image"] = [Text("mediaId", true), Text("alt", true), Text("caption")],
        ["video"] = [new("url", Kind.Text, true, HttpUrl: true), Text("caption"), Text("description"), Text("transcript")],
        ["document"] = [Text("mediaId", true), Text("title", true)],
        ["callout"] = [new("variant", Kind.Text, true, OneOf: ["info", "warning", "tip"]), Text("text", true)],
        ["knowledgecheck"] = [Id("quizId")],
        ["quiz"] = [Id("quizId")],
        ["exercise"] = [Id("exerciseId")],
    };

    public static bool Known(string type) => Schemas.ContainsKey(type);

    /// <summary>
    /// Problems with <paramref name="data"/>. Drafts (strict = false) only reject wrong value types so authors can save half-finished
    /// blocks; strict (publish) also enforces required fields.
    /// </summary>
    public static List<string> Problems(string type, JsonElement data, bool strict)
    {
        var problems = new List<string>();
        if (!Schemas.TryGetValue(type, out var fields)) return [$"Unknown block type '{type}'."];
        if (data.ValueKind != JsonValueKind.Object) return ["Block data must be an object."];

        foreach (var f in fields)
        {
            var present = data.TryGetProperty(f.Name, out var v) && v.ValueKind != JsonValueKind.Null;
            if (present && v.ValueKind != (f.Kind == Kind.Text ? JsonValueKind.String : JsonValueKind.Number))
            {
                problems.Add($"'{f.Name}' must be a {(f.Kind == Kind.Text ? "string" : "number")}.");
                continue;
            }
            var empty = !present || (f.Kind == Kind.Text && string.IsNullOrWhiteSpace(v.GetString()));
            if (empty)
            {
                if (strict && f.Required) problems.Add($"'{f.Name}' is required.");
                continue;
            }
            var text = f.Kind == Kind.Text ? v.GetString()! : "";
            if (f.OneOf is not null && !f.OneOf.Contains(text)) problems.Add($"'{f.Name}' must be one of {string.Join(", ", f.OneOf)}.");
            if (f.HttpUrl && !(Uri.TryCreate(text, UriKind.Absolute, out var u) && (u.Scheme is "http" or "https"))) problems.Add($"'{f.Name}' must be an http(s) URL.");
        }
        return problems;
    }
}
