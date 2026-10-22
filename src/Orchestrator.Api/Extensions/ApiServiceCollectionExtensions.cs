using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Orchestrator.Api.Access;
using Orchestrator.Api.Errors;
using Orchestrator.Api.Middleware;
using Orchestrator.Application;
using Orchestrator.Application.Access;

namespace Orchestrator.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserId, HttpCurrentUserId>();
        services.AddApplication();
        services.AddAuth(configuration);

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
                    "Every endpoint except the health checks, sign in and refresh needs an 'Authorization: Bearer <access token>' header " +
                    "from POST /api/v1/auth/login. Errors are application/problem+json " +
                    "(status, title, detail, traceId, correlationId; 400 adds an errors map). " +
                    "Send an optional X-Correlation-Id header (letters, digits, . _ -; max 64) and it is echoed back, " +
                    "otherwise one is generated. The API version is part of the URL (/api/v1/...).";
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" },
                };
                return Task.CompletedTask;
            }));

        return services;
    }

    private static void AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthOptions>().BindConfiguration(AuthOptions.Section).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton<JwtIssuer>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<IOptions<AuthOptions>>((jwt, auth) =>
        {
            var o = auth.Value;
            jwt.MapInboundClaims = false;
            jwt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = o.Issuer,
                ValidateAudience = true,
                ValidAudience = o.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = JwtIssuer.Key(o),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.FromSeconds(30),
            };
            jwt.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
                    {
                        return;
                    }

                    var db = context.HttpContext.RequestServices.GetRequiredService<IAppDbContext>();
                    var active = int.TryParse(context.Principal?.FindFirst("sub")?.Value, out var id)
                        && await db.Users.AnyAsync(u => u.Id == id && u.IsActive, context.HttpContext.RequestAborted);
                    if (!active)
                    {
                        context.Fail("Unknown or inactive user.");
                    }
                },
            };
        });

        services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        var permits = configuration.GetValue("Auth:LoginPermitsPerMinute", 10);
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy("auth", http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = TimeSpan.FromMinutes(1) }));
        });
    }

    public static WebApplication UseApiPlatform(this WebApplication app)
    {
        app.Use((context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            if (context.Request.Path.StartsWithSegments("/api/v1/auth"))
            {
                headers.CacheControl = "no-store";
            }

            return next(context);
        });
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}
