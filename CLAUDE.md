# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

ASP.NET Core 9 REST API + React 19 frontend. Task & project management with Kanban board. PostgreSQL + Redis + JWT + ASP.NET Identity. Full sub-document reference: `.claude/` — read those before writing any code.

## Commands

```bash
# Full stack (Docker)
make up           # build + start web, api, postgres, redis
make down         # stop (volumes preserved)
make reset        # wipe all data and restart
make migrate      # run EF Core migrations inside container
make logs         # tail all logs
make api-logs     # API only

# Tests (run on host, no Docker needed)
make test
dotnet test --filter "FullyQualifiedName~ClassName"       # single class
dotnet test --filter "FullyQualifiedName~MethodName"      # single test

# React (host)
make web-install  # npm install (first time only)
cd taskflow-web && npm run dev    # local dev server (proxies /api → localhost:5000)
```

**URLs after `make up`:** `http://localhost:3000` (app) · `http://localhost:5000/swagger` (Swagger)

## Architecture

```
TaskFlow.Api/            HTTP only — controllers, middleware, background services
TaskFlow.Application/    Business logic — services, DTOs, validators, interfaces
TaskFlow.Infrastructure/ EF Core, Redis, repository implementations
TaskFlow.Tests/          NUnit 4 unit tests (mocked — no live DB/Redis)
taskflow-web/            React 19 + Vite + TypeScript (Tailwind, TanStack Query, Zustand)
```

Dependency rule: `Api → Application ← Infrastructure`. Infrastructure never references Api.

## Critical EF Core Rule — Navigation Properties on Untracked Entities

**Always use `db.Entry(entity).State = EntityState.Modified` instead of `db.Update(entity)` or `db.UpdateRange()`** when the entity was loaded with `AsNoTracking()` and has navigation properties set.

`db.Update(entity)` calls `AttachGraph`, which traverses all loaded navigation properties and either:
- Tries to INSERT the related entity if it looks new (causes PK violation)
- Performs relationship fixup that overwrites your FK changes with the navigation property's PK

`db.Entry(entity).State = Modified` marks only the root entity, skips graph traversal, and uses scalar FK values as-is. All repository `UpdateAsync` / `UpdateRangeAsync` methods use this pattern.

**Cursor pagination sort key**: The `GetPagedByColumnAsync` cursor encodes `(int, Guid)` as `"int_guid"` in base64. The int is the primary sort field (`Number`). If you ever change the sort key, existing client cursors silently decode as the new field — tasks will be skipped or duplicated for in-flight sessions. Add a version prefix to the cursor string if the sort key changes again.

## Exception Hierarchy → HTTP Status

| Exception | Status |
|-----------|--------|
| `BadRequestException` | 400 |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `ForbiddenException` | 403 |
| `UnauthorizedException` | 401 |

All live in `TaskFlow.Application/Exceptions/AppExceptions.cs`. `GlobalExceptionMiddleware` maps them. Zero try/catch in controllers or services.

## Backend Patterns (quick ref)

- **Controllers** are thin: validate (auto via `AddFluentValidationAutoValidation`), call service, return result. No business logic, no EF, no try/catch.
- **Services** own business logic, call repositories, log with structured properties (`{PropName}`, never `$"..."`), return DTOs. No EF access.
- **Repositories** own DB access with `AsNoTracking()` on reads. Use `db.Entry(entity).State` for updates (see above).
- **AutoMapper** via `Profile` classes with `ConstructUsing` — see `.claude/10_automapper.md`. Never set navigation properties on a new entity before `db.Add()` — assign them after save for mapping purposes only.
- **User ID in controllers**: `User.GetUserId()` extension — never inline `FindFirstValue(ClaimTypes.NameIdentifier)`.
- **Workspace RBAC**: `[Authorize(Policy = WorkspacePolicies.Member/Admin/Owner)]` — role resolved per-request from DB by `WorkspaceRoleHandler`, not embedded in JWT.

## Frontend Patterns (quick ref)

```
src/api/          All HTTP calls (never fetch/axios in components or hooks)
src/hooks/        React Query hooks wrapping api/ calls
src/components/   UI only — no API calls
src/pages/        Route-level components
src/store/        Zustand (authStore — JWT persisted via localStorage)
src/types/        TypeScript interfaces mirroring backend DTOs
```

- `apiClient` in `src/api/client.ts` — Axios instance with JWT interceptor and 401 → refresh flow.
- Mutations invalidate query cache on success via `useQueryClient().invalidateQueries`.
- Auth state: `useAuthStore` — `user`, `accessToken`, `refreshToken`. After any profile change that modifies the JWT payload, call `setAuth` to update the store.

## AppUser Fields

`AppUser : IdentityUser` adds: `DisplayName` (string), `CreatedAt` (DateTime), `AvatarColor` (string, hex — added for profile feature). `IdentityUser` provides `Id`, `Email`, `UserName`, `PasswordHash`, etc. Use `UserManager<AppUser>` for all user mutations — never write directly to `AspNetUsers` via EF.
