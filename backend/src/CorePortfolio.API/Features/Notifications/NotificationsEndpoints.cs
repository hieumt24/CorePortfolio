using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Notifications;

public static class NotificationsEndpoints
{
    public static void MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").RequireAuthorization();
        group.MapGet("", async (AppDbContext db, ICurrentUserService current, [FromQuery] bool unreadOnly = false) =>
        {
            var query = db.Notifications.AsNoTracking().Where(n => n.UserId == current.UserId);
            if (unreadOnly) query = query.Where(n => n.ReadAt == null);
            return Results.Ok(await query.OrderByDescending(n => n.CreatedAt).Take(50).ToListAsync());
        });
        group.MapPost("/{id:guid}/read", async (Guid id, AppDbContext db, ICurrentUserService current) =>
        {
            var item = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == current.UserId);
            if (item is null) return Results.NotFound(); item.ReadAt = DateTime.UtcNow; await db.SaveChangesAsync(); return Results.NoContent();
        });
        group.MapPost("/read-all", async (AppDbContext db, ICurrentUserService current) =>
        {
            var items = await db.Notifications.Where(n => n.UserId == current.UserId && n.ReadAt == null).ToListAsync();
            foreach (var item in items) item.ReadAt = DateTime.UtcNow; await db.SaveChangesAsync(); return Results.NoContent();
        });
    }
}
