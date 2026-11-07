using System.Security.Claims;
using Learner.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Learner.Api.Features;

/// <summary>
/// Server-side authorisation for courses. Every builder endpoint resolves its target through here,
/// so "can this user touch this course" is answered in exactly one place.
/// Null means "not found or not yours": callers return 404 so private courses are not revealed.
/// </summary>
public class Access(AppDb db)
{
    public async Task<bool> CanManage(Course c, ClaimsPrincipal u) =>
        u.IsInRole(Roles.Admin) || c.OwnerId == u.UserId() ||
        await db.CourseAccesses.AnyAsync(a => a.CourseId == c.Id && a.UserId == u.UserId() && a.Kind == AccessKind.Collaborator);

    public async Task<Course?> Manageable(int courseId, ClaimsPrincipal u)
    {
        var c = await db.Courses.FindAsync(courseId);
        return c is not null && await CanManage(c, u) ? c : null;
    }

    public async Task<Module?> ManageableModule(int moduleId, ClaimsPrincipal u)
    {
        var m = await db.Modules.Include(m => m.Course).FirstOrDefaultAsync(m => m.Id == moduleId);
        return m is not null && await CanManage(m.Course, u) ? m : null;
    }

    public async Task<Lesson?> ManageableLesson(int lessonId, ClaimsPrincipal u)
    {
        var l = await db.Lessons.Include(l => l.Module).ThenInclude(m => m.Course).FirstOrDefaultAsync(l => l.Id == lessonId);
        return l is not null && await CanManage(l.Module.Course, u) ? l : null;
    }

    /// <summary>Returns <paramref name="entity"/> only if the user can manage the course it belongs to.</summary>
    async Task<T?> Owned<T>(T? entity, Func<T, int> courseId, ClaimsPrincipal u) where T : class
    {
        if (entity is null) return null;
        var c = await db.Courses.FindAsync(courseId(entity));
        return c is not null && await CanManage(c, u) ? entity : null;
    }

    public async Task<QuestionBank?> ManageableBank(int id, ClaimsPrincipal u) => await Owned(await db.Banks.FindAsync(id), b => b.CourseId, u);

    public async Task<Question?> ManageableQuestion(int id, ClaimsPrincipal u) =>
        await Owned(await db.Questions.Include(q => q.Bank).Include(q => q.Competencies).FirstOrDefaultAsync(q => q.Id == id), q => q.Bank.CourseId, u);

    public async Task<Quiz?> ManageableQuiz(int id, ClaimsPrincipal u) =>
        await Owned(await db.Quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == id), q => q.CourseId, u);

    public async Task<ExerciseConfiguration?> ManageableExercise(int id, ClaimsPrincipal u) =>
        await Owned(await db.Exercises.Include(e => e.Competencies).FirstOrDefaultAsync(e => e.Id == id), e => e.CourseId, u);

    public async Task<Competency?> ManageableCompetency(int id, ClaimsPrincipal u) => await Owned(await db.Competencies.FindAsync(id), c => c.CourseId, u);

    // ---- learner side

    public Task<bool> IsEnrolled(int courseId, string userId) =>
        db.Enrolments.AnyAsync(e => e.CourseId == courseId && e.UserId == userId && e.UnenrolledAt == null);

    /// <summary>Published, and either public, the user was invited, or the user manages the course (so teachers can try it as a learner).</summary>
    public async Task<bool> CanEnrol(Course c, ClaimsPrincipal u) =>
        c.Status == CourseStatus.Published &&
        (c.Visibility == Visibility.Public || await CanManage(c, u) ||
         await db.CourseAccesses.AnyAsync(a => a.CourseId == c.Id && a.UserId == u.UserId()));

    /// <summary>Managers (preview), anyone allowed to enrol, and past learners of an archived course.</summary>
    public async Task<bool> CanView(Course c, ClaimsPrincipal u) =>
        await CanManage(c, u) || await CanEnrol(c, u) ||
        (c.Status == CourseStatus.Archived && await db.Enrolments.AnyAsync(e => e.CourseId == c.Id && e.UserId == u.UserId()));

    public async Task<Course?> Viewable(int courseId, ClaimsPrincipal u)
    {
        var c = await db.Courses.FindAsync(courseId);
        return c is not null && await CanView(c, u) ? c : null;
    }

    public async Task<Lesson?> ViewableLesson(int lessonId, ClaimsPrincipal u)
    {
        var l = await db.Lessons.Include(l => l.Module).ThenInclude(m => m.Course).FirstOrDefaultAsync(l => l.Id == lessonId);
        return l is not null && await CanView(l.Module.Course, u) ? l : null;
    }
}
