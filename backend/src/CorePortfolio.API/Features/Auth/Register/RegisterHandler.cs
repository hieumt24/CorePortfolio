using CorePortfolio.Domain.Entities;
using CorePortfolio.API.Common;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

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

    public RegisterHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterResult
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email
        };
    }
}
