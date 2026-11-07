using System.Text.Json;
using System.Text.Json.Nodes;

namespace Learner.Api.Features;

/// <summary>
/// First exercise component. The teacher supplies contract clauses and marks which contain a planted problem;
/// the learner flags the clauses they think are problematic.
///
/// config:      { scenario, instructions, clauses: [{ id, title, text }], issues: [{ clauseId, explanation, points }] }
/// submission:  { flags: [{ clauseId, note }] }
/// Score = points for each issue found, minus 1 for each clause flagged that has no issue (never below zero).
/// </summary>
public static class ContractReview
{
    public static readonly ExerciseType Type = new("contract-review", "Contract review", Validate, ForLearner, Evaluate);

    // Array items come straight from the learner, so tolerate anything that is not an object.
    static string Str(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
    static IEnumerable<JsonElement> Items(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array ? v.EnumerateArray() : [];
    static int Points(JsonElement issue) => issue.TryGetProperty("points", out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var n) && n > 0 ? n : 1;

    static string? Validate(JsonElement c)
    {
        if (c.ValueKind != JsonValueKind.Object) return "Configuration must be an object.";
        var clauses = Items(c, "clauses").ToList();
        if (clauses.Count == 0) return "Add at least one clause.";
        var ids = clauses.Select(x => Str(x, "id")).ToList();
        if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != ids.Count) return "Every clause needs a unique id.";
        if (clauses.Any(x => string.IsNullOrWhiteSpace(Str(x, "text")))) return "Every clause needs text.";

        var issues = Items(c, "issues").ToList();
        if (issues.Count == 0) return "Mark at least one clause as containing an issue.";
        var issueClauses = issues.Select(x => Str(x, "clauseId")).ToList();
        if (issueClauses.Any(i => !ids.Contains(i)) || issueClauses.Distinct().Count() != issueClauses.Count) return "Each issue must refer to a different, existing clause.";
        if (issues.Any(x => string.IsNullOrWhiteSpace(Str(x, "explanation")))) return "Explain every issue so learners get feedback.";
        return null;
    }

    static JsonNode ForLearner(JsonElement c)
    {
        var node = JsonNode.Parse(c.GetRawText())!.AsObject();
        node.Remove("issues"); // the marking key
        return node;
    }

    static ExerciseResult Evaluate(JsonElement c, JsonElement submission)
    {
        var clauseIds = Items(c, "clauses").Select(x => Str(x, "id")).ToHashSet();
        var flagged = Items(submission, "flags").Select(x => Str(x, "clauseId")).Where(clauseIds.Contains).ToHashSet();

        var issues = Items(c, "issues").Select(i => (clause: Str(i, "clauseId"), explanation: Str(i, "explanation"), points: Points(i))).ToList();
        var found = issues.Where(i => flagged.Contains(i.clause)).ToList();
        var falseFlags = flagged.Where(f => issues.All(i => i.clause != f)).Order().ToList();

        var max = issues.Sum(i => i.points);
        var score = Math.Max(0, found.Sum(i => i.points) - falseFlags.Count);
        var details = new JsonObject
        {
            ["issues"] = new JsonArray(issues.Select(i => (JsonNode)new JsonObject
            {
                ["clauseId"] = i.clause,
                ["explanation"] = i.explanation,
                ["points"] = i.points,
                ["found"] = flagged.Contains(i.clause),
            }).ToArray()),
            ["falseFlags"] = new JsonArray(falseFlags.Select(f => (JsonNode)f).ToArray()),
        };
        return new ExerciseResult(score, max, $"Found {found.Count} of {issues.Count} issues, with {falseFlags.Count} incorrect {(falseFlags.Count == 1 ? "flag" : "flags")}.", details);
    }
}
