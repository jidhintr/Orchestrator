using Asp.Versioning;
using Microsoft.AspNetCore.OpenApi;
using Orchestrator.Api.Departments;
using Orchestrator.Api.Errors;
using Orchestrator.Api.Middleware;
using Orchestrator.Api.Roles;
using Orchestrator.Api.Users;

namespace Orchestrator.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiPlatform(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddScoped<DepartmentService>();
        services.AddScoped<UserService>();
        services.AddScoped<RoleService>();

        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            if (context.HttpContext.Items[CorrelationIdMiddleware.ItemKey] is string correlationId)
            {
                context.ProblemDetails.Extensions["correlationId"] = correlationId;
            }
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            })
            .AddOpenApi();

        services.Configure<OpenApiOptions>("v1", options => options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Description =
                    "No authentication yet: all endpoints are anonymous. Errors are application/problem+json " +
                    "(status, title, detail, traceId, correlationId; 400 adds an errors map). " +
                    "Send an optional X-Correlation-Id header (letters, digits, . _ -; max 64) and it is echoed back, " +
                    "otherwise one is generated. The API version is part of the URL (/api/v1/...).";
                return Task.CompletedTask;
            }));

        return services;
    }

    public static WebApplication UseApiPlatform(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}
