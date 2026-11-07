using System.Net;
using Learner.Api.Data;
using Learner.Api.Features;

namespace Learner.Api.Tests;

public class BuilderCourseTests : IClassFixture<TestApp>
{
    readonly TestApp app;
    public BuilderCourseTests(TestApp app) => this.app = app;

    static async Task<int> NewCourse(HttpClient teacher, string title = "Course")
    {
        var created = await teacher.Post("/api/builder/courses", new TitleRequest(title)).Json<Dictionary<string, int>>();
        return created["id"];
    }

    [Fact]
    public async Task Learners_and_anonymous_cannot_use_the_builder()
    {
        (await (await app.SignedIn()).GetAsync("/api/builder/courses")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await app.NewClient().GetAsync("/api/builder/courses")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Teacher_builds_a_course_with_modules_and_lessons_and_reorders_them()
    {
        var t = await app.SignedIn(Roles.Teacher);
        var id = await NewCourse(t);

        await t.Post($"/api/builder/courses/{id}/modules", new TitleRequest("M1"));
        var course = await t.Post($"/api/builder/courses/{id}/modules", new TitleRequest("M2")).Json<CourseDetail>();
        course.Modules.Select(m => m.Title).ToArray().ShouldBe(["M1", "M2"]);

        var m1 = course.Modules[0].Id;
        await t.Post($"/api/builder/modules/{m1}/lessons", new TitleRequest("L1"));
        course = await t.Post($"/api/builder/modules/{m1}/lessons", new TitleRequest("L2")).Json<CourseDetail>();
        var lessons = course.Modules[0].Lessons;
        course = await t.Put($"/api/builder/modules/{m1}/lessons/order", new OrderRequest([lessons[1].Id, lessons[0].Id])).Json<CourseDetail>();
        course.Modules[0].Lessons.Select(l => l.Title).ToArray().ShouldBe(["L2", "L1"]);

        course = await t.Put($"/api/builder/courses/{id}/modules/order", new OrderRequest([course.Modules[1].Id, course.Modules[0].Id])).Json<CourseDetail>();
        course.Modules.Select(m => m.Title).ToArray().ShouldBe(["M2", "M1"]);

        (await t.Put($"/api/builder/courses/{id}/modules/order", new OrderRequest([1]))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Metadata_is_saved_and_title_required()
    {
        var t = await app.SignedIn(Roles.Teacher);
        var id = await NewCourse(t);
        var update = new CourseUpdate("New title", "Desc", null, null, Visibility.Private, Difficulty.Advanced, 90, ["Know A", " ", "Know B"]);
        var c = await t.Put($"/api/builder/courses/{id}", update).Json<CourseDetail>();
        c.Visibility.ShouldBe(Visibility.Private);
        c.Difficulty.ShouldBe(Difficulty.Advanced);
        c.LearningObjectives.ToArray().ShouldBe(["Know A", "Know B"]);
        (await t.Put($"/api/builder/courses/{id}", update with { Title = " " })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Other_teachers_cannot_see_or_edit_until_made_collaborator()
    {
        var owner = await app.SignedIn(Roles.Teacher);
        var other = await app.SignedIn(Roles.Teacher, "collab@test.com");
        var id = await NewCourse(owner);

        (await other.GetAsync($"/api/builder/courses/{id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.Post($"/api/builder/courses/{id}/modules", new TitleRequest("x"))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.Get<List<CourseSummary>>("/api/builder/courses")).ShouldBe([]);

        (await owner.Post($"/api/builder/courses/{id}/access", new AccessRequest("collab@test.com", AccessKind.Collaborator))).EnsureSuccessStatusCode();
        (await other.GetAsync($"/api/builder/courses/{id}")).EnsureSuccessStatusCode();
        // collaborators edit but cannot hand out access
        (await other.Post($"/api/builder/courses/{id}/access", new AccessRequest("collab@test.com", AccessKind.Invited))).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // admins can manage everything
        var admin = await app.SignedIn(Roles.Admin);
        (await admin.GetAsync($"/api/builder/courses/{id}")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Publish_is_validated_and_published_courses_protect_their_structure()
    {
        var t = await app.SignedIn(Roles.Teacher);
        var id = await NewCourse(t);
        (await t.Post($"/api/builder/courses/{id}/publish")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        await t.Put($"/api/builder/courses/{id}", new CourseUpdate("T", "Has a description", null, null, Visibility.Public, Difficulty.Beginner, 10, []));
        var c = await t.Post($"/api/builder/courses/{id}/modules", new TitleRequest("M")).Json<CourseDetail>();
        (await t.Post($"/api/builder/courses/{id}/publish")).StatusCode.ShouldBe(HttpStatusCode.BadRequest); // empty module
        c = await t.Post($"/api/builder/modules/{c.Modules[0].Id}/lessons", new TitleRequest("L")).Json<CourseDetail>();
        (await t.Post($"/api/builder/courses/{id}/publish")).StatusCode.ShouldBe(HttpStatusCode.BadRequest); // lesson has no content
        await t.Post($"/api/builder/lessons/{c.Modules[0].Lessons[0].Id}/blocks", new NewBlock("richtext", System.Text.Json.JsonSerializer.SerializeToElement(new { markdown = "Hello" })));

        var published = await t.Post($"/api/builder/courses/{id}/publish").Json<CourseDetail>();
        published.Status.ShouldBe(CourseStatus.Published);
        published.Version.ShouldBe(2);

        (await t.DeleteAsync($"/api/builder/lessons/{c.Modules[0].Lessons[0].Id}")).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await t.Post($"/api/builder/courses/{id}/unpublish")).EnsureSuccessStatusCode();
        (await t.DeleteAsync($"/api/builder/lessons/{c.Modules[0].Lessons[0].Id}")).EnsureSuccessStatusCode();
    }
}
