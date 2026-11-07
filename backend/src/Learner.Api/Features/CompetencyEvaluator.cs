namespace Learner.Api.Features;

public enum CompetencyState { NotAssessed, Developing, Demonstrated, RequiresReview }

/// <summary>Evaluation rules. Bound from the "Competency" section of appsettings.json so they can change without code changes.</summary>
public class CompetencyRules
{
    /// <summary>Weighted score (0..1) over recent evidence needed to count as demonstrated.</summary>
    public double DemonstratedThreshold { get; set; } = 0.8;
    /// <summary>Minimum number of pieces of recent evidence. One lucky quiz is not enough.</summary>
    public int MinEvidence { get; set; } = 2;
    /// <summary>Only this many of the newest pieces of evidence count, so the profile follows current performance.</summary>
    public int RecentEvidence { get; set; } = 5;
    /// <summary>How much more or less each kind of evidence counts. Unlisted sources count 1.</summary>
    public Dictionary<string, double> SourceWeights { get; set; } = new() { ["quiz"] = 1, ["exercise"] = 1.5 };
}

public record EvidenceItem(string Source, double Score, double Weight, DateTime At);
public record CompetencyResult(CompetencyState State, double Score, int Evidence);

/// <summary>Turns evidence into a competency state. Pure: no database, no clock.</summary>
public static class CompetencyEvaluator
{
    // ponytail: RequiresReview is reserved for evidence needing a teacher (manual exercise marking); nothing produces it yet.
    public static CompetencyResult Evaluate(IEnumerable<EvidenceItem> evidence, CompetencyRules rules)
    {
        var all = evidence.OrderByDescending(e => e.At).ToList();
        if (all.Count == 0) return new(CompetencyState.NotAssessed, 0, 0);

        var recent = all.Take(rules.RecentEvidence).ToList();
        double Weight(EvidenceItem e) => e.Weight * rules.SourceWeights.GetValueOrDefault(e.Source, 1);
        var total = recent.Sum(Weight);
        var score = total > 0 ? recent.Sum(e => e.Score * Weight(e)) / total : 0;

        var demonstrated = recent.Count >= rules.MinEvidence && score >= rules.DemonstratedThreshold;
        return new(demonstrated ? CompetencyState.Demonstrated : CompetencyState.Developing, score, all.Count);
    }
}
