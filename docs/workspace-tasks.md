# Workspace Tasks

Kanban cards that live inside a workspace column. Tasks are the primary unit of work —
each card has a title, description, priority, optional assignee, and optional due date.
The column the task belongs to is surfaced as a `columnName` ("status") field on the DTO.

---

## Data Model

| Field         | Type           | Description                                              |
|---------------|----------------|----------------------------------------------------------|
| `id`          | `guid`         | Primary key                                              |
| `number`      | `int`          | Workspace-scoped sequential number (like GitHub `#42`). Unique per workspace. |
| `workspaceId` | `guid`         | FK → `Workspaces.Id` (cascade delete)                   |
| `columnId`    | `guid`         | FK → `WorkspaceColumns.Id` (restrict — guard in service) |
| `title`       | `string`       | Required, max 200 characters                             |
| `description` | `string?`      | Optional, max 2000 characters                            |
| `order`       | `int`          | 0-based position within the column                      |
| `priority`    | `TaskPriority` | `Low` / `Medium` / `High`; stored as string in DB        |
| `assigneeId`  | `string?`      | FK → `AspNetUsers.Id` (set null on user delete)          |
| `dueDate`     | `datetime?`    | Optional UTC deadline                                    |
| `createdById` | `string`       | FK → `AspNetUsers.Id` (restrict)                         |
| `createdAt`   | `datetime`     | UTC creation timestamp                                   |
| `updatedAt`   | `datetime`     | UTC timestamp of last modification (create / update / move) |

### Column-delete guard

Deleting a `WorkspaceColumn` that still contains tasks is blocked with **409 Conflict**.
The caller must move or delete all tasks in the column first.

---

## Authorization

All endpoints are under `api/v1/workspaces/{workspaceId}/tasks` and require a valid JWT.
All workspace roles (Owner, Admin, Member) can perform every task operation.

| Action                   | Required policy   |
|--------------------------|-------------------|
| List / Get / Create / Update / Move / Delete | `WorkspaceMember` |

---

## Endpoints

### List tasks

```
GET /api/v1/workspaces/{workspaceId}/tasks
```

The endpoint has two modes determined by whether `columnId` is supplied.

#### Mode 1 — flat filtered list (no `columnId`)

Returns all tasks in the workspace ordered by `columnId, order ASC`. Supports optional
server-side filtering; all params are AND-ed together.

| Query param  | Type              | Description |
|--------------|-------------------|-------------|
| `search`     | `string`          | Case-insensitive substring match on `title`. If the value is a plain integer it also matches `number`. |
| `assigneeId` | `string`          | Filter by assignee user ID. Pass the special value `"unassigned"` to return tasks with no assignee. |
| `priorities` | `TaskPriority[]`  | Repeat the param to filter by multiple values: `?priorities=High&priorities=Low` |

**Responses**

| Status | Body                        |
|--------|-----------------------------|
| 200    | `WorkspaceTaskDto[]`        |
| 401    | —                           |
| 403    | `ProblemDetails`            |

**Example response**
```json
[
  {
    "id": "...", "workspaceId": "...", "columnId": "...", "columnName": "Todo",
    "title": "Design login page", "description": null,
    "order": 0, "priority": "High",
    "assigneeId": null, "assigneeName": null,
    "dueDate": "2026-07-01T00:00:00Z",
    "isOverdue": false,
    "createdById": "...", "createdByName": "Jane Smith",
    "createdAt": "2026-06-25T19:00:00Z", "updatedAt": "2026-06-25T20:00:00Z"
  }
]
```

#### Mode 2 — paginated column view (`columnId` required)

Returns a cursor-paginated page of tasks for a single column. Used by the Kanban board to
load tasks on demand — each column fetches its own pages independently.

All filter params (`search`, `assigneeId`, `priorities`) are also supported in this mode.

| Query param | Type     | Default | Description |
|-------------|----------|---------|-------------|
| `columnId`  | `guid`   | —       | **Required** to activate this mode |
| `cursor`    | `string` | —       | Opaque base64 cursor from the previous page's `nextCursor`. Omit for the first page. |
| `limit`     | `int`    | `20`    | Tasks per page. |

**Cursor encoding:** base64 of `"{order}_{id}"`. Keyset condition:
`(Order > cursorOrder) OR (Order = cursorOrder AND Id > cursorId)` — stable across concurrent reorders.

**Responses**

| Status | Body                            |
|--------|---------------------------------|
| 200    | `PagedResult<WorkspaceTaskDto>` |
| 401    | —                               |
| 403    | `ProblemDetails`                |

**Example response**
```json
{
  "items": [ { "id": "...", "title": "Fix login bug", ... } ],
  "nextCursor": "MF8zNTBl...",
  "hasMore": true
}
```

**Indexes:** `(ColumnId, Order)` supports the keyset range scan without a sort step.

---

### Get task

```
GET /api/v1/workspaces/{workspaceId}/tasks/{taskId}
```

**Responses**

| Status | Body                  |
|--------|-----------------------|
| 200    | `WorkspaceTaskDto`    |
| 401    | —                     |
| 403    | `ProblemDetails`      |
| 404    | `ProblemDetails`      |

---

### Create task

```
POST /api/v1/workspaces/{workspaceId}/tasks
```

Creates a task in the specified column. The task is appended to the end of the column
(`order = current count`). The `columnId` must belong to the workspace.

**Request body**
```json
{
  "title":       "Design login page",
  "columnId":    "<todo-column-id>",
  "description": "Include OAuth and email/password flows",
  "priority":    "High",
  "assigneeId":  "<user-id>",
  "dueDate":     "2026-07-01T00:00:00Z"
}
```

| Field         | Required | Rules                              |
|---------------|----------|------------------------------------|
| `title`       | yes      | non-empty, max 200                 |
| `columnId`    | yes      | must exist in this workspace       |
| `description` | no       | max 2000                           |
| `priority`    | no       | `"Low"` / `"Medium"` / `"High"`, default `"Medium"` |
| `assigneeId`  | no       | must be a workspace member's userId|
| `dueDate`     | no       | ISO 8601 UTC                       |

**Responses**

| Status | Body                      |
|--------|---------------------------|
| 201    | `WorkspaceTaskDto`        |
| 400    | `ValidationProblemDetails`|
| 401    | —                         |
| 403    | `ProblemDetails`          |
| 404    | `ProblemDetails` — columnId not found in workspace |

---

### Update task

```
PUT /api/v1/workspaces/{workspaceId}/tasks/{taskId}
```

Updates title, description, priority, assignee, and due date. Does **not** change the column
or position — use the move endpoint for that.

**Request body**
```json
{
  "title":      "Design login page v2",
  "description": null,
  "priority":   "Medium",
  "assigneeId": null,
  "dueDate":    null
}
```

| Field         | Required | Rules               |
|---------------|----------|---------------------|
| `title`       | yes      | non-empty, max 200  |
| `description` | no       | max 2000            |
| `priority`    | yes      | `"Low"` / `"Medium"` / `"High"` |
| `assigneeId`  | no       | nullable            |
| `dueDate`     | no       | nullable            |

**Responses**

| Status | Body                      |
|--------|---------------------------|
| 200    | `WorkspaceTaskDto`        |
| 400    | `ValidationProblemDetails`|
| 401    | —                         |
| 403    | `ProblemDetails`          |
| 404    | `ProblemDetails`          |

---

### Move task

```
PUT /api/v1/workspaces/{workspaceId}/tasks/{taskId}/move
```

Moves the task to a column at a specific position. Works for both inter-column moves
(drag to another column) and intra-column reorders (drag within same column). Tasks
at or after the target `order` are shifted down by one.

Returns the updated task so the frontend can update its local state without a second GET.

**Request body**
```json
{ "columnId": "<in-progress-id>", "order": 0 }
```

| Field      | Required | Rules                                     |
|------------|----------|-------------------------------------------|
| `columnId` | yes      | must exist in this workspace              |
| `order`    | yes      | ≥ 0; clamped to valid range automatically |

**Responses**

| Status | Body                      |
|--------|---------------------------|
| 200    | `WorkspaceTaskDto`        |
| 400    | `ValidationProblemDetails` |
| 401    | —                         |
| 403    | `ProblemDetails`          |
| 404    | `ProblemDetails` — task or column not found |

---

### Delete task

```
DELETE /api/v1/workspaces/{workspaceId}/tasks/{taskId}
```

**Responses**

| Status | Body             |
|--------|------------------|
| 204    | —                |
| 401    | —                |
| 403    | `ProblemDetails` |
| 404    | `ProblemDetails` |

---

## Business Rules

- `order` is 0-based and assigned as `count of tasks in column` on creation (appended to end).
- Move `order` is clamped to `[0, taskCount]` — out-of-range values are silently adjusted.
- The `columnId` on create and move must belong to the given `workspaceId`; mismatches return 404.
- A task's `workspaceId` is validated on every mutation — a task ID from another workspace returns 404.
- Deleting a column that still has tasks returns **409**. Tasks must be removed or moved first.
- Filter params are AND-ed: `search=fix&priorities=High` returns only high-priority tasks whose title contains "fix".
- `assigneeId=unassigned` is a sentinel value that maps to `WHERE AssigneeId IS NULL` — it is not a real user ID.
- Paginated mode (`columnId` present) always returns results in `(order, id)` order within the column; the cursor encodes this position so pages remain stable even if other tasks are reordered concurrently.

---

## DTO Reference

```typescript
// PagedResult<T>  — returned by paginated column mode
{
  items:      T[]           // up to `limit` tasks
  nextCursor: string | null // pass as ?cursor= on the next request; null when no more pages
  hasMore:    boolean       // false when this is the last page
}

// WorkspaceTaskDto
{
  id:            string          // guid
  number:        number          // workspace-scoped sequential int (like GitHub #42)
  workspaceId:   string          // guid
  columnId:      string          // guid
  columnName:    string          // e.g. "In Progress" — the status label
  title:         string
  description:   string | null
  order:         number          // 0-based within column
  priority:      "Low" | "Medium" | "High"
  assigneeId:    string | null
  assigneeName:  string | null   // AppUser.DisplayName
  dueDate:       string | null   // ISO 8601 UTC
  isOverdue:     boolean         // true when dueDate is set and in the past
  createdById:   string
  createdByName: string          // AppUser.DisplayName, falls back to createdById
  createdAt:     string          // ISO 8601 UTC
  updatedAt:     string          // ISO 8601 UTC — refreshed on every write
}
```
