using System.Text.Json;
using System.Text.Json.Nodes;

namespace Learner.Api.Features;

/// <summary>
/// A question type: how its payload is validated when authored and how a learner response is graded.
/// <see cref="Secret"/> lists the payload keys that reveal the answer; they are stripped before a learner sees the question.
/// Adding a type (matching, ordering, ...) = one entry in <see cref="QuestionTypes.All"/> plus an editor and player in the frontend.
/// </summary>
public record QuestionType(string Key, string[] Secret, Func<JsonElement, string?> Validate, Func<JsonElement, JsonElement, double> Grade);

public static class QuestionTypes
{
    public static readonly Dictionary<string, QuestionType> All = new QuestionType[]
    {
        // ponytail: multiple choice is all-or-nothing. Add partial credit here if teachers ask for it.
        new("single", ["correct"], d => ValidateChoices(d, multiple: false), (d, r) => Eq(Str(r, "choice"), Str(d, "correct"))),
        new("multiple", ["correct"], d => ValidateChoices(d, multiple: true), (d, r) => SameSet(Strings(r, "choices"), Strings(d, "correct"))),
        new("truefalse", ["correct"],
            d => d.TryGetProperty("correct", out var c) && c.ValueKind is JsonValueKind.True or JsonValueKind.False ? null : "'correct' must be true or false.",
            (d, r) => r.TryGetProperty("value", out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False && v.GetBoolean() == d.GetProperty("correct").GetBoolean() ? 1 : 0),
        new("fillblank", ["accepted", "caseSensitive"], ValidateFill, GradeFill),
        new("numeric", ["answer", "tolerance"], ValidateNumeric, GradeNumeric),
    }.ToDictionary(t => t.Key);

    /// <summary>The payload as a learner may see it: answer keys removed.</summary>
    public static JsonNode ForLearner(QuestionType type, string dataJson)
    {
        var node = JsonNode.Parse(dataJson)!.AsObject();
        foreach (var key in type.Secret) node.Remove(key);
        return node;
    }

    // ---- helpers

    static string Str(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";

    static List<string> Strings(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array
            ? v.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList() : [];

    static double Eq(string a, string b) => a != "" && a == b ? 1 : 0;

    static double SameSet(List<string> a, List<string> b) => a.Count > 0 && a.ToHashSet().SetEquals(b) ? 1 : 0;

    static string? ValidateChoices(JsonElement d, bool multiple)
    {
        if (!d.TryGetProperty("options", out var opts) || opts.ValueKind != JsonValueKind.Array || opts.GetArrayLength() < 2) return "Provide at least two options.";
        var ids = new List<string>();
        foreach (var o in opts.EnumerateArray())
        {
            if (o.ValueKind != JsonValueKind.Object || Str(o, "id") == "" || Str(o, "text").Trim() == "") return "Every option needs an id and text.";
            ids.Add(Str(o, "id"));
        }
        if (ids.Distinct().Count() != ids.Count) return "Option ids must be unique.";
        if (!multiple) return ids.Contains(Str(d, "correct")) ? null : "Choose the correct option.";
        var correct = Strings(d, "correct");
        return correct.Count > 0 && correct.All(ids.Contains) ? null : "Choose at least one correct option.";
    }

    static string? ValidateFill(JsonElement d)
    {
        var accepted = Strings(d, "accepted");
        return accepted.Count > 0 && accepted.All(a => a.Trim() != "") ? null : "Provide at least one accepted answer.";
    }

    static double GradeFill(JsonElement d, JsonElement r)
    {
        var given = string.Join(' ', Str(r, "text").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (given == "") return 0;
        var cs = d.TryGetProperty("caseSensitive", out var c) && c.ValueKind == JsonValueKind.True;
        var cmp = cs ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return Strings(d, "accepted").Any(a => string.Equals(string.Join(' ', a.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)), given, cmp)) ? 1 : 0;
    }

    static string? ValidateNumeric(JsonElement d)
    {
        if (!d.TryGetProperty("answer", out var a) || a.ValueKind != JsonValueKind.Number) return "'answer' must be a number.";
        if (d.TryGetProperty("tolerance", out var t) && (t.ValueKind != JsonValueKind.Number || t.GetDouble() < 0)) return "'tolerance' must be a number of zero or more.";
        return null;
    }

    static double GradeNumeric(JsonElement d, JsonElement r)
    {
        if (!r.TryGetProperty("value", out var v)) return 0;
        double given;
        if (v.ValueKind == JsonValueKind.Number) given = v.GetDouble();
        else if (!(v.ValueKind == JsonValueKind.String && double.TryParse(v.GetString(), System.Globalization.CultureInfo.InvariantCulture, out given))) return 0;
        var tol = d.TryGetProperty("tolerance", out var t) ? t.GetDouble() : 0;
        return Math.Abs(given - d.GetProperty("answer").GetDouble()) <= tol + 1e-9 ? 1 : 0;
    }
}
