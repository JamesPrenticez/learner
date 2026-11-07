using System.Security.Claims;
using Learner.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Learner.Api.Features;

public record CatalogueItem(
    int Id, string Title, string Description, string? CoverMediaId, string? Curriculum, string Author,
    Difficulty Difficulty, int EstimatedMinutes, int Lessons, bool Enrolled);
public record OutlineLesson(int Id, string Title, bool Completed);
public record OutlineModule(int Id, string Title, List<OutlineLesson> Lessons);
public record CourseOverview(
    int Id, string Title, string Description, string? CoverMediaId, string? Curriculum, string Author,
    Difficulty Difficulty, int EstimatedMinutes, List<string> LearningObjectives, CourseStatus Status, Visibility Visibility,
    int Version, bool Enrolled, bool CanManage, bool CanEnrol, int Completed, int Total, List<OutlineModule> Modules);
public record LessonView(
    int Id, string Title, int CourseId, string CourseTitle, string ModuleTitle, bool Enrolled, bool Completed,
    int? PrevId, int? NextId, List<BlockDto> Blocks);
public record MyCourse(int Id, string Title, string? CoverMediaId, int Completed, int Total, int? ContinueLessonId, DateTime LastActivity);
public record CompleteRequest(bool Completed);

public static class Learning
{
    public static void MapLearning(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api").RequireAuthorization();

        // Public catalogue, or (scope=private) published private courses the user was invited to.
        g.MapGet("/catalogue", async (string? search, int? curriculumId, Difficulty? difficulty, string? scope, ClaimsPrincipal user, AppDb db) =>
        {
            var uid = user.UserId();
            var q = db.Courses.Where(c => c.Status == CourseStatus.Published);
            q = scope == "private"
                ? q.Where(c => c.Visibility == Visibility.Private && db.CourseAccesses.Any(a => a.CourseId == c.Id && a.UserId == uid))
                : q.Where(c => c.Visibility == Visibility.Public);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var like = $"%{search.Trim()}%";
                q = q.Where(c => EF.Functions.Like(c.Title, like) || EF.Functions.Like(c.Description, like));
            }
            if (curriculumId is { } cid) q = q.Where(c => c.CurriculumId == cid);
            if (difficulty is { } d) q = q.Where(c => c.Difficulty == d);

            return await q.OrderBy(c => c.Title).Select(c => new CatalogueItem(
                c.Id, c.Title, c.Description, c.CoverMediaId, c.Curriculum != null ? c.Curriculum.Title : null, c.Owner.DisplayName,
                c.Difficulty, c.EstimatedMinutes, c.Modules.SelectMany(m => m.Lessons).Count(),
                db.Enrolments.Any(e => e.CourseId == c.Id && e.UserId == uid && e.UnenrolledAt == null))).ToListAsync();
        });

        g.MapGet("/courses/{id:int}", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.Viewable(id, user) is null) return Results.NotFound();
            var c = await db.Courses.Include(c => c.Owner).Include(c => c.Curriculum).Include(c => c.Modules).ThenInclude(m => m.Lessons)
                .AsSplitQuery().FirstAsync(c => c.Id == id);
            var uid = user.UserId();
            var done = (await db.Progress.Where(p => p.UserId == uid && p.CompletedAt != null && p.Lesson.Module.CourseId == id).Select(p => p.LessonId).ToListAsync()).ToHashSet();
            var modules = c.Modules.OrderBy(m => m.Order).Select(m => new OutlineModule(m.Id, m.Title,
                m.Lessons.OrderBy(l => l.Order).Select(l => new OutlineLesson(l.Id, l.Title, done.Contains(l.Id))).ToList())).ToList();
            var total = modules.Sum(m => m.Lessons.Count);
            return Results.Ok(new CourseOverview(
                c.Id, c.Title, c.Description, c.CoverMediaId, c.Curriculum?.Title, c.Owner.DisplayName, c.Difficulty, c.EstimatedMinutes,
                c.LearningObjectives, c.Status, c.Visibility, c.Version, await access.IsEnrolled(id, uid), await access.CanManage(c, user),
                await access.CanEnrol(c, user), modules.Sum(m => m.Lessons.Count(l => l.Completed)), total, modules));
        });

        g.MapPost("/courses/{id:int}/enrol", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            var c = await db.Courses.FindAsync(id);
            if (c is null || !await access.CanView(c, user)) return Results.NotFound();
            if (!await access.CanEnrol(c, user)) return Http.Invalid("course", "This course is not open for enrolment.");
            var uid = user.UserId();
            if (!await access.IsEnrolled(id, uid))
            {
                db.Enrolments.Add(new Enrolment { UserId = uid, CourseId = id });
                await db.SaveChangesAsync();
            }
            return Results.NoContent();
        });

        g.MapPost("/courses/{id:int}/unenrol", async (int id, ClaimsPrincipal user, AppDb db) =>
        {
            var uid = user.UserId();
            foreach (var e in await db.Enrolments.Where(e => e.CourseId == id && e.UserId == uid && e.UnenrolledAt == null).ToListAsync())
                e.UnenrolledAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapGet("/lessons/{id:int}", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ViewableLesson(id, user) is not { } lesson) return Results.NotFound();
            var course = lesson.Module.Course;
            var uid = user.UserId();
            var enrolled = await access.IsEnrolled(course.Id, uid);

            // Recommended sequence = course order. Never a restriction.
            var order = await db.Lessons.Where(l => l.Module.CourseId == course.Id)
                .OrderBy(l => l.Module.Order).ThenBy(l => l.Order).Select(l => l.Id).ToListAsync();
            var i = order.IndexOf(id);

            var progress = await db.Progress.FindAsync(uid, id);
            // Side effect on purpose: powers "continue learning" / recent activity. Only tracked for enrolled learners.
            if (enrolled)
            {
                if (progress is null) db.Progress.Add(progress = new LessonProgress { UserId = uid, LessonId = id });
                else progress.LastAccessedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            var blocks = await db.Blocks.Where(b => b.LessonId == id).OrderBy(b => b.Order).ToListAsync();
            return Results.Ok(new LessonView(
                id, lesson.Title, course.Id, course.Title, lesson.Module.Title, enrolled, progress?.CompletedAt != null,
                i > 0 ? order[i - 1] : null, i < order.Count - 1 ? order[i + 1] : null, blocks.Select(BuilderBlocks.ToDto).ToList()));
        });

        g.MapPost("/lessons/{id:int}/complete", async (int id, CompleteRequest req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ViewableLesson(id, user) is not { } lesson) return Results.NotFound();
            var uid = user.UserId();
            if (!await access.IsEnrolled(lesson.Module.CourseId, uid)) return Results.Conflict(new { title = "Enrol in the course to track progress." });
            var p = await db.Progress.FindAsync(uid, id);
            if (p is null) db.Progress.Add(p = new LessonProgress { UserId = uid, LessonId = id });
            p.CompletedAt = req.Completed ? DateTime.UtcNow : null;
            p.LastAccessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapGet("/me/courses", async (ClaimsPrincipal user, AppDb db) =>
        {
            var uid = user.UserId();
            var enrolments = await db.Enrolments.Where(e => e.UserId == uid && e.UnenrolledAt == null).Include(e => e.Course).ToListAsync();
            var ids = enrolments.Select(e => e.CourseId).ToList();
            var lessons = await db.Lessons.Where(l => ids.Contains(l.Module.CourseId))
                .OrderBy(l => l.Module.Order).ThenBy(l => l.Order).Select(l => new { l.Id, l.Module.CourseId }).ToListAsync();
            var progress = await db.Progress.Where(p => p.UserId == uid && ids.Contains(p.Lesson.Module.CourseId))
                .Select(p => new { p.LessonId, p.Lesson.Module.CourseId, p.CompletedAt, p.LastAccessedAt }).ToListAsync();

            return enrolments.Select(e =>
            {
                var all = lessons.Where(l => l.CourseId == e.CourseId).Select(l => l.Id).ToList();
                var mine = progress.Where(p => p.CourseId == e.CourseId).ToList();
                var done = mine.Where(p => p.CompletedAt != null).Select(p => p.LessonId).ToHashSet();
                // Continue where the learner last was; otherwise the first lesson they have not completed.
                var last = mine.OrderByDescending(p => p.LastAccessedAt).FirstOrDefault();
                int? next = last?.LessonId ?? all.FirstOrDefault(l => !done.Contains(l));
                if (next == 0) next = null;
                return new MyCourse(e.CourseId, e.Course.Title, e.Course.CoverMediaId, done.Count, all.Count, next, last?.LastAccessedAt ?? e.EnrolledAt);
            }).OrderByDescending(c => c.LastActivity).ToList();
        });
    }
}
