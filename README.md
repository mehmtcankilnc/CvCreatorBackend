# CvCreator Backend

> **Archived.** This repository is no longer maintained and is not deployed. The CvCreator mobile app has moved to Supabase and generates PDFs on the device, so this server is no longer needed. The active project is [CvCreator](https://github.com/mehmtcankilnc/CvCreator). This code is kept for reference only.

CvCreator Backend was the REST API behind the CvCreator mobile app, a resume and cover letter builder. It handled authentication, stored users' documents in PostgreSQL, and rendered resumes and cover letters to PDF on the server.

## Why it was retired

The API ran on a self-hosted VPS. When the server was shut down, the app was migrated to a serverless setup: Supabase (Postgres, Auth, Row Level Security) for data and accounts, and on-device HTML-to-PDF rendering in place of server-side Playwright. That removed the need to run and pay for a server. Data from this backend was not migrated.

## What it did

- **Authentication**: Google sign-in and anonymous (guest) sign-in, issuing short-lived JWT access tokens with refresh-token support
- **Resumes and cover letters**: create, list, fetch, update, and delete, with form data stored as JSONB
- **PDF generation**: Handlebars HTML templates (classic, minimal, modern, vertical, and cover letter) rendered to PDF with headless Chromium through Playwright
- **Account deletion**: remove a user and their documents
- **Operational features**: rate limiting, health checks, structured logging, tracing, and metrics

## Tech stack

| Area | Choice |
|---|---|
| Framework | ASP.NET Core 8 (Web API, controllers) |
| Language | C# on .NET 8 |
| Database | PostgreSQL via Entity Framework Core 8 and Npgsql (JSONB for form values) |
| Identity | ASP.NET Core Identity + JWT bearer tokens, Google ID token validation (`Google.Apis.Auth`) |
| Templates and PDF | Handlebars.Net + Microsoft Playwright (Chromium) |
| Mapping | AutoMapper |
| API versioning | Asp.Versioning (URL segment, `/api/v1/...`) |
| Logging | Serilog (console and rolling JSON files) |
| Observability | OpenTelemetry (traces over OTLP to Jaeger, Prometheus metrics), Grafana |
| Security | NetEscapades security headers, HSTS, HTTPS redirection, rate limiting |
| Containers | Docker (Playwright base image) and Docker Compose for the observability stack |

## Architecture

The solution follows a layered (clean architecture) layout:

```
CvCreator.API/             # Controllers, middleware, rate limiting, Program.cs, Dockerfile
CvCreator.Application/     # Service contracts, DTOs, AutoMapper profile, Result model
CvCreator.Domain/          # Entities (AppUser, Resume, CoverLetter) and form value models
CvCreator.Infrastructure/  # EF Core DbContext and migrations, Identity, services, HTML templates
docker-compose.yml         # Jaeger, Prometheus, Grafana
prometheus.yml             # Prometheus scrape config
```

Dependencies point inward: the API depends on Application, Infrastructure implements the Application contracts, and Domain has no dependencies.

### Notable decisions

- **Result pattern.** Services return a `Result` object instead of throwing for expected failures, and a global exception middleware turns anything unexpected into a consistent error response.
- **Two rate-limit policies.** `StandardTraffic` is a per-user (or per-IP) token bucket for ordinary calls. `HeavyResource` is a global concurrency limit for PDF rendering and document writes, which protects the machine from too many simultaneous Chromium instances.
- **Singleton Playwright.** One Playwright instance is created at startup and shared by scoped PDF services.
- **Guest accounts.** Anonymous login creates a real user, so guests go through the same ownership checks as signed-in users.
- **Layered observability.** Requests are traced with OpenTelemetry, metrics are exposed on a Prometheus scraping endpoint, and logs carry trace and span IDs through Serilog enrichment.

## API overview

All routes are prefixed with `/api/v1`.

| Method | Route | Purpose |
|---|---|---|
| POST | `/auth/google-login` | Sign in with a Google ID token |
| POST | `/auth/anonymous-login` | Create a guest session |
| POST | `/auth/refresh-token` | Exchange a refresh token for new tokens |
| GET | `/auth/user` | Current user details |
| POST | `/resumes` | Create a resume |
| GET | `/resumes` | List the user's resumes |
| GET | `/resumes/{id}` | Fetch one resume |
| GET | `/resumes/download/{id}` | Render and download the resume as PDF |
| PUT | `/resumes/{id}` | Update a resume |
| DELETE | `/resumes/{id}` | Delete a resume |
| POST, GET, PUT, DELETE | `/coverletters`, `/coverletters/{id}`, `/coverletters/download/{id}` | Same operations for cover letters |
| DELETE | `/users` | Delete the account |
| GET | `/health` | Health check (PostgreSQL) |

## Running it (for reference)

This project is archived, so these steps are provided for reference and are not guaranteed to work against current dependencies.

Prerequisites: .NET SDK 8.0.413 (pinned in `global.json`), PostgreSQL, and Docker (optional).

1. Provide configuration (for example with user secrets or environment variables):
   - `ConnectionStrings:Postgres`
   - `JwtSettings:Issuer`, `JwtSettings:Audience`, `JwtSettings:Secret`
   - Google OAuth client ID for ID token validation
   - `AutoMapper:LicenseKey`
2. Apply the database migrations:
   ```bash
   dotnet ef database update --project CvCreator.Infrastructure --startup-project CvCreator.API
   ```
3. Install the Playwright browsers once after building (`playwright install chromium`).
4. Run the API:
   ```bash
   dotnet run --project CvCreator.API
   ```
5. Optionally start Jaeger, Prometheus, and Grafana:
   ```bash
   docker compose up -d
   ```

The API also ships with a Dockerfile based on the official Playwright .NET image.

## Related

- [CvCreator](https://github.com/mehmtcankilnc/CvCreator): the React Native app that replaces this backend
