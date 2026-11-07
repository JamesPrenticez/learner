using System.Net;
using Learner.Api.Data;
using Learner.Api.Features;
using static Learner.Api.Tests.TestData;

namespace Learner.Api.Tests;

/// <summary>The evaluator on its own: evidence in, state out. No database.</summary>
public class CompetencyEvaluatorTests
{
    static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    static EvidenceItem E(double score, int day, string source = "quiz", double weight = 1) => new(source, score, weight, T0.AddDays(day));
    static readonly CompetencyRules Rules = new();

    [Fact]
    public void No_evidence_means_not_assessed() => Evaluate().State.ShouldBe(CompetencyState.NotAssessed);

    [Fact]
    public void One_good_result_is_not_enough_but_repeated_performance_demonstrates()
    {
        Evaluate(E(1, 1)).State.ShouldBe(CompetencyState.Developing);
        Evaluate(E(1, 1), E(0.9, 2)).State.ShouldBe(CompetencyState.Demonstrated);
    }

    [Fact]
    public void Below_the_threshold_stays_developing()
    {
        var r = Evaluate(E(0.7, 1), E(0.8, 2));
        r.State.ShouldBe(CompetencyState.Developing);
        r.Evidence.ShouldBe(2);
    }

    [Fact]
    public void Only_recent_evidence_counts_so_the_profile_follows_current_performance()
    {
        // five perfect results long ago, then five failures: no longer demonstrated
        var slump = Enumerable.Range(1, 5).Select(d => E(1, d)).Concat(Enumerable.Range(6, 5).Select(d => E(0, d)));
        Evaluate(slump.ToArray()).State.ShouldBe(CompetencyState.Developing);
        // and the other way round: early failures are forgiven once recent evidence is strong
        var recovery = Enumerable.Range(1, 5).Select(d => E(0, d)).Concat(Enumerable.Range(6, 5).Select(d => E(1, d)));
        Evaluate(recovery.ToArray()).State.ShouldBe(CompetencyState.Demonstrated);
    }

    [Fact]
    public void Evidence_is_weighted_by_the_marks_behind_it() =>
        Math.Round(Evaluate(E(1, 1, weight: 1), E(0, 2, weight: 9)).Score, 2).ShouldBe(0.1);

    [Fact]
    public void Source_weights_are_configurable()
    {
        // quiz 100%, exercise 60%: equal weighting averages exactly 80% (demonstrated); weighting exercises 1.5x pulls it to 76%
        var evidence = new[] { E(1, 1, "quiz"), E(0.6, 2, "exercise") };
        var equal = new CompetencyRules { SourceWeights = new() { ["quiz"] = 1, ["exercise"] = 1 } };
        var heavy = new CompetencyRules { SourceWeights = new() { ["quiz"] = 1, ["exercise"] = 1.5 } };
        CompetencyEvaluator.Evaluate(evidence, equal).State.ShouldBe(CompetencyState.Demonstrated);
        CompetencyEvaluator.Evaluate(evidence, heavy).State.ShouldBe(CompetencyState.Developing);
    }

    [Fact]
    public void Thresholds_come_from_the_rules()
    {
        var strict = new CompetencyRules { DemonstratedThreshold = 0.95, MinEvidence = 3 };
        CompetencyEvaluator.Evaluate([E(1, 1), E(1, 2)], strict).State.ShouldBe(CompetencyState.Developing);
        CompetencyEvaluator.Evaluate([E(1, 1), E(1, 2), E(0.96, 3)], strict).State.ShouldBe(CompetencyState.Demonstrated);
    }

    static CompetencyResult Evaluate(params EvidenceItem[] evidence) => CompetencyEvaluator.Evaluate(evidence, Rules);
}

public class CompetencyApiTests : IClassFixture<TestApp>
{
    readonly TestApp app;
    public CompetencyApiTests(TestApp app) => this.app = app;

    static QuestionInput Question(string text, List<int>? competencies) => new(
        "single", text, J(new { options = new[] { new { id = "a", text = "A" }, new { id = "b", text = "B" } }, correct = "a" }),
        "", null, Difficulty.Beginner, 1, null, null, null, competencies);

    /// <summary>Course with one competency, two tagged questions, a quiz over them, and a lesson that teaches the competency.</summary>
    async Task<(HttpClient teacher, int course, int quiz, int competency, int lesson)> Setup()
    {
        var teacher = await app.SignedIn(Roles.Teacher);
        var (course, lessons) = await PublishedCourse(teacher);
        var comp = await teacher.Post($"/api/builder/courses/{course}/competencies", new CompetencyInput("Variation assessment", null)).Json<CompetencyDto>();
        var bank = await teacher.Post($"/api/builder/courses/{course}/banks", new NameRequest("B")).Json<BankDto>();
        var q1 = await teacher.Post($"/api/builder/banks/{bank.Id}/questions", Question("Q1", [comp.Id])).Json<QuestionDto>();
        var q2 = await teacher.Post($"/api/builder/banks/{bank.Id}/questions", Question("Q2", [comp.Id])).Json<QuestionDto>();
        var quiz = await teacher.Post($"/api/builder/courses/{course}/quizzes", new NewQuiz("Variations quiz", QuizKind.Quiz)).Json<QuizDto>();
        await teacher.Put($"/api/builder/quizzes/{quiz.Id}", new QuizInput("Variations quiz", "", QuizKind.Quiz, null, 70, true, true, true, null, null, null, null, [q1.Id, q2.Id]));
        (await teacher.Put($"/api/builder/lessons/{lessons[0]}/competencies", new CompetencyIdsRequest([comp.Id]))).EnsureSuccessStatusCode();
        return (teacher, course, quiz.Id, comp.Id, lessons[0]);
    }

    static async Task Sit(HttpClient learner, int quiz, bool correct)
    {
        var a = await learner.Post($"/api/quizzes/{quiz}/attempts").Json<AttemptView>();
        if (correct)
            foreach (var q in a.Questions) await learner.Post($"/api/attempts/{a.Id}/answers", new AnswerRequest(q.Id, J(new { choice = "a" })));
        (await learner.Post($"/api/attempts/{a.Id}/finish")).EnsureSuccessStatusCode(); // unanswered questions score zero
    }

    static async Task<CompetencyStatus> Status(HttpClient learner, int competency) =>
        (await learner.Get<List<CompetencyStatus>>("/api/me/competencies")).Single(c => c.Id == competency);

    [Fact]
    public async Task Profile_evolves_with_assessment_evidence_independently_of_lesson_completion()
    {
        var (_, course, quiz, comp, lesson) = await Setup();
        var learner = await app.SignedIn();
        await learner.Post($"/api/courses/{course}/enrol");

        var start = await Status(learner, comp);
        start.State.ShouldBe(CompetencyState.NotAssessed);
        start.Recommended.Single().Id.ShouldBe(lesson); // a gap points at the lesson that teaches it

        await Sit(learner, quiz, correct: true);
        var one = await Status(learner, comp);
        (one.State, one.Evidence, one.Percent).ShouldBe((CompetencyState.Developing, 1, 100.0)); // one result is not enough

        await Sit(learner, quiz, correct: true);
        var two = await Status(learner, comp);
        two.State.ShouldBe(CompetencyState.Demonstrated);
        two.Recommended.ShouldBe([]);

        // Completing lessons never changes competency; sliding results do.
        await learner.Post($"/api/lessons/{lesson}/complete", new CompleteRequest(true));
        (await Status(learner, comp)).State.ShouldBe(CompetencyState.Demonstrated);
        for (var i = 0; i < 5; i++) await Sit(learner, quiz, correct: false);
        var slipped = await Status(learner, comp);
        slipped.State.ShouldBe(CompetencyState.Developing);
        slipped.Recommended.Single().Completed.ShouldBeTrue();
    }

    [Fact]
    public async Task Evidence_is_traceable_to_its_attempt_and_private_to_the_learner()
    {
        var (_, course, quiz, comp, _) = await Setup();
        var learner = await app.SignedIn();
        var other = await app.SignedIn();
        await learner.Post($"/api/courses/{course}/enrol");
        await other.Post($"/api/courses/{course}/enrol");

        var a = await learner.Post($"/api/quizzes/{quiz}/attempts").Json<AttemptView>();
        await learner.Post($"/api/attempts/{a.Id}/answers", new AnswerRequest(a.Questions[0].Id, J(new { choice = "a" })));
        await learner.Post($"/api/attempts/{a.Id}/finish");

        var evidence = await learner.Get<List<EvidenceRow>>($"/api/me/competencies/{comp}/evidence");
        var row = evidence.Single();
        (row.Source, row.SourceId, row.Title, row.Percent, row.Weight).ShouldBe(("quiz", a.Id, "Variations quiz", 50.0, 2.0)); // 1 of 2 marks

        (await other.Get<List<EvidenceRow>>($"/api/me/competencies/{comp}/evidence")).ShouldBe([]);
        (await Status(other, comp)).State.ShouldBe(CompetencyState.NotAssessed);
    }

    [Fact]
    public async Task Evidence_survives_deleting_the_questions_and_blocks_deleting_the_competency()
    {
        var (teacher, course, quiz, comp, _) = await Setup();
        var learner = await app.SignedIn();
        await learner.Post($"/api/courses/{course}/enrol");
        await Sit(learner, quiz, correct: true);

        foreach (var q in await teacher.Get<List<QuestionDto>>($"/api/builder/courses/{course}/questions"))
            await teacher.DeleteAsync($"/api/builder/questions/{q.Id}");
        (await teacher.DeleteAsync($"/api/builder/competencies/{comp}")).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await learner.Get<List<EvidenceRow>>($"/api/me/competencies/{comp}/evidence")).Count.ShouldBe(1);

        var unused = await teacher.Post($"/api/builder/courses/{course}/competencies", new CompetencyInput("Unused", null)).Json<CompetencyDto>();
        (await teacher.DeleteAsync($"/api/builder/competencies/{unused.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Lesson_competencies_must_belong_to_the_course_and_to_a_course_the_teacher_manages()
    {
        var (teacher, _, _, _, lesson) = await Setup();
        var (_, _, _, foreignComp, _) = await Setup();
        (await teacher.Put($"/api/builder/lessons/{lesson}/competencies", new CompetencyIdsRequest([foreignComp]))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var outsider = await app.SignedIn(Roles.Teacher);
        (await outsider.Put($"/api/builder/lessons/{lesson}/competencies", new CompetencyIdsRequest([]))).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var edit = await teacher.Get<LessonEdit>($"/api/builder/lessons/{lesson}");
        edit.CompetencyIds.Count.ShouldBe(1);

        // Regression: one competency tags many lessons (EF once modelled this as one-to-many, so a second lesson stole the tag).
        var course = (await teacher.Get<CourseDetail>($"/api/builder/courses/{edit.CourseId}"));
        var second = course.Modules[0].Lessons.First(l => l.Id != lesson).Id;
        await teacher.Put($"/api/builder/lessons/{second}/competencies", new CompetencyIdsRequest(edit.CompetencyIds.ToArray()));
        (await teacher.Get<LessonEdit>($"/api/builder/lessons/{lesson}")).CompetencyIds.ToArray().ShouldBe(edit.CompetencyIds.ToArray());
        (await teacher.Get<LessonEdit>($"/api/builder/lessons/{second}")).CompetencyIds.ToArray().ShouldBe(edit.CompetencyIds.ToArray());
        var cleared = await teacher.Put($"/api/builder/lessons/{lesson}/competencies", new CompetencyIdsRequest([])).Json<LessonEdit>();
        cleared.CompetencyIds.ShouldBe([]);
    }
}
