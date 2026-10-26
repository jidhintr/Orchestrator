using System.ComponentModel.DataAnnotations;
using Orchestrator.Domain;

namespace Orchestrator.Application.Categories;

public sealed record CategoryRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string Name { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }
}

public sealed record CategoryResponse(int Id, string Name, string? Description, bool IsActive)
{
    public static CategoryResponse From(Category c) => new(c.Id, c.Name, c.Description, c.IsActive);
}
