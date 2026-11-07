using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Learner.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Learner.Api.Features;

public record ExerciseTypeDto(string Key, string Label);
public record ExerciseSummary(int Id, string Title, string TypeKey, int Version, string? Problem);
public record ExerciseDetail(int Id, int CourseId, string TypeKey, string Title, JsonElement Config, int Version, List<int> CompetencyIds, string? Problem);
public record NewExercise(string TypeKey, string Title);
public record ExerciseInput(string Title, JsonElement Config, List<int>? CompetencyIds);
public record ExerciseView(int Id, int CourseId, string Title, string TypeKey, int Version, JsonNode Config, int Attempts, double? BestPercent, string? Problem);
public record SubmitRequest(JsonElement Submission);
public record ExerciseAttemptView(
    int Id, int ExerciseId, string Title, string TypeKey, int ConfigVersion, JsonNode Config, JsonElement Submission, string Summary,
    JsonElement Details, double Score, double MaxScore, double Percent, DateTime SubmittedAt);
public record ExerciseAttemptSummary(int Id, int ExerciseId, string Title, int CourseId, string CourseTitle, double Score, double MaxScore, double Percent, DateTime SubmittedAt);

/// <summary>The exercise framework: storage, access, attempts and evidence. Component-specific logic lives in ExerciseTypes.</summary>
public static class Exercises
{
    const int MaxPayload = 100_000;
    static double Percent(double score, double max) => max > 0 ? Math.Round(score / max * 100, 1) : 0;
    static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    /// <summary>Why this configuration cannot run yet, or null if it can.</summary>
    public static string? Problem(ExerciseConfiguration e) =>
        !ExerciseTypes.All.TryGetValue(e.TypeKey, out var type) ? $"Unknown exercise type '{e.TypeKey}'."
        : Parse(e.ConfigJson).ValueKind != JsonValueKind.Object ? "Configuration must be an object."
        : type.Validate(Parse(e.ConfigJson));

    static ExerciseDetail ToDetail(ExerciseConfiguration e) =>
        new(e.Id, e.CourseId, e.TypeKey, e.Title, Parse(e.ConfigJson), e.Version, e.Competencies.Select(c => c.Id).Order().ToList(), Problem(e));

    static ExerciseAttemptView ToView(ExerciseAttempt a, string title)
    {
        var result = JsonNode.Parse(a.ResultJson)!.AsObject();
        return new ExerciseAttemptView(a.Id, a.ExerciseId, title, a.TypeKey, a.ConfigVersion,
            ExerciseTypes.All[a.TypeKey].ForLearner(Parse(a.ConfigSnapshotJson)), Parse(a.SubmissionJson),
            result["summary"]!.GetValue<string>(), Parse(result["details"]!.ToJsonString()), a.Score, a.MaxScore, Percent(a.Score, a.MaxScore), a.SubmittedAt);
    }

    public static void MapBuilderExercises(this RouteGroupBuilder b)
    {
        b.MapGet("/exercise-types", () => ExerciseTypes.All.Values.Select(t => new ExerciseTypeDto(t.Key, t.Label)));

        b.MapGet("/courses/{id:int}/exercises", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
            await access.Manageable(id, user) is null ? Results.NotFound()
                : Results.Ok((await db.Exercises.Where(e => e.CourseId == id).OrderBy(e => e.Title).ToListAsync())
                    .Select(e => new ExerciseSummary(e.Id, e.Title, e.TypeKey, e.Version, Problem(e)))));

        b.MapPost("/courses/{id:int}/exercises", async (int id, NewExercise req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.Manageable(id, user) is null) return Results.NotFound();
            if (!ExerciseTypes.All.ContainsKey(req.TypeKey)) return Http.Invalid("typeKey", $"Unknown exercise type '{req.TypeKey}'.");
            if (string.IsNullOrWhiteSpace(req.Title)) return Http.Invalid("title", "Title is required.");
            var e = new ExerciseConfiguration { CourseId = id, TypeKey = req.TypeKey, Title = req.Title.Trim() };
            db.Exercises.Add(e);
            await db.SaveChangesAsync();
            return Results.Ok(ToDetail(e));
        });

        b.MapGet("/exercises/{id:int}", async (int id, ClaimsPrincipal user, Access access) =>
            await access.ManageableExercise(id, user) is not { } e ? Results.NotFound() : Results.Ok(ToDetail(e)));

        // Drafts may be incomplete (like blocks); ExerciseDetail.Problem says what is missing, and publishing/attempts require it to be empty.
        b.MapPut("/exercises/{id:int}", async (int id, ExerciseInput req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableExercise(id, user) is not { } e) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(req.Title)) return Http.Invalid("title", "Title is required.");
            if (req.Config.ValueKind != JsonValueKind.Object) return Http.Invalid("config", "Configuration must be an object.");
            if (req.Config.GetRawText().Length > MaxPayload) return Http.Invalid("config", "Configuration is too large.");

            var compIds = (req.CompetencyIds ?? []).Distinct().ToList();
            var comps = await db.Competencies.Where(c => c.CourseId == e.CourseId && compIds.Contains(c.Id)).ToListAsync();
            if (comps.Count != compIds.Count) return Http.Invalid("competencyIds", "Competencies must belong to this course.");

            var raw = req.Config.GetRawText();
            if (raw != e.ConfigJson) { e.ConfigJson = raw; e.Version++; }
            e.Title = req.Title.Trim();
            e.Competencies = comps;
            await db.SaveChangesAsync();
            return Results.Ok(ToDetail(e));
        });

        b.MapDelete("/exercises/{id:int}", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableExercise(id, user) is not { } e) return Results.NotFound();
            if (await db.ExerciseAttempts.AnyAsync(a => a.ExerciseId == id)) return Results.Conflict(new { title = "Learners have attempts on this exercise, so it is kept. Remove it from lessons instead." });
            db.Exercises.Remove(e);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    public static void MapExercises(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api").RequireAuthorization();

        g.MapGet("/exercises/{id:int}", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            var e = await db.Exercises.FindAsync(id);
            if (e is null || await access.Viewable(e.CourseId, user) is null) return Results.NotFound();
            var uid = user.UserId();
            var mine = await db.ExerciseAttempts.Where(a => a.UserId == uid && a.ExerciseId == id).Select(a => new { a.Score, a.MaxScore }).ToListAsync();
            var problem = Problem(e);
            return Results.Ok(new ExerciseView(e.Id, e.CourseId, e.Title, e.TypeKey, e.Version,
                problem is null ? ExerciseTypes.All[e.TypeKey].ForLearner(Parse(e.ConfigJson)) : new JsonObject(),
                mine.Count, mine.Count == 0 ? null : mine.Max(a => Percent(a.Score, a.MaxScore)), problem));
        });

        g.MapPost("/exercises/{id:int}/attempts", async (int id, SubmitRequest req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            var e = await db.Exercises.Include(x => x.Competencies).FirstOrDefaultAsync(x => x.Id == id);
            if (e is null || await access.Viewable(e.CourseId, user) is not { } course) return Results.NotFound();
            var uid = user.UserId();
            if (!await access.IsEnrolled(course.Id, uid) && !await access.CanManage(course, user))
                return Results.Conflict(new { title = "Enrol in the course to complete exercises." });
            if (Problem(e) is { } problem) return Results.Conflict(new { title = $"This exercise is not ready: {problem}" });
            if (req.Submission.ValueKind != JsonValueKind.Object) return Http.Invalid("submission", "Submission must be an object.");
            if (req.Submission.GetRawText().Length > MaxPayload) return Http.Invalid("submission", "Submission is too large.");

            var result = ExerciseTypes.All[e.TypeKey].Evaluate(Parse(e.ConfigJson), req.Submission);
            var attempt = new ExerciseAttempt
            {
                UserId = uid,
                ExerciseId = id,
                CourseId = e.CourseId,
                TypeKey = e.TypeKey,
                ConfigVersion = e.Version,
                ConfigSnapshotJson = e.ConfigJson,
                SubmissionJson = req.Submission.GetRawText(),
                Score = result.Score,
                MaxScore = result.MaxScore,
                ResultJson = new JsonObject { ["summary"] = result.Summary, ["details"] = result.Details.DeepClone() }.ToJsonString(),
            };
            db.ExerciseAttempts.Add(attempt);
            await db.SaveChangesAsync();

            // The exercise's evidence for every competency it was tagged with, weighted by the points available.
            foreach (var c in e.Competencies)
                db.Evidence.Add(new CompetencyEvidence
                {
                    UserId = uid,
                    CompetencyId = c.Id,
                    CourseId = e.CourseId,
                    Source = "exercise",
                    SourceId = attempt.Id,
                    Score = result.MaxScore > 0 ? result.Score / result.MaxScore : 0,
                    Weight = result.MaxScore,
                    RecordedAt = attempt.SubmittedAt,
                });
            await db.SaveChangesAsync();
            return Results.Ok(ToView(attempt, e.Title));
        });

        g.MapGet("/exercise-attempts/{id:int}", async (int id, ClaimsPrincipal user, AppDb db) =>
        {
            var uid = user.UserId();
            var a = await db.ExerciseAttempts.Include(x => x.Exercise).FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            return a is null ? Results.NotFound() : Results.Ok(ToView(a, a.Exercise.Title));
        });

        g.MapGet("/me/exercise-attempts", async (ClaimsPrincipal user, AppDb db) =>
        {
            var uid = user.UserId();
            return (await db.ExerciseAttempts.Where(a => a.UserId == uid).OrderByDescending(a => a.SubmittedAt).Take(200)
                    .Select(a => new { a, a.Exercise.Title, CourseTitle = a.Exercise.Course.Title }).ToListAsync())
                .Select(x => new ExerciseAttemptSummary(x.a.Id, x.a.ExerciseId, x.Title, x.a.CourseId, x.CourseTitle, x.a.Score, x.a.MaxScore, Percent(x.a.Score, x.a.MaxScore), x.a.SubmittedAt));
        });
    }
}
