using CorePortfolio.Domain.Entities;
using CorePortfolio.API.Common;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Auth.Register;

public class RegisterCommand : IRequest<RegisterResult>
{
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class RegisterResult
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class RegisterHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly AppDbContext _dbContext;
    private readonly NotificationWriter _notificationWriter;

    public RegisterHandler(
        AppDbContext dbContext,
        NotificationWriter notificationWriter)
    {
        _dbContext = dbContext;
        _notificationWriter = notificationWriter;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();

        if (username.Length is < 3 or > 50)
            throw new RequestValidationException("Username must contain between 3 and 50 characters.");
        if (request.Password.Length is < 8 or > 72)
            throw new RequestValidationException("Password must contain between 8 and 72 characters.");
        if (email is not null && (email.Length > 160 || !MailAddress.TryCreate(email, out _)))
            throw new RequestValidationException("Email address is not valid.");

        if (await _dbContext.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower(), cancellationToken))
        {
            throw new ResourceConflictException("Username already exists.");
        }
        if (email is not null && await _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken))
            throw new ResourceConflictException("Email address is already in use.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User", // Default role
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        var activeAdminIds = await _dbContext.Users
            .AsNoTracking()
            .Where(item => item.Role == "Admin" && item.IsActive)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        foreach (var adminId in activeAdminIds)
        {
            await _notificationWriter.QueueAsync(
                new NotificationWriteRequest(
                    adminId,
                    NotificationType.System,
                    NotificationSeverity.Info,
                    "Có người dùng mới đăng ký",
                    $"Tài khoản {user.Username} vừa được tạo.",
                    $"system:user-registered:{user.Id:N}",
                    $"/admin/users?search={Uri.EscapeDataString(user.Username)}",
                    "User",
                    user.Id,
                    "Xem người dùng",
                    Metadata: new Dictionary<string, string?>
                    {
                        ["username"] = user.Username,
                        ["registeredAt"] = user.CreatedAt.ToString("O")
                    }),
                cancellationToken);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterResult
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email
        };
    }
}
