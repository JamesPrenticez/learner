using System.Security.Claims;
using Learner.Api.Data;

namespace Learner.Api.Features;

public static class Media
{
    const long MaxBytes = 20 * 1024 * 1024;

    // Allow-list only. SVG is excluded on purpose (scriptable); served with nosniff.
    static readonly HashSet<string> Allowed = ["image/png", "image/jpeg", "image/gif", "image/webp", "application/pdf"];

    static string PathFor(IConfiguration cfg, IWebHostEnvironment env, string id) =>
        Path.Combine(env.ContentRootPath, cfg["Media:Path"] ?? "uploads", id);

    public static void MapMedia(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/media", async (IFormFile file, ClaimsPrincipal user, AppDb db, IConfiguration cfg, IWebHostEnvironment env) =>
        {
            if (file.Length == 0 || file.Length > MaxBytes) return Http.Invalid("file", "File must be between 1 byte and 20 MB.");
            if (!Allowed.Contains(file.ContentType)) return Http.Invalid("file", "Only PNG, JPEG, GIF, WebP images and PDF documents are allowed.");

            var asset = new MediaAsset { OwnerId = user.UserId(), FileName = Path.GetFileName(file.FileName), ContentType = file.ContentType, Size = file.Length };
            var path = PathFor(cfg, env, asset.Id);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using (var fs = File.Create(path)) await file.CopyToAsync(fs);

            db.Media.Add(asset);
            await db.SaveChangesAsync();
            return Results.Ok(new { asset.Id, asset.FileName, asset.ContentType });
        }).RequireAuthorization(p => p.RequireRole(Roles.Teacher, Roles.Admin)).DisableAntiforgery();

        // ponytail: any signed-in user with the (unguessable) id can fetch. Per-course checks if private media needs stronger isolation.
        app.MapGet("/api/media/{id}", async (string id, HttpContext http, AppDb db, IConfiguration cfg, IWebHostEnvironment env) =>
        {
            var asset = await db.Media.FindAsync(id);
            var path = PathFor(cfg, env, id);
            if (asset is null || !File.Exists(path)) return Results.NotFound();
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(path, asset.ContentType, asset.FileName);
        }).RequireAuthorization();
    }
}
