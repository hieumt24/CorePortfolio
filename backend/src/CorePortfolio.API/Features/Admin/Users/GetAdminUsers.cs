using CorePortfolio.API.Common.Models;
using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CorePortfolio.API.Features.Admin.Users;

public record AdminUserDto(
    Guid Id,
    string Username,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    string? LastLoginIpAddress,
    DateTime? LastActivityAt,
    bool IsOnline,
    bool TwoFactorEnabled,
    int PortfolioCount,
    int TransactionCount);

public record GetAdminUsersQuery(
    string? Search,
    string? Role,
    bool? IsActive,
    bool? IsOnline,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedResult<AdminUserDto>>;

public sealed class GetAdminUsersHandler(
    AppDbContext dbContext,
    IOptions<UserActivityOptions> activityOptions)
    : IRequestHandler<GetAdminUsersQuery, PaginatedResult<AdminUserDto>>
{
    public async Task<PaginatedResult<AdminUserDto>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 10, 100);
        var query = dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(user =>
                user.Username.ToLower().Contains(search) ||
                (user.DisplayName != null && user.DisplayName.ToLower().Contains(search)) ||
                (user.Email != null && user.Email.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
            query = query.Where(user => user.Role == request.Role);

        if (request.IsActive.HasValue)
            query = query.Where(user => user.IsActive == request.IsActive.Value);

        var onlineCutoff = UserPresence.GetOnlineCutoff(activityOptions.Value);
        if (request.IsOnline == true)
            query = query.Where(user =>
                user.IsActive &&
                user.LastActivityAt != null &&
                user.LastActivityAt >= onlineCutoff);
        else if (request.IsOnline == false)
            query = query.Where(user =>
                !user.IsActive ||
                user.LastActivityAt == null ||
                user.LastActivityAt < onlineCutoff);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(user => user.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new AdminUserDto(
                user.Id,
                user.Username,
                user.Role,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt,
                user.LastLoginIpAddress,
                user.LastActivityAt,
                user.IsActive && user.LastActivityAt != null && user.LastActivityAt >= onlineCutoff,
                user.TwoFactorEnabled,
                user.Portfolios.Count,
                user.Portfolios.SelectMany(portfolio => portfolio.Transactions).Count()))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<AdminUserDto>(items, totalCount, page, pageSize);
    }
}
