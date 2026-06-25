# Features Breakdown

High-level map of every implemented feature, its status, and where to find its documentation.
Update this file whenever a new feature is added or an existing one changes scope.

---

## Implemented

| Feature              | Status      | Doc                                              | Base route                                    |
|----------------------|-------------|--------------------------------------------------|-----------------------------------------------|
| Auth                 | Complete    | [auth.md](auth.md)                               | `POST /api/v1/auth/*`                         |
| Workspaces           | Complete    | [workspaces.md](workspaces.md)                   | `/api/v1/workspaces`                          |
| Workspace Members    | Complete    | [workspace-members.md](workspace-members.md)     | `/api/v1/workspaces/{id}/members`             |
| Workspace Columns    | Complete    | [workspace-columns.md](workspace-columns.md)     | `/api/v1/workspaces/{id}/columns`             |

---

## Planned

| Feature        | Notes                                                                 |
|----------------|-----------------------------------------------------------------------|
| Tasks          | Created inside a workspace, assigned to a column; CRUD + file attach  |
| File Attachments | Upload to blob storage; server-generated filenames                  |
| Task Comments  | Threaded comments per task                                            |
| Task Assignments | Assign a task to one or more workspace members                      |

---

## Role Matrix (current)

Summarises who can do what across all implemented features.

| Action                        | Owner | Admin | Member |
|-------------------------------|:-----:|:-----:|:------:|
| Register / Login / Refresh    | ✓     | ✓     | ✓      |
| List own workspaces           | ✓     | ✓     | ✓      |
| Create workspace              | ✓     | ✓     | ✓      |
| View workspace                | ✓     | ✓     | ✓      |
| Rename workspace              | ✓     | ✗     | ✗      |
| Delete workspace              | ✓     | ✗     | ✗      |
| List members                  | ✓     | ✓     | ✓      |
| Add / remove / change member  | ✓     | ✓     | ✗      |
| List columns                  | ✓     | ✓     | ✓      |
| Create / rename / reorder / delete column | ✓ | ✓ | ✗   |

---

## Architecture Notes

- All business logic lives in **Services** (`TaskFlow.Application/Services/`).
- All DB access lives in **Repositories** (`TaskFlow.Infrastructure/Repositories/`).
- Authorization is enforced by `WorkspaceRoleHandler` via route-param `workspaceId`.
- Errors are normalised to `ProblemDetails` by `GlobalExceptionMiddleware`.
- Validation uses FluentValidation with auto-discovery — no manual registration per validator.
