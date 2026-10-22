using Microsoft.Extensions.DependencyInjection;
using Orchestrator.Application.Access;
using Orchestrator.Application.ApprovalChains;
using Orchestrator.Application.Catalogs;
using Orchestrator.Application.Clients;
using Orchestrator.Application.Departments;
using Orchestrator.Application.JobTypes;
using Orchestrator.Application.Roles;
using Orchestrator.Application.Users;

namespace Orchestrator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) => services
        .AddScoped<DepartmentService>()
        .AddScoped<UserService>()
        .AddScoped<RoleService>()
        .AddScoped<CatalogService>()
        .AddScoped<ClientService>()
        .AddScoped<ApprovalChainService>()
        .AddScoped<JobTypeService>()
        .AddScoped<ActingUser>();
}
