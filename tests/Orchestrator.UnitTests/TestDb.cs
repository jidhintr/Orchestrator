using Microsoft.EntityFrameworkCore;
using Orchestrator.Application.Access;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.UnitTests;

internal sealed class FakeUserId(int? id) : ICurrentUserId
{
    public int? Id => id;
}

internal static class TestDb
{
    public const int SuperAdminUser = 1;
    public const int AdminUser = 2;

    public static OrchestratorDbContext Create()
    {
        var db = new OrchestratorDbContext(new DbContextOptionsBuilder<OrchestratorDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Database.EnsureCreated();
        var admin = new User { Id = AdminUser, FirstName = "Admin", LastName = "Tester", Email = "admin.tester@orchestrator.local", Mobile = "0000000002" };
        admin.Departments.AddRange(db.Departments.Where(d => d.Id == 2));
        db.Users.Add(admin);
        db.SaveChanges();
        return db;
    }

    public static CurrentUser As(this OrchestratorDbContext db, int? userId) => new(new FakeUserId(userId), db);

    public static Department AddDepartment(this OrchestratorDbContext db, string code, params int[] roleIds)
    {
        var department = new Department { Name = code, Code = code };
        department.Roles.AddRange(db.Roles.Where(r => roleIds.Contains(r.Id)));
        db.Departments.Add(department);
        db.SaveChanges();
        return department;
    }

    public static User AddUser(this OrchestratorDbContext db, string name, int[] departmentIds, params int[] roleIds)
    {
        var user = new User { FirstName = name, LastName = "Test", Email = $"{name}@test.local".ToLowerInvariant(), Mobile = "0123456789" };
        user.Departments.AddRange(db.Departments.Where(d => departmentIds.Contains(d.Id)));
        user.Roles.AddRange(db.Roles.Where(r => roleIds.Contains(r.Id)));
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static JobType AddJobType(this OrchestratorDbContext db, string name, bool active = true)
    {
        var jobType = new JobType { Name = name, IsActive = active };
        db.JobTypes.Add(jobType);
        db.SaveChanges();
        return jobType;
    }

    public static Client AddClient(this OrchestratorDbContext db, string email = "client@test.local", int? userId = null)
    {
        var client = new Client { CompanyName = "Acme", ContactName = "Jane", Email = email, Phone = "+1234567890", ShippingAddress = "1 Main St", UserId = userId };
        db.Clients.Add(client);
        db.SaveChanges();
        return client;
    }

    public static Order AddOrder(this OrchestratorDbContext db, int catalogId, int? clientId = null)
    {
        var order = new Order { ClientId = clientId ?? db.AddClient($"c{Guid.NewGuid():N}@test.local").Id, PriorityId = SystemIds.NormalPriority };
        order.Items.Add(new OrderItem { CatalogId = catalogId, CatalogSku = "SKU", CatalogName = "Catalog", Quantity = 1 });
        db.Orders.Add(order);
        db.SaveChanges();
        return order;
    }

    public static void AddChain(this OrchestratorDbContext db, Catalog catalog, params int[] departmentIds)
    {
        var chain = new ApprovalChain { Name = $"Chain {catalog.Sku}", CatalogId = catalog.Id };
        chain.Stages.AddRange(departmentIds.Select((d, i) => new ApprovalChainStage { DepartmentId = d, Sequence = i + 1 }));
        db.ApprovalChains.Add(chain);
        db.SaveChanges();
    }

    public static Catalog AddCatalog(this OrchestratorDbContext db, string sku, params JobType[] steps)
    {
        var catalog = new Catalog { Sku = sku, Name = $"Catalog {sku}", BasePrice = 10m };
        catalog.CatalogJobSteps.AddRange(steps.Select((j, i) => new CatalogJobStep { JobTypeId = j.Id, Sequence = i + 1 }));
        db.Catalogs.Add(catalog);
        db.SaveChanges();
        return catalog;
    }
}
