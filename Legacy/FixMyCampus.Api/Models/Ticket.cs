using FixMyCampus.Api.Enums;

namespace FixMyCampus.Api.Models;

public class Ticket
{
    public int Id { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Building { get; set; } = string.Empty;

    public string Room { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.New;

    public Urgency Urgency { get; set; }

    public int ReporterId { get; set; }

    public int? TechnicianId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Reporter { get; set; } = null!;

    public User? Technician { get; set; }

    public ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
}