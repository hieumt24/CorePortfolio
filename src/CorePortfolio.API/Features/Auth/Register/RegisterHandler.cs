using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Auth.Register;

public class RegisterCommand : IRequest<RegisterResult>
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterResult
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
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
        if (await _dbContext.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower(), cancellationToken))
        {
            throw new InvalidOperationException("Username already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User", // Default role
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterResult
        {
            UserId = user.Id,
            Username = user.Username
        };
    }
}
