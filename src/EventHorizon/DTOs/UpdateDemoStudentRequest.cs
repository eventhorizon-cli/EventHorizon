using System.ComponentModel.DataAnnotations;

namespace EventHorizon.DTOs;

public sealed record UpdateDemoStudentRequest
{
    [Required]
    [StringLength(100)]
    public required string Name { get; init; }

    [Range(1, 120)]
    public int Age { get; init; }

    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}
