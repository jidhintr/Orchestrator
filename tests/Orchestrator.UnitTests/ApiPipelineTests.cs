using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Orchestrator.Api.Access;
using Orchestrator.Api.Errors;
using Orchestrator.Api.Middleware;
using Orchestrator.Application;
using Orchestrator.Application.Access;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.UnitTests;

public class ApiPipelineTests
{
    private static async Task<(int Status, JsonElement Body)> Handle(Exception exception)
    {
        using var provider = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var handler = new GlobalExceptionHandler(provider.GetRequiredService<IProblemDetailsService>(), NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Response.Body = new MemoryStream();

        Assert.True(await handler.TryHandleAsync(context, exception, default));

        context.Response.Body.Position = 0;
        return (context.Response.StatusCode, (await JsonDocument.ParseAsync(context.Response.Body)).RootElement.Clone());
    }

    [Theory]
    [InlineData(typeof(NotFoundException), 404, "Resource not found.")]
    [InlineData(typeof(UnauthorizedException), 401, "Unauthorized.")]
    [InlineData(typeof(ForbiddenException), 403, "Forbidden.")]
    [InlineData(typeof(BadRequestException), 400, "Bad request.")]
    [InlineData(typeof(ConflictException), 409, "Conflict.")]
    public async Task Application_exceptions_map_to_problem_details(Type type, int status, string title)
    {
        var (actual, body) = await Handle((Exception)Activator.CreateInstance(type, "the detail")!);
        Assert.Equal(status, actual);
        Assert.Equal(title, body.GetProperty("title").GetString());
        Assert.Equal("the detail", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Unexpected_exception_is_500_without_exception_text()
    {
        var (status, body) = await Handle(new InvalidOperationException("secret connection string"));
        Assert.Equal(500, status);
        Assert.Equal("An unexpected error occurred.", body.GetProperty("title").GetString());
        Assert.DoesNotContain("secret", body.GetRawText(), StringComparison.Ordinal);
    }

    private static async Task<DefaultHttpContext> Correlate(string? supplied)
    {
        var context = new DefaultHttpContext();
        if (supplied is not null)
        {
            context.Request.Headers[CorrelationIdMiddleware.HeaderName] = supplied;
        }

        var called = false;
        await new CorrelationIdMiddleware(_ => { called = true; return Task.CompletedTask; }, NullLogger<CorrelationIdMiddleware>.Instance).InvokeAsync(context);
        Assert.True(called);
        return context;
    }

    [Theory]
    [InlineData("test-corr_1.2")]
    [InlineData("A")]
    public async Task Middleware_keeps_a_safe_supplied_id(string supplied) =>
        Assert.Equal(supplied, (await Correlate(supplied)).Items[CorrelationIdMiddleware.ItemKey]);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("semi;colon")]
    [InlineData("<script>")]
    public async Task Middleware_generates_an_id_when_missing_or_unsafe(string? supplied)
    {
        var id = Assert.IsType<string>((await Correlate(supplied)).Items[CorrelationIdMiddleware.ItemKey]);
        Assert.Matches("^[0-9a-f]{32}$", id);
    }

    [Fact]
    public async Task Middleware_rejects_ids_longer_than_64_characters()
    {
        var id = Assert.IsType<string>((await Correlate(new string('a', 65))).Items[CorrelationIdMiddleware.ItemKey]);
        Assert.Equal(32, id.Length);
        Assert.Equal(new string('a', 64), (await Correlate(new string('a', 64))).Items[CorrelationIdMiddleware.ItemKey]);
    }

    [Fact]
    public void Current_user_id_reads_the_sub_claim()
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "42")], "test")) };
        Assert.Equal(42, new HttpCurrentUserId(new HttpContextAccessor { HttpContext = context }).Id);
    }

    [Fact]
    public void Current_user_id_is_null_without_a_usable_sub_claim()
    {
        Assert.Null(new HttpCurrentUserId(new HttpContextAccessor { HttpContext = new DefaultHttpContext() }).Id);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "abc")], "test")) };
        Assert.Null(new HttpCurrentUserId(new HttpContextAccessor { HttpContext = context }).Id);
    }

    private static async Task<bool> RunAdminOnly(Func<OrchestratorDbContext, int?> actor)
    {
        using var db = TestDb.Create();
        using var provider = new ServiceCollection()
            .AddSingleton<IAppDbContext>(db)
            .AddSingleton<ICurrentUserId>(new FakeUserId(actor(db)))
            .AddScoped<CurrentUser>()
            .BuildServiceProvider();
        var action = new ActionContext(new DefaultHttpContext { RequestServices = provider }, new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(action, [], new Dictionary<string, object?>(), new object());

        var nextCalled = false;
        await new AdminOnlyAttribute().OnActionExecutionAsync(executing, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(action, [], new object()));
        });
        return nextCalled;
    }

    [Fact]
    public async Task AdminOnly_lets_admins_and_super_admins_through()
    {
        Assert.True(await RunAdminOnly(_ => 2));
        Assert.True(await RunAdminOnly(_ => 1));
    }

    [Fact]
    public async Task AdminOnly_rejects_missing_unknown_and_low_rank_users()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => RunAdminOnly(_ => null));
        await Assert.ThrowsAsync<UnauthorizedException>(() => RunAdminOnly(_ => 9999));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            RunAdminOnly(db => db.AddUser("plain", [SystemIds.UnassignedDepartment]).Id));
    }

    [Fact]
    public void AdminOnly_runs_before_automatic_validation() => Assert.True(new AdminOnlyAttribute().Order < -2000);
}
