using System.Net;
using System.Text.Json;
using Learner.Api.Data;
using Learner.Api.Features;
using Microsoft.EntityFrameworkCore;
using static Learner.Api.Tests.TestData;

namespace Learner.Api.Tests;

public class AssessmentTests : IClassFixture<TestApp>
{
    readonly TestApp app;
    public AssessmentTests(TestApp app) => this.app = app;

    static QuestionInput Single(string text, string correct = "a", int marks = 1, List<int>? competencies = null) => new(
        "single", text, J(new { options = new[] { new { id = "a", text = "Option A" }, new { id = "b", text = "Option B" } }, correct }),
        "Because of clause 4.", "Think about clause 4.", Difficulty.Beginner, marks, "Basics", ["tag"], null, competencies);

    static QuizInput Settings(QuizKind kind = QuizKind.Quiz, int? count = null, int pass = 70, bool instant = true, int? limit = null, int? max = null,
        DateTime? from = null, DateTime? until = null, List<int>? questions = null) =>
        new("Quiz", "", kind, count, pass, true, true, instant, limit, max, from, until, questions);

    /// <summary>A published course with a bank of single-choice questions and a quiz over them.</summary>
    async Task<(HttpClient teacher, int course, int quiz, List<int> questions)> Setup(int questionCount = 3, Func<QuizInput, QuizInput>? tweak = null)
    {
        var teacher = await app.SignedIn(Roles.Teacher);
        var (course, _) = await PublishedCourse(teacher);
        var bank = await teacher.Post($"/api/builder/courses/{course}/banks", new NameRequest("Bank")).Json<BankDto>();
        var ids = new List<int>();
        for (var i = 1; i <= questionCount; i++)
            ids.Add((await teacher.Post($"/api/builder/banks/{bank.Id}/questions", Single($"Question {i}")).Json<QuestionDto>()).Id);
        var quiz = await teacher.Post($"/api/builder/courses/{course}/quizzes", new NewQuiz("Quiz", QuizKind.Quiz)).Json<QuizDto>();
        var input = Settings(questions: ids);
        (await teacher.Put($"/api/builder/quizzes/{quiz.Id}", tweak?.Invoke(input) ?? input)).EnsureSuccessStatusCode();
        return (teacher, course, quiz.Id, ids);
    }

    async Task<HttpClient> Enrolled(int course)
    {
        var learner = await app.SignedIn();
        (await learner.Post($"/api/courses/{course}/enrol")).EnsureSuccessStatusCode();
        return learner;
    }

    static Task<AttemptView> Start(HttpClient c, int quiz) => c.Post($"/api/quizzes/{quiz}/attempts").Json<AttemptView>();
    static Task<HttpResponseMessage> Answer(HttpClient c, int attempt, int question, string choice) =>
        c.Post($"/api/attempts/{attempt}/answers", new AnswerRequest(question, J(new { choice })));

    // ---- authoring

    [Fact]
    public async Task Teachers_author_competencies_banks_questions_and_quizzes()
    {
        var t = await app.SignedIn(Roles.Teacher);
        var (course, _) = await PublishedCourse(t);

        var comp = await t.Post($"/api/builder/courses/{course}/competencies", new CompetencyInput("Variations", "Assess variations")).Json<CompetencyDto>();
        var bank = await t.Post($"/api/builder/courses/{course}/banks", new NameRequest("Variations bank")).Json<BankDto>();
        var q = await t.Post($"/api/builder/banks/{bank.Id}/questions", Single("What is a variation?", competencies: [comp.Id])).Json<QuestionDto>();
        q.CompetencyIds.ToArray().ShouldBe([comp.Id]);
        q.Tags.ToArray().ShouldBe(["tag"]);

        var edited = await t.Put($"/api/builder/questions/{q.Id}", Single("Changed text", "b", marks: 5)).Json<QuestionDto>();
        (edited.Text, edited.Marks, edited.CompetencyIds.Count).ShouldBe(("Changed text", 5, 0));

        var quiz = await t.Post($"/api/builder/courses/{course}/quizzes", new NewQuiz("Final exam", QuizKind.Exam)).Json<QuizDto>();
        quiz.ImmediateFeedback.ShouldBe(false); // exams default to feedback at the end
        var saved = await t.Put($"/api/builder/quizzes/{quiz.Id}", Settings(QuizKind.Exam, count: 1, pass: 80, limit: 30, max: 2, questions: [q.Id])).Json<QuizDto>();
        (saved.QuestionIds.Count, saved.PassPercent, saved.TimeLimitMinutes, saved.MaxAttempts).ShouldBe((1, 80, 30, 2));

        (await t.Get<List<BankDto>>($"/api/builder/courses/{course}/banks")).Single().Questions.ShouldBe(1);
    }

    [Fact]
    public async Task Invalid_questions_and_quiz_settings_are_rejected()
    {
        var (t, course, quiz, ids) = await Setup(2);
        var bank = (await t.Get<List<BankDto>>($"/api/builder/courses/{course}/banks")).Single();

        (await t.Post($"/api/builder/banks/{bank.Id}/questions", Single("x", correct: "zzz"))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await t.Post($"/api/builder/banks/{bank.Id}/questions", Single(" "))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await t.Post($"/api/builder/banks/{bank.Id}/questions", Single("x") with { Type = "nope" })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await t.Post($"/api/builder/banks/{bank.Id}/questions", Single("x", competencies: [9999]))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await t.Put($"/api/builder/quizzes/{quiz}", Settings(pass: 150, questions: ids))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await t.Put($"/api/builder/quizzes/{quiz}", Settings(count: 5, questions: ids))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await t.Put($"/api/builder/quizzes/{quiz}", Settings(questions: [123456]))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Assessment_authoring_is_limited_to_the_courses_teachers()
    {
        var (_, course, quiz, ids) = await Setup(1);
        var learner = await app.SignedIn();
        var other = await app.SignedIn(Roles.Teacher);

        (await learner.GetAsync($"/api/builder/courses/{course}/banks")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await other.GetAsync($"/api/builder/courses/{course}/banks")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.Put($"/api/builder/questions/{ids[0]}", Single("hack"))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.Put($"/api/builder/quizzes/{quiz}", Settings())).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.DeleteAsync($"/api/builder/quizzes/{quiz}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.Post($"/api/builder/courses/{course}/competencies", new CompetencyInput("x", null))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Publishing_requires_quiz_blocks_to_point_at_a_quiz_with_questions()
    {
        var (t, course, quiz, _) = await Setup(1);
        await t.Post($"/api/builder/courses/{course}/unpublish");
        var lesson = (await t.Get<CourseDetail>($"/api/builder/courses/{course}")).Modules[0].Lessons[0].Id;
        var empty = await t.Post($"/api/builder/courses/{course}/quizzes", new NewQuiz("Empty", QuizKind.Quiz)).Json<QuizDto>();

        var block = (await t.Post($"/api/builder/lessons/{lesson}/blocks", new NewBlock("quiz", J(new { quizId = empty.Id }))).Json<LessonEdit>()).Blocks.Last();
        (await t.Post($"/api/builder/courses/{course}/publish")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await t.Put($"/api/builder/blocks/{block.Id}", new BlockData(J(new { quizId = 987654 })));
        (await t.Post($"/api/builder/courses/{course}/publish")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await t.Put($"/api/builder/blocks/{block.Id}", new BlockData(J(new { quizId = quiz })));
        (await t.Post($"/api/builder/courses/{course}/publish")).EnsureSuccessStatusCode();
    }

    // ---- taking a quiz

    [Fact]
    public async Task Attempt_flow_gives_immediate_feedback_scores_and_keeps_history()
    {
        var (_, course, quiz, ids) = await Setup(3);
        var learner = await Enrolled(course);

        var attempt = await Start(learner, quiz);
        attempt.Questions.Count.ShouldBe(3);
        attempt.Questions.All(q => !q.Data.AsObject().ContainsKey("correct")).ShouldBeTrue(); // key is never sent up front
        attempt.Questions.All(q => q.Result is null).ShouldBeTrue();

        var first = attempt.Questions[0];
        var correct = await (await Answer(learner, attempt.Id, first.Id, "a")).Json<AnswerResult>();
        correct.Result!.Correct.ShouldBeTrue();
        correct.Result.Explanation.ShouldBe("Because of clause 4.");
        var wrong = await (await Answer(learner, attempt.Id, attempt.Questions[1].Id, "b")).Json<AnswerResult>();
        wrong.Result!.Correct.ShouldBe(false);
        wrong.Result.AnswerKey.GetProperty("correct").GetString().ShouldBe("a"); // revealed only after answering

        (await Answer(learner, attempt.Id, first.Id, "b")).StatusCode.ShouldBe(HttpStatusCode.Conflict); // cannot change an answer
        (await Answer(learner, attempt.Id, 424242, "a")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await learner.Post($"/api/attempts/{attempt.Id}/answers", new AnswerRequest(attempt.Questions[2].Id, J(5)))).StatusCode.ShouldBe(HttpStatusCode.BadRequest); // malformed, not a crash

        var done = await learner.Post($"/api/attempts/{attempt.Id}/finish").Json<AttemptView>();
        (done.Finished, done.Score, done.MaxScore, done.Passed).ShouldBe((true, 1, 3, false)); // 33% < 70% pass mark
        done.Questions.All(q => q.Result is not null).ShouldBeTrue(); // review includes the unanswered one
        (await Answer(learner, attempt.Id, attempt.Questions[2].Id, "a")).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // History is kept; a new attempt does not touch the old one.
        var second = await Start(learner, quiz);
        second.Id.ShouldBe(attempt.Id + 1);
        var history = await learner.Get<List<AttemptSummary>>("/api/me/attempts");
        history.Single().Id.ShouldBe(attempt.Id); // second is still in progress
        (await learner.Get<AttemptView>($"/api/attempts/{attempt.Id}")).Score.ShouldBe(1);

        var info = await learner.Get<QuizInfo>($"/api/quizzes/{quiz}");
        (info.AttemptsUsed, info.InProgressAttemptId, info.BestPercent).ShouldBe((2, second.Id, 33.3));
    }

    [Fact]
    public async Task Starting_again_resumes_the_open_attempt_and_attempts_are_private_to_their_learner()
    {
        var (_, course, quiz, _) = await Setup(2);
        var learner = await Enrolled(course);
        var a = await Start(learner, quiz);
        (await Start(learner, quiz)).Id.ShouldBe(a.Id);

        var other = await Enrolled(course);
        (await other.GetAsync($"/api/attempts/{a.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await Answer(other, a.Id, a.Questions[0].Id, "a")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.Post($"/api/attempts/{a.Id}/finish")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Taking_a_quiz_requires_enrolment_and_course_access()
    {
        var (teacher, course, quiz, _) = await Setup(1);
        var notEnrolled = await app.SignedIn();
        (await notEnrolled.Post($"/api/quizzes/{quiz}/attempts")).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // teachers can try their own quiz without enrolling
        (await teacher.Post($"/api/quizzes/{quiz}/attempts")).EnsureSuccessStatusCode();

        // a private course the learner has no access to does not even reveal its quizzes
        var (priv, _) = await PublishedCourse(teacher, "Private", Visibility.Private);
        var bank = await teacher.Post($"/api/builder/courses/{priv}/banks", new NameRequest("b")).Json<BankDto>();
        var q = await teacher.Post($"/api/builder/banks/{bank.Id}/questions", Single("q")).Json<QuestionDto>();
        var pq = await teacher.Post($"/api/builder/courses/{priv}/quizzes", new NewQuiz("p", QuizKind.Quiz)).Json<QuizDto>();
        await teacher.Put($"/api/builder/quizzes/{pq.Id}", Settings(questions: [q.Id]));
        (await notEnrolled.GetAsync($"/api/quizzes/{pq.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await notEnrolled.Post($"/api/quizzes/{pq.Id}/attempts")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_new_attempt_never_opens_with_the_question_the_previous_one_ended_on()
    {
        var (_, course, quiz, _) = await Setup(3);
        var learner = await Enrolled(course);

        var lastShown = -1;
        for (var i = 0; i < 12; i++)
        {
            var a = await Start(learner, quiz);
            a.Questions.Count.ShouldBe(3);
            Assert.NotEqual(lastShown, a.Questions[0].Id);
            lastShown = a.Questions[^1].Id;
            (await learner.Post($"/api/attempts/{a.Id}/finish")).EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task A_one_question_quiz_can_be_repeated_indefinitely()
    {
        var (_, course, quiz, ids) = await Setup(1);
        var learner = await Enrolled(course);
        for (var i = 0; i < 4; i++)
        {
            var a = await Start(learner, quiz);
            a.Questions.Single().Id.ShouldBe(ids[0]);
            await learner.Post($"/api/attempts/{a.Id}/finish");
        }
    }

    [Fact]
    public async Task Random_selection_draws_the_configured_number_from_the_pool()
    {
        var (_, course, quiz, ids) = await Setup(5, s => s with { QuestionCount = 2 });
        var learner = await Enrolled(course);
        var a = await Start(learner, quiz);
        a.Questions.Count.ShouldBe(2);
        a.Questions.All(q => ids.Contains(q.Id)).ShouldBeTrue();
        a.Questions.Select(q => q.Id).Distinct().Count().ShouldBe(2);
        a.MaxScore.ShouldBe(2);
    }

    [Fact]
    public async Task Exams_enforce_attempt_limits_hide_feedback_and_availability()
    {
        var (_, course, exam, _) = await Setup(2, s => s with { Kind = QuizKind.Exam, ImmediateFeedback = false, MaxAttempts = 1 });
        var learner = await Enrolled(course);

        var a = await Start(learner, exam);
        var answered = await (await Answer(learner, a.Id, a.Questions[0].Id, "a")).Json<AnswerResult>();
        (answered.Recorded, answered.Result).ShouldBe((true, null)); // no feedback mid-exam
        (await learner.Get<AttemptView>($"/api/attempts/{a.Id}")).Questions.All(q => q.Result is null).ShouldBeTrue();
        var done = await learner.Post($"/api/attempts/{a.Id}/finish").Json<AttemptView>();
        done.Questions.All(q => q.Result is not null).ShouldBeTrue(); // revealed at the end

        (await learner.Post($"/api/quizzes/{exam}/attempts")).StatusCode.ShouldBe(HttpStatusCode.Conflict); // used its one attempt

        var (_, course2, closed, _) = await Setup(1, s => s with { AvailableUntil = DateTime.UtcNow.AddDays(-1), AvailableFrom = DateTime.UtcNow.AddDays(-2) });
        var late = await Enrolled(course2);
        (await late.Post($"/api/quizzes/{closed}/attempts")).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await late.Get<QuizInfo>($"/api/quizzes/{closed}")).ClosedReason.ShouldBe("This assessment has closed.");
    }

    [Fact]
    public async Task Time_limit_closes_the_attempt_at_the_deadline()
    {
        var (_, course, quiz, _) = await Setup(2, s => s with { TimeLimitMinutes = 10 });
        var learner = await Enrolled(course);
        var a = await Start(learner, quiz);
        a.Deadline.HasValue.ShouldBeTrue();
        (await Answer(learner, a.Id, a.Questions[0].Id, "a")).EnsureSuccessStatusCode();

        await app.WithDb(async db =>
        {
            var row = await db.Attempts.FirstAsync(x => x.Id == a.Id);
            row.StartedAt = DateTime.UtcNow.AddMinutes(-30);
            await db.SaveChangesAsync();
        });

        (await Answer(learner, a.Id, a.Questions[1].Id, "a")).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var closed = await learner.Get<AttemptView>($"/api/attempts/{a.Id}");
        (closed.Finished, closed.Score, closed.Passed).ShouldBe((true, 1, false));
    }

    [Fact]
    public async Task Past_attempts_can_be_reconstructed_after_the_quiz_is_edited_or_its_questions_deleted()
    {
        var (teacher, course, quiz, ids) = await Setup(1);
        var learner = await Enrolled(course);
        var a = await Start(learner, quiz);
        await Answer(learner, a.Id, ids[0], "a");
        await learner.Post($"/api/attempts/{a.Id}/finish");

        await teacher.Put($"/api/builder/questions/{ids[0]}", Single("Completely different text", "b"));
        (await teacher.DeleteAsync($"/api/builder/questions/{ids[0]}")).EnsureSuccessStatusCode();

        var review = await learner.Get<AttemptView>($"/api/attempts/{a.Id}");
        review.Questions.Single().Text.ShouldBe("Question 1");
        review.Questions.Single().Result!.Correct.ShouldBeTrue();
        review.Questions.Single().Result!.AnswerKey.GetProperty("correct").GetString().ShouldBe("a");

        // A quiz with attempts cannot be deleted; one without can.
        (await teacher.DeleteAsync($"/api/builder/quizzes/{quiz}")).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var spare = await teacher.Post($"/api/builder/courses/{course}/quizzes", new NewQuiz("Spare", QuizKind.Quiz)).Json<QuizDto>();
        (await teacher.DeleteAsync($"/api/builder/quizzes/{spare.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
