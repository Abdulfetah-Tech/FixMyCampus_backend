using System.Security.Claims;
using System.Text;
using FixMyCampus.Application.Common;
using FixMyCampus.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// Services
// --------------------------------------------------

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);

// --------------------------------------------------
// JWT configuration
// --------------------------------------------------

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
{
    throw new InvalidOperationException(
        "Jwt:Secret is missing.");
}

if (Encoding.UTF8.GetBytes(jwtSettings.Secret).Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Secret must be at least 32 bytes.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
{
    throw new InvalidOperationException(
        "Jwt:Issuer is missing.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    throw new InvalidOperationException(
        "Jwt:Audience is missing.");
}

// --------------------------------------------------
// Authentication
// --------------------------------------------------

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSettings.Secret)),

                NameClaimType = ClaimTypes.NameIdentifier,
                RoleClaimType = ClaimTypes.Role,

                ClockSkew = TimeSpan.Zero
            };

        // JWT is stored in the HttpOnly access_token cookie.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue(
                        "access_token",
                        out var accessToken))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// --------------------------------------------------
// Authorization
// --------------------------------------------------

builder.Services.AddAuthorization();

// --------------------------------------------------
// CORS
// --------------------------------------------------
// Allows the Angular development server
// (http://localhost:4200) to call the API.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// --------------------------------------------------
// Build
// --------------------------------------------------

var app = builder.Build();

// --------------------------------------------------
// HTTP pipeline
// --------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("FixMyCampus API");
    });
}

// Redirect HTTP requests to HTTPS.
app.UseHttpsRedirection();

// Serve files from wwwroot.
app.UseStaticFiles();

// Allow Angular to call the API.
app.UseCors("AngularClient");

// Read and validate JWT.
app.UseAuthentication();

// Check [Authorize] and [Authorize(Roles = "...")].
app.UseAuthorization();

// Map API controllers.
app.MapControllers();

app.Run();