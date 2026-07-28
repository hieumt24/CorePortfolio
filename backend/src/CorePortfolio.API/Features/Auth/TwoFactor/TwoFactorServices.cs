using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Admin.ControlPlane;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CorePortfolio.API.Features.Auth.TwoFactor;

public sealed class TwoFactorOptions
{
    public const string SectionName = "Security:TwoFactor";

    public bool EnforceForPrivilegedRoles { get; set; }
    public string EncryptionKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "CorePortfolio";
    public int ChallengeLifetimeMinutes { get; set; } = 5;
    public int MaxVerificationAttempts { get; set; } = 5;
    public int AllowedTimeStepDrift { get; set; } = 1;
    public int RecoveryCodeCount { get; set; } = 10;
    public int CleanupIntervalMinutes { get; set; } = 60;
    public int ChallengeRetentionHours { get; set; } = 24;

    public bool HasValidEncryptionKey()
    {
        try
        {
            return Convert.FromBase64String(EncryptionKey).Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed class TwoFactorPolicy(IOptions<TwoFactorOptions> options)
{
    public bool IsPrivilegedRole(string? role) =>
        AdminPermissionCatalog.Has(role, AdminPermissionCatalog.AdminAccess);

    public bool RequiresTwoFactor(User user) =>
        user.TwoFactorEnabled ||
        (options.Value.EnforceForPrivilegedRoles && IsPrivilegedRole(user.Role));

    public bool CanDisable(User user) =>
        !options.Value.EnforceForPrivilegedRoles || !IsPrivilegedRole(user.Role);
}

public sealed class TwoFactorSecretProtector(IOptions<TwoFactorOptions> options)
{
    private const byte FormatVersion = 1;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const string ConfigurationError =
        "Two-factor authentication is not available because its encryption key is not configured.";

    public bool IsConfigured => options.Value.HasValidEncryptionKey();

    public void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new ServiceUnavailableException(ConfigurationError);
    }

    public string Protect(string plaintext, Guid userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        var key = GetEncryptionKey();
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag, userId.ToByteArray());

        var payload = new byte[1 + NonceSize + TagSize + ciphertext.Length];
        payload[0] = FormatVersion;
        nonce.CopyTo(payload.AsSpan(1, NonceSize));
        tag.CopyTo(payload.AsSpan(1 + NonceSize, TagSize));
        ciphertext.CopyTo(payload.AsSpan(1 + NonceSize + TagSize));
        return WebEncoders.Base64UrlEncode(payload);
    }

    public string Unprotect(string protectedValue, Guid userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedValue);
        var payload = WebEncoders.Base64UrlDecode(protectedValue);
        if (payload.Length <= 1 + NonceSize + TagSize || payload[0] != FormatVersion)
            throw new CryptographicException("Unsupported two-factor secret format.");

        var nonce = payload.AsSpan(1, NonceSize);
        var tag = payload.AsSpan(1 + NonceSize, TagSize);
        var ciphertext = payload.AsSpan(1 + NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(GetEncryptionKey(), TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, userId.ToByteArray());
        return Encoding.UTF8.GetString(plaintext);
    }

    private byte[] GetEncryptionKey()
    {
        try
        {
            var key = Convert.FromBase64String(options.Value.EncryptionKey);
            if (key.Length == 32) return key;
        }
        catch (FormatException)
        {
            // Throw the same configuration error below without echoing the configured value.
        }

        throw new ServiceUnavailableException(ConfigurationError);
    }
}

public sealed class TotpService(IOptions<TwoFactorOptions> options)
{
    private const int TimeStepSeconds = 30;
    private const int Digits = 6;

    public string GenerateSecret() => Base32.Encode(RandomNumberGenerator.GetBytes(20));

    public string BuildProvisioningUri(string username, string secret)
    {
        var issuer = options.Value.Issuer.Trim();
        var label = Uri.EscapeDataString($"{issuer}:{username}");
        return $"otpauth://totp/{label}?secret={Uri.EscapeDataString(secret)}" +
            $"&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={Digits}&period={TimeStepSeconds}";
    }

    public bool TryVerify(
        string secret,
        string? code,
        DateTime utcNow,
        out long acceptedTimeStep)
    {
        acceptedTimeStep = 0;
        var normalizedCode = NormalizeCode(code);
        if (normalizedCode is null) return false;

        var currentStep = GetTimeStep(utcNow);
        var drift = Math.Clamp(options.Value.AllowedTimeStepDrift, 0, 2);
        for (var offset = -drift; offset <= drift; offset++)
        {
            var candidateStep = currentStep + offset;
            var expected = ComputeCode(secret, candidateStep);
            if (!FixedTimeEquals(expected, normalizedCode)) continue;
            acceptedTimeStep = candidateStep;
            return true;
        }

        return false;
    }

    public string ComputeCode(string secret, DateTime utcNow) =>
        ComputeCode(secret, GetTimeStep(utcNow));

    internal static string ComputeCode(string secret, long timeStep)
    {
        var key = Base32.Decode(secret);
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, timeStep);
        var hash = HMACSHA1.HashData(key, counter);
        var offset = hash[^1] & 0x0f;
        var binaryCode = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, 4)) & 0x7fffffff;
        return (binaryCode % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static long GetTimeStep(DateTime utcNow) =>
        new DateTimeOffset(utcNow.ToUniversalTime()).ToUnixTimeSeconds() / TimeStepSeconds;

    private static string? NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var normalized = new string(code.Where(char.IsDigit).ToArray());
        return normalized.Length == Digits ? normalized : null;
    }

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(left),
            Encoding.ASCII.GetBytes(right));
}

public sealed class RecoveryCodeService(IOptions<TwoFactorOptions> options)
{
    public IReadOnlyList<string> GenerateCodes()
    {
        var count = Math.Clamp(options.Value.RecoveryCodeCount, 8, 12);
        return Enumerable.Range(0, count)
            .Select(_ => Format(Base32.Encode(RandomNumberGenerator.GetBytes(10))))
            .ToList();
    }

    public static string Hash(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(code))))
            .ToLowerInvariant();

    private static string Normalize(string code) =>
        new(code.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static string Format(string code) =>
        string.Join("-", Enumerable.Range(0, 4).Select(index => code.Substring(index * 4, 4)));
}

public sealed record IssuedTwoFactorChallenge(
    TwoFactorChallenge Challenge,
    string Token);

public sealed class TwoFactorChallengeService(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IOptions<TwoFactorOptions> options)
{
    public IssuedTwoFactorChallenge Issue(User user, TwoFactorChallengePurpose purpose, DateTime now)
    {
        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = new TwoFactorChallenge
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            Purpose = purpose,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(Math.Clamp(options.Value.ChallengeLifetimeMinutes, 2, 10)),
            MaxAttempts = Math.Clamp(options.Value.MaxVerificationAttempts, 3, 10),
            IpAddress = ClientIpAddress.Resolve(httpContextAccessor.HttpContext),
            UserAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString()
        };
        dbContext.TwoFactorChallenges.Add(challenge);
        return new IssuedTwoFactorChallenge(challenge, rawToken);
    }

    public Task<TwoFactorChallenge?> FindActiveAsync(
        string? rawToken,
        TwoFactorChallengePurpose? purpose,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return Task.FromResult<TwoFactorChallenge?>(null);

        var tokenHash = HashToken(rawToken);
        return dbContext.TwoFactorChallenges
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item => item.TokenHash == tokenHash &&
                    item.ConsumedAt == null &&
                    item.ExpiresAt > now &&
                    item.FailedAttemptCount < item.MaxAttempts &&
                    (!purpose.HasValue || item.Purpose == purpose.Value),
                cancellationToken);
    }

    public static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)))
            .ToLowerInvariant();
}

internal static class Base32
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Encode(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return string.Empty;
        var output = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var value in data)
        {
            buffer = (buffer << 8) | value;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                output.Append(Alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
            output.Append(Alphabet[(buffer << (5 - bitsLeft)) & 31]);
        return output.ToString();
    }

    public static byte[] Decode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().Replace("=", string.Empty).ToUpperInvariant();
        var output = new List<byte>(normalized.Length * 5 / 8);
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var character in normalized)
        {
            var index = Alphabet.IndexOf(character);
            if (index < 0) throw new RequestValidationException("Invalid authenticator secret.");
            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft < 8) continue;
            output.Add((byte)((buffer >> (bitsLeft - 8)) & 255));
            bitsLeft -= 8;
        }
        return output.ToArray();
    }
}
