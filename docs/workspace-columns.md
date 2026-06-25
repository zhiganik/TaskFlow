# Workspace Columns

Kanban-style columns that belong to a workspace. Columns define the stages tasks move through
(e.g. "Todo → In Progress → Done"). Every workspace ships with three default columns and
supports up to seven total.

---

## Data Model

| Field         | Type       | Description                                      |
|---------------|------------|--------------------------------------------------|
| `id`          | `guid`     | Primary key                                      |
| `workspaceId` | `guid`     | FK → `Workspaces.Id` (cascade delete)            |
| `name`        | `string`   | Column label, max 50 characters                  |
| `color`       | `string`   | Hex color string, exactly 7 chars e.g. `#FF5733`. Random from palette on creation. |
| `order`       | `int`      | 0-based position; lower = further left on board  |
| `createdAt`   | `datetime` | UTC timestamp                                    |

### Default columns

Every new workspace is created with three columns in this order:

| Order | Name          | Default Color          |
|-------|---------------|------------------------|
| 0     | Todo          | `#6366F1` (indigo)     |
| 1     | In Progress   | `#F59E0B` (amber)      |
| 2     | Done          | `#10B981` (emerald)    |

---

## Authorization

All endpoints are under `api/v1/workspaces/{workspaceId}/columns` and require a valid JWT.
The `workspaceId` route param is checked against the caller's membership on every request.

| Role   | Read (GET) | Create / Rename / Reorder / Delete |
|--------|:----------:|:----------------------------------:|
| Owner  | ✓          | ✓                                  |
| Admin  | ✓          | ✓                                  |
| Member | ✓          | ✗ → 403                            |

---

## Endpoints

### List columns

```
GET /api/v1/workspaces/{workspaceId}/columns
```

Returns all columns for the workspace sorted by `order` ascending.

**Responses**

| Status | Body                          |
|--------|-------------------------------|
| 200    | `WorkspaceColumnDto[]`        |
| 401    | —                             |
| 403    | `ProblemDetails`              |

**Example response**
```json
[
  { "id": "...", "workspaceId": "...", "name": "Todo",        "color": "#6366F1", "order": 0, "createdAt": "..." },
  { "id": "...", "workspaceId": "...", "name": "In Progress", "color": "#F59E0B", "order": 1, "createdAt": "..." },
  { "id": "...", "workspaceId": "...", "name": "Done",        "color": "#10B981", "order": 2, "createdAt": "..." }
]
```

---

### Create column

```
POST /api/v1/workspaces/{workspaceId}/columns
```

Appends a new column at the end. Fails with 409 if the workspace already has 7 columns.

**Request body**
```json
{ "name": "Review", "color": "#3B82F6" }
```

| Field   | Required | Rules                                              |
|---------|----------|----------------------------------------------------|
| `name`  | yes      | non-empty, max 50                                  |
| `color` | no       | hex string matching `^#[0-9A-Fa-f]{6}$`; random if omitted |

**Responses**

| Status | Body                |
|--------|---------------------|
| 201    | `WorkspaceColumnDto`|
| 400    | `ValidationProblemDetails` |
| 401    | —                   |
| 403    | `ProblemDetails`    |
| 409    | `ProblemDetails` — column limit reached |

---

### Rename column

```
PUT /api/v1/workspaces/{workspaceId}/columns/{columnId}
```

Updates the column name and optionally the color. Order is unchanged.

**Request body**
```json
{ "name": "QA", "color": "#EF4444" }
```

| Field   | Required | Rules                                         |
|---------|----------|-----------------------------------------------|
| `name`  | yes      | non-empty, max 50                             |
| `color` | no       | hex string matching `^#[0-9A-Fa-f]{6}$`; existing color kept if omitted |

**Responses**

| Status | Body                |
|--------|---------------------|
| 200    | `WorkspaceColumnDto`|
| 400    | `ValidationProblemDetails` |
| 401    | —                   |
| 403    | `ProblemDetails`    |
| 404    | `ProblemDetails`    |

---

### Reorder columns

```
PUT /api/v1/workspaces/{workspaceId}/columns/reorder
```

Accepts the complete ordered list of column IDs. The backend sets each column's `order` to its
index in the supplied array. The list must contain **exactly** the workspace's current column IDs
— no extras, no omissions.

**Request body**
```json
{ "columnIds": ["<done-id>", "<in-progress-id>", "<todo-id>"] }
```

| Field       | Required | Rules                                   |
|-------------|----------|-----------------------------------------|
| `columnIds` | yes      | non-empty, count ≤ 7, exact match of existing IDs |

**Responses**

| Status | Body             |
|--------|------------------|
| 204    | —                |
| 400    | `ValidationProblemDetails` |
| 401    | —                |
| 403    | `ProblemDetails` |
| 409    | `ProblemDetails` — IDs don't match workspace columns |

---

### Delete column

```
DELETE /api/v1/workspaces/{workspaceId}/columns/{columnId}
```

Permanently removes the column. Tasks assigned to this column (future feature) will need
to be handled at that point — no cascade behaviour is defined yet.

**Responses**

| Status | Body             |
|--------|------------------|
| 204    | —                |
| 401    | —                |
| 403    | `ProblemDetails` |
| 404    | `ProblemDetails` |

---

## Business Rules

- Maximum **7 columns** per workspace. Attempting to create an 8th returns `409 Conflict`.
- `order` values are **0-based** and contiguous after a reorder. Gaps are not stored.
- New columns are always appended at the end (`order = current count`).
- Reorder is an **atomic replace** — supply the full list in the desired order.
- A column's `workspaceId` is validated on every mutation. A column ID that exists but
  belongs to a different workspace returns `404` (not `403`) to avoid leaking existence.

---

## DTO Reference

```typescript
// WorkspaceColumnDto
{
  id:          string   // guid
  workspaceId: string   // guid
  name:        string
  color:       string   // hex, e.g. "#6366F1"
  order:       number   // 0-based
  createdAt:   string   // ISO 8601 UTC
}
```
