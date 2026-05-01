using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Authentication;
using NodeScope.Domain.Entities;
using NodeScope.Domain.Enums;
using NodeScope.Infrastructure.Data;

namespace NodeScope.Api.Development;

/// <summary>
/// Ensures a deterministic operator exists for local UI validation without provisioning scripts.
/// </summary>
internal static class DevDataSeeder
{
    internal const string DevEmail = "dev@nodescope.local";
    internal const string DevPassword = "ChangeMe123!";

    internal static async Task SeedDevelopmentUserAsync(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var hasher = scope.ServiceProvider.GetRequiredService<IUserPasswordHasher>();

        var normalizedEmail = DevEmail.Trim().ToLowerInvariant();
        if (await db.Users.AsNoTracking().AnyAsync(u => u.Email == normalizedEmail).ConfigureAwait(false))
        {
            return;
        }

        var user = new User(DevEmail, "TEMP", "Development user", UserRole.Admin);
        var pwdHash = hasher.Hash(user, DevPassword);
        user.SetPasswordHash(pwdHash);

        db.Users.Add(user);
        _ = await db.SaveChangesAsync().ConfigureAwait(false);
    }
}
