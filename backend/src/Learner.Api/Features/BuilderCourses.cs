using System.Security.Claims;
using System.Text.Json;
using Learner.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Learner.Api.Features;

public record CourseSummary(int Id, string Title, CourseStatus Status, Visibility Visibility, int Version, DateTime UpdatedAt, int Modules, int Lessons);
public record LessonDto(int Id, string Title, int Order);
public record ModuleDto(int Id, string Title, int Order, List<LessonDto> Lessons);
public record CourseDetail(
    int Id, string Title, string Description, string? CoverMediaId, int? CurriculumId, string OwnerId, string OwnerName,
    Visibility Visibility, CourseStatus Status, Difficulty Difficulty, int EstimatedMinutes, List<string> LearningObjectives,
    int Version, DateTime CreatedAt, DateTime UpdatedAt, List<ModuleDto> Modules);
public record CourseUpdate(
    string Title, string Description, string? CoverMediaId, int? CurriculumId, Visibility Visibility,
    Difficulty Difficulty, int EstimatedMinutes, List<string> LearningObjectives);
public record TitleRequest(string Title);
public record OrderRequest(int[] Ids);
public record AccessRow(string UserId, string Email, string DisplayName, AccessKind Kind);
public record AccessRequest(string Email, AccessKind Kind);
public record CurriculumRequest(string Title, string Description);

public static class BuilderCourses
{
    public static CourseDetail ToDetail(Course c) => new(
        c.Id, c.Title, c.Description, c.CoverMediaId, c.CurriculumId, c.OwnerId, c.Owner.DisplayName,
        c.Visibility, c.Status, c.Difficulty, c.EstimatedMinutes, c.LearningObjectives,
        c.Version, c.CreatedAt, c.UpdatedAt,
        c.Modules.OrderBy(m => m.Order).Select(m => new ModuleDto(m.Id, m.Title, m.Order,
            m.Lessons.OrderBy(l => l.Order).Select(l => new LessonDto(l.Id, l.Title, l.Order)).ToList())).ToList());

    public static async Task<CourseDetail?> LoadDetail(AppDb db, int id)
    {
        var c = await db.Courses.Include(c => c.Owner).Include(c => c.Modules).ThenInclude(m => m.Lessons)
            .AsSplitQuery().FirstOrDefaultAsync(c => c.Id == id);
        return c is null ? null : ToDetail(c);
    }

    /// <summary>Reasons a course cannot be published yet (empty = ready).</summary>
    public static async Task<List<string>> PublishProblems(AppDb db, Course c)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(c.Description)) problems.Add("Add a course description.");
        if (await db.Modules.AnyAsync(m => m.CourseId == c.Id && !m.Lessons.Any())) problems.Add("Remove or fill empty modules.");

        var quizzes = await db.Quizzes.Where(q => q.CourseId == c.Id).Select(q => new { q.Id, Questions = q.Questions.Count }).ToDictionaryAsync(q => q.Id, q => q.Questions);
        var exercises = (await db.Exercises.Where(e => e.CourseId == c.Id).ToListAsync()).ToDictionary(e => e.Id, e => Exercises.Problem(e));
        var lessons = await db.Lessons.Where(l => l.Module.CourseId == c.Id).Include(l => l.Blocks).ToListAsync();
        if (lessons.Count == 0) problems.Add("Add at least one lesson.");
        foreach (var l in lessons)
        {
            if (l.Blocks.Count == 0) problems.Add($"Lesson \"{l.Title}\" has no content.");
            foreach (var (b, i) in l.Blocks.OrderBy(b => b.Order).Select((b, i) => (b, i)))
            {
                var data = JsonDocument.Parse(b.DataJson).RootElement;
                var issues = BlockSchemas.Problems(b.Type, data, strict: true);
                // Quiz blocks must point at a quiz of this course that has questions.
                if (issues.Count == 0 && b.Type is "quiz" or "knowledgecheck")
                {
                    var qid = data.GetProperty("quizId").GetInt32();
                    if (!quizzes.TryGetValue(qid, out var n)) issues.Add("the selected quiz does not exist in this course.");
                    else if (n == 0) issues.Add("the selected quiz has no questions.");
                }
                // Exercise blocks must point at a complete exercise of this course.
                if (issues.Count == 0 && b.Type == "exercise")
                {
                    if (!exercises.TryGetValue(data.GetProperty("exerciseId").GetInt32(), out var problem)) issues.Add("the selected exercise does not exist in this course.");
                    else if (problem is not null) issues.Add($"the selected exercise is incomplete ({problem})");
                }
                foreach (var issue in issues) problems.Add($"Lesson \"{l.Title}\", block {i + 1} ({b.Type}): {issue}");
            }
        }
        return problems;
    }

    public static void MapBuilderCourses(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/curricula", async (AppDb db) => await db.Curricula.OrderBy(c => c.Title).ToListAsync()).RequireAuthorization();

        var b = app.MapGroup("/api/builder").RequireAuthorization(p => p.RequireRole(Roles.Teacher, Roles.Admin));

        b.MapBuilderBlocks();
        b.MapBuilderAssessment();
        b.MapBuilderExercises();

        b.MapPost("/curricula", async (CurriculumRequest req, AppDb db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title)) return Http.Invalid("title", "Title is required.");
            var c = new Curriculum { Title = req.Title.Trim(), Description = req.Description?.Trim() ?? "" };
            db.Curricula.Add(c);
            await db.SaveChangesAsync();
            return Results.Ok(c);
        });

        // ---- courses
        b.MapGet("/courses", async (ClaimsPrincipal user, AppDb db) =>
        {
            var uid = user.UserId();
            var all = user.IsInRole(Roles.Admin);
            return await db.Courses
                .Where(c => all || c.OwnerId == uid || db.CourseAccesses.Any(a => a.CourseId == c.Id && a.UserId == uid && a.Kind == AccessKind.Collaborator))
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new CourseSummary(c.Id, c.Title, c.Status, c.Visibility, c.Version, c.UpdatedAt, c.Modules.Count, c.Modules.SelectMany(m => m.Lessons).Count()))
                .ToListAsync();
        });

        b.MapPost("/courses", async (TitleRequest req, ClaimsPrincipal user, AppDb db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title)) return Http.Invalid("title", "Title is required.");
            var c = new Course { Title = req.Title.Trim(), OwnerId = user.UserId(), Visibility = Visibility.Public, Status = CourseStatus.Draft };
            db.Courses.Add(c);
            await db.SaveChangesAsync();
            return Results.Ok(new { c.Id });
        });

        b.MapGet("/courses/{id:int}", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
            await access.Manageable(id, user) is null ? Results.NotFound() : Results.Ok(await LoadDetail(db, id)));

        b.MapPut("/courses/{id:int}", async (int id, CourseUpdate req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.Manageable(id, user) is not { } c) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(req.Title)) return Http.Invalid("title", "Title is required.");
            if (req.CurriculumId is { } cid && !await db.Curricula.AnyAsync(x => x.Id == cid)) return Http.Invalid("curriculumId", "Unknown curriculum.");
            if (req.CoverMediaId is { } mid && !await db.Media.AnyAsync(x => x.Id == mid)) return Http.Invalid("coverMediaId", "Unknown image.");
            c.Title = req.Title.Trim();
            c.Description = req.Description?.Trim() ?? "";
            c.CoverMediaId = req.CoverMediaId;
            c.CurriculumId = req.CurriculumId;
            c.Visibility = req.Visibility;
            c.Difficulty = req.Difficulty;
            c.EstimatedMinutes = Math.Max(0, req.EstimatedMinutes);
            c.LearningObjectives = req.LearningObjectives.Select(o => o.Trim()).Where(o => o != "").ToList();
            c.Touch();
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetail(db, id));
        });

        b.MapPost("/courses/{id:int}/publish", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.Manageable(id, user) is not { } c) return Results.NotFound();
            if (c.Status == CourseStatus.Published) return Results.Ok(await LoadDetail(db, id));
            var problems = await PublishProblems(db, c);
            if (problems.Count > 0) return Results.ValidationProblem(new Dictionary<string, string[]> { ["publish"] = [.. problems] });
            c.Status = CourseStatus.Published;
            c.Version++;
            c.Touch();
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetail(db, id));
        });

        b.MapPost("/courses/{id:int}/unpublish", (int id, ClaimsPrincipal user, Access access, AppDb db) => SetStatus(id, CourseStatus.Draft, user, access, db));
        b.MapPost("/courses/{id:int}/archive", (int id, ClaimsPrincipal user, Access access, AppDb db) => SetStatus(id, CourseStatus.Archived, user, access, db));

        // ---- who else can see / edit this course (owner or admin only)
        b.MapGet("/courses/{id:int}/access", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
            await access.Manageable(id, user) is null ? Results.NotFound()
                : Results.Ok(await db.CourseAccesses.Where(a => a.CourseId == id)
                    .Select(a => new AccessRow(a.UserId, a.User.Email!, a.User.DisplayName, a.Kind)).ToListAsync()));

        b.MapPost("/courses/{id:int}/access", async (int id, AccessRequest req, ClaimsPrincipal user, Access access, AppDb db, UserManager<AppUser> users) =>
        {
            if (await access.Manageable(id, user) is not { } c) return Results.NotFound();
            if (c.OwnerId != user.UserId() && !user.IsInRole(Roles.Admin)) return Results.Forbid();
            var target = await users.FindByEmailAsync(req.Email);
            if (target is null) return Http.Invalid("email", "No user with that email.");
            if (target.Id == c.OwnerId) return Http.Invalid("email", "That user already owns the course.");
            var row = await db.CourseAccesses.FindAsync(id, target.Id);
            if (row is null) db.CourseAccesses.Add(new CourseAccess { CourseId = id, UserId = target.Id, Kind = req.Kind });
            else row.Kind = req.Kind;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        b.MapDelete("/courses/{id:int}/access/{userId}", async (int id, string userId, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.Manageable(id, user) is not { } c) return Results.NotFound();
            if (c.OwnerId != user.UserId() && !user.IsInRole(Roles.Admin)) return Results.Forbid();
            if (await db.CourseAccesses.FindAsync(id, userId) is { } row) { db.CourseAccesses.Remove(row); await db.SaveChangesAsync(); }
            return Results.NoContent();
        });

        // ---- modules
        b.MapPost("/courses/{id:int}/modules", async (int id, TitleRequest req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.Manageable(id, user) is not { } c) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(req.Title)) return Http.Invalid("title", "Title is required.");
            var order = (await db.Modules.Where(m => m.CourseId == id).MaxAsync(m => (int?)m.Order) ?? -1) + 1;
            db.Modules.Add(new Module { CourseId = id, Title = req.Title.Trim(), Order = order });
            c.Touch();
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetail(db, id));
        });

        b.MapPut("/courses/{id:int}/modules/order", async (int id, OrderRequest req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.Manageable(id, user) is not { } c) return Results.NotFound();
            if (!Http.ApplyOrder(await db.Modules.Where(m => m.CourseId == id).ToListAsync(), req.Ids)) return Http.Invalid("ids", "Ids must list every module exactly once.");
            c.Touch();
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetail(db, id));
        });

        b.MapPut("/modules/{id:int}", async (int id, TitleRequest req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableModule(id, user) is not { } m) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(req.Title)) return Http.Invalid("title", "Title is required.");
            m.Title = req.Title.Trim();
            m.Course.Touch();
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetail(db, m.CourseId));
        });

        b.MapDelete("/modules/{id:int}", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableModule(id, user) is not { } m) return Results.NotFound();
            if (m.Course.Status == CourseStatus.Published) return Results.Conflict(new { title = "Unpublish the course before deleting modules." });
            db.Modules.Remove(m);
            m.Course.Touch();
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetail(db, m.CourseId));
        });

        // ---- lessons
        b.MapPost("/modules/{id:int}/lessons", async (int id, TitleRequest req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableModule(id, user) is not { } m) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(req.Title)) return Http.Invalid("title", "Title is required.");
            var order = (await db.Lessons.Where(l => l.ModuleId == id).MaxAsync(l => (int?)l.Order) ?? -1) + 1;
            db.Lessons.Add(new Lesson { ModuleId = id, Title = req.Title.Trim(), Order = order });
            m.Course.Touch();
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetail(db, m.CourseId));
        });

        b.MapPut("/modules/{id:int}/lessons/order", async (int id, OrderRequest req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableModule(id, user) is not { } m) return Results.NotFound();
            if (!Http.ApplyOrder(await db.Lessons.Where(l => l.ModuleId == id).ToListAsync(), req.Ids)) return Http.Invalid("ids", "Ids must list every lesson exactly once.");
            m.Course.Touch();
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetail(db, m.CourseId));
        });

        b.MapPut("/lessons/{id:int}", async (int id, TitleRequest req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableLesson(id, user) is not { } l) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(req.Title)) return Http.Invalid("title", "Title is required.");
            l.Title = req.Title.Trim();
            l.Module.Course.Touch();
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetail(db, l.Module.CourseId));
        });

        b.MapDelete("/lessons/{id:int}", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableLesson(id, user) is not { } l) return Results.NotFound();
            if (l.Module.Course.Status == CourseStatus.Published) return Results.Conflict(new { title = "Unpublish the course before deleting lessons." });
            db.Lessons.Remove(l);
            l.Module.Course.Touch();
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetail(db, l.Module.CourseId));
        });
    }

    static async Task<IResult> SetStatus(int id, CourseStatus status, ClaimsPrincipal user, Access access, AppDb db)
    {
        if (await access.Manageable(id, user) is not { } c) return Results.NotFound();
        c.Status = status;
        c.Touch();
        await db.SaveChangesAsync();
        return Results.Ok(await LoadDetail(db, id));
    }
}
