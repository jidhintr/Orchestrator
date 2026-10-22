using System.ComponentModel.DataAnnotations;
using Orchestrator.Application.ApprovalChains;
using Orchestrator.Application.Catalogs;
using Orchestrator.Application.Clients;
using Orchestrator.Application.Departments;
using Orchestrator.Application.JobTypes;
using Orchestrator.Application.Roles;
using Orchestrator.Application.Users;

namespace Orchestrator.UnitTests;

public class RequestValidationTests
{
    private static List<string> Invalid(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return [.. results.SelectMany(r => r.MemberNames)];
    }

    private static string Long(int length) => new('a', length);

    private static CreateUserRequest User => new() { FirstName = "Ann", LastName = "Lee", Mobile = "0123456789" };

    private static ClientRequest Client => new()
    {
        CompanyName = "Acme",
        ContactName = "Jane",
        Email = "a@acme.test",
        Phone = "+1234567890",
        ShippingAddress = "1 Main St",
    };

    private static CatalogRequest Catalog => new() { Sku = "SKU-1", Name = "Book", JobTypeIds = [1] };

    [Fact]
    public void Valid_requests_pass()
    {
        Assert.Empty(Invalid(User));
        Assert.Empty(Invalid(Client));
        Assert.Empty(Invalid(Catalog));
        Assert.Empty(Invalid(new RoleRequest { Name = "Packer", Description = "" }));
        Assert.Empty(Invalid(new CreateDepartmentRequest { Name = "Press", Code = "PRESS_1" }));
        Assert.Empty(Invalid(new JobTypeRequest { Name = "Cut" }));
        Assert.Empty(Invalid(new CreateApprovalChainRequest { Name = "Rush", CatalogId = 1, DepartmentIds = [3] }));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("")]
    [InlineData(null)]
    public void User_names_need_two_characters(string? name)
    {
        Assert.Contains("FirstName", Invalid(User with { FirstName = name! }));
        Assert.Contains("LastName", Invalid(User with { LastName = name! }));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1234")]
    [InlineData("+12 345")]
    [InlineData("12345678901234567890123")]
    public void User_mobile_must_be_digits_with_optional_plus(string mobile) =>
        Assert.Contains("Mobile", Invalid(User with { Mobile = mobile }));

    [Fact]
    public void User_email_must_be_an_address_but_is_optional()
    {
        Assert.Contains("Email", Invalid(User with { Email = "not-an-email" }));
        Assert.Empty(Invalid(User with { Email = null }));
    }

    [Fact]
    public void Update_user_requires_the_same_profile_rules() =>
        Assert.Equal(["FirstName", "Mobile"], Invalid(new UpdateUserRequest { FirstName = "A", LastName = "Lee", Mobile = "x", IsActive = true }).Distinct());

    [Fact]
    public void Assign_requests_need_their_lists()
    {
        Assert.Contains("Roles", Invalid(new AssignRolesRequest { Roles = null! }));
        Assert.Contains("DepartmentIds", Invalid(new AssignDepartmentsRequest { DepartmentIds = null! }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Role_name_needs_two_characters(string name) =>
        Assert.Contains("Name", Invalid(new RoleRequest { Name = name, Description = "x" }));

    [Fact]
    public void Role_name_and_description_have_maximum_lengths()
    {
        Assert.Contains("Name", Invalid(new RoleRequest { Name = Long(51), Description = "x" }));
        Assert.Contains("Description", Invalid(new RoleRequest { Name = "Ok", Description = Long(501) }));
    }

    [Theory]
    [InlineData("press")]
    [InlineData("PRESS ROOM")]
    [InlineData("P")]
    [InlineData("PRESS-1")]
    public void Department_code_must_be_upper_case_letters_digits_underscore(string code) =>
        Assert.Contains("Code", Invalid(new CreateDepartmentRequest { Name = "Press", Code = code }));

    [Fact]
    public void Department_name_is_required_and_limited()
    {
        Assert.Contains("Name", Invalid(new CreateDepartmentRequest { Name = "", Code = "PRESS" }));
        Assert.Contains("Name", Invalid(new UpdateDepartmentRequest { Name = Long(101) }));
        Assert.Contains("Description", Invalid(new UpdateDepartmentRequest { Name = "Ok", Description = Long(501) }));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("")]
    public void Job_type_name_needs_two_characters(string name) =>
        Assert.Contains("Name", Invalid(new JobTypeRequest { Name = name }));

    [Fact]
    public void Job_type_costs_are_bounded()
    {
        Assert.Contains("LaborCost", Invalid(new JobTypeRequest { Name = "Cut", LaborCost = -1m }));
        Assert.Contains("CommissionPercent", Invalid(new JobTypeRequest { Name = "Cut", CommissionPercent = 100.01m }));
        Assert.Contains("ProfitPercent", Invalid(new JobTypeRequest { Name = "Cut", ProfitPercent = -0.01m }));
        Assert.Empty(Invalid(new JobTypeRequest { Name = "Cut", LaborCost = 0m, CommissionPercent = 100m, ProfitPercent = 0m }));
    }

    [Fact]
    public void Catalog_sku_and_name_need_three_characters()
    {
        Assert.Contains("Sku", Invalid(Catalog with { Sku = "ab" }));
        Assert.Contains("Name", Invalid(Catalog with { Name = "ab" }));
        Assert.Contains("Sku", Invalid(new CloneCatalogRequest { Sku = "ab", Name = "Copy" }));
        Assert.Contains("Name", Invalid(new CloneCatalogRequest { Sku = "SKU-2", Name = "ab" }));
    }

    [Fact]
    public void Catalog_amounts_and_percentages_are_bounded()
    {
        Assert.Contains("BasePrice", Invalid(Catalog with { BasePrice = -1m }));
        Assert.Contains("LaborCost", Invalid(Catalog with { LaborCost = -1m }));
        Assert.Contains("CatalogDiscountPercent", Invalid(Catalog with { CatalogDiscountPercent = 101m }));
        Assert.Contains("CommissionPercent", Invalid(Catalog with { CommissionPercent = 101m }));
        Assert.Contains("ProfitPercent", Invalid(Catalog with { ProfitPercent = -1m }));
    }

    [Fact]
    public void Catalog_needs_at_least_one_job_type() =>
        Assert.Contains("JobTypeIds", Invalid(Catalog with { JobTypeIds = [] }));

    [Fact]
    public void Client_fields_are_validated()
    {
        Assert.Contains("CompanyName", Invalid(Client with { CompanyName = "A" }));
        Assert.Contains("ContactName", Invalid(Client with { ContactName = "J" }));
        Assert.Contains("Email", Invalid(Client with { Email = "nope" }));
        Assert.Contains("Phone", Invalid(Client with { Phone = "abc" }));
        Assert.Contains("Phone", Invalid(Client with { Phone = "123" }));
        Assert.Contains("ShippingAddress", Invalid(Client with { ShippingAddress = "" }));
        Assert.Contains("Tier", Invalid(Client with { Tier = (Orchestrator.Domain.ClientTier)99 }));
    }

    [Fact]
    public void Client_status_must_be_a_defined_value() =>
        Assert.Contains("Status", Invalid(new ClientStatusRequest { Status = (Orchestrator.Domain.ClientStatus)99 }));

    [Fact]
    public void Approval_chain_fields_are_validated()
    {
        Assert.Contains("Name", Invalid(new CreateApprovalChainRequest { Name = "A", CatalogId = 1, DepartmentIds = [3] }));
        Assert.Contains("CatalogId", Invalid(new CreateApprovalChainRequest { Name = "Rush", CatalogId = 0, DepartmentIds = [3] }));
        Assert.Contains("DepartmentIds", Invalid(new CreateApprovalChainRequest { Name = "Rush", CatalogId = 1, DepartmentIds = [] }));
        Assert.Contains("Name", Invalid(new UpdateApprovalChainRequest { Name = Long(81), DepartmentIds = [3] }));
        Assert.Contains("DepartmentIds", Invalid(new UpdateApprovalChainRequest { Name = "Rush", DepartmentIds = [] }));
        Assert.Contains("Description", Invalid(new UpdateApprovalChainRequest { Name = "Rush", Description = Long(251), DepartmentIds = [3] }));
    }
}
