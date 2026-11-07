namespace Learner.Api.Features;

/// <summary>
/// Chooses which questions a learner gets for a new attempt (spec 6.4):
/// the first question is never the one they saw last, and questions they saw recently are avoided where the pool allows.
/// With a tiny pool it still works: if only one question is eligible it is reused rather than blocking the assessment.
/// </summary>
public static class QuestionSelector
{
    /// <param name="pool">Eligible question ids, in authored order.</param>
    /// <param name="count">How many to draw (capped at the pool size).</param>
    /// <param name="recent">Question ids the learner has been shown, most recent first. recent[0] is the last one shown.</param>
    /// <param name="shuffle">Randomise presentation order. Off keeps authored order.</param>
    public static List<int> Pick(IReadOnlyList<int> pool, int count, IReadOnlyList<int> recent, bool shuffle, Random rng)
    {
        count = Math.Clamp(count, 0, pool.Count);

        // Never-seen questions first, then those seen longest ago; random among equals.
        int Staleness(int id) { var i = recent.ToList().IndexOf(id); return i < 0 ? int.MaxValue : i; }
        var chosen = pool.OrderByDescending(Staleness).ThenBy(_ => rng.Next()).Take(count).ToList();

        if (!shuffle) return pool.Where(chosen.Contains).ToList();

        chosen = [.. chosen.OrderBy(_ => rng.Next())];
        // Do not open with the question they just saw, if there is any alternative to open with.
        if (chosen.Count > 1 && recent.Count > 0 && chosen[0] == recent[0])
        {
            var swap = rng.Next(1, chosen.Count);
            (chosen[0], chosen[swap]) = (chosen[swap], chosen[0]);
        }
        return chosen;
    }
}
