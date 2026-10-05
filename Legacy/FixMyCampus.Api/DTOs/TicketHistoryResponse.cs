namespace FixMyCampus.Api.DTOs;

public class TicketHistoryResponse
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
