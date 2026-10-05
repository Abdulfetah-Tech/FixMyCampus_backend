# FixMyCampus Backend

FixMyCampus backend is the server-side application for a campus issue reporting and maintenance tracking system. It manages user authentication, ticket workflows, technician/admin operations, and operational dashboards for reporting and resolving facilities issues across a campus.

## Overview

This repository is a .NET-based backend API built with ASP.NET Core. It follows a clean layered architecture:

- FixMyCampus.Api — ASP.NET Core Web API
- FixMyCampus.Application — application services, DTOs, and interfaces
- FixMyCampus.Domain — domain entities, enums, and core business rules
- FixMyCampus.Infrastructure — EF Core, Identity, JWT, and infrastructure services

## Key Features

- User registration and login
- JWT-based authentication with refresh-token support
- Secure HTTP-only auth cookies
- Role-based authorization for Reporter, Technician, and Admin roles
- Ticket creation and management
- Ticket assignment to technicians
- Status updates and resolution workflow
- Confirmation of fix by reporters
- Similar ticket detection based on building, room, and category
- Dashboard summaries for campus maintenance operations
- Lookup endpoints for campus metadata
- PostgreSQL persistence with Entity Framework Core
- OpenAPI and Scalar API documentation in development

## Architecture

The solution is organized as follows:

```text
FixMyCampus_backend/
├── FixMyCampus.Api/
│   ├── Controllers/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── FixMyCampus.Api.csproj
│   └── FixMyCampus.Api.http
├── FixMyCampus.Application/
│   ├── Common/
│   ├── DTOs/
│   ├── Interfaces/
│   └── FixMyCampus.Application.csproj
├── FixMyCampus.Domain/
│   ├── Common/
│   ├── Entities/
│   ├── Enums/
│   ├── Rules/
│   └── FixMyCampus.Domain.csproj
├── FixMyCampus.Infrastructure/
│   ├── Identity/
│   ├── Persistence/
│   ├── Services/
│   ├── DependencyInjection.cs
│   └── FixMyCampus.Infrastructure.csproj
├── FixMyCampus.slnx
├── Legacy/
└── README.md
```

## Domain Model

The core business model centers on campus maintenance workflows:

- AppUser — application user and identity account
- Technician — specialized user profile for maintenance work
- Ticket — issue or request raised for a building/room
- Building — campus-building reference
- TicketAttachment — file attachments linked to a ticket
- TicketStatusHistory — change history for the ticket lifecycle
- RefreshToken — session refresh token management

Key ticket fields include:

- category
- building
- room
- description
- urgency
- status
- reporter
- assigned technician
- timestamps for assignment, resolution, and confirmation

## API Controllers

The API exposes the following controller groups:

- AuthController
  - register
  - login
  - refresh token
  - logout
  - current user profile

- TicketsController
  - create ticket
  - fetch ticket feed
  - fetch user-owned tickets
  - get ticket by ID
  - find similar tickets
  - assign ticket
  - update ticket status
  - confirm ticket fix

- UsersController
  - user-related operations and approval workflows

- DashboardController
  - summary and dashboard metrics

- LookupsController
  - lookup data such as building/category metadata

## Technology Stack

- ASP.NET Core 10
- C#
- Entity Framework Core
- PostgreSQL
- ASP.NET Core Identity
- JWT bearer authentication
- Scalar.AspNetCore for API docs
- Newtonsoft-style JSON enum handling via System.Text.Json converters

## Prerequisites

Before running the project, ensure you have:

- .NET 10 SDK
- PostgreSQL database server
- Access to a valid database connection string
- Proper JWT configuration values

## Configuration

The application reads configuration from `FixMyCampus.Api/appsettings.json` and expects values such as:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=...;Username=...;Password=..."
  },
  "Jwt": {
    "Issuer": "FixMyCampus.Api",
    "Audience": "FixMyCampus.Angular",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  },
  "AuthCookies": {
    "Secure": true,
    "SameSite": "None"
  }
}
```

Important configuration notes:

- `ConnectionStrings:DefaultConnection` must be set for EF Core to connect to PostgreSQL.
- JWT settings must be valid for token generation and validation.
- The app expects a proper web client origin for CORS, such as `http://localhost:4200`.

## Run Locally

From the repository root:

```bash
dotnet restore
dotnet build
dotnet run --project FixMyCampus.Api
```

The application starts the backend API and, in development mode, exposes OpenAPI/Scalar documentation.

## Authentication Flow

The system uses JWT bearer authentication and stores tokens in HTTP-only cookies:

- `access_token` — short-lived access token
- `refresh_token` — refresh token to renew the session

The `AuthController` handles:

- registration
- login
- token refresh
- logout
- current-user retrieval

## Authorization Model

The app uses role-based authorization:

- Reporter — can report issues and track their own tickets
- Technician — can work on assigned maintenance tickets
- Admin — can manage ticket assignment and workflow

## Database and Persistence

The infrastructure layer configures:

- PostgreSQL via `UseNpgsql`
- ASP.NET Core Identity with roles
- AppDbContext for EF Core data access
- service registration for JWT, auth, user approval, ticket, lookup, and dashboard-related functionality

## Notes

This repository includes a `Legacy` folder, which suggests an earlier or transitional implementation used during design or migration. The active project appears to be the current `FixMyCampus.Api` solution structure.

## License

No explicit license file was found in the repository. If you plan to publish the project publicly, consider adding a proper LICENSE file.

## Contributing

1. Create a feature branch.
2. Make your changes in a focused commit.
3. Build and validate the backend locally.
4. Open a pull request with a clear explanation of the update.

## Contact

For repository or project questions, reach out through the repository owner or project maintainers.
