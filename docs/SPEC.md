# Customer Support CRM — Requirements & Specification

## 1. Purpose

A Customer Support CRM that lets support agents manage customers and support
tickets end-to-end: create/track tickets, assign them to agents, see a
per-agent dashboard, and have all of this behind authentication with a basic
role boundary (Admin vs Agent).

This spec covers **only the mandatory scope** approved in
`Task Scope v1.0` (see the assessment rubric). Everything in the original
feature brief that is not listed as Mandatory (AI features, WhatsApp/Email/SMS
channels, SLA automation, knowledge base, customer portal, reports,
multi-branch/i18n, etc.) is explicitly **out of scope** for this build and is
called out below as "Not built — bonus scope."

## 2. In scope (Mandatory, from Task Scope v1.0)

| # | Capability | Acceptance evidence (from rubric) |
|---|---|---|
| 1 | Customer CRUD with validation | Create/read/update/delete customer data with validation |
| 2 | Ticket CRUD and workflow | Category, priority, status and assignment are persisted |
| 3 | Agent dashboard | Shows assigned tickets and useful customer/ticket context |
| 4 | End-to-end flow | Frontend → API → database flow works coherently |
| 5 | Authentication and authorization | Auth plus at least one role/permission boundary |
| 6 | README and architecture notes | Clear setup, run steps, assumptions, design overview |
| 7 | Tests or verification evidence | Automated tests or documented repeatable verification |

## 3. Explicitly out of scope (bonus only, not built)

AI features (summaries/suggested replies/chatbot), Email/WhatsApp/SMS/live
chat channels, SLA & automation, Knowledge base, Customer self-service portal,
Reports & management dashboards beyond the agent dashboard, ERP/external
integrations, multi-branch/multi-department, Arabic localization, custom
branding. These are not attempted so that 100% of the effort goes toward a
solid, well-tested mandatory scope rather than a shallow pass over everything.

## 4. Actors & roles

- **Admin** — manages agents (create agent accounts), manages all customers
  and tickets, can assign/reassign any ticket, sees all tickets.
- **Agent** — manages customers, creates/updates tickets, can only be
  assigned tickets by an Admin (or self-assign an unassigned ticket), sees
  their own dashboard of assigned tickets. Cannot manage other users.

This is the permission boundary required by capability #5.

## 5. Domain model

**Customer**
- Id (guid), FullName, Email (unique), Phone, Company (optional), Notes
  (optional), CreatedAt, UpdatedAt

**Ticket**
- Id (guid), Subject, Description, CustomerId (FK), Category (enum:
  General, Billing, Technical, Account, Other), Priority (enum: Low, Medium,
  High, Urgent), Status (enum: Open, InProgress, Waiting, Resolved, Closed),
  AssignedAgentId (FK to User, nullable), CreatedAt, UpdatedAt

**TicketActivity** (append-only history — satisfies "ticket history" /
"interaction history" and gives the dashboard real content)
- Id, TicketId (FK), Type (Created, StatusChanged, Assigned, CommentAdded),
  Description, CreatedByUserId, CreatedAt

**ApplicationUser** (ASP.NET Core Identity)
- Id, FullName, Email, Role (Admin | Agent)

## 6. Validation rules (mandatory capability #1/#2)

- Customer: FullName required (2–200 chars), Email required + valid format +
  unique, Phone optional but validated format when present.
- Ticket: Subject required (3–200 chars), Description required, Category and
  Priority required (must be a valid enum value), CustomerId must reference
  an existing customer, Status transitions are constrained (e.g. cannot move
  from `Closed` back to `Open` directly — must reopen explicitly) — this is
  the "workflow" part of capability #2, not just free-form CRUD.

## 7. API surface (high level — see PLAN.md for build order)

- `POST /api/auth/login`, `POST /api/auth/register` (Admin-only, creates
  Agent accounts)
- `GET/POST/PUT/DELETE /api/customers`, `/api/customers/{id}`
- `GET/POST/PUT /api/tickets`, `/api/tickets/{id}`,
  `POST /api/tickets/{id}/assign`, `POST /api/tickets/{id}/status`
- `GET /api/tickets/{id}/activity`
- `GET /api/dashboard/me` (current agent's assigned tickets + counts)

## 8. Non-functional / delivery constraints

- Runnable locally with only the .NET SDK (no external DB server, no Node.js)
  — SQLite via EF Core. This is an environment constraint (see
  `docs/AI_USAGE.md` / README "Assumptions") not a product requirement, and
  is documented as such.
- Passwords hashed via ASP.NET Core Identity, never logged or returned by
  any API response.
- JWT-based auth for the API; Blazor WebAssembly client stores the token and
  attaches it to requests.

## 9. Definition of done for mandatory scope

- All 7 mandatory capabilities above demonstrable end-to-end through the UI.
- `dotnet test` passes with tests covering: ticket status-transition rules,
  customer validation, and at least one API integration test per resource.
- README lets a new developer clone, run migrations, seed data, and log in
  within a few commands.
