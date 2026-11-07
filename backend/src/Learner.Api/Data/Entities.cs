namespace Learner.Api.Data;

public enum Visibility { Public, Private }
public enum CourseStatus { Draft, Published, Archived }
public enum Difficulty { Beginner, Intermediate, Advanced }
/// <summary>Collaborator = teacher who can edit the course. Invited = learner allowed into a private course.</summary>
public enum AccessKind { Collaborator, Invited }

/// <summary>Anything with a manual position inside its parent.</summary>
public interface IOrdered
{
    int Id { get; }
    int Order { get; set; }
}

public class Curriculum
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? CoverMediaId { get; set; }
    public int? CurriculumId { get; set; }
    public Curriculum? Curriculum { get; set; }
    public string OwnerId { get; set; } = "";
    public AppUser Owner { get; set; } = null!;
    public Visibility Visibility { get; set; }
    public CourseStatus Status { get; set; }
    public Difficulty Difficulty { get; set; }
    public int EstimatedMinutes { get; set; }
    public List<string> LearningObjectives { get; set; } = [];
    /// <summary>Bumped each time the course is published.</summary>
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<Module> Modules { get; set; } = [];

    public void Touch() => UpdatedAt = DateTime.UtcNow;
}

public class CourseAccess
{
    public int CourseId { get; set; }
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    public AccessKind Kind { get; set; }
}

public class Module : IOrdered
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string Title { get; set; } = "";
    public int Order { get; set; }
    public List<Lesson> Lessons { get; set; } = [];
}

public class Lesson : IOrdered
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public string Title { get; set; } = "";
    public int Order { get; set; }
    public List<ContentBlock> Blocks { get; set; } = [];
    /// <summary>Competencies this lesson teaches. Used for recommendations only: viewing a lesson is not evidence.</summary>
    public List<Competency> Competencies { get; set; } = [];
}

/// <summary>One unit of lesson content. <see cref="Type"/> picks the schema and renderer; the payload is JSON (see BlockSchemas).</summary>
public class ContentBlock : IOrdered
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public string Type { get; set; } = "";
    public int Order { get; set; }
    public string DataJson { get; set; } = "{}";
}

/// <summary>Uploaded file metadata. The bytes live on disk under Media:Path, named by <see cref="Id"/>.</summary>
public class MediaAsset
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string OwnerId { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A learner's registration in a course. Unenrolling stamps <see cref="UnenrolledAt"/>; rows are kept as history.</summary>
public class Enrolment
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? UnenrolledAt { get; set; }
}

/// <summary>Completion and last activity for one learner in one lesson. Completion is not competency.</summary>
public class LessonProgress
{
    public string UserId { get; set; } = "";
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public DateTime? CompletedAt { get; set; }
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A skill or knowledge area a course wants to evidence. Courses define their own framework.</summary>
public class Competency
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public class QuestionBank
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string Name { get; set; } = "";
    public List<Question> Questions { get; set; } = [];
}

public class Question
{
    public int Id { get; set; }
    public int QuestionBankId { get; set; }
    public QuestionBank Bank { get; set; } = null!;
    /// <summary>single, multiple, truefalse, fillblank, numeric. See QuestionTypes.</summary>
    public string Type { get; set; } = "";
    public string Text { get; set; } = "";
    /// <summary>Type-specific options and answer key (JSON). Never sent to a learner before they answer.</summary>
    public string DataJson { get; set; } = "{}";
    public string Explanation { get; set; } = "";
    public string Hint { get; set; } = "";
    public Difficulty Difficulty { get; set; }
    public int Marks { get; set; } = 1;
    public string Category { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public string? ImageMediaId { get; set; }
    public List<Competency> Competencies { get; set; } = [];
}

public enum QuizKind { KnowledgeCheck, Quiz, ModuleAssessment, Exam }

/// <summary>One configured assessment: knowledge check, quiz, module assessment or final exam. Same engine for all.</summary>
public class Quiz
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public QuizKind Kind { get; set; }
    /// <summary>Draw this many questions from the pool per attempt. Null = all of them.</summary>
    public int? QuestionCount { get; set; }
    public int PassPercent { get; set; } = 70;
    public bool ShuffleQuestions { get; set; } = true;
    public bool ShuffleOptions { get; set; } = true;
    /// <summary>Show correctness and explanations after each answer (otherwise only at the end).</summary>
    public bool ImmediateFeedback { get; set; } = true;
    public int? TimeLimitMinutes { get; set; }
    public int? MaxAttempts { get; set; }
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }
    public List<QuizQuestion> Questions { get; set; } = [];
}

public class QuizQuestion
{
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
}

/// <summary>
/// One sitting of a quiz. The snapshot freezes the questions, answer keys and settings as they were, so a result can always be
/// reconstructed even after the quiz or course is edited. Attempts are never overwritten or deleted.
/// </summary>
public class AssessmentAttempt
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public int CourseId { get; set; }
    public int CourseVersion { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public double Score { get; set; }
    public double MaxScore { get; set; }
    public bool? Passed { get; set; }
    /// <summary>Question ids in the order they were presented (drives the no-immediate-repeat rule).</summary>
    public List<int> QuestionOrder { get; set; } = [];
    public string SnapshotJson { get; set; } = "{}";
    public List<AssessmentResponse> Responses { get; set; } = [];
}

public class AssessmentResponse
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public AssessmentAttempt Attempt { get; set; } = null!;
    public int QuestionId { get; set; }
    public string ResponseJson { get; set; } = "{}";
    public double Score { get; set; }
    public double MaxScore { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// One piece of evidence that a learner did (or did not) demonstrate a competency, traceable to its source.
/// Competency state is always derived from these rows, never stored, so evaluation rules can change later.
/// </summary>
public class CompetencyEvidence
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public int CompetencyId { get; set; }
    public Competency Competency { get; set; } = null!;
    public int CourseId { get; set; }
    /// <summary>"quiz" or "exercise".</summary>
    public string Source { get; set; } = "";
    /// <summary>Attempt id in the source's table.</summary>
    public int SourceId { get; set; }
    /// <summary>Fraction achieved, 0..1.</summary>
    public double Score { get; set; }
    /// <summary>How much this evidence counts (marks available for that competency in the source).</summary>
    public double Weight { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A teacher-configured instance of an exercise component (a scenario, its materials and its marking key).</summary>
public class ExerciseConfiguration
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    /// <summary>Which exercise component runs this (see ExerciseTypes).</summary>
    public string TypeKey { get; set; } = "";
    public string Title { get; set; } = "";
    /// <summary>Component-specific configuration (JSON). Includes the answer key, so learners only ever get ExerciseType.ForLearner of it.</summary>
    public string ConfigJson { get; set; } = "{}";
    /// <summary>Bumped whenever the configuration changes; attempts record the version they were taken against.</summary>
    public int Version { get; set; } = 1;
    public List<Competency> Competencies { get; set; } = [];
}

/// <summary>One learner submission. Immutable: it keeps the configuration it was marked against, so the result can always be reconstructed.</summary>
public class ExerciseAttempt
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public int ExerciseId { get; set; }
    public ExerciseConfiguration Exercise { get; set; } = null!;
    public int CourseId { get; set; }
    public string TypeKey { get; set; } = "";
    public int ConfigVersion { get; set; }
    public string ConfigSnapshotJson { get; set; } = "{}";
    public string SubmissionJson { get; set; } = "{}";
    public string ResultJson { get; set; } = "{}";
    public double Score { get; set; }
    public double MaxScore { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
