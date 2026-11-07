using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Learner.Api.Data;

public static class Roles
{
    public const string Learner = "Learner";
    public const string Teacher = "Teacher";
    public const string Admin = "Admin";
    public static readonly string[] All = [Learner, Teacher, Admin];
}

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
}

public class AppDb(DbContextOptions<AppDb> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Curriculum> Curricula => Set<Curriculum>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseAccess> CourseAccesses => Set<CourseAccess>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<ContentBlock> Blocks => Set<ContentBlock>();
    public DbSet<MediaAsset> Media => Set<MediaAsset>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<LessonProgress> Progress => Set<LessonProgress>();
    public DbSet<Competency> Competencies => Set<Competency>();
    public DbSet<QuestionBank> Banks => Set<QuestionBank>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<AssessmentAttempt> Attempts => Set<AssessmentAttempt>();
    public DbSet<AssessmentResponse> Responses => Set<AssessmentResponse>();
    public DbSet<CompetencyEvidence> Evidence => Set<CompetencyEvidence>();
    public DbSet<ExerciseConfiguration> Exercises => Set<ExerciseConfiguration>();
    public DbSet<ExerciseAttempt> ExerciseAttempts => Set<ExerciseAttempt>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        // Without these, EF reads a collection with no inverse as one-to-many, and a competency could tag only one question and one lesson.
        b.Entity<Question>().HasMany(q => q.Competencies).WithMany();
        b.Entity<Lesson>().HasMany(l => l.Competencies).WithMany();
        b.Entity<ExerciseConfiguration>().HasMany(e => e.Competencies).WithMany();
        // History must survive: an exercise with attempts cannot be deleted out from under them.
        b.Entity<ExerciseAttempt>().HasOne(a => a.Exercise).WithMany().HasForeignKey(a => a.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ExerciseAttempt>().HasIndex(a => new { a.UserId, a.ExerciseId });
        // Evidence must outlive edits: a competency with evidence cannot be deleted.
        b.Entity<CompetencyEvidence>().HasOne(e => e.Competency).WithMany().HasForeignKey(e => e.CompetencyId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CompetencyEvidence>().HasIndex(e => new { e.UserId, e.CompetencyId });
        b.Entity<QuizQuestion>().HasKey(q => new { q.QuizId, q.QuestionId });
        b.Entity<QuizQuestion>().HasOne(q => q.Question).WithMany().HasForeignKey(q => q.QuestionId);
        // History must survive: a quiz with attempts cannot be deleted out from under them.
        b.Entity<AssessmentAttempt>().HasOne(a => a.Quiz).WithMany().HasForeignKey(a => a.QuizId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AssessmentAttempt>().HasIndex(a => new { a.UserId, a.QuizId });
        b.Entity<LessonProgress>().HasKey(p => new { p.UserId, p.LessonId });
        b.Entity<Enrolment>().HasIndex(e => new { e.UserId, e.CourseId });
        b.Entity<CourseAccess>().HasKey(a => new { a.CourseId, a.UserId });
        b.Entity<CourseAccess>().HasOne<Course>().WithMany().HasForeignKey(a => a.CourseId);
        b.Entity<Course>().HasOne(c => c.Owner).WithMany().HasForeignKey(c => c.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Course>().HasOne(c => c.Curriculum).WithMany().OnDelete(DeleteBehavior.SetNull);
    }
}
