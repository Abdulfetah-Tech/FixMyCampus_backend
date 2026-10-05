using System.ComponentModel.DataAnnotations;

namespace FixMyCampus.Api.DTOs;

public class AssignTicketRequest
{
    [Required]
    public int TechnicianId { get; set; }
}
