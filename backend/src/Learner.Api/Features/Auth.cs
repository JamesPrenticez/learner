using System.Security.Claims;
using Learner.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Learner.Api.Features;

public record RegisterRequest(string Email, string Password, string DisplayName);
public record LoginRequest(string Email, string Password);
public record SetRolesRequest(string[] Roles);
public record Me(string Id, string Email, string DisplayName, IList<string> Roles);

public static class Auth
{
    public static async Task<Me> ToMe(this UserManager<AppUser> users, AppUser u) =>
        new(u.Id, u.Email!, u.DisplayName, await users.GetRolesAsync(u));

    public static string UserId(this ClaimsPrincipal p) => p.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth");

        auth.MapPost("/register", async (RegisterRequest req, UserManager<AppUser> users, SignInManager<AppUser> signIn) =>
        {
            if (string.IsNullOrWhiteSpace(req.DisplayName))
                return Http.Invalid("displayName", "Name is required.");

            var user = new AppUser { UserName = req.Email, Email = req.Email, DisplayName = req.DisplayName.Trim() };
            var created = await users.CreateAsync(user, req.Password);
            if (!created.Succeeded)
                return Results.ValidationProblem(created.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

            // Self-registration is always a Learner; elevated roles are granted by an Admin.
            await users.AddToRoleAsync(user, Roles.Learner);
            await signIn.SignInAsync(user, isPersistent: true);
            return Results.Ok(await users.ToMe(user));
        });

        auth.MapPost("/login", async (LoginRequest req, UserManager<AppUser> users, SignInManager<AppUser> signIn) =>
        {
            var user = await users.FindByEmailAsync(req.Email);
            if (user is null) return Results.Unauthorized();
            var result = await signIn.PasswordSignInAsync(user, req.Password, isPersistent: true, lockoutOnFailure: true);
            return result.Succeeded ? Results.Ok(await users.ToMe(user)) : Results.Unauthorized();
        });

        auth.MapPost("/logout", async (SignInManager<AppUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.NoContent();
        });

        auth.MapGet("/me", async (ClaimsPrincipal principal, UserManager<AppUser> users) =>
        {
            var user = await users.GetUserAsync(principal);
            return user is null ? Results.Unauthorized() : Results.Ok(await users.ToMe(user));
        }).RequireAuthorization();

        var admin = app.MapGroup("/api/admin/users").RequireAuthorization(p => p.RequireRole(Roles.Admin));

        admin.MapGet("/", async (UserManager<AppUser> users) =>
        {
            var list = await users.Users.OrderBy(u => u.Email).ToListAsync();
            var result = new List<Me>();
            foreach (var u in list) result.Add(await users.ToMe(u));
            return result;
        });

        admin.MapPut("/{id}/roles", async (string id, SetRolesRequest req, ClaimsPrincipal principal, UserManager<AppUser> users) =>
        {
            var user = await users.FindByIdAsync(id);
            if (user is null) return Results.NotFound();
            if (req.Roles.Any(r => !Roles.All.Contains(r)))
                return Http.Invalid("roles", "Unknown role.");
            if (id == principal.UserId() && !req.Roles.Contains(Roles.Admin))
                return Http.Invalid("roles", "You cannot remove your own Admin role.");

            var current = await users.GetRolesAsync(user);
            await users.RemoveFromRolesAsync(user, current.Except(req.Roles));
            await users.AddToRolesAsync(user, req.Roles.Except(current));
            return Results.Ok(await users.ToMe(user));
        });
    }
}
