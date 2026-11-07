using System.Text.Json;
using System.Text.Json.Nodes;

namespace Learner.Api.Features;

/// <summary>What marking a submission produced. <see cref="Details"/> is component-specific and rendered by that component's Player.</summary>
public record ExerciseResult(double Score, double MaxScore, string Summary, JsonNode Details);

/// <summary>
/// The server half of the exercise integration contract (see .docs/exercise-contract.md).
/// A component supplies three pure functions; the framework handles storage, access, attempts, versioning and competency evidence.
/// </summary>
/// <param name="Validate">Is this teacher configuration complete and usable? Returns the first problem, or null.</param>
/// <param name="ForLearner">The configuration with the marking key removed, safe to send before the learner submits.</param>
/// <param name="Evaluate">Marks a submission against the full configuration.</param>
public record ExerciseType(
    string Key, string Label,
    Func<JsonElement, string?> Validate,
    Func<JsonElement, JsonNode> ForLearner,
    Func<JsonElement, JsonElement, ExerciseResult> Evaluate);

public static class ExerciseTypes
{
    /// <summary>Add a component by writing its ExerciseType and listing it here. Nothing else on the server changes.</summary>
    public static readonly Dictionary<string, ExerciseType> All = new[] { ContractReview.Type }.ToDictionary(t => t.Key);
}
