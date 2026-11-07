using System.Text.Json;
using Learner.Api.Data;
using Learner.Api.Features;

namespace Learner.Api.Tests;

static class TestData
{
    public static JsonElement J(object o) => JsonSerializer.SerializeToElement(o);

    /// <summary>Creates and publishes a course (one module, one text block per lesson). Returns its id and lesson ids in order.</summary>
    public static async Task<(int course, int[] lessons)> PublishedCourse(
        HttpClient teacher, string title = "Course", Visibility visibility = Visibility.Public,
        Difficulty difficulty = Difficulty.Beginner, string[]? lessonTitles = null)
    {
        lessonTitles ??= ["L1", "L2"];
        var id = (await teacher.Post("/api/builder/courses", new TitleRequest(title)).Json<Dictionary<string, int>>())["id"];
        await teacher.Put($"/api/builder/courses/{id}", new CourseUpdate(title, $"About {title}", null, null, visibility, difficulty, 30, ["Objective"]));
        var c = await teacher.Post($"/api/builder/courses/{id}/modules", new TitleRequest("Module 1")).Json<CourseDetail>();
        foreach (var l in lessonTitles) c = await teacher.Post($"/api/builder/modules/{c.Modules[0].Id}/lessons", new TitleRequest(l)).Json<CourseDetail>();
        var lessons = c.Modules[0].Lessons.Select(l => l.Id).ToArray();
        foreach (var l in lessons)
            await teacher.Post($"/api/builder/lessons/{l}/blocks", new NewBlock("richtext", J(new { markdown = "Content" })));
        (await teacher.Post($"/api/builder/courses/{id}/publish")).EnsureSuccessStatusCode();
        return (id, lessons);
    }

    public static async Task<string> UserId(HttpClient client) => (await client.Get<Me>("/api/auth/me")).Id;
}
