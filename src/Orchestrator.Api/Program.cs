using Microsoft.EntityFrameworkCore;
using Orchestrator.Api.Extensions;
using Orchestrator.Application.Auth;
using Orchestrator.Domain;
using Orchestrator.Infrastructure;
using Orchestrator.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiPlatform(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseApiPlatform();

if (app.Configuration.GetValue<bool>("MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
    await SeedDevSuperAdminAsync(app);
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/health").AllowAnonymous();
app.MapHealthChecks("/api/health/live", new() { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/api/health/ready").AllowAnonymous();

app.Run();

// Dev phase only: lets the owner sign in as the seeded Super Admin. Remove with the seeded user (docs/TODO-auth-end-of-dev.md).
static async Task SeedDevSuperAdminAsync(WebApplication app)
{
    if (app.Configuration["Auth:DevSeedPassword"] is not { Length: > 0 } password)
    {
        return;
    }

    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
        if ((await db.Database.GetPendingMigrationsAsync()).Any() || await db.Credentials.AnyAsync(c => c.UserId == SystemIds.SuperAdminUser))
        {
            return;
        }

        await scope.ServiceProvider.GetRequiredService<AuthService>().SetPasswordAsync(SystemIds.SuperAdminUser, password, default);
        await db.SaveChangesAsync();
    }
    catch (Exception e) when (e is not OperationCanceledException)
    {
        // The API must still start with the database down so readiness can report it instead of the process dying.
        SeedSkipped(app.Logger, e.GetType().Name, null);
    }
}

public partial class Program
{
    private static readonly Action<ILogger, string, Exception?> SeedSkipped = LoggerMessage.Define<string>(
        LogLevel.Warning, new EventId(1, "SeedSkipped"), "Dev super admin seed skipped, the database is unavailable ({Reason}).");
}
