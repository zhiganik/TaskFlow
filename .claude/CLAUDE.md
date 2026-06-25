# TaskFlow — Claude Code Context

This is the root context file. Claude Code reads this automatically on every session.
Sub-documents are in `.claude/` — read them before writing any code.

## Project in One Line
ASP.NET Core 9 REST API + React 19 frontend. Task & project management with file attachments. PostgreSQL + Redis + JWT + ASP.NET Identity. Portfolio project — every decision must be defensible to a senior engineer.

## Read These Before Writing Any Code
- `.claude/01_architecture.md` — layer rules, DI patterns, Program.cs structure
- `.claude/02_coding_standards.md` — naming, patterns, what is always/never done
- `.claude/03_models_and_db.md` — entities, EF Fluent API, indexes, migrations
- `.claude/04_auth.md` — ASP.NET Identity, JWT, RBAC policies
- `.claude/05_api_routes.md` — all routes, DTOs, Swagger annotations
- `.claude/06_infrastructure.md` — Redis, IBlobService, Channel<T>, Serilog, middleware
- `.claude/07_testing.md` — NUnit 4, Moq, FluentAssertions patterns
- `.claude/08_docker.md` — full Docker stack including React frontend, Makefile, env vars
- `.claude/09_frontend.md` — React project structure, API client, auth flow, conventions
- `docs/features-breakdown.md` — implemented features, planned features, full role matrix

## Documentation Rules (Never Skip)
After implementing any new backend feature:
1. Create `docs/<feature-name>.md` following the structure of existing docs (data model →
   authorization → endpoints with request/response tables → business rules → DTO reference).
2. Add a row to the **Implemented** table in `docs/features-breakdown.md`.
3. Remove the feature from the **Planned** table if it was listed there.

## Backend Hard Rules (Never Break)
1. **No secrets in `appsettings.json`** — env vars only, `.env` never committed
2. **Controllers are thin** — no business logic, no EF queries, no try/catch
3. **Services own logic** — no EF Core code in services, delegate to repositories
4. **Repositories own DB** — no business decisions, only query operations
5. **Every async method takes `CancellationToken ct = default`**
6. **Fluent API only** — no data annotations on entities
7. **Structured logging only** — `_logger.LogX("Message {Prop}", value)` never `$"..."`
8. **IOptions<T> for all config** — never `IConfiguration.GetValue<string>("...")`
9. **FluentValidation for all DTOs** — no manual ModelState checks in controllers
10. **ProblemDetails for all errors** — GlobalExceptionMiddleware handles everything
11. **Server-generated filenames for uploads** — never use original filename as a path

## Frontend Hard Rules (Never Break)
1. **All API calls go through `src/api/`** — never `fetch` or `axios` directly in components
2. **JWT persisted via the `authStore` zustand `persist` middleware (`localStorage`)** — deliberate choice over in-memory-only so a page reload doesn't force a re-login; mitigated by a short-lived access token and a refresh token that rotates (single-use) on every refresh
3. **No business logic in components** — components call hooks, hooks call API client
4. **TypeScript strict mode** — no `any`, no unchecked nulls
5. **`.env.local` never committed** — `VITE_*` secrets use `.env.local.example` as template
