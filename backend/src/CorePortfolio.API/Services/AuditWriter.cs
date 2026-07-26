using System.Security.Claims;
using System.Text.Json;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;

namespace CorePortfolio.API.Services;

public sealed class AuditWriter(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor)
{
    public void Add(
        string action,
        string entityType,
        string? entityId,
        object? metadata = null,
        string outcome = "Succeeded")
    {
        var context = httpContextAccessor.HttpContext;
        var actorValue = context?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        dbContext.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = Guid.TryParse(actorValue, out var actorId) ? actorId : null,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Outcome = outcome,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
            CorrelationId = context?.Response.Headers["X-Correlation-ID"].FirstOrDefault(),
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
            OccurredAt = DateTime.UtcNow
        });
    }
}
