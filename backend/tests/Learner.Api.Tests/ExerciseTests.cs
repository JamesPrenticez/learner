using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Learner.Api.Data;
using Learner.Api.Features;
using static Learner.Api.Tests.TestData;

namespace Learner.Api.Tests;

/// <summary>The contract-review component on its own: config in, marks out.</summary>
public class ContractReviewTests
{
    static JsonElement Config(object? clauses = null, object? issues = null) => J(new
    {
        scenario = "S",
        instructions = "I",
        clauses = clauses ?? new[] { new { id = "c1", title = "1", text = "Pay when paid" }, new { id = "c2", title = "2", text = "Fair" }, new { id = "c3", title = "3", text = "No EOT" } },
        issues = issues ?? new[] { new { clauseId = "c1", explanation = "Void under SOPA", points = 2 }, new { clauseId = "c3", explanation = "Time bar", points = 1 } },
    });

    static JsonElement Flags(params string[] ids) => J(new { flags = ids.Select(i => new { clauseId = i, note = "" }).ToArray() });
    static ExerciseResult Mark(JsonElement flags) => ContractReview.Type.Evaluate(Config(), flags);

    [Fact]
    public void Marks_issues_found_and_penalises_false_flags()
    {
        Mark(Flags("c1", "c3")).Score.ShouldBe(3);                 // everything found
        Mark(Flags("c1")).Score.ShouldBe(2);                       // points per issue
        Mark(Flags("c1", "c2")).Score.ShouldBe(1);                 // 2 found - 1 false flag
        Mark(Flags("c2")).Score.ShouldBe(0);                       // never below zero
        Mark(Flags()).Score.ShouldBe(0);
        Mark(Flags("c1", "c3", "nonsense")).Score.ShouldBe(3);     // unknown clause ids are ignored, not penalised
        Mark(Flags("c1", "c1", "c1")).Score.ShouldBe(2);           // repeats count once
        Mark(Flags()).MaxScore.ShouldBe(3);
    }

    [Fact]
    public void Result_details_tell_the_learner_what_they_missed()
    {
        var r = Mark(Flags("c1", "c2"));
        r.Summary.ShouldBe("Found 1 of 2 issues, with 1 incorrect flag.");
        var issues = r.Details["issues"]!.AsArray();
        issues.Single(i => i!["clauseId"]!.GetValue<string>() == "c1")!["found"]!.GetValue<bool>().ShouldBeTrue();
        issues.Single(i => i!["clauseId"]!.GetValue<string>() == "c3")!["found"]!.GetValue<bool>().ShouldBe(false);
        r.Details["falseFlags"]!.AsArray().Single()!.GetValue<string>().ShouldBe("c2");
    }

    [Fact]
    public void Malformed_submissions_do_not_crash_marking()
    {
        Mark(J(new { })).Score.ShouldBe(0);
        Mark(J(new { flags = "nope" })).Score.ShouldBe(0);
        Mark(J(new { flags = new object[] { 1, "x", new { clauseId = 5 } } })).Score.ShouldBe(0);
    }

    [Fact]
    public void The_marking_key_never_reaches_the_learner()
    {
        var node = ContractReview.Type.ForLearner(Config()).AsObject();
        node.ContainsKey("issues").ShouldBe(false);
        node["clauses"]!.AsArray().Count.ShouldBe(3);
        node["scenario"]!.GetValue<string>().ShouldBe("S");
    }

    [Fact]
    public void Incomplete_configurations_are_reported()
    {
        Assert.Null(ContractReview.Type.Validate(Config()));
        Assert.NotNull(ContractReview.Type.Validate(J(new { })));
        Assert.NotNull(ContractReview.Type.Validate(Config(clauses: Array.Empty<object>())));
        Assert.NotNull(ContractReview.Type.Validate(Config(clauses: new[] { new { id = "c1", title = "", text = " " } })));
        Assert.NotNull(ContractReview.Type.Validate(Config(clauses: new[] { new { id = "a", title = "", text = "x" }, new { id = "a", title = "", text = "y" } })));
        Assert.NotNull(ContractReview.Type.Validate(Config(issues: Array.Empty<object>())));
        Assert.NotNull(ContractReview.Type.Validate(Config(issues: new[] { new { clauseId = "zzz", explanation = "x", points = 1 } })));
        Assert.NotNull(ContractReview.Type.Validate(Config(issues: new[] { new { clauseId = "c1", explanation = " ", points = 1 } })));
    }
}

public class ExerciseApiTests : IClassFixture<TestApp>
{
    readonly TestApp app;
    public ExerciseApiTests(TestApp app) => this.app = app;

    static readonly object FullConfig = new
    {
        scenario = "Subcontract review",
        instructions = "Flag problem clauses",
        clauses = new[] { new { id = "c1", title = "1", text = "Pay when paid" }, new { id = "c2", title = "2", text = "Fair" } },
        issues = new[] { new { clauseId = "c1", explanation = "Void under SOPA", points = 2 } },
    };

    /// <summary>Published course with one exercise (complete config) tagged with a competency.</summary>
    async Task<(HttpClient teacher, int course, int exercise, int competency)> Setup()
    {
        var teacher = await app.SignedIn(Roles.Teacher);
        var (course, _) = await PublishedCourse(teacher);
        var comp = await teacher.Post($"/api/builder/courses/{course}/competencies", new CompetencyInput("Contract review", null)).Json<CompetencyDto>();
        var ex = await teacher.Post($"/api/builder/courses/{course}/exercises", new NewExercise("contract-review", "Review the subcontract")).Json<ExerciseDetail>();
        (await teacher.Put($"/api/builder/exercises/{ex.Id}", new ExerciseInput("Review the subcontract", J(FullConfig), [comp.Id]))).EnsureSuccessStatusCode();
        return (teacher, course, ex.Id, comp.Id);
    }

    async Task<HttpClient> Enrolled(int course)
    {
        var learner = await app.SignedIn();
        (await learner.Post($"/api/courses/{course}/enrol")).EnsureSuccessStatusCode();
        return learner;
    }

    static Task<HttpResponseMessage> Submit(HttpClient c, int exercise, params string[] flags) =>
        c.Post($"/api/exercises/{exercise}/attempts", new SubmitRequest(J(new { flags = flags.Select(f => new { clauseId = f, note = "" }).ToArray() })));

    [Fact]
    public async Task Authoring_lists_types_and_flags_incomplete_configurations()
    {
        var t = await app.SignedIn(Roles.Teacher);
        var (course, _) = await PublishedCourse(t);

        (await t.Get<List<ExerciseTypeDto>>("/api/builder/exercise-types")).Single(x => x.Key == "contract-review").Label.ShouldBe("Contract review");
        (await t.Post($"/api/builder/courses/{course}/exercises", new NewExercise("nope", "x"))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var ex = await t.Post($"/api/builder/courses/{course}/exercises", new NewExercise("contract-review", "Draft")).Json<ExerciseDetail>();
        Assert.NotNull(ex.Problem); // drafts may be incomplete...
        (await t.Put($"/api/builder/exercises/{ex.Id}", new ExerciseInput("Draft", J(new { scenario = "only this" }), []))).EnsureSuccessStatusCode();
        var saved = await t.Put($"/api/builder/exercises/{ex.Id}", new ExerciseInput("Draft", J(FullConfig), [])).Json<ExerciseDetail>();
        Assert.Null(saved.Problem); // ...until complete
        saved.Version.ShouldBe(3);   // every config change bumps the version
        (await t.Put($"/api/builder/exercises/{ex.Id}", new ExerciseInput("Renamed", J(FullConfig), [])).Json<ExerciseDetail>()).Version.ShouldBe(3); // title only: same version

        (await t.Put($"/api/builder/exercises/{ex.Id}", new ExerciseInput("x", J(FullConfig), [424242]))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await t.Get<List<ExerciseSummary>>($"/api/builder/courses/{course}/exercises")).Single().Title.ShouldBe("Renamed");
    }

    [Fact]
    public async Task Exercise_authoring_is_limited_to_the_courses_teachers()
    {
        var (_, course, exercise, _) = await Setup();
        var learner = await app.SignedIn();
        var other = await app.SignedIn(Roles.Teacher);

        (await learner.GetAsync($"/api/builder/exercises/{exercise}")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await other.GetAsync($"/api/builder/exercises/{exercise}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.Put($"/api/builder/exercises/{exercise}", new ExerciseInput("hack", J(FullConfig), []))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.DeleteAsync($"/api/builder/exercises/{exercise}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.Post($"/api/builder/courses/{course}/exercises", new NewExercise("contract-review", "x"))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Learner_sees_no_marking_key_submits_and_gets_structured_results()
    {
        var (_, course, exercise, _) = await Setup();
        var learner = await Enrolled(course);

        var view = await learner.Get<ExerciseView>($"/api/exercises/{exercise}");
        view.Config.AsObject().ContainsKey("issues").ShouldBe(false);
        (view.Attempts, view.BestPercent, view.Problem).ShouldBe((0, null, null));

        var attempt = await Submit(learner, exercise, "c1").Json<ExerciseAttemptView>();
        (attempt.Score, attempt.MaxScore, attempt.Percent).ShouldBe((2, 2, 100.0));
        attempt.Summary.ShouldBe("Found 1 of 1 issues, with 0 incorrect flags.");
        attempt.Details.GetProperty("issues")[0].GetProperty("explanation").GetString().ShouldBe("Void under SOPA"); // revealed after submitting

        var worse = await Submit(learner, exercise, "c2").Json<ExerciseAttemptView>();
        worse.Score.ShouldBe(0);
        var after = await learner.Get<ExerciseView>($"/api/exercises/{exercise}");
        (after.Attempts, after.BestPercent).ShouldBe((2, 100.0));

        (await learner.Get<List<ExerciseAttemptSummary>>("/api/me/exercise-attempts")).Count.ShouldBe(2);
        (await learner.Get<ExerciseAttemptView>($"/api/exercise-attempts/{attempt.Id}")).Score.ShouldBe(2);
        (await (await app.SignedIn()).GetAsync($"/api/exercise-attempts/{attempt.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound); // private to the learner
    }

    [Fact]
    public async Task Submitting_requires_enrolment_a_ready_exercise_and_a_valid_submission()
    {
        var (teacher, course, exercise, _) = await Setup();
        var outsider = await app.SignedIn();
        (await Submit(outsider, exercise, "c1")).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var learner = await Enrolled(course);
        (await learner.Post($"/api/exercises/{exercise}/attempts", new SubmitRequest(J(new[] { 1, 2 })))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var blank = await teacher.Post($"/api/builder/courses/{course}/exercises", new NewExercise("contract-review", "Blank")).Json<ExerciseDetail>();
        (await Submit(learner, blank.Id, "c1")).StatusCode.ShouldBe(HttpStatusCode.Conflict); // not ready
        (await learner.Get<ExerciseView>($"/api/exercises/{blank.Id}")).Problem.ShouldBe(blank.Problem);
        (await learner.GetAsync("/api/exercises/987654")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Editing_an_exercise_never_rewrites_past_attempts_and_exercises_with_attempts_are_kept()
    {
        var (teacher, course, exercise, comp) = await Setup();
        var learner = await Enrolled(course);
        var first = await Submit(learner, exercise, "c1").Json<ExerciseAttemptView>();
        first.MaxScore.ShouldBe(2);

        // The teacher changes the marking key: now both clauses are issues, worth 5 points.
        var changed = new
        {
            scenario = "New scenario",
            instructions = "I",
            clauses = new[] { new { id = "c1", title = "1", text = "Pay when paid" }, new { id = "c2", title = "2", text = "Fair" } },
            issues = new[] { new { clauseId = "c1", explanation = "Changed", points = 5 }, new { clauseId = "c2", explanation = "Also bad", points = 1 } },
        };
        var updated = await teacher.Put($"/api/builder/exercises/{exercise}", new ExerciseInput("Review", J(changed), [comp])).Json<ExerciseDetail>();
        Assert.True(updated.Version > first.ConfigVersion);

        var old = await learner.Get<ExerciseAttemptView>($"/api/exercise-attempts/{first.Id}");
        (old.Score, old.MaxScore, old.ConfigVersion).ShouldBe((2, 2, first.ConfigVersion)); // still marked against what they sat
        old.Config["scenario"]!.GetValue<string>().ShouldBe("Subcontract review");

        var next = await Submit(learner, exercise, "c1").Json<ExerciseAttemptView>();
        (next.MaxScore, next.ConfigVersion).ShouldBe((6, updated.Version));

        (await teacher.DeleteAsync($"/api/builder/exercises/{exercise}")).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var spare = await teacher.Post($"/api/builder/courses/{course}/exercises", new NewExercise("contract-review", "Spare")).Json<ExerciseDetail>();
        (await teacher.DeleteAsync($"/api/builder/exercises/{spare.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Exercise_results_feed_the_competency_profile_with_traceable_evidence()
    {
        var (_, course, exercise, comp) = await Setup();
        var learner = await Enrolled(course);
        (await learner.Get<List<CompetencyStatus>>("/api/me/competencies")).Single(c => c.Id == comp).State.ShouldBe(CompetencyState.NotAssessed);

        var a = await Submit(learner, exercise, "c1").Json<ExerciseAttemptView>();
        var status = (await learner.Get<List<CompetencyStatus>>("/api/me/competencies")).Single(c => c.Id == comp);
        (status.State, status.Evidence, status.Percent).ShouldBe((CompetencyState.Developing, 1, 100.0)); // one piece is not enough
        await Submit(learner, exercise, "c1");
        (await learner.Get<List<CompetencyStatus>>("/api/me/competencies")).Single(c => c.Id == comp).State.ShouldBe(CompetencyState.Demonstrated);

        var evidence = (await learner.Get<List<EvidenceRow>>($"/api/me/competencies/{comp}/evidence")).OrderBy(e => e.SourceId).First();
        (evidence.Source, evidence.SourceId, evidence.Title, evidence.Weight).ShouldBe(("exercise", a.Id, "Review the subcontract", 2.0));
    }

    [Fact]
    public async Task Publishing_requires_exercise_blocks_to_point_at_a_complete_exercise()
    {
        var (t, course, exercise, _) = await Setup();
        await t.Post($"/api/builder/courses/{course}/unpublish");
        var lesson = (await t.Get<CourseDetail>($"/api/builder/courses/{course}")).Modules[0].Lessons[0].Id;
        var blank = await t.Post($"/api/builder/courses/{course}/exercises", new NewExercise("contract-review", "Blank")).Json<ExerciseDetail>();

        var block = (await t.Post($"/api/builder/lessons/{lesson}/blocks", new NewBlock("exercise", J(new { exerciseId = blank.Id }))).Json<LessonEdit>()).Blocks.Last();
        (await t.Post($"/api/builder/courses/{course}/publish")).StatusCode.ShouldBe(HttpStatusCode.BadRequest); // incomplete
        await t.Put($"/api/builder/blocks/{block.Id}", new BlockData(J(new { exerciseId = 987654 })));
        (await t.Post($"/api/builder/courses/{course}/publish")).StatusCode.ShouldBe(HttpStatusCode.BadRequest); // missing
        await t.Put($"/api/builder/blocks/{block.Id}", new BlockData(J(new { exerciseId = exercise })));
        (await t.Post($"/api/builder/courses/{course}/publish")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_new_exercise_component_plugs_in_without_any_framework_change()
    {
        // "test-echo" is a throwaway component the framework has never heard of (registered once in TestApp).
        var t = await app.SignedIn(Roles.Teacher);
        var (course, _) = await PublishedCourse(t);
        var comp = await t.Post($"/api/builder/courses/{course}/competencies", new CompetencyInput("Echoing", null)).Json<CompetencyDto>();
        var ex = await t.Post($"/api/builder/courses/{course}/exercises", new NewExercise("test-echo", "Say it")).Json<ExerciseDetail>();
        await t.Put($"/api/builder/exercises/{ex.Id}", new ExerciseInput("Say it", J(new { prompt = "Say 42", answer = "42" }), [comp.Id]));

        var learner = await Enrolled(course);
        var view = await learner.Get<ExerciseView>($"/api/exercises/{ex.Id}");
        view.Config.AsObject().ContainsKey("answer").ShouldBe(false);
        (await learner.Post($"/api/exercises/{ex.Id}/attempts", new SubmitRequest(J(new { said = "42" }))).Json<ExerciseAttemptView>()).Score.ShouldBe(1);
        (await learner.Get<List<CompetencyStatus>>("/api/me/competencies")).Single(c => c.Id == comp.Id).Evidence.ShouldBe(1);
    }
}
