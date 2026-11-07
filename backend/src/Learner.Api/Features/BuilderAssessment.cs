using System.Security.Claims;
using System.Text.Json;
using Learner.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Learner.Api.Features;

public record CompetencyDto(int Id, string Name, string Description);
public record CompetencyInput(string Name, string? Description);
public record BankDto(int Id, string Name, int Questions);
public record QuestionDto(
    int Id, int BankId, string Type, string Text, JsonElement Data, string Explanation, string Hint, Difficulty Difficulty,
    int Marks, string Category, List<string> Tags, string? ImageMediaId, List<int> CompetencyIds);
public record QuestionInput(
    string Type, string Text, JsonElement Data, string? Explanation, string? Hint, Difficulty Difficulty,
    int Marks, string? Category, List<string>? Tags, string? ImageMediaId, List<int>? CompetencyIds);
public record QuizDto(
    int Id, string Title, string Description, QuizKind Kind, int? QuestionCount, int PassPercent, bool ShuffleQuestions, bool ShuffleOptions,
    bool ImmediateFeedback, int? TimeLimitMinutes, int? MaxAttempts, DateTime? AvailableFrom, DateTime? AvailableUntil, List<int> QuestionIds);
public record QuizInput(
    string Title, string? Description, QuizKind Kind, int? QuestionCount, int PassPercent, bool ShuffleQuestions, bool ShuffleOptions,
    bool ImmediateFeedback, int? TimeLimitMinutes, int? MaxAttempts, DateTime? AvailableFrom, DateTime? AvailableUntil, List<int>? QuestionIds);
public record NewQuiz(string Title, QuizKind Kind);
public record NameRequest(string Name);

public static class BuilderAssessment
{
    static QuestionDto ToDto(Question q) => new(
        q.Id, q.QuestionBankId, q.Type, q.Text, JsonDocument.Parse(q.DataJson).RootElement.Clone(), q.Explanation, q.Hint, q.Difficulty,
        q.Marks, q.Category, q.Tags, q.ImageMediaId, q.Competencies.Select(c => c.Id).Order().ToList());

    static QuizDto ToDto(Quiz q) => new(
        q.Id, q.Title, q.Description, q.Kind, q.QuestionCount, q.PassPercent, q.ShuffleQuestions, q.ShuffleOptions, q.ImmediateFeedback,
        q.TimeLimitMinutes, q.MaxAttempts, q.AvailableFrom, q.AvailableUntil, q.Questions.Select(x => x.QuestionId).Order().ToList());

    /// <summary>First problem with a question, or null.</summary>
    static string? Check(QuestionInput r)
    {
        if (string.IsNullOrWhiteSpace(r.Text)) return "Question text is required.";
        if (!QuestionTypes.All.TryGetValue(r.Type, out var type)) return $"Unknown question type '{r.Type}'.";
        if (r.Data.ValueKind != JsonValueKind.Object) return "Question data must be an object.";
        if (r.Marks is < 1 or > 100) return "Marks must be between 1 and 100.";
        return type.Validate(r.Data);
    }

    /// <summary>First problem with quiz settings, or null.</summary>
    static string? Check(QuizInput r)
    {
        if (string.IsNullOrWhiteSpace(r.Title)) return "Title is required.";
        if (r.PassPercent is < 0 or > 100) return "Pass mark must be between 0 and 100.";
        if (r.QuestionCount is < 1) return "Question count must be at least 1.";
        if (r.TimeLimitMinutes is < 1) return "Time limit must be at least 1 minute.";
        if (r.MaxAttempts is < 1) return "Attempt limit must be at least 1.";
        if (r.AvailableFrom is { } f && r.AvailableUntil is { } u && u <= f) return "Availability must end after it starts.";
        return null;
    }

    public static void MapBuilderAssessment(this RouteGroupBuilder b)
    {
        // ---- competencies
        b.MapGet("/courses/{id:int}/competencies", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
            await access.Manageable(id, user) is null ? Results.NotFound()
                : Results.Ok(await db.Competencies.Where(c => c.CourseId == id).OrderBy(c => c.Name).Select(c => new CompetencyDto(c.Id, c.Name, c.Description)).ToListAsync()));

        b.MapPost("/courses/{id:int}/competencies", async (int id, CompetencyInput req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.Manageable(id, user) is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(req.Name)) return Http.Invalid("name", "Name is required.");
            var c = new Competency { CourseId = id, Name = req.Name.Trim(), Description = req.Description?.Trim() ?? "" };
            db.Competencies.Add(c);
            await db.SaveChangesAsync();
            return Results.Ok(new CompetencyDto(c.Id, c.Name, c.Description));
        });

        b.MapPut("/competencies/{id:int}", async (int id, CompetencyInput req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableCompetency(id, user) is not { } c) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(req.Name)) return Http.Invalid("name", "Name is required.");
            c.Name = req.Name.Trim();
            c.Description = req.Description?.Trim() ?? "";
            await db.SaveChangesAsync();
            return Results.Ok(new CompetencyDto(c.Id, c.Name, c.Description));
        });

        b.MapDelete("/competencies/{id:int}", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableCompetency(id, user) is not { } c) return Results.NotFound();
            if (await db.Evidence.AnyAsync(e => e.CompetencyId == id)) return Results.Conflict(new { title = "Learners have evidence for this competency, so it is kept." });
            db.Competencies.Remove(c);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ---- question banks
        b.MapGet("/courses/{id:int}/banks", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
            await access.Manageable(id, user) is null ? Results.NotFound()
                : Results.Ok(await db.Banks.Where(x => x.CourseId == id).OrderBy(x => x.Name).Select(x => new BankDto(x.Id, x.Name, x.Questions.Count)).ToListAsync()));

        b.MapPost("/courses/{id:int}/banks", async (int id, NameRequest req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.Manageable(id, user) is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(req.Name)) return Http.Invalid("name", "Name is required.");
            var bank = new QuestionBank { CourseId = id, Name = req.Name.Trim() };
            db.Banks.Add(bank);
            await db.SaveChangesAsync();
            return Results.Ok(new BankDto(bank.Id, bank.Name, 0));
        });

        b.MapPut("/banks/{id:int}", async (int id, NameRequest req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableBank(id, user) is not { } bank) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(req.Name)) return Http.Invalid("name", "Name is required.");
            bank.Name = req.Name.Trim();
            await db.SaveChangesAsync();
            return Results.Ok(new BankDto(bank.Id, bank.Name, await db.Questions.CountAsync(q => q.QuestionBankId == id)));
        });

        b.MapDelete("/banks/{id:int}", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableBank(id, user) is not { } bank) return Results.NotFound();
            db.Banks.Remove(bank); // questions go too; past attempts keep their snapshots
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ---- questions
        b.MapGet("/courses/{id:int}/questions", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
            await access.Manageable(id, user) is null ? Results.NotFound()
                : Results.Ok((await db.Questions.Include(q => q.Competencies).Where(q => q.Bank.CourseId == id).OrderBy(q => q.Id).ToListAsync()).Select(ToDto)));

        b.MapPost("/banks/{id:int}/questions", async (int id, QuestionInput req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableBank(id, user) is not { } bank) return Results.NotFound();
            var q = new Question { QuestionBankId = id };
            return await Save(q, bank.CourseId, req, db, isNew: true);
        });

        b.MapPut("/questions/{id:int}", async (int id, QuestionInput req, ClaimsPrincipal user, Access access, AppDb db) =>
            await access.ManageableQuestion(id, user) is not { } q ? Results.NotFound() : await Save(q, q.Bank.CourseId, req, db, isNew: false));

        b.MapDelete("/questions/{id:int}", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableQuestion(id, user) is not { } q) return Results.NotFound();
            db.Questions.Remove(q);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ---- quizzes (knowledge checks, quizzes, module assessments, exams)
        b.MapGet("/courses/{id:int}/quizzes", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
            await access.Manageable(id, user) is null ? Results.NotFound()
                : Results.Ok((await db.Quizzes.Include(q => q.Questions).Where(q => q.CourseId == id).OrderBy(q => q.Title).ToListAsync()).Select(ToDto)));

        b.MapPost("/courses/{id:int}/quizzes", async (int id, NewQuiz req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.Manageable(id, user) is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(req.Title)) return Http.Invalid("title", "Title is required.");
            // Exams default to no instant feedback and a single sitting's worth of structure; everything else is practice.
            var quiz = new Quiz { CourseId = id, Title = req.Title.Trim(), Kind = req.Kind, ImmediateFeedback = req.Kind != QuizKind.Exam };
            db.Quizzes.Add(quiz);
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(quiz));
        });

        b.MapGet("/quizzes/{id:int}", async (int id, ClaimsPrincipal user, Access access) =>
            await access.ManageableQuiz(id, user) is not { } q ? Results.NotFound() : Results.Ok(ToDto(q)));

        b.MapPut("/quizzes/{id:int}", async (int id, QuizInput req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableQuiz(id, user) is not { } quiz) return Results.NotFound();
            if (Check(req) is { } problem) return Http.Invalid("quiz", problem);

            var ids = (req.QuestionIds ?? []).Distinct().ToList();
            var valid = await db.Questions.Where(q => q.Bank.CourseId == quiz.CourseId && ids.Contains(q.Id)).Select(q => q.Id).ToListAsync();
            if (valid.Count != ids.Count) return Http.Invalid("questionIds", "Questions must belong to this course.");
            if (req.QuestionCount > ids.Count && ids.Count > 0) return Http.Invalid("questionCount", "Cannot draw more questions than the pool contains.");

            quiz.Title = req.Title.Trim();
            quiz.Description = req.Description?.Trim() ?? "";
            quiz.Kind = req.Kind;
            quiz.QuestionCount = req.QuestionCount;
            quiz.PassPercent = req.PassPercent;
            quiz.ShuffleQuestions = req.ShuffleQuestions;
            quiz.ShuffleOptions = req.ShuffleOptions;
            quiz.ImmediateFeedback = req.ImmediateFeedback;
            quiz.TimeLimitMinutes = req.TimeLimitMinutes;
            quiz.MaxAttempts = req.MaxAttempts;
            quiz.AvailableFrom = req.AvailableFrom;
            quiz.AvailableUntil = req.AvailableUntil;
            quiz.Questions.RemoveAll(x => !ids.Contains(x.QuestionId));
            foreach (var qid in ids.Where(qid => quiz.Questions.All(x => x.QuestionId != qid))) quiz.Questions.Add(new QuizQuestion { QuizId = id, QuestionId = qid });
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(quiz));
        });

        b.MapDelete("/quizzes/{id:int}", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableQuiz(id, user) is not { } quiz) return Results.NotFound();
            if (await db.Attempts.AnyAsync(a => a.QuizId == id)) return Results.Conflict(new { title = "This quiz has learner attempts, so it is kept. Remove it from lessons instead." });
            db.Quizzes.Remove(quiz);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    static async Task<IResult> Save(Question q, int courseId, QuestionInput req, AppDb db, bool isNew)
    {
        if (Check(req) is { } problem) return Http.Invalid("question", problem);
        if (req.ImageMediaId is { } mid && !await db.Media.AnyAsync(m => m.Id == mid)) return Http.Invalid("imageMediaId", "Unknown image.");

        var compIds = (req.CompetencyIds ?? []).Distinct().ToList();
        var comps = await db.Competencies.Where(c => c.CourseId == courseId && compIds.Contains(c.Id)).ToListAsync();
        if (comps.Count != compIds.Count) return Http.Invalid("competencyIds", "Competencies must belong to this course.");

        q.Type = req.Type;
        q.Text = req.Text.Trim();
        q.DataJson = req.Data.GetRawText();
        q.Explanation = req.Explanation?.Trim() ?? "";
        q.Hint = req.Hint?.Trim() ?? "";
        q.Difficulty = req.Difficulty;
        q.Marks = req.Marks;
        q.Category = req.Category?.Trim() ?? "";
        q.Tags = (req.Tags ?? []).Select(t => t.Trim()).Where(t => t != "").Distinct().ToList();
        q.ImageMediaId = req.ImageMediaId;
        q.Competencies = comps;
        if (isNew) db.Questions.Add(q);
        await db.SaveChangesAsync();
        return Results.Ok(ToDto(q));
    }
}
