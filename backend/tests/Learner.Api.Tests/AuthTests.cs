using System.Net;
using System.Net.Http.Json;
using Learner.Api.Data;
using Learner.Api.Features;

namespace Learner.Api.Tests;

public class AuthTests : IClassFixture<TestApp>
{
    readonly TestApp app;
    public AuthTests(TestApp app) => this.app = app;

    [Fact]
    public async Task Register_signs_in_as_learner_and_me_works()
    {
        var c = app.NewClient();
        var me = await (await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest("a@test.com", "Passw0rd!", "Ann"))).Json<Me>();
        me.Roles.ShouldBe([Roles.Learner]);
        (await c.GetFromJsonAsync<Me>("/api/auth/me"))!.Email.ShouldBe("a@test.com");
    }

    [Fact]
    public async Task Me_requires_login() =>
        (await app.NewClient().GetAsync("/api/auth/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Short_password_and_duplicate_email_are_rejected()
    {
        var c = app.NewClient();
        (await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest("w@test.com", "short", "W"))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest("s@test.com", "simple", "S"))).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest("d@test.com", "Passw0rd!", "D"))).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest("d@test.com", "Passw0rd!", "D"))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_rejects_bad_password_and_logout_ends_session()
    {
        var c = await app.SignedIn(Roles.Learner, "l@test.com");
        (await app.NewClient().PostAsJsonAsync("/api/auth/login", new LoginRequest("l@test.com", "wrong"))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await c.PostAsync("/api/auth/logout", null)).EnsureSuccessStatusCode();
        (await c.GetAsync("/api/auth/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_endpoints_are_admin_only_and_roles_can_be_changed()
    {
        var learner = await app.SignedIn();
        (await learner.GetAsync("/api/admin/users")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await app.NewClient().GetAsync("/api/admin/users")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var admin = await app.SignedIn(Roles.Admin);
        var target = (await admin.GetFromJsonAsync<List<Me>>("/api/admin/users"))!.First(u => u.Roles.Contains(Roles.Learner));
        var updated = await (await admin.PutAsJsonAsync($"/api/admin/users/{target.Id}/roles", new SetRolesRequest([Roles.Learner, Roles.Teacher]))).Json<Me>();
        updated.Roles.OrderBy(r => r).ToArray().ShouldBe([Roles.Learner, Roles.Teacher]);
    }

    [Fact]
    public async Task Admin_cannot_remove_own_admin_role()
    {
        var admin = await app.SignedIn(Roles.Admin);
        var me = (await admin.GetFromJsonAsync<Me>("/api/auth/me"))!;
        (await admin.PutAsJsonAsync($"/api/admin/users/{me.Id}/roles", new SetRolesRequest([Roles.Learner]))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
