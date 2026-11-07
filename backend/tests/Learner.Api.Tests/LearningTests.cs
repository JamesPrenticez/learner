using System.Net;
using Learner.Api.Data;
using Learner.Api.Features;
using Microsoft.EntityFrameworkCore;
using static Learner.Api.Tests.TestData;

namespace Learner.Api.Tests;

public class LearningTests : IClassFixture<TestApp>
{
    readonly TestApp app;
    public LearningTests(TestApp app) => this.app = app;

    [Fact]
    public async Task Catalogue_lists_only_published_public_courses_and_filters()
    {
        var teacher = await app.SignedIn(Roles.Teacher);
        var learner = await app.SignedIn();
        await PublishedCourse(teacher, "Alpha Contracts", difficulty: Difficulty.Beginner);
        await PublishedCourse(teacher, "Beta Variations", difficulty: Difficulty.Advanced);
        await PublishedCourse(teacher, "Hidden Private", Visibility.Private);
        await teacher.Post("/api/builder/courses", new TitleRequest("Unpublished draft"));

        // The fixture's database is shared by the whole class, so assert on these titles rather than on the full list.
        var titles = (await learner.Get<List<CatalogueItem>>("/api/catalogue")).Select(c => c.Title).ToList();
        Assert.Contains("Alpha Contracts", titles);
        Assert.Contains("Beta Variations", titles);
        Assert.DoesNotContain("Hidden Private", titles);
        Assert.DoesNotContain("Unpublished draft", titles);
        (await learner.Get<List<CatalogueItem>>("/api/catalogue?search=variation")).Single().Title.ShouldBe("Beta Variations");
        (await learner.Get<List<CatalogueItem>>("/api/catalogue?difficulty=Advanced")).Single().Title.ShouldBe("Beta Variations");
        (await learner.Get<List<CatalogueItem>>("/api/catalogue?search=zzz")).ShouldBe([]);
        (await app.NewClient().GetAsync("/api/catalogue")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Private_courses_are_only_for_invited_learners()
    {
        var teacher = await app.SignedIn(Roles.Teacher);
        var invited = await app.SignedIn(Roles.Learner, "invited@test.com");
        var outsider = await app.SignedIn();
        var (course, lessons) = await PublishedCourse(teacher, "Secret", Visibility.Private);
        await teacher.Post($"/api/builder/courses/{course}/access", new AccessRequest("invited@test.com", AccessKind.Invited));

        (await invited.Get<List<CatalogueItem>>("/api/catalogue?scope=private")).Single().Title.ShouldBe("Secret");
        (await outsider.Get<List<CatalogueItem>>("/api/catalogue?scope=private")).ShouldBe([]);
        Assert.DoesNotContain("Secret", (await invited.Get<List<CatalogueItem>>("/api/catalogue")).Select(c => c.Title)); // never in the public catalogue

        (await outsider.GetAsync($"/api/courses/{course}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await outsider.GetAsync($"/api/lessons/{lessons[0]}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await outsider.Post($"/api/courses/{course}/enrol")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await invited.GetAsync($"/api/courses/{course}")).EnsureSuccessStatusCode();
        (await invited.Post($"/api/courses/{course}/enrol")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Drafts_are_hidden_from_learners_but_previewable_by_managers()
    {
        var teacher = await app.SignedIn(Roles.Teacher);
        var learner = await app.SignedIn();
        var id = (await teacher.Post("/api/builder/courses", new TitleRequest("Draft")).Json<Dictionary<string, int>>())["id"];

        (await learner.GetAsync($"/api/courses/{id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await learner.Post($"/api/courses/{id}/enrol")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var preview = await teacher.Get<CourseOverview>($"/api/courses/{id}");
        preview.CanManage.ShouldBeTrue();
        preview.CanEnrol.ShouldBe(false);
    }

    [Fact]
    public async Task Learner_enrols_navigates_freely_and_tracks_progress()
    {
        var teacher = await app.SignedIn(Roles.Teacher);
        var learner = await app.SignedIn();
        var (course, lessons) = await PublishedCourse(teacher, lessonTitles: ["One", "Two", "Three"]);

        // Anyone with access may read any lesson, in any order, without enrolling.
        var third = await learner.Get<LessonView>($"/api/lessons/{lessons[2]}");
        third.Enrolled.ShouldBe(false);
        third.PrevId.ShouldBe(lessons[1]);
        third.NextId.ShouldBe(null);
        third.Blocks.Single().Type.ShouldBe("richtext");
        // ...but progress needs enrolment.
        (await learner.Post($"/api/lessons/{lessons[2]}/complete", new CompleteRequest(true))).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await learner.Post($"/api/courses/{course}/enrol")).EnsureSuccessStatusCode();
        (await learner.Post($"/api/courses/{course}/enrol")).EnsureSuccessStatusCode(); // idempotent

        (await learner.Post($"/api/lessons/{lessons[2]}/complete", new CompleteRequest(true))).EnsureSuccessStatusCode(); // out of order is fine
        var overview = await learner.Get<CourseOverview>($"/api/courses/{course}");
        overview.Enrolled.ShouldBeTrue();
        (overview.Completed, overview.Total).ShouldBe((1, 3));
        overview.Modules[0].Lessons.Select(l => l.Completed).ToArray().ShouldBe([false, false, true]);

        var mine = (await learner.Get<List<MyCourse>>("/api/me/courses")).Single();
        (mine.Completed, mine.Total, mine.ContinueLessonId).ShouldBe((1, 3, lessons[2]));

        (await learner.Post($"/api/lessons/{lessons[2]}/complete", new CompleteRequest(false))).EnsureSuccessStatusCode(); // can un-complete / repeat
        (await learner.Get<CourseOverview>($"/api/courses/{course}")).Completed.ShouldBe(0);

        // Progress is per learner.
        var other = await app.SignedIn();
        (await other.Get<CourseOverview>($"/api/courses/{course}")).Completed.ShouldBe(0);
    }

    [Fact]
    public async Task Unenrol_keeps_history_and_archived_courses_close_to_new_learners()
    {
        var teacher = await app.SignedIn(Roles.Teacher);
        var learner = await app.SignedIn();
        var newcomer = await app.SignedIn();
        var (course, _) = await PublishedCourse(teacher);
        await learner.Post($"/api/courses/{course}/enrol");

        (await learner.Post($"/api/courses/{course}/unenrol")).EnsureSuccessStatusCode();
        (await learner.Get<List<MyCourse>>("/api/me/courses")).ShouldBe([]);
        (await learner.Post($"/api/courses/{course}/enrol")).EnsureSuccessStatusCode(); // re-enrol allowed
        await app.WithDb(async db => (await db.Enrolments.CountAsync(e => e.CourseId == course)).ShouldBe(2)); // both periods kept

        (await teacher.Post($"/api/builder/courses/{course}/archive")).EnsureSuccessStatusCode();
        Assert.DoesNotContain(course, (await newcomer.Get<List<CatalogueItem>>("/api/catalogue")).Select(c => c.Id));
        (await newcomer.GetAsync($"/api/courses/{course}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await learner.GetAsync($"/api/courses/{course}")).EnsureSuccessStatusCode(); // existing learners keep access to their history
    }
}
