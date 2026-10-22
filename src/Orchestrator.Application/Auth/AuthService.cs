using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Orchestrator.Application.Access;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;

namespace Orchestrator.Application.Auth;

public sealed class AuthService(IAppDbContext db, CurrentUser current)
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan Lockout = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);
    private static readonly string DummyHash = PasswordHasher.Hash("not-a-real-password");

    public async Task<SignedIn> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var login = request.Login.Trim();
        var credential = await db.Credentials.Include(c => c.User)
            .FirstOrDefaultAsync(c => c.User!.IsActive && (c.User.Email == login || c.User.Mobile == login), cancellationToken);
        var now = DateTime.UtcNow;

        if (credential is null)
        {
            PasswordHasher.Verify(request.Password, DummyHash);
            throw InvalidCredentials();
        }

        if (credential.LockoutEnd > now)
        {
            throw InvalidCredentials();
        }

        if (!PasswordHasher.Verify(request.Password, credential.PasswordHash))
        {
            credential.FailedCount++;
            if (credential.FailedCount >= MaxFailures)
            {
                credential.FailedCount = 0;
                credential.LockoutEnd = now + Lockout;
            }

            await db.SaveChangesAsync(cancellationToken);
            throw InvalidCredentials();
        }

        credential.FailedCount = 0;
        credential.LockoutEnd = null;
        return await IssueAsync(credential.UserId, Guid.NewGuid(), cancellationToken);
    }

    public async Task<SignedIn> RefreshAsync(string? token, CancellationToken cancellationToken)
    {
        var stored = await FindTokenAsync(token, cancellationToken) ?? throw Expired();
        if (stored.RevokedAt is not null)
        {
            await RevokeFamilyAsync(stored.FamilyId, cancellationToken);
            throw Expired();
        }

        var active = stored.ExpiresAt > DateTime.UtcNow && await db.Users.AnyAsync(u => u.Id == stored.UserId && u.IsActive, cancellationToken);
        if (!active)
        {
            throw Expired();
        }

        stored.RevokedAt = DateTime.UtcNow;
        return await IssueAsync(stored.UserId, stored.FamilyId, cancellationToken);
    }

    public async Task LogoutAsync(string? token, CancellationToken cancellationToken)
    {
        if (await FindTokenAsync(token, cancellationToken) is { } stored)
        {
            await RevokeFamilyAsync(stored.FamilyId, cancellationToken);
        }
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var actor = await current.RequireAsync(Rank.Employee, cancellationToken);
        var credential = await db.Credentials.SingleOrDefaultAsync(c => c.UserId == actor.Id, cancellationToken);
        if (credential is null || !PasswordHasher.Verify(request.CurrentPassword, credential.PasswordHash))
        {
            throw new BadRequestException("The current password is incorrect.");
        }

        await SetPasswordAsync(actor.Id, request.NewPassword, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    // Tracks the change only; the caller saves, so it commits together with its own work.
    public async Task SetPasswordAsync(int userId, string password, CancellationToken cancellationToken)
    {
        var credential = await db.Credentials.SingleOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (credential is null)
        {
            db.Credentials.Add(new UserCredential { UserId = userId, PasswordHash = PasswordHasher.Hash(password), PasswordChangedAt = DateTime.UtcNow });
        }
        else
        {
            credential.PasswordHash = PasswordHasher.Hash(password);
            credential.PasswordChangedAt = DateTime.UtcNow;
            credential.FailedCount = 0;
            credential.LockoutEnd = null;
        }

        await RevokeTokensAsync(userId, cancellationToken);
    }

    public async Task RevokeTokensAsync(int userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        foreach (var token in await db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync(cancellationToken))
        {
            token.RevokedAt = now;
        }
    }

    private async Task<SignedIn> IssueAsync(int userId, Guid family, CancellationToken cancellationToken)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var expires = DateTime.UtcNow + RefreshLifetime;
        db.RefreshTokens.Add(new RefreshToken { UserId = userId, TokenHash = Hash(raw), FamilyId = family, ExpiresAt = expires });
        await db.SaveChangesAsync(cancellationToken);
        return new SignedIn(userId, raw, expires);
    }

    private async Task RevokeFamilyAsync(Guid family, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        foreach (var token in await db.RefreshTokens.Where(t => t.FamilyId == family && t.RevokedAt == null).ToListAsync(cancellationToken))
        {
            token.RevokedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<RefreshToken?> FindTokenAsync(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var hash = Hash(token);
        return await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static UnauthorizedException InvalidCredentials() => new("Invalid credentials.");

    private static UnauthorizedException Expired() => new("Your session has expired. Sign in again.");
}
