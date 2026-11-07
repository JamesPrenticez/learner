using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Learner.Api.Data;
using Learner.Api.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Learner.Api.Tests;

/// <summary>Fresh SQLite file per test class, no demo seed data. Each client is its own cookie session.</summary>
public class TestApp : WebApplicationFactory<Program>, IDisposable
{
    /// <summary>
    /// Registers a throwaway exercise component once, before any API-backed test can run (they all need a TestApp), so tests can show
    /// the framework works with components it knows nothing about, without mutating the registry while other tests read it.
    /// The learner must answer config.answer; the prompt is public, the answer is not.
    /// </summary>
    static TestApp() =>
        ExerciseTypes.All["test-echo"] = new ExerciseType("test-echo", "Echo",
            c => c.TryGetProperty("answer", out _) ? null : "Set an answer.",
            c => new System.Text.Json.Nodes.JsonObject { ["prompt"] = c.GetProperty("prompt").GetString() },
            (c, s) => new ExerciseResult(s.GetProperty("said").GetString() == c.GetProperty("answer").GetString() ? 1 : 0, 1, "Echo marked.", new System.Text.Json.Nodes.JsonObject()));

    readonly string dbPath = Path.Combine(Path.GetTempPath(), $"learner-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={dbPath};Pooling=False");
        builder.UseSetting("Seed:Demo", "false");
        builder.UseSetting("Media:Path", Path.Combine(Path.GetTempPath(), $"learner-media-{Guid.NewGuid():N}"));
    }

    public HttpClient NewClient() => CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    /// <summary>Creates a user with the given role directly in the DB and returns a client signed in as them.</summary>
    public async Task<HttpClient> SignedIn(string role = Roles.Learner, string? email = null)
    {
        email ??= $"{role.ToLower()}-{Guid.NewGuid():N}@test.com";
        using (var scope = Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = new AppUser { UserName = email, Email = email, DisplayName = email };
            (await users.CreateAsync(user, "Passw0rd!")).Succeeded.ShouldBeTrue();
            await users.AddToRoleAsync(user, role);
        }
        var client = NewClient();
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Passw0rd!"))).EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>Runs <paramref name="work"/> with a scoped DbContext (for arranging data the API cannot create yet).</summary>
    public async Task WithDb(Func<AppDb, Task> work)
    {
        using var scope = Services.CreateScope();
        await work(scope.ServiceProvider.GetRequiredService<AppDb>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var f in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(dbPath) + "*"))
            try { File.Delete(f); } catch (IOException) { }
    }
}

static class Assertions
{
    /// <summary>Same JSON shape as the API: web defaults, enums as strings.</summary>
    public static readonly JsonSerializerOptions Opts = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static void ShouldBeTrue(this bool v) => Assert.True(v);
    public static void ShouldBe<T>(this T actual, T expected) => Assert.Equal(expected, actual);

    public static async Task<T> Json<T>(this HttpResponseMessage res)
    {
        if (!res.IsSuccessStatusCode) throw new HttpRequestException($"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return (await res.Content.ReadFromJsonAsync<T>(Opts))!;
    }

    public static async Task<T> Json<T>(this Task<HttpResponseMessage> res) => await (await res).Json<T>();

    public static Task<HttpResponseMessage> Post(this HttpClient c, string url, object? body = null) => c.PostAsJsonAsync(url, body ?? new { }, Opts);
    public static Task<HttpResponseMessage> Put(this HttpClient c, string url, object body) => c.PutAsJsonAsync(url, body, Opts);
    public static Task<T> Get<T>(this HttpClient c, string url) => c.GetAsync(url).Json<T>();
}
