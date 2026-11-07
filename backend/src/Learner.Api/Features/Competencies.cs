using System.Security.Claims;
using Learner.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Learner.Api.Features;

public record RecommendedLesson(int Id, string Title, int CourseId, bool Completed);
public record CompetencyStatus(
    int Id, string Name, int CourseId, string CourseTitle, CompetencyState State, double Percent, int Evidence,
    DateTime? LastEvidence, List<RecommendedLesson> Recommended);
public record EvidenceRow(int Id, string Source, int SourceId, string Title, double Percent, double Weight, DateTime At);

public static class Competencies
{
    /// <summary>
    /// Records what an attempt showed about each competency its questions were tagged with (as tagged when the attempt started).
    /// One evidence row per competency per attempt, weighted by the marks that were available for it.
    /// </summary>
    public static async Task RecordQuizEvidence(AppDb db, AssessmentAttempt a, Snapshot snap)
    {
        var responses = a.Responses.ToDictionary(r => r.QuestionId);
        var tagged = snap.Questions.SelectMany(q => q.CompetencyIds.Select(c => (competency: c, q))).GroupBy(x => x.competency).ToList();
        var ids = tagged.Select(g => g.Key).ToList();
        var existing = (await db.Competencies.Where(c => ids.Contains(c.Id)).Select(c => c.Id).ToListAsync()).ToHashSet(); // may have been deleted meanwhile

        foreach (var g in tagged.Where(g => existing.Contains(g.Key)))
        {
            var max = g.Sum(x => (double)x.q.Marks);
            var got = g.Sum(x => responses.TryGetValue(x.q.Id, out var r) ? r.Score : 0);
            db.Evidence.Add(new CompetencyEvidence
            {
                UserId = a.UserId,
                CompetencyId = g.Key,
                CourseId = a.CourseId,
                Source = "quiz",
                SourceId = a.Id,
                Score = max > 0 ? got / max : 0,
                Weight = max,
                RecordedAt = a.FinishedAt ?? DateTime.UtcNow,
            });
        }
    }

    /// <summary>A learner's competency states for the given courses, with recommended lessons for any gap.</summary>
    public static async Task<List<CompetencyStatus>> Profile(AppDb db, CompetencyRules rules, string userId, List<int> courseIds)
    {
        var competencies = await db.Competencies.Where(c => courseIds.Contains(c.CourseId)).Include(c => c.Course).OrderBy(c => c.Course.Title).ThenBy(c => c.Name).ToListAsync();
        var evidence = (await db.Evidence.Where(e => e.UserId == userId && courseIds.Contains(e.CourseId)).ToListAsync()).ToLookup(e => e.CompetencyId);
        var done = (await db.Progress.Where(p => p.UserId == userId && p.CompletedAt != null).Select(p => p.LessonId).ToListAsync()).ToHashSet();
        var tagged = await db.Lessons.Where(l => courseIds.Contains(l.Module.CourseId) && l.Competencies.Any())
            .OrderBy(l => l.Module.Order).ThenBy(l => l.Order)
            .Select(l => new { l.Id, l.Title, l.Module.CourseId, Competencies = l.Competencies.Select(c => c.Id).ToList() }).ToListAsync();

        return competencies.Select(c =>
        {
            var mine = evidence[c.Id].ToList();
            var result = CompetencyEvaluator.Evaluate(mine.Select(e => new EvidenceItem(e.Source, e.Score, e.Weight, e.RecordedAt)), rules);
            // Where there is a gap, suggest the lessons that teach it (unfinished ones first). Never a restriction.
            var recommended = result.State == CompetencyState.Demonstrated ? [] : tagged.Where(l => l.Competencies.Contains(c.Id))
                .Select(l => new RecommendedLesson(l.Id, l.Title, l.CourseId, done.Contains(l.Id))).OrderBy(l => l.Completed).Take(3).ToList();
            return new CompetencyStatus(c.Id, c.Name, c.CourseId, c.Course.Title, result.State, Math.Round(result.Score * 100, 1), result.Evidence,
                mine.Count == 0 ? null : mine.Max(e => e.RecordedAt), recommended);
        }).ToList();
    }

    public static void MapCompetencies(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api").RequireAuthorization();

        g.MapGet("/me/competencies", async (ClaimsPrincipal user, AppDb db, IOptions<CompetencyRules> rules) =>
        {
            var uid = user.UserId();
            var courseIds = (await db.Enrolments.Where(e => e.UserId == uid && e.UnenrolledAt == null).Select(e => e.CourseId).ToListAsync())
                .Union(await db.Evidence.Where(e => e.UserId == uid).Select(e => e.CourseId).Distinct().ToListAsync()).ToList();
            return await Profile(db, rules.Value, uid, courseIds);
        });

        // The evidence behind one competency, so a result can be traced to the attempt that produced it.
        g.MapGet("/me/competencies/{id:int}/evidence", async (int id, ClaimsPrincipal user, AppDb db) =>
        {
            var uid = user.UserId();
            var rows = await db.Evidence.Where(e => e.UserId == uid && e.CompetencyId == id).OrderByDescending(e => e.RecordedAt).ToListAsync();
            var attemptIds = rows.Where(r => r.Source == "quiz").Select(r => r.SourceId).ToList();
            var titles = await db.Attempts.Where(a => attemptIds.Contains(a.Id)).Select(a => new { a.Id, a.Quiz.Title }).ToDictionaryAsync(a => a.Id, a => a.Title);
            var exerciseIds = rows.Where(r => r.Source == "exercise").Select(r => r.SourceId).ToList();
            var exerciseTitles = await db.ExerciseAttempts.Where(a => exerciseIds.Contains(a.Id)).Select(a => new { a.Id, a.Exercise.Title }).ToDictionaryAsync(a => a.Id, a => a.Title);
            string TitleOf(CompetencyEvidence r) => (r.Source == "exercise" ? exerciseTitles : titles).GetValueOrDefault(r.SourceId, r.Source);
            return rows.Select(r => new EvidenceRow(r.Id, r.Source, r.SourceId, TitleOf(r), Math.Round(r.Score * 100, 1), r.Weight, r.RecordedAt));
        });
    }
}
