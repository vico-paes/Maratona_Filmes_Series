using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Maratona.Api.Data;
using Maratona.Api.Data.Entities;
using Maratona.Api.Infrastructure;
using Maratona.Api.Titles;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Lists;

public record ListRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [StringLength(1000)] string? Description,
    bool IsPublic = true);

public record ListItemRequest([Range(1, int.MaxValue)] int TitleId);

public record ListSummaryDto(int Id, string Name, string? Description, bool IsPublic, int ItemCount, List<string> Posters, DateTime UpdatedAt);

public record ListItemDto(int TitleId, int Position, TitleCardDto Title, DateTime AddedAt);

public record ListDto(
    int Id,
    string Name,
    string? Description,
    bool IsPublic,
    UserSummaryDto Owner,
    bool IsMine,
    List<ListItemDto> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public static class ListEndpoints
{
    private const int MaxLists = 100;
    private const int MaxItems = 500;

    public static void MapListEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/users/{username}/lists", UserLists);
        app.MapGet("/lists/{listId:int}", GetList);

        var me = app.MapGroup("/me/lists").RequireAuthorization();
        me.MapPost("", Create);
        me.MapPut("/{listId:int}", Update);
        me.MapDelete("/{listId:int}", Delete);
        me.MapPost("/{listId:int}/items", AddItem);
        me.MapDelete("/{listId:int}/items/{titleId:int}", RemoveItem);
    }

    private static async Task<IResult> UserLists(string username, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var owner = await db.Users.FirstOrDefaultAsync(u => u.Username == username.ToLower(), ct);
        if (owner is null)
        {
            return Results.NotFound();
        }
        var isMine = user.UserIdOrNull() == owner.Id;
        var lists = await db.Lists
            .Where(l => l.UserId == owner.Id && (isMine || l.IsPublic))
            .OrderByDescending(l => l.UpdatedAt)
            .Select(l => new ListSummaryDto(
                l.Id,
                l.Name,
                l.Description,
                l.IsPublic,
                l.Items.Count,
                l.Items.OrderBy(i => i.Position).Where(i => i.Title.PosterPath != null).Take(4).Select(i => i.Title.PosterPath!).ToList(),
                l.UpdatedAt))
            .ToListAsync(ct);
        return Results.Ok(lists);
    }

    private static async Task<IResult> GetList(int listId, ClaimsPrincipal user, AppDbContext db, TitleService titles, CancellationToken ct)
    {
        var list = await db.Lists
            .Include(l => l.User)
            .Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == listId, ct);
        var isMine = list is not null && user.UserIdOrNull() == list.UserId;
        if (list is null || (!list.IsPublic && !isMine))
        {
            return Results.NotFound();
        }
        var cards = await titles.Cards(list.Items.Select(i => i.TitleId), ct);
        return Results.Ok(new ListDto(
            list.Id,
            list.Name,
            list.Description,
            list.IsPublic,
            Common.ToSummary(list.User),
            isMine,
            list.Items
                .OrderBy(i => i.Position)
                .Select(i => new ListItemDto(i.TitleId, i.Position, cards[i.TitleId], i.AddedAt))
                .ToList(),
            list.CreatedAt,
            list.UpdatedAt));
    }

    private static async Task<IResult> Create(ListRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var userId = user.UserId();
        if (await db.Lists.CountAsync(l => l.UserId == userId, ct) >= MaxLists)
        {
            return Common.Invalid("name", $"Limite de {MaxLists} listas atingido.");
        }
        var list = new UserList { UserId = userId };
        Apply(list, request);
        db.Lists.Add(list);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/lists/{list.Id}", new ListSummaryDto(list.Id, list.Name, list.Description, list.IsPublic, 0, [], list.UpdatedAt));
    }

    private static async Task<IResult> Update(int listId, ListRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var list = await Own(db, user, listId, ct);
        if (list is null)
        {
            return Results.NotFound();
        }
        Apply(list, request);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Delete(int listId, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var userId = user.UserId();
        var deleted = await db.Lists.Where(l => l.Id == listId && l.UserId == userId).ExecuteDeleteAsync(ct);
        return deleted == 0 ? Results.NotFound() : Results.NoContent();
    }

    private static async Task<IResult> AddItem(int listId, ListItemRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var list = await Own(db, user, listId, ct);
        if (list is null || !await db.Titles.AnyAsync(t => t.Id == request.TitleId, ct))
        {
            return Results.NotFound();
        }
        var items = db.ListItems.Where(i => i.ListId == listId);
        if (await items.AnyAsync(i => i.TitleId == request.TitleId, ct))
        {
            return Results.NoContent();
        }
        if (await items.CountAsync(ct) >= MaxItems)
        {
            return Common.Invalid("titleId", $"Uma lista pode ter até {MaxItems} títulos.");
        }
        var position = (await items.MaxAsync(i => (int?)i.Position, ct) ?? 0) + 1;
        db.ListItems.Add(new ListItem { ListId = listId, TitleId = request.TitleId, Position = position });
        list.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveItem(int listId, int titleId, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var list = await Own(db, user, listId, ct);
        if (list is null)
        {
            return Results.NotFound();
        }
        await db.ListItems.Where(i => i.ListId == listId && i.TitleId == titleId).ExecuteDeleteAsync(ct);
        list.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static Task<UserList?> Own(AppDbContext db, ClaimsPrincipal user, int listId, CancellationToken ct)
    {
        var userId = user.UserId();
        return db.Lists.FirstOrDefaultAsync(l => l.Id == listId && l.UserId == userId, ct);
    }

    private static void Apply(UserList list, ListRequest request)
    {
        list.Name = request.Name.Trim();
        list.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        list.IsPublic = request.IsPublic;
        list.UpdatedAt = DateTime.UtcNow;
    }
}
