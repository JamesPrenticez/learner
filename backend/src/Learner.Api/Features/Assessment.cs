using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Learner.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Learner.Api.Features;

public record SnapQuestion(int Id, string Type, string Text, JsonElement Data, string Explanation, string Hint, int Marks, string? ImageMediaId, List<int> CompetencyIds);
public record Snapshot(string Title, QuizKind Kind, int PassPercent, bool ImmediateFeedback, int? TimeLimitMinutes, List<SnapQuestion> Questions);

public record QuestionResult(double Score, double MaxScore, bool Correct, string Explanation, JsonElement AnswerKey, JsonElement Response);
public record AttemptQuestion(int Id, string Type, string Text, string Hint, int Marks, string? ImageMediaId, JsonNode Data, bool Answered, QuestionResult? Result);
public record AttemptView(
    int Id, int QuizId, int CourseId, string Title, QuizKind Kind, int PassPercent, DateTime StartedAt, DateTime? Deadline, bool Finished,
    double Score, double MaxScore, double Percent, bool? Passed, List<AttemptQuestion> Questions);
public record AnswerRequest(int QuestionId, JsonElement Response);
public record AnswerResult(bool Recorded, QuestionResult? Result);
public record AttemptSummary(
    int Id, int QuizId, string QuizTitle, QuizKind Kind, int CourseId, string CourseTitle, DateTime StartedAt, DateTime? FinishedAt,
    double Score, double MaxScore, double Percent, bool? Passed);
public record QuizInfo(
    int Id, int CourseId, string Title, string Description, QuizKind Kind, int Questions, int PassPercent, int? TimeLimitMinutes, int? MaxAttempts,
    int AttemptsUsed, string? ClosedReason, int? InProgressAttemptId, double? BestPercent);

/// <summary>The learner-facing assessment engine: start, answer, finish, review. Same flow for every quiz kind.</summary>
public static class Assessment
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    static Snapshot Snap(AssessmentAttempt a) => JsonSerializer.Deserialize<Snapshot>(a.SnapshotJson, Json)!;
    static DateTime? Deadline(AssessmentAttempt a, Snapshot s) => s.TimeLimitMinutes is { } m ? a.StartedAt.AddMinutes(m) : null;
    static double Percent(double score, double max) => max > 0 ? Math.Round(score / max * 100, 1) : 0;

    public static AttemptView ToView(AssessmentAttempt a)
    {
        var snap = Snap(a);
        var finished = a.FinishedAt is not null;
        var responses = a.Responses.ToDictionary(r => r.QuestionId);
        var none = JsonDocument.Parse("null").RootElement;

        var questions = snap.Questions.Select(q =>
        {
            responses.TryGetValue(q.Id, out var r);
            // Feedback appears right after answering when the quiz allows it, and for everything once the attempt is over.
            QuestionResult? result = null;
            if (r is not null && (snap.ImmediateFeedback || finished))
                result = new(r.Score, r.MaxScore, r.Score >= r.MaxScore, q.Explanation, q.Data, JsonDocument.Parse(r.ResponseJson).RootElement.Clone());
            else if (finished)
                result = new(0, q.Marks, false, q.Explanation, q.Data, none);
            return new AttemptQuestion(q.Id, q.Type, q.Text, q.Hint, q.Marks, q.ImageMediaId,
                QuestionTypes.ForLearner(QuestionTypes.All[q.Type], q.Data.GetRawText()), r is not null, result);
        }).ToList();

        return new AttemptView(a.Id, a.QuizId, a.CourseId, snap.Title, snap.Kind, snap.PassPercent, a.StartedAt, Deadline(a, snap), finished,
            a.Score, a.MaxScore, Percent(a.Score, a.MaxScore), a.Passed, questions);
    }

    /// <summary>Closes an attempt: totals the score and decides pass/fail. Idempotent.</summary>
    public static async Task Finalize(AppDb db, AssessmentAttempt a, DateTime? at = null)
    {
        if (a.FinishedAt is not null) return;
        var snap = Snap(a);
        a.FinishedAt = at ?? DateTime.UtcNow;
        a.Score = a.Responses.Sum(r => r.Score);
        a.Passed = Percent(a.Score, a.MaxScore) >= snap.PassPercent;
        await Competencies.RecordQuizEvidence(db, a, snap);
        await db.SaveChangesAsync();
    }

    /// <summary>If the time limit has passed, finishes the attempt as of the deadline. True when it did.</summary>
    static async Task<bool> ExpireIfOverdue(AppDb db, AssessmentAttempt a)
    {
        if (a.FinishedAt is not null || Deadline(a, Snap(a)) is not { } d || d > DateTime.UtcNow) return false;
        await Finalize(db, a, d);
        return true;
    }

    static Task<AssessmentAttempt?> Mine(AppDb db, int id, string uid) =>
        db.Attempts.Include(a => a.Responses).FirstOrDefaultAsync(a => a.Id == id && a.UserId == uid);

    static string? ClosedReason(Quiz q, DateTime now) =>
        q.AvailableFrom > now ? $"Opens {q.AvailableFrom:u}." : q.AvailableUntil < now ? "This assessment has closed." : null;

    /// <summary>Same options, random order (single/multiple choice).</summary>
    static JsonElement ShuffleOptions(string dataJson)
    {
        var node = JsonNode.Parse(dataJson)!.AsObject();
        if (node["options"] is JsonArray opts)
        {
            var shuffled = opts.Select(o => o!.DeepClone()).OrderBy(_ => Random.Shared.Next()).ToArray();
            node["options"] = new JsonArray(shuffled);
        }
        return JsonSerializer.SerializeToElement(node);
    }

    public static void MapAssessment(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api").RequireAuthorization();

        g.MapGet("/quizzes/{id:int}", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            var quiz = await db.Quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == id);
            if (quiz is null || await access.Viewable(quiz.CourseId, user) is null) return Results.NotFound();
            var uid = user.UserId();
            var mine = await db.Attempts.Where(a => a.UserId == uid && a.QuizId == id).Include(a => a.Responses).ToListAsync();
            foreach (var a in mine) await ExpireIfOverdue(db, a);
            var open = mine.FirstOrDefault(a => a.FinishedAt is null);
            var finished = mine.Where(a => a.FinishedAt is not null).ToList();
            var pool = quiz.Questions.Count;
            return Results.Ok(new QuizInfo(quiz.Id, quiz.CourseId, quiz.Title, quiz.Description, quiz.Kind, Math.Min(quiz.QuestionCount ?? pool, pool),
                quiz.PassPercent, quiz.TimeLimitMinutes, quiz.MaxAttempts, mine.Count, ClosedReason(quiz, DateTime.UtcNow), open?.Id,
                finished.Count == 0 ? null : finished.Max(a => Percent(a.Score, a.MaxScore))));
        });

        g.MapPost("/quizzes/{id:int}/attempts", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            var quiz = await db.Quizzes.Include(q => q.Questions).ThenInclude(x => x.Question).ThenInclude(q => q.Competencies)
                .AsSplitQuery().FirstOrDefaultAsync(q => q.Id == id);
            if (quiz is null || await access.Viewable(quiz.CourseId, user) is not { } course) return Results.NotFound();
            var uid = user.UserId();
            if (!await access.IsEnrolled(course.Id, uid) && !await access.CanManage(course, user))
                return Results.Conflict(new { title = "Enrol in the course to take assessments." });

            // One sitting at a time: resume it unless the clock ran out.
            var open = await Mine(db, await db.Attempts.Where(a => a.UserId == uid && a.QuizId == id && a.FinishedAt == null).Select(a => a.Id).FirstOrDefaultAsync(), uid);
            if (open is not null && !await ExpireIfOverdue(db, open)) return Results.Ok(ToView(open));

            var now = DateTime.UtcNow;
            if (ClosedReason(quiz, now) is { } closed) return Results.Conflict(new { title = closed });
            var previous = await db.Attempts.Where(a => a.UserId == uid && a.QuizId == id).OrderByDescending(a => a.StartedAt).Take(3).Select(a => a.QuestionOrder).ToListAsync();
            if (quiz.MaxAttempts is { } max && await db.Attempts.CountAsync(a => a.UserId == uid && a.QuizId == id) >= max)
                return Results.Conflict(new { title = "You have used all your attempts." });

            var pool = quiz.Questions.Select(x => x.Question).OrderBy(q => q.Id).ToList();
            if (pool.Count == 0) return Http.Invalid("quiz", "This assessment has no questions yet.");

            // Questions the learner saw lately, most recent first (each earlier attempt is walked backwards from its last question).
            var recent = previous.SelectMany(o => Enumerable.Reverse(o)).Distinct().ToList();
            var picked = QuestionSelector.Pick(pool.Select(q => q.Id).ToList(), quiz.QuestionCount ?? pool.Count, recent, quiz.ShuffleQuestions, Random.Shared);
            var questions = picked.Select(qid => pool.First(q => q.Id == qid)).Select(q => new SnapQuestion(
                q.Id, q.Type, q.Text, quiz.ShuffleOptions ? ShuffleOptions(q.DataJson) : JsonDocument.Parse(q.DataJson).RootElement.Clone(),
                q.Explanation, q.Hint, q.Marks, q.ImageMediaId, q.Competencies.Select(c => c.Id).ToList())).ToList();

            var attempt = new AssessmentAttempt
            {
                UserId = uid,
                QuizId = id,
                CourseId = course.Id,
                CourseVersion = course.Version,
                StartedAt = now,
                QuestionOrder = picked,
                MaxScore = questions.Sum(q => q.Marks),
                SnapshotJson = JsonSerializer.Serialize(new Snapshot(quiz.Title, quiz.Kind, quiz.PassPercent, quiz.ImmediateFeedback, quiz.TimeLimitMinutes, questions), Json),
            };
            db.Attempts.Add(attempt);
            await db.SaveChangesAsync();
            return Results.Ok(ToView(attempt));
        });

        g.MapGet("/attempts/{id:int}", async (int id, ClaimsPrincipal user, AppDb db) =>
        {
            if (await Mine(db, id, user.UserId()) is not { } a) return Results.NotFound();
            await ExpireIfOverdue(db, a);
            return Results.Ok(ToView(a));
        });

        g.MapPost("/attempts/{id:int}/answers", async (int id, AnswerRequest req, ClaimsPrincipal user, AppDb db) =>
        {
            if (await Mine(db, id, user.UserId()) is not { } a) return Results.NotFound();
            if (await ExpireIfOverdue(db, a) || a.FinishedAt is not null) return Results.Conflict(new { title = "This attempt is finished." });
            var snap = Snap(a);
            if (snap.Questions.FirstOrDefault(x => x.Id == req.QuestionId) is not { } q) return Http.Invalid("questionId", "Question is not part of this attempt.");
            if (a.Responses.Any(r => r.QuestionId == q.Id)) return Results.Conflict(new { title = "Already answered." });
            if (req.Response.ValueKind != JsonValueKind.Object) return Http.Invalid("response", "Response must be an object.");
            if (req.Response.GetRawText().Length > 10_000) return Http.Invalid("response", "Response is too large.");

            var fraction = Math.Clamp(QuestionTypes.All[q.Type].Grade(q.Data, req.Response), 0, 1);
            var r = new AssessmentResponse { AttemptId = id, QuestionId = q.Id, ResponseJson = req.Response.GetRawText(), Score = fraction * q.Marks, MaxScore = q.Marks };
            db.Responses.Add(r);
            await db.SaveChangesAsync();

            return Results.Ok(snap.ImmediateFeedback
                ? new AnswerResult(true, new QuestionResult(r.Score, r.MaxScore, r.Score >= r.MaxScore, q.Explanation, q.Data, req.Response.Clone()))
                : new AnswerResult(true, null));
        });

        g.MapPost("/attempts/{id:int}/finish", async (int id, ClaimsPrincipal user, AppDb db) =>
        {
            if (await Mine(db, id, user.UserId()) is not { } a) return Results.NotFound();
            await Finalize(db, a);
            return Results.Ok(ToView(a));
        });

        g.MapGet("/me/attempts", async (int? courseId, ClaimsPrincipal user, AppDb db) =>
        {
            var uid = user.UserId();
            var q = db.Attempts.Where(a => a.UserId == uid && a.FinishedAt != null);
            if (courseId is { } cid) q = q.Where(a => a.CourseId == cid);
            return (await q.OrderByDescending(a => a.FinishedAt).Take(200).Select(a => new { a, QuizTitle = a.Quiz.Title, a.Quiz.Kind, CourseTitle = a.Quiz.Course.Title }).ToListAsync())
                .Select(x => new AttemptSummary(x.a.Id, x.a.QuizId, x.QuizTitle, x.Kind, x.a.CourseId, x.CourseTitle, x.a.StartedAt, x.a.FinishedAt,
                    x.a.Score, x.a.MaxScore, Percent(x.a.Score, x.a.MaxScore), x.a.Passed));
        });
    }
}
