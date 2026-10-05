using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FixMyCampus.Api.Data;
using FixMyCampus.Api.DTOs;
using FixMyCampus.Api.Enums;
using FixMyCampus.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FixMyCampus.Api.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public LoginResponse? Login(string email, string password)
    {
        var normalizedEmail = email.Trim();
        var user = _context.Users.FirstOrDefault(u => u.Email == normalizedEmail);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }

        var token = GenerateJwtToken(user);

        return new LoginResponse
        {
            Token = token,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString()
        };
    }

    public async Task SeedDemoUsersAsync()
    {
        if (await _context.Users.AnyAsync())
        {
            return;
        }

        var users = new[]
        {
            new User
            {
                Name = "Admin User",
                Email = "admin@hackathon.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Name = "Reporter User",
                Email = "user@hackathon.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                Role = UserRole.Reporter,
                CreatedAt = DateTime.UtcNow
            },
            new User { Name = "Ahmed Hassan", Email = "ahmed@hackathon.local", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech123!"), Role = UserRole.Technician, CreatedAt = DateTime.UtcNow },
            new User { Name = "Mohammed Ali", Email = "mohammed@hackathon.local", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech123!"), Role = UserRole.Technician, CreatedAt = DateTime.UtcNow },
            new User { Name = "Fatima Ahmed", Email = "fatima@hackathon.local", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech123!"), Role = UserRole.Technician, CreatedAt = DateTime.UtcNow }
        };

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();
    }

    public string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "default_secret_key_for_fixmycampus"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
