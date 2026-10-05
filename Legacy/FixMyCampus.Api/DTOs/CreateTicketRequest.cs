using System.ComponentModel.DataAnnotations;
using FixMyCampus.Api.Enums;

namespace FixMyCampus.Api.DTOs;

public class CreateTicketRequest
{
    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Building { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Room { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public Urgency Urgency { get; set; }
}