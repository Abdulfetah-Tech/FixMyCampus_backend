using FixMyCampus.Api.Enums;
using FixMyCampus.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FixMyCampus.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure database is created and any pending migrations are applied
        await context.Database.MigrateAsync();

        // 1. Seed Demo Users if none exist
        if (!await context.Users.AnyAsync())
        {
            var admin = new User
            {
                Name = "Admin User",
                Email = "admin@hackathon.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };

            var reporter = new User
            {
                Name = "Reporter User",
                Email = "user@hackathon.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                Role = UserRole.Reporter,
                CreatedAt = DateTime.UtcNow
            };

            var tech1 = new User
            {
                Name = "Ahmed Hassan",
                Email = "ahmed@hackathon.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech123!"),
                Role = UserRole.Technician,
                CreatedAt = DateTime.UtcNow
            };

            var tech2 = new User
            {
                Name = "Mohammed Ali",
                Email = "mohammed@hackathon.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech123!"),
                Role = UserRole.Technician,
                CreatedAt = DateTime.UtcNow
            };

            var tech3 = new User
            {
                Name = "Fatima Ahmed",
                Email = "fatima@hackathon.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech123!"),
                Role = UserRole.Technician,
                CreatedAt = DateTime.UtcNow
            };

            await context.Users.AddRangeAsync(admin, reporter, tech1, tech2, tech3);
            await context.SaveChangesAsync();
        }

        // 2. Seed Sample Tickets & Ticket History if none exist
        if (!await context.Tickets.AnyAsync())
        {
            var reporter = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Reporter);
            var admin = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);
            var techAhmed = await context.Users.FirstOrDefaultAsync(u => u.Email == "ahmed@hackathon.local");
            var techMohammed = await context.Users.FirstOrDefaultAsync(u => u.Email == "mohammed@hackathon.local");
            var techFatima = await context.Users.FirstOrDefaultAsync(u => u.Email == "fatima@hackathon.local");

            if (reporter != null && admin != null)
            {
                // Ticket 1: New
                var ticket1 = new Ticket
                {
                    Category = "Electrical",
                    Building = "Science Hall",
                    Room = "Lab 204",
                    Description = "Fluorescent ceiling fixtures flickering intermittently and making buzzing noises during lecture hours.",
                    Status = TicketStatus.New,
                    Urgency = Urgency.Medium,
                    ReporterId = reporter.Id,
                    TechnicianId = null,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                };

                // Ticket 2: Assigned
                var ticket2 = new Ticket
                {
                    Category = "Plumbing",
                    Building = "Library",
                    Room = "Restroom 2B",
                    Description = "Severe water pipe leak causing water pooling near the digital study stations.",
                    Status = TicketStatus.Assigned,
                    Urgency = Urgency.High,
                    ReporterId = reporter.Id,
                    TechnicianId = techAhmed?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                };

                // Ticket 3: InProgress
                var ticket3 = new Ticket
                {
                    Category = "HVAC",
                    Building = "Engineering Complex",
                    Room = "Room 105",
                    Description = "Central AC cooling unit has shut down; ambient temperature in the computer lab exceeds 32°C.",
                    Status = TicketStatus.InProgress,
                    Urgency = Urgency.High,
                    ReporterId = reporter.Id,
                    TechnicianId = techMohammed?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                    UpdatedAt = DateTime.UtcNow.AddHours(-4)
                };

                // Ticket 4: Resolved
                var ticket4 = new Ticket
                {
                    Category = "Furniture",
                    Building = "Student Center",
                    Room = "Main Lounge",
                    Description = "Damaged auditorium chair seat hinges repaired and secured.",
                    Status = TicketStatus.Resolved,
                    Urgency = Urgency.Low,
                    ReporterId = reporter.Id,
                    TechnicianId = techFatima?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                };

                // Ticket 5: New
                var ticket5 = new Ticket
                {
                    Category = "IT Support",
                    Building = "Administration Building",
                    Room = "Boardroom 301",
                    Description = "High-definition video conference display HDMI matrix switch is unresponsive.",
                    Status = TicketStatus.New,
                    Urgency = Urgency.High,
                    ReporterId = reporter.Id,
                    TechnicianId = null,
                    CreatedAt = DateTime.UtcNow.AddHours(-6),
                    UpdatedAt = DateTime.UtcNow.AddHours(-6)
                };

                await context.Tickets.AddRangeAsync(ticket1, ticket2, ticket3, ticket4, ticket5);
                await context.SaveChangesAsync();

                // Add Ticket History for tickets with state changes
                var histories = new List<TicketHistory>
                {
                    new TicketHistory
                    {
                        TicketId = ticket2.Id,
                        FromStatus = TicketStatus.New,
                        ToStatus = TicketStatus.Assigned,
                        ChangedById = admin.Id,
                        ChangedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new TicketHistory
                    {
                        TicketId = ticket3.Id,
                        FromStatus = TicketStatus.New,
                        ToStatus = TicketStatus.Assigned,
                        ChangedById = admin.Id,
                        ChangedAt = DateTime.UtcNow.AddDays(-3)
                    },
                    new TicketHistory
                    {
                        TicketId = ticket3.Id,
                        FromStatus = TicketStatus.Assigned,
                        ToStatus = TicketStatus.InProgress,
                        ChangedById = techMohammed?.Id ?? admin.Id,
                        ChangedAt = DateTime.UtcNow.AddHours(-4)
                    },
                    new TicketHistory
                    {
                        TicketId = ticket4.Id,
                        FromStatus = TicketStatus.New,
                        ToStatus = TicketStatus.Assigned,
                        ChangedById = admin.Id,
                        ChangedAt = DateTime.UtcNow.AddDays(-4)
                    },
                    new TicketHistory
                    {
                        TicketId = ticket4.Id,
                        FromStatus = TicketStatus.Assigned,
                        ToStatus = TicketStatus.InProgress,
                        ChangedById = techFatima?.Id ?? admin.Id,
                        ChangedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new TicketHistory
                    {
                        TicketId = ticket4.Id,
                        FromStatus = TicketStatus.InProgress,
                        ToStatus = TicketStatus.Resolved,
                        ChangedById = techFatima?.Id ?? admin.Id,
                        ChangedAt = DateTime.UtcNow.AddDays(-1)
                    }
                };

                await context.TicketHistories.AddRangeAsync(histories);
                await context.SaveChangesAsync();
            }
        }
    }
}
