using Microsoft.EntityFrameworkCore;
using Orchestrator.Api.Extensions;
using Orchestrator.Infrastructure;
using Orchestrator.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiPlatform();
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
    app.MapOpenApi().WithDocumentPerVersion();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/health");

app.Run();

public partial class Program;
