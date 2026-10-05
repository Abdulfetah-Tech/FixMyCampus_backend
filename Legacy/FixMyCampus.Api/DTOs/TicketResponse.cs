namespace FixMyCampus.Api.DTOs;

public class TicketResponse
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public string? TechnicianName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<TicketHistoryResponse>? History { get; set; }
}
