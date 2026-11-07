using System.Security.Claims;
using System.Text.Json;
using Learner.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Learner.Api.Features;

public record BlockDto(int Id, string Type, int Order, JsonElement Data);
public record LessonEdit(int Id, string Title, int CourseId, int ModuleId, List<BlockDto> Blocks, List<int> CompetencyIds);
public record CompetencyIdsRequest(int[] Ids);
public record NewBlock(string Type, JsonElement? Data);
public record BlockData(JsonElement Data);
public record CopyBlock(int? LessonId);

public static class BuilderBlocks
{
    const int MaxPayload = 200_000;

    public static BlockDto ToDto(ContentBlock b) => new(b.Id, b.Type, b.Order, JsonDocument.Parse(b.DataJson).RootElement.Clone());

    static async Task<LessonEdit> Load(AppDb db, int lessonId)
    {
        var l = await db.Lessons.Include(l => l.Module).Include(l => l.Blocks).Include(l => l.Competencies).AsSplitQuery().FirstAsync(l => l.Id == lessonId);
        return new LessonEdit(l.Id, l.Title, l.Module.CourseId, l.ModuleId, l.Blocks.OrderBy(b => b.Order).Select(ToDto).ToList(), l.Competencies.Select(c => c.Id).Order().ToList());
    }

    static async Task<ContentBlock?> ManageableBlock(AppDb db, Access access, int id, ClaimsPrincipal user)
    {
        var b = await db.Blocks.FindAsync(id);
        return b is not null && await access.ManageableLesson(b.LessonId, user) is not null ? b : null;
    }

    public static void MapBuilderBlocks(this RouteGroupBuilder b)
    {
        b.MapGet("/lessons/{id:int}", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
            await access.ManageableLesson(id, user) is null ? Results.NotFound() : Results.Ok(await Load(db, id)));

        b.MapPut("/lessons/{id:int}/competencies", async (int id, CompetencyIdsRequest req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableLesson(id, user) is null) return Results.NotFound();
            var ids = req.Ids.Distinct().ToList();
            var courseId = (await db.Lessons.Where(l => l.Id == id).Select(l => l.Module.CourseId).FirstAsync());
            var comps = await db.Competencies.Where(c => c.CourseId == courseId && ids.Contains(c.Id)).ToListAsync();
            if (comps.Count != ids.Count) return Http.Invalid("ids", "Competencies must belong to this course.");
            var lesson = await db.Lessons.Include(l => l.Competencies).FirstAsync(l => l.Id == id);
            lesson.Competencies = comps;
            await db.SaveChangesAsync();
            return Results.Ok(await Load(db, id));
        });

        b.MapPost("/lessons/{id:int}/blocks", async (int id, NewBlock req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableLesson(id, user) is not { } lesson) return Results.NotFound();
            var data = req.Data ?? JsonDocument.Parse("{}").RootElement;
            if (!BlockSchemas.Known(req.Type)) return Http.Invalid("type", $"Unknown block type '{req.Type}'.");
            if (BlockSchemas.Problems(req.Type, data, strict: false) is { Count: > 0 } p) return Http.Invalid("data", string.Join(" ", p));
            if (data.GetRawText().Length > MaxPayload) return Http.Invalid("data", "Block is too large.");

            var order = (await db.Blocks.Where(x => x.LessonId == id).MaxAsync(x => (int?)x.Order) ?? -1) + 1;
            db.Blocks.Add(new ContentBlock { LessonId = id, Type = req.Type, Order = order, DataJson = data.GetRawText() });
            lesson.Module.Course.Touch();
            await db.SaveChangesAsync();
            return Results.Ok(await Load(db, id));
        });

        b.MapPut("/blocks/{id:int}", async (int id, BlockData req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await ManageableBlock(db, access, id, user) is not { } block) return Results.NotFound();
            if (BlockSchemas.Problems(block.Type, req.Data, strict: false) is { Count: > 0 } p) return Http.Invalid("data", string.Join(" ", p));
            if (req.Data.GetRawText().Length > MaxPayload) return Http.Invalid("data", "Block is too large.");
            block.DataJson = req.Data.GetRawText();
            (await db.Lessons.Include(l => l.Module).ThenInclude(m => m.Course).FirstAsync(l => l.Id == block.LessonId)).Module.Course.Touch();
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(block));
        });

        b.MapDelete("/blocks/{id:int}", async (int id, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await ManageableBlock(db, access, id, user) is not { } block) return Results.NotFound();
            db.Blocks.Remove(block);
            await db.SaveChangesAsync();
            return Results.Ok(await Load(db, block.LessonId));
        });

        b.MapPut("/lessons/{id:int}/blocks/order", async (int id, OrderRequest req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await access.ManageableLesson(id, user) is null) return Results.NotFound();
            if (!Http.ApplyOrder(await db.Blocks.Where(x => x.LessonId == id).ToListAsync(), req.Ids)) return Http.Invalid("ids", "Ids must list every block exactly once.");
            await db.SaveChangesAsync();
            return Results.Ok(await Load(db, id));
        });

        // Duplicate within the lesson (no target) or reuse in another lesson the user can edit.
        b.MapPost("/blocks/{id:int}/copy", async (int id, CopyBlock req, ClaimsPrincipal user, Access access, AppDb db) =>
        {
            if (await ManageableBlock(db, access, id, user) is not { } src) return Results.NotFound();
            var targetId = req.LessonId ?? src.LessonId;
            if (await access.ManageableLesson(targetId, user) is null) return Results.NotFound();

            var siblings = await db.Blocks.Where(x => x.LessonId == targetId).OrderBy(x => x.Order).ToListAsync();
            // Same lesson: slot right after the original. Elsewhere: append.
            var at = targetId == src.LessonId ? src.Order + 1 : siblings.Count;
            foreach (var s in siblings.Where(s => s.Order >= at)) s.Order++;
            db.Blocks.Add(new ContentBlock { LessonId = targetId, Type = src.Type, Order = at, DataJson = src.DataJson });
            await db.SaveChangesAsync();
            return Results.Ok(await Load(db, targetId));
        });
    }
}
