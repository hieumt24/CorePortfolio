namespace CorePortfolio.API.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Role { get; }
}
