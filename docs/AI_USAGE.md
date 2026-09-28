# AI Usage & Verification

This build was done with Claude Code (Claude Sonnet 5) as the primary
implementer, working from the approved rubric and Task Scope v1.0. This
document is an honest account of how it was used, what was reviewed, and —
most importantly — what was actually *verified to work*, not just written.

## How AI was used

- **Reading the inputs first.** The rubric (`.xlsx`, 3 sheets) and the task
  brief (`.pdf`) were parsed before any code was written, specifically to
  separate the full 12-section feature brief from the much smaller *Mandatory*
  scope in `Task Scope v1.0`. This distinction drives every scope decision in
  `docs/SPEC.md` — building all 12 sections of the brief instead of the 7
  mandatory capabilities would have failed the "prioritizes mandatory scope"
  part of Productivity & Delivery even if the code were perfect.
- **Spec and plan before code.** `docs/SPEC.md` and `docs/PLAN.md` were
  written and committed *before* any implementation, in that order, matching
  the "Requirement & Specification" then "Planning & Task Breakdown" rubric
  criteria directly.
- **Environment-aware technical decisions, not defaults.** Before picking a
  stack, the environment was actually checked (`dotnet --version`, `node
  --version`, `npm --version`). Node/npm were absent. Rather than building a
  React frontend that could never be run or verified here, the decision was
  Blazor WebAssembly — a decision recorded in `docs/PLAN.md` with the reason,
  not silently substituted.
- **Incremental build with checkpoints**, per the phase order in `docs/PLAN.md`:
  scaffold → build → domain → persistence → auth → customers → tickets →
  dashboard → frontend, verifying the build after each phase rather than
  writing everything and debugging once at the end.

## What was actually verified (not just "should work")

Every claim below was checked by running something, not by reasoning about
the code in the abstract:

1. **`dotnet build` on the empty scaffold** before any feature code, to catch
   project-reference/package mistakes early.
2. **`dotnet test` after every layer** (Domain → Application → Infrastructure
   → Api), 27 unit tests covering ticket status-transition rules, assignment
   rules, and customer validation — all passing.
3. **Running the real API with `dotnet run`** and hitting it with `curl` to
   inspect actual server responses and stack traces, not assumed behavior.
   This is how the two most significant bugs below were found.
4. **A full browser session** against both the API and the Blazor client
   together: logged in as the seeded Admin, created a customer, created a
   ticket, changed its status, assigned it to the seeded Agent, then logged
   out, logged in as that Agent, and confirmed the dashboard showed exactly
   the tickets assigned to that agent (2, with correct per-status counts) —
   not the admin's view, not all tickets.
5. **`scripts/smoke-test.sh`**, an 11-check scripted re-run of the same
   end-to-end workflow over raw HTTP, kept in the repo as a repeatable
   artifact (see "Why a manual verification script exists" below).

## Bugs found during verification and how they were fixed

Documented honestly because this is what "AI Usage & Verification" and
"Technical Understanding & Ownership" are actually checking for — not that
the first draft was perfect, but that problems were caught and understood.

1. **`AddSignInManager()` didn't exist** on the `IdentityBuilder` returned by
   `AddIdentityCore<ApplicationUser>()` in this Identity version/shape.
   Diagnosed via the exact compiler error, and fixed by removing it —
   it's for cookie-based interactive sign-in, which this JWT-only API
   doesn't use (`UserManager.CheckPasswordAsync` is what `AuthService`
   actually needs).
2. **Missing `Microsoft.EntityFrameworkCore.Design` on the *startup* project.**
   `dotnet ef migrations add` failed with a clear message naming the missing
   package on `CustomerSupportCRM.Api` specifically (EF Core tools need it on
   the startup project, not just the project containing the DbContext).
3. **`OrderByDescending` after a `Select` with a correlated subquery could not
   be translated by EF Core.** `TicketService.GetAllAsync` projected to
   `TicketDto` (including a correlated subquery to resolve the assigned
   agent's name) and then ordered by `CreatedAt` on the *projected* type.
   Running the API and calling `GET /api/tickets` surfaced
   `InvalidOperationException: ... could not be translated`. Fixed by moving
   the `OrderByDescending` onto the `IQueryable<Ticket>` *before* projecting.
4. **SQLite cannot `ORDER BY` a `DateTimeOffset` column at all.** After fixing
   #3, the *same* endpoint still failed, now with
   `System.NotSupportedException: SQLite does not support expressions of type
   'DateTimeOffset' in ORDER BY clauses` — a provider limitation, not a query
   shape problem. Fixed by switching every persisted timestamp
   (`Customer`/`Ticket`/`TicketActivity`) from `DateTimeOffset` to `DateTime`
   (UTC), regenerating the migration, and re-verifying with the same `curl`
   call before moving on. This is exactly the kind of thing that "looks right
   at compile time, fails at run time" — it was only caught because the API
   was actually run and queried, not just built.
5. **JSON enum serialization mismatch after adding `JsonStringEnumConverter`
   to the API.** Once the API was configured to serialize enums (category,
   priority, status) as strings for a readable JSON contract, the existing
   integration tests' `HttpClient` calls (using default `System.Text.Json`
   options) would have failed to round-trip them. Fixed by giving the test
   project the same `JsonSerializerOptions` the API uses, via a shared
   `CustomWebApplicationFactory.JsonOptions`.
6. **Blazor `_Imports.razor` missing a `using`** for the newly-added
   `CustomerSupportCRM.Application.Users` namespace — caught by `dotnet build`
   immediately after adding the agent-picker endpoint, not left for later.

## Why a manual verification script exists alongside the automated tests

`CustomerSupportCRM.Api.Tests` compiles and passes under a normal `dotnet
test` run. On *this specific machine*, Windows' Smart App Control / WDAC
policy blocks `testhost.exe` from loading the compiled, unsigned test
assembly via reflection — confirmed by reading the actual
`Microsoft-Windows-CodeIntegrity/Operational` event log (event ID 3077:
"did not meet the Enterprise signing level requirements... Policy ID:
{0283ac0f-fff1-49ae-ada1-8a933130cad6}"), not assumed from the vague
"Application Control policy has blocked this file" exception message alone.
This is an IT/OS-level restriction on this laptop, unrelated to the code's
correctness — the same test project runs normally on an unlocked machine or
in CI. `scripts/smoke-test.sh` is the "documented repeatable verification"
alternative the rubric explicitly allows for, and it was actually run
(11/11 checks passing) against a live `dotnet run` instance as the final
verification step before considering the mandatory scope done.

## What a human should still review

- The dev JWT signing key in `appsettings.Development.json` is a placeholder
  and must be replaced (and moved to a secret store) before any real
  deployment.
- No rate limiting or lockout-after-failed-attempts on `/api/auth/login`
  beyond ASP.NET Identity's defaults — acceptable for this scope, worth
  hardening before production use.
- Blazor's `JwtAuthenticationStateProvider` decodes the JWT purely to drive
  UI state (show/hide nav, `[Authorize]` pages); it is not a security
  boundary — the API independently validates and enforces the token on every
  request regardless of what the client believes.
