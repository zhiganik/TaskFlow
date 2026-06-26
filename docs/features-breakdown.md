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
| Task Search & Filter | Complete    | [workspace-tasks.md](workspace-tasks.md)         | `GET /api/v1/workspaces/{id}/tasks?search=&assigneeId=&priorities=` |
| Task Pagination      | Complete    | [workspace-tasks.md](workspace-tasks.md)         | `GET /api/v1/workspaces/{id}/tasks?columnId=&cursor=&limit=` |
| Task Comments        | Complete    | [task-comments.md](task-comments.md)             | `/api/v1/workspaces/{id}/tasks/{id}/comments` |
| Redis Caching        | Complete    | [`.claude/06_infrastructure.md`](../.claude/06_infrastructure.md) | `GET /api/v1/admin/cache-stats` |
| Profile Management   | Complete    | [profile.md](profile.md)                                          | `/api/v1/me`                    |
| Task Labels          | Complete    | [labels.md](labels.md)                                            | `/api/v1/workspaces/{id}/labels` |
| Priority Display Config | Complete | [labels.md](labels.md)                                            | `/api/v1/workspaces/{id}/priority-configs` |
| Workspace Settings   | Complete    | [workspace-settings.md](workspace-settings.md)                    | `/workspaces/:id/settings`      |

---

## Planned

| Feature        | Notes                                                                 |
|----------------|-----------------------------------------------------------------------|
| File Attachments | Upload to blob storage per task; server-generated filenames         |

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
| View / update own profile            | ✓ | ✓  | ✓      |

---

## Architecture Notes

- All business logic lives in **Services** (`TaskFlow.Application/Services/`).
- All DB access lives in **Repositories** (`TaskFlow.Infrastructure/Repositories/`).
- Authorization is enforced by `WorkspaceRoleHandler` via route-param `workspaceId`.
- Errors are normalised to `ProblemDetails` by `GlobalExceptionMiddleware`.
- Validation uses FluentValidation with auto-discovery — no manual registration per validator.
