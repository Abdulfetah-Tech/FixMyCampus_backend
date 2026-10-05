using FixMyCampus.Api.Enums;

namespace FixMyCampus.Api.Models;

public class TicketHistory
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public TicketStatus? FromStatus { get; set; }

    public TicketStatus ToStatus { get; set; }

    public int ChangedById { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public Ticket Ticket { get; set; } = null!;

    public User ChangedBy { get; set; } = null!;
}