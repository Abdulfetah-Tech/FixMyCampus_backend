using System.ComponentModel.DataAnnotations;
using FixMyCampus.Api.Enums;

namespace FixMyCampus.Api.DTOs;

public class UpdateStatusRequest
{
    [Required]
    public TicketStatus Status { get; set; }
}
