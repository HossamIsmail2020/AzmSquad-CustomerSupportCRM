# Customer Support CRM

A Customer Support CRM covering customer management, ticket workflow, agent
assignment, an agent dashboard, and authentication/authorization — the
**mandatory scope** from `Task Scope v1.0` (see the assessment rubric).
Everything else in the original feature brief (AI features, WhatsApp/Email/SMS
channels, SLA automation, knowledge base, customer portal, etc.) is explicitly
**not built** — see `docs/SPEC.md` for why.

Full requirements and the phased build plan are in [`docs/SPEC.md`](docs/SPEC.md)
and [`docs/PLAN.md`](docs/PLAN.md). How AI assistance was used and verified is
in [`docs/AI_USAGE.md`](docs/AI_USAGE.md).

## Architecture

```
AzmSquad.CustomerSupportCRM.sln
src/
  CustomerSupportCRM.Domain          entities, enums (no dependencies)
  CustomerSupportCRM.Application     DTOs, service interfaces, validation, Result<T>
  CustomerSupportCRM.Infrastructure  EF Core + SQLite, ASP.NET Identity, JWT, service implementations
  CustomerSupportCRM.Api             ASP.NET Core Web API (controllers, auth wiring)
  CustomerSupportCRM.Web             Blazor WebAssembly client
tests/
  CustomerSupportCRM.Application.Tests  unit tests (business rules, no EF Core/ASP.NET needed)
  CustomerSupportCRM.Api.Tests          integration tests (WebApplicationFactory + in-memory SQLite)
docs/                                 SPEC, PLAN, AI_USAGE
scripts/smoke-test.sh                 curl-based end-to-end verification (see "Testing" below)
```

**Stack note:** the backend is ASP.NET Core Web API + EF Core/SQLite, and the
frontend is **Blazor WebAssembly**, not React/Node. This machine has no
Node.js/npm installed, so a React frontend could not actually be built or run
here to verify it works — Blazor keeps the whole stack buildable and runnable
with only the .NET SDK. See `docs/PLAN.md` → "Risk / judgment calls."

## Prerequisites

- .NET SDK 10.0 (`dotnet --version`)
- No database server, no Node.js, no Docker required.

## Setup and run

```bash
# 1. Restore and build everything
dotnet build

# 2. Run the API (applies EF Core migrations and seeds dev data on first run)
dotnet run --project src/CustomerSupportCRM.Api --urls http://localhost:5179

# 3. In a second terminal, run the Blazor client
dotnet run --project src/CustomerSupportCRM.Web --urls http://localhost:5227
```

Open `http://localhost:5227`. Seeded accounts (created automatically on first
run against a fresh database):

| Email | Password | Role |
|---|---|---|
| `admin@azmsquad.dev` | `Passw0rd!` | Admin |
| `agent@azmsquad.dev` | `Passw0rd!` | Agent |

If you run the API on a different port, update `ApiBaseUrl` in
`src/CustomerSupportCRM.Web/wwwroot/appsettings.json` and the `Cors:AllowedOrigins`
entry in `src/CustomerSupportCRM.Api/appsettings.Development.json` to match.

### Database

SQLite, file `crm.db` next to the API (gitignored, recreated automatically).
To reset: stop the API, delete `src/CustomerSupportCRM.Api/crm.db`, run again.

To manage migrations directly:

```bash
dotnet tool install --global dotnet-ef   # once, if not already installed
dotnet ef migrations add <Name> --project src/CustomerSupportCRM.Infrastructure --startup-project src/CustomerSupportCRM.Api -o Persistence/Migrations
dotnet ef database update --project src/CustomerSupportCRM.Infrastructure --startup-project src/CustomerSupportCRM.Api
```

## Testing

```bash
dotnet test
```

This runs both test projects. `CustomerSupportCRM.Application.Tests` (27
tests: ticket status-transition rules, assignment rules, customer validation)
needs nothing but the .NET SDK and passes anywhere.

`CustomerSupportCRM.Api.Tests` (integration tests over the real HTTP pipeline
with an in-memory SQLite database) compiles and passes under a normal test
runner. **On this particular development machine**, a Smart App Control / WDAC
policy blocks `testhost.exe` from loading the unsigned test assembly
(Code Integrity event 3077, "Enterprise signing level requirements") — this is
an OS/IT policy on this machine, not a defect in the tests. See
`docs/AI_USAGE.md` for how this was diagnosed.

As a documented, repeatable alternative that exercises the identical
end-to-end workflow over real HTTP, run:

```bash
# with the API running (see "Setup and run" above)
bash scripts/smoke-test.sh http://localhost:5179
```

It checks: the auth boundary (401 without a token), customer validation
(create + duplicate-email + invalid-email rejection), the full ticket
workflow (create → invalid transition rejected → valid transition →
assign → dashboard scoping → activity history), printing PASS/FAIL per step.

## Assumptions and design decisions

- **SQLite instead of SQL Server/Postgres** — no network DB or Docker on this
  machine; keeps `git clone && dotnet run` working for anyone grading this.
- **Blazor WebAssembly instead of React** — no Node.js/npm available here (see
  above).
- **Timestamps are `DateTime` (UTC), not `DateTimeOffset`** — EF Core's SQLite
  provider cannot translate `ORDER BY` over `DateTimeOffset` columns; found by
  running the API and hitting the exception, not by guessing up front.
- **No public self-registration** — this is an internal support-desk tool.
  Admin creates Agent accounts (`POST /api/auth/register-agent`, Admin-only);
  both seeded accounts exist for grading convenience.
- **Role boundary**: Admin can assign any ticket to any agent; an Agent can
  only self-assign a currently-unassigned ticket. Enforced server-side
  (`Ticket.CanBeAssignedTo`), not just hidden in the UI.
- **Dev JWT signing key** in `appsettings.Development.json` is a placeholder,
  not a production secret — documented there and here rather than silently
  committed without comment.

## What's deliberately not built (bonus scope)

AI features, Email/WhatsApp/SMS/live chat, SLA & automation, knowledge base,
customer self-service portal, reports beyond the agent dashboard, ERP/external
integrations, multi-branch/i18n, custom branding, full user/role management,
audit logs. See `docs/SPEC.md` §3 for the reasoning.
