using FixMyCampus.Api.Data;
using FixMyCampus.Api.DTOs;
using FixMyCampus.Api.Enums;
using FixMyCampus.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FixMyCampus.Api.Services;

public class TicketService
{
    private const string InvalidTransitionErrorMessage = 
        "Invalid status transition. A ticket must move from New → Assigned → In Progress → Resolved.";

    private readonly AppDbContext _context;

    public TicketService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lists all tickets with optional filtering by building and status.
    /// </summary>
    public async Task<List<TicketResponse>> GetAllTicketsAsync(string? building, TicketStatus? status)
    {
        var query = _context.Tickets
            .Include(t => t.Reporter)
            .Include(t => t.Technician)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(building))
        {
            var trimmedBuilding = building.Trim().ToLower();
            query = query.Where(t => t.Building.ToLower().Contains(trimmedBuilding));
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return tickets.Select(t => MapToResponse(t, includeHistory: false)).ToList();
    }

    /// <summary>
    /// Retrieves a single ticket with full details and audit history.
    /// </summary>
    public async Task<TicketResponse?> GetTicketByIdAsync(int id)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Reporter)
            .Include(t => t.Technician)
            .Include(t => t.History)
                .ThenInclude(h => h.ChangedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        return ticket == null ? null : MapToResponse(ticket, includeHistory: true);
    }

    /// <summary>
    /// Retrieves tickets relevant to the current user (reported by them or assigned to them).
    /// </summary>
    public async Task<List<TicketResponse>> GetTicketsForUserAsync(int userId, string? userRole = null)
    {
        var query = _context.Tickets
            .Include(t => t.Reporter)
            .Include(t => t.Technician)
            .AsNoTracking()
            .AsQueryable();

        if (string.Equals(userRole, UserRole.Technician.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => t.TechnicianId == userId || t.ReporterId == userId);
        }
        else
        {
            query = query.Where(t => t.ReporterId == userId);
        }

        var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return tickets.Select(t => MapToResponse(t, includeHistory: false)).ToList();
    }

    /// <summary>
    /// Creates a new ticket. Enforces that initial status is strictly 'New' and unassigned.
    /// </summary>
    public async Task<TicketResponse> CreateTicketAsync(CreateTicketRequest request, int reporterId)
    {
        var reporterExists = await _context.Users.AnyAsync(u => u.Id == reporterId);
        if (!reporterExists)
        {
            throw new InvalidOperationException("Reporter user not found.");
        }

        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Category = request.Category.Trim(),
            Building = request.Building.Trim(),
            Room = request.Room.Trim(),
            Description = request.Description.Trim(),
            Urgency = request.Urgency,
            ReporterId = reporterId,
            TechnicianId = null,
            Status = TicketStatus.New,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Create initial history record
        var initialHistory = new TicketHistory
        {
            TicketId = ticket.Id,
            FromStatus = null,
            ToStatus = TicketStatus.New,
            ChangedById = reporterId,
            ChangedAt = now
        };
        _context.TicketHistories.Add(initialHistory);
        await _context.SaveChangesAsync();

        var created = await _context.Tickets
            .Include(t => t.Reporter)
            .Include(t => t.Technician)
            .Include(t => t.History)
                .ThenInclude(h => h.ChangedBy)
            .AsNoTracking()
            .FirstAsync(t => t.Id == ticket.Id);

        return MapToResponse(created, includeHistory: true);
    }

    /// <summary>
    /// Assigns a technician to a ticket. 
    /// Enforces the lifecycle transition: ticket MUST be in 'New' status and transitions to 'Assigned'.
    /// </summary>
    public async Task<TicketResponse?> AssignTicketAsync(int ticketId, int technicianId, int changedById)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Reporter)
            .Include(t => t.Technician)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
        {
            return null;
        }

        // Mandatory Lifecycle Rule: Can only assign when ticket is New
        if (ticket.Status != TicketStatus.New)
        {
            throw new InvalidOperationException(InvalidTransitionErrorMessage);
        }

        // Validate that technician exists and holds the Technician role
        var technician = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == technicianId && u.Role == UserRole.Technician);

        if (technician == null)
        {
            throw new InvalidOperationException("Technician not found or user is not a technician.");
        }

        var now = DateTime.UtcNow;
        ticket.TechnicianId = technician.Id;
        ticket.Status = TicketStatus.Assigned;
        ticket.UpdatedAt = now;

        var history = new TicketHistory
        {
            TicketId = ticket.Id,
            FromStatus = TicketStatus.New,
            ToStatus = TicketStatus.Assigned,
            ChangedById = changedById,
            ChangedAt = now
        };

        _context.TicketHistories.Add(history);
        await _context.SaveChangesAsync();

        // Reload technician navigation reference
        await _context.Entry(ticket).Reference(t => t.Technician).LoadAsync();

        return MapToResponse(ticket, includeHistory: false);
    }

    /// <summary>
    /// Updates the status of an existing ticket.
    /// Strictly validates that the transition follows: Assigned → InProgress → Resolved.
    /// Any other transition or backward jump throws an InvalidOperationException (HTTP 400).
    /// </summary>
    public async Task<TicketResponse?> UpdateStatusAsync(int ticketId, TicketStatus newStatus, int changedById)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Reporter)
            .Include(t => t.Technician)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
        {
            return null;
        }

        // 1. Same status is not a valid progression
        if (ticket.Status == newStatus)
        {
            throw new InvalidOperationException($"Ticket is already in '{ticket.Status}' status.");
        }

        // 2. Server-side validation of mandatory lifecycle transitions:
        //    - From Assigned -> InProgress
        //    - From InProgress -> Resolved
        //    Any other transition is illegal!
        if (!IsValidStatusTransition(ticket.Status, newStatus))
        {
            throw new InvalidOperationException(InvalidTransitionErrorMessage);
        }

        var previousStatus = ticket.Status;
        var now = DateTime.UtcNow;

        ticket.Status = newStatus;
        ticket.UpdatedAt = now;

        var history = new TicketHistory
        {
            TicketId = ticket.Id,
            FromStatus = previousStatus,
            ToStatus = newStatus,
            ChangedById = changedById,
            ChangedAt = now
        };

        _context.TicketHistories.Add(history);
        await _context.SaveChangesAsync();

        return MapToResponse(ticket, includeHistory: false);
    }

    /// <summary>
    /// Retrieves chronological status transition history for a ticket.
    /// </summary>
    public async Task<List<TicketHistoryResponse>?> GetTicketHistoryAsync(int ticketId)
    {
        var ticketExists = await _context.Tickets.AnyAsync(t => t.Id == ticketId);
        if (!ticketExists)
        {
            return null;
        }

        var history = await _context.TicketHistories
            .Include(h => h.ChangedBy)
            .Where(h => h.TicketId == ticketId)
            .OrderBy(h => h.ChangedAt)
            .AsNoTracking()
            .ToListAsync();

        return history.Select(MapHistoryToResponse).ToList();
    }

    /// <summary>
    /// Retrieves list of all available technicians.
    /// </summary>
    public async Task<List<TechnicianResponse>> GetTechniciansAsync()
    {
        var users = await _context.Users
            .Where(u => u.Role == UserRole.Technician)
            .OrderBy(u => u.Name)
            .AsNoTracking()
            .ToListAsync();

        return users.Select(u => new TechnicianResponse
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email
        }).ToList();
    }

    /// <summary>
    /// Lifecycle Validator:
    /// Status changes via status update endpoint must follow:
    /// Assigned → InProgress → Resolved
    /// (Note: New → Assigned is performed through technician assignment).
    /// </summary>
    private static bool IsValidStatusTransition(TicketStatus current, TicketStatus target)
    {
        return (current, target) switch
        {
            (TicketStatus.Assigned, TicketStatus.InProgress) => true,
            (TicketStatus.InProgress, TicketStatus.Resolved) => true,
            _ => false
        };
    }

    private static TicketResponse MapToResponse(Ticket ticket, bool includeHistory)
    {
        var response = new TicketResponse
        {
            Id = ticket.Id,
            Category = ticket.Category,
            Building = ticket.Building,
            Room = ticket.Room,
            Description = ticket.Description,
            Status = ticket.Status.ToString(),
            Urgency = ticket.Urgency.ToString(),
            ReporterName = ticket.Reporter?.Name ?? string.Empty,
            TechnicianName = ticket.Technician?.Name,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt
        };

        if (includeHistory && ticket.History != null)
        {
            response.History = ticket.History
                .OrderBy(h => h.ChangedAt)
                .Select(MapHistoryToResponse)
                .ToList();
        }

        return response;
    }

    private static TicketHistoryResponse MapHistoryToResponse(TicketHistory h)
    {
        return new TicketHistoryResponse
        {
            Id = h.Id,
            TicketId = h.TicketId,
            FromStatus = h.FromStatus?.ToString(),
            ToStatus = h.ToStatus.ToString(),
            ChangedByName = h.ChangedBy?.Name ?? "System",
            ChangedAt = h.ChangedAt
        };
    }
}
