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
| Workspace Tasks      | Complete    | [workspace-tasks.md](workspace-tasks.md)         | `/api/v1/workspaces/{id}/tasks`               |

---

## Planned

| Feature        | Notes                                                                 |
|----------------|-----------------------------------------------------------------------|
| File Attachments | Upload to blob storage per task; server-generated filenames           |
| File Attachments | Upload to blob storage per task; server-generated filenames         |
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
| List / get tasks              | ✓     | ✓     | ✓      |
| Create / update / move / delete task | ✓ | ✓  | ✓      |

---

## Architecture Notes

- All business logic lives in **Services** (`TaskFlow.Application/Services/`).
- All DB access lives in **Repositories** (`TaskFlow.Infrastructure/Repositories/`).
- Authorization is enforced by `WorkspaceRoleHandler` via route-param `workspaceId`.
- Errors are normalised to `ProblemDetails` by `GlobalExceptionMiddleware`.
- Validation uses FluentValidation with auto-discovery — no manual registration per validator.
