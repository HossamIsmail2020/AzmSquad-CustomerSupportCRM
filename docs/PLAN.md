# Implementation Plan & Task Breakdown

Scope: mandatory capabilities only, per `docs/SPEC.md`. Built as one solution
with clean layering so each layer can be tested independently.

## Architecture

```
AzmSquad.CustomerSupportCRM.sln
src/
  CustomerSupportCRM.Domain          entities, enums, no dependencies
  CustomerSupportCRM.Application     DTOs, service interfaces + implementations,
                                      validation, business rules (status
                                      transitions, assignment rules)
  CustomerSupportCRM.Infrastructure  EF Core DbContext, migrations,
                                      Identity, repositories
  CustomerSupportCRM.Api             ASP.NET Core Web API, controllers,
                                      JWT auth wiring, DI composition root
  CustomerSupportCRM.Web             Blazor WebAssembly client
tests/
  CustomerSupportCRM.Application.Tests  unit tests (business rules)
  CustomerSupportCRM.Api.Tests          integration tests (WebApplicationFactory)
docs/
  SPEC.md, PLAN.md, AI_USAGE.md
```

Dependency direction: `Api` → `Application` + `Infrastructure` → `Domain`.
`Application` never references `Infrastructure` directly (only via
interfaces), so business rules are unit-testable without a database.

Why this shape: it is the minimum layering that lets (a) business rules
(ticket status transitions, validation) be unit tested without spinning up
EF Core, and (b) the API and the Blazor client both consume the same DTO
contracts, which is what capability #4 (coherent E2E flow) actually checks.
A single-project "fat controller" approach would be faster to type but fails
the Engineering Foundations criterion (separation of concerns) and makes
Testing/Security/Edge Cases harder to hit.

## Build order and why

The order below is chosen so that every step produces something runnable and
testable — never a multi-day stretch with nothing to verify.

### Phase 0 — Scaffolding
1. Create solution + 5 src projects + 2 test projects with correct references.
2. Add NuGet packages: EF Core + SQLite, Identity, JWT Bearer, xUnit,
   WebApplicationFactory, FluentValidation (or DataAnnotations — decide
   during implementation based on what stays simplest).
3. Verify `dotnet build` succeeds on the empty skeleton before writing any
   feature code.

### Phase 1 — Domain + persistence
4. Domain entities: `Customer`, `Ticket`, `TicketActivity`, enums
   (`TicketCategory`, `TicketPriority`, `TicketStatus`, `TicketActivityType`).
5. `ApplicationUser : IdentityUser` with `FullName`, `Role`.
6. `AppDbContext` (Identity + domain sets), fluent configuration (unique
   index on Customer.Email, required fields, FK constraints).
7. Initial EF Core migration, verify it applies to a local SQLite file.
8. Seed data on startup (dev only): one Admin, one Agent, a couple of sample
   customers/tickets — so the app is demoable immediately after clone.

*Checkpoint:* `dotnet ef database update` produces a working `.db` file;
inspect schema.

### Phase 2 — Auth
9. Identity + JWT configuration (issuer/audience/signing key from
   `appsettings.Development.json`, never committed as a real secret —
   documented in README).
10. `AuthController`: `POST /api/auth/login` (returns JWT + role claim),
    `POST /api/auth/register` (Admin-only, creates Agent accounts).
11. `[Authorize]` on all other controllers; `[Authorize(Roles = "Admin")]`
    on user-management and ticket-reassignment-across-agents endpoints —
    this is the role/permission boundary required by capability #5.

*Checkpoint:* can log in via Swagger/HTTP file and get a token; unauthorized
requests correctly get 401/403.

### Phase 3 — Customers (capability #1)
12. `Application`: `ICustomerService`, DTOs (`CustomerDto`,
    `CreateCustomerRequest`, `UpdateCustomerRequest`), validation rules.
13. `Infrastructure`: EF-backed implementation.
14. `Api`: `CustomersController` — full CRUD, returns 400 with field errors
    on invalid input, 404 on missing id, 409 on duplicate email.
15. Unit tests: validation edge cases (missing name, bad email, duplicate
    email). Integration test: create → get → update → delete round trip.

### Phase 4 — Tickets + workflow (capability #2)
16. `Application`: `ITicketService` with explicit status-transition rules
    (e.g. `Open → InProgress/Waiting/Closed`, `Resolved → Closed/Open`
    (reopen), `Closed` is terminal except explicit reopen) — this is the
    core "business logic" the rubric's Backend/API/Database criterion is
    checking for, not just a status string with no rules.
17. `TicketActivity` is appended automatically on create, status change, and
    (re)assignment — gives the dashboard and ticket-detail view real history.
18. `Api`: `TicketsController` — CRUD, `POST /{id}/assign`,
    `POST /{id}/status`, `GET /{id}/activity`.
19. Unit tests: every valid/invalid status transition; assignment rules
    (Agent can self-assign an unassigned ticket, cannot assign to someone
    else; Admin can assign to anyone).
20. Integration tests: full ticket lifecycle through the HTTP API.

### Phase 5 — Agent dashboard (capability #3)
21. `GET /api/dashboard/me`: current agent's assigned tickets grouped by
    status, counts, and the customer context for each (name/email) — the
    rubric explicitly asks for "useful customer/ticket context," not just a
    bare ticket list.
22. Integration test: dashboard only returns the logged-in agent's tickets
    (also proves the auth boundary works, not just that it compiles).

### Phase 6 — Frontend (capability #4)
23. Blazor WebAssembly project: typed `HttpClient` wrapping the API,
    JWT stored in browser storage, auth state provider so `[Authorize]`
    pages work client-side too.
24. Pages: Login, Customers (list/create/edit/delete), Tickets
    (list/create/edit, assign, change status, view activity), Dashboard.
25. Manual E2E pass through the running app (documented as the final
    verification step, in addition to automated tests — see README
    "Verification").

### Phase 7 — Docs & delivery hygiene
26. README: prerequisites, setup, run, seeded login credentials, assumptions,
    architecture diagram (text), how to run tests.
27. `docs/AI_USAGE.md`: how AI assistance was used during this build, what
    was reviewed/changed by hand, and what was verified (this is itself a
    graded criterion — documented honestly rather than omitted).
28. Commit history: one focused commit per phase above (not one giant
    commit), so the delivery/prioritization signal in the rubric
    (Productivity & Delivery) is visible in `git log`, not just asserted in
    prose.

## Explicit non-goals for this pass

No pagination/sorting/filtering UI polish beyond what's needed to demo the
flow; no password-reset email flow (out of scope — no email channel);
no soft-delete/audit log beyond `TicketActivity` (Security & Administration
"audit logs" module is bonus scope, not mandatory). These are called out so
they read as deliberate scope decisions, not oversights.

## Risk / judgment calls flagged up front

- **SQLite instead of SQL Server/Postgres**: environment here has no network
  DB and no Docker; SQLite keeps `git clone && dotnet run` working for
  anyone grading this. Swapping the provider later is a one-line
  `UseSqlite` → `UseSqlServer` change plus a new migration, isolated to
  `Infrastructure`.
- **Blazor WebAssembly instead of React**: this machine has no Node.js/npm
  installed, so a React build could not actually be run or verified here.
  Blazor keeps the whole stack buildable/runnable with only the .NET SDK.
- **DataAnnotations vs FluentValidation**: decided in Phase 3 based on
  whether validation rules stay simple (DataAnnotations sufficient) or need
  cross-field/async rules (FluentValidation). Documented in code comments at
  the point of decision, not here, since it's an implementation detail.
