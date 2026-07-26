namespace CorePortfolio.API.Features.Profile;

public sealed record ProfileResponse(
    Guid Id,
    string Username,
    string DisplayName,
    string? Email,
    string Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public sealed record UpdateProfileRequest(
    string Username,
    string DisplayName,
    string? Email);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword);
