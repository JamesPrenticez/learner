using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Learner.Api.Data;
using Learner.Api.Features;

namespace Learner.Api.Tests;

public class BlockTests : IClassFixture<TestApp>
{
    readonly TestApp app;
    public BlockTests(TestApp app) => this.app = app;

    static JsonElement J(object o) => JsonSerializer.SerializeToElement(o);

    /// <summary>Course with one module and two lessons; returns (courseId, lessonA, lessonB).</summary>
    static async Task<(int course, int a, int b)> Course(HttpClient t)
    {
        var id = (await t.Post("/api/builder/courses", new TitleRequest("C")).Json<Dictionary<string, int>>())["id"];
        await t.Put($"/api/builder/courses/{id}", new CourseUpdate("C", "Desc", null, null, Visibility.Public, Difficulty.Beginner, 5, []));
        var c = await t.Post($"/api/builder/courses/{id}/modules", new TitleRequest("M")).Json<CourseDetail>();
        await t.Post($"/api/builder/modules/{c.Modules[0].Id}/lessons", new TitleRequest("A"));
        c = await t.Post($"/api/builder/modules/{c.Modules[0].Id}/lessons", new TitleRequest("B")).Json<CourseDetail>();
        return (id, c.Modules[0].Lessons[0].Id, c.Modules[0].Lessons[1].Id);
    }

    [Fact]
    public async Task Blocks_can_be_added_edited_reordered_copied_and_deleted()
    {
        var t = await app.SignedIn(Roles.Teacher);
        var (_, a, b) = await Course(t);

        await t.Post($"/api/builder/lessons/{a}/blocks", new NewBlock("richtext", J(new { markdown = "One" })));
        var lesson = await t.Post($"/api/builder/lessons/{a}/blocks", new NewBlock("callout", J(new { variant = "tip", text = "Two" }))).Json<LessonEdit>();
        lesson.Blocks.Select(x => x.Type).ToArray().ShouldBe(["richtext", "callout"]);

        var edited = await t.Put($"/api/builder/blocks/{lesson.Blocks[0].Id}", new BlockData(J(new { markdown = "Changed" }))).Json<BlockDto>();
        edited.Data.GetProperty("markdown").GetString().ShouldBe("Changed");

        lesson = await t.Put($"/api/builder/lessons/{a}/blocks/order", new OrderRequest([lesson.Blocks[1].Id, lesson.Blocks[0].Id])).Json<LessonEdit>();
        lesson.Blocks.Select(x => x.Type).ToArray().ShouldBe(["callout", "richtext"]);

        // duplicate lands right after the original
        lesson = await t.Post($"/api/builder/blocks/{lesson.Blocks[0].Id}/copy", new CopyBlock(null)).Json<LessonEdit>();
        lesson.Blocks.Select(x => x.Type).ToArray().ShouldBe(["callout", "callout", "richtext"]);

        // reuse in another lesson appends
        var other = await t.Post($"/api/builder/blocks/{lesson.Blocks[2].Id}/copy", new CopyBlock(b)).Json<LessonEdit>();
        other.Id.ShouldBe(b);
        other.Blocks.Select(x => x.Type).ToArray().ShouldBe(["richtext"]);

        lesson = await t.DeleteAsync($"/api/builder/blocks/{lesson.Blocks[0].Id}").Json<LessonEdit>();
        lesson.Blocks.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Drafts_may_be_incomplete_but_wrong_shapes_are_rejected_and_publish_is_strict()
    {
        var t = await app.SignedIn(Roles.Teacher);
        var (course, a, b) = await Course(t);

        (await t.Post($"/api/builder/lessons/{a}/blocks", new NewBlock("nope", J(new { })))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await t.Post($"/api/builder/lessons/{a}/blocks", new NewBlock("richtext", J(new { markdown = 5 })))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await t.Post($"/api/builder/lessons/{a}/blocks", new NewBlock("video", J(new { url = "javascript:alert(1)" })))).StatusCode.ShouldBe(HttpStatusCode.BadRequest); // bad values never saved

        // empty required fields are fine in a draft...
        await t.Post($"/api/builder/lessons/{a}/blocks", new NewBlock("video", null));
        var empty = await t.Post($"/api/builder/lessons/{b}/blocks", new NewBlock("richtext", null)).Json<LessonEdit>();
        var res = await t.Post($"/api/builder/courses/{course}/publish");
        res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await res.Content.ReadAsStringAsync();
        body.Contains("'url' is required").ShouldBeTrue(); // ...but publish is strict
        body.Contains("'markdown' is required").ShouldBeTrue();

        await t.Put($"/api/builder/blocks/{empty.Blocks[0].Id}", new BlockData(J(new { markdown = "Now filled" })));
        var vid = (await t.Get<LessonEdit>($"/api/builder/lessons/{a}")).Blocks[0].Id;
        await t.Put($"/api/builder/blocks/{vid}", new BlockData(J(new { url = "https://example.com/v.mp4" })));
        (await t.Post($"/api/builder/courses/{course}/publish")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Unrelated_teachers_cannot_touch_blocks()
    {
        var owner = await app.SignedIn(Roles.Teacher);
        var other = await app.SignedIn(Roles.Teacher);
        var (_, a, _) = await Course(owner);
        var lesson = await owner.Post($"/api/builder/lessons/{a}/blocks", new NewBlock("richtext", J(new { markdown = "x" }))).Json<LessonEdit>();
        var blockId = lesson.Blocks[0].Id;

        (await other.GetAsync($"/api/builder/lessons/{a}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.Post($"/api/builder/lessons/{a}/blocks", new NewBlock("richtext", null))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.Put($"/api/builder/blocks/{blockId}", new BlockData(J(new { markdown = "hack" })))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.DeleteAsync($"/api/builder/blocks/{blockId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.Post($"/api/builder/blocks/{blockId}/copy", new CopyBlock(null))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    static MultipartFormDataContent File(string name, string contentType, byte[] bytes)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        return new MultipartFormDataContent { { file, "file", name } };
    }

    [Fact]
    public async Task Media_upload_is_teacher_only_allow_listed_and_readable_when_signed_in()
    {
        var teacher = await app.SignedIn(Roles.Teacher);
        var learner = await app.SignedIn();
        byte[] png = [137, 80, 78, 71, 1, 2, 3];

        (await learner.PostAsync("/api/media", File("a.png", "image/png", png))).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await teacher.PostAsync("/api/media", File("a.svg", "image/svg+xml", png))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await teacher.PostAsync("/api/media", File("a.png", "image/png", []))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var asset = await teacher.PostAsync("/api/media", File("a.png", "image/png", png)).Json<JsonElement>();
        var id = asset.GetProperty("id").GetString();
        var res = await learner.GetAsync($"/api/media/{id}");
        res.EnsureSuccessStatusCode();
        res.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
        (await res.Content.ReadAsByteArrayAsync()).ShouldBe(png);
        (await app.NewClient().GetAsync($"/api/media/{id}")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await learner.GetAsync("/api/media/doesnotexist")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
