# Task & Column Improvements

**Date:** 2026-06-25  
**Migration:** `TaskImprovements`

---

## Breaking Changes

### `PUT /api/v1/workspaces/{workspaceId}/tasks/{taskId}/move`

| Before | After |
|--------|-------|
| `204 No Content` | `200 OK` + `WorkspaceTaskDto` body |

The move endpoint now returns the updated task DTO so the frontend can update its local state atomically without a second GET.

---

## New Fields on `WorkspaceTaskDto`

| Field | Type | Description |
|---|---|---|
| `number` | `number` | Workspace-scoped sequential integer (like GitHub `#42`). Assigned on creation, never changes. |
| `updatedAt` | `string` (ISO 8601 UTC) | Timestamp of last modification — set on create, update, and move. |
| `createdByName` | `string` | Display name of the user who created the task. Falls back to `createdById` if name is unavailable. |
| `isOverdue` | `boolean` | `true` when `dueDate` is set and is in the past. Computed server-side. Frontend may choose to suppress this for tasks in a "Done"-equivalent column. |

### Updated TypeScript type

```typescript
interface WorkspaceTaskDto {
  id:            string
  number:        number          // NEW — workspace-scoped, e.g. #1, #2, #3
  workspaceId:   string
  columnId:      string
  columnName:    string
  title:         string
  description:   string | null
  order:         number
  priority:      'Low' | 'Medium' | 'High'
  assigneeId:    string | null
  assigneeName:  string | null
  dueDate:       string | null   // ISO 8601 UTC
  isOverdue:     boolean         // NEW — true when dueDate < now
  createdById:   string
  createdByName: string          // NEW — display name of creator
  createdAt:     string          // ISO 8601 UTC
  updatedAt:     string          // NEW — ISO 8601 UTC, last write
}
```

---

## New Fields on `WorkspaceColumnDto`

| Field | Type | Description |
|---|---|---|
| `color` | `string` | Hex color string (e.g. `#6366F1`). Assigned on creation, changeable via `PUT /columns/{id}`. |

### Updated TypeScript type

```typescript
interface WorkspaceColumnDto {
  id:          string
  workspaceId: string
  name:        string
  color:       string   // NEW — hex color, e.g. "#6366F1"
  order:       number
  createdAt:   string
}
```

---

## Column Color — Create & Update

### `POST /api/v1/workspaces/{workspaceId}/columns`

`color` is now an optional field. If omitted, a color is picked randomly from the palette below.

```json
{
  "name": "Review",
  "color": "#3B82F6"   // optional — omit for random
}
```

### `PUT /api/v1/workspaces/{workspaceId}/columns/{columnId}`

`color` is now an optional field. If omitted, the existing color is preserved.

```json
{
  "name": "Review",
  "color": "#EF4444"   // optional — omit to keep current color
}
```

**Validation:** `color` must match `^#[0-9A-Fa-f]{6}$` when provided. Invalid format returns `400`.

### Default colors for seeded columns

| Column | Color |
|--------|-------|
| Todo | `#6366F1` (indigo) |
| In Progress | `#F59E0B` (amber) |
| Done | `#10B981` (emerald) |

### Color palette (used for random assignment)

```
#6366F1  #F59E0B  #10B981  #EF4444
#3B82F6  #8B5CF6  #F97316  #06B6D4
#84CC16  #EC4899
```

---

## Summary of Changes by Endpoint

| Endpoint | Change |
|---|---|
| `GET /tasks` | DTO gains `number`, `updatedAt`, `createdByName`, `isOverdue` |
| `GET /tasks/{id}` | DTO gains same 4 fields |
| `POST /tasks` | DTO gains same 4 fields |
| `PUT /tasks/{id}` | DTO gains same 4 fields; `updatedAt` is refreshed |
| `PUT /tasks/{id}/move` | Now returns `200 WorkspaceTaskDto` instead of `204`. DTO gains same 4 fields; `updatedAt` is refreshed |
| `DELETE /tasks/{id}` | No change |
| `GET /columns` | DTO gains `color` |
| `POST /columns` | Request accepts optional `color`; DTO gains `color` |
| `PUT /columns/{id}` | Request accepts optional `color`; DTO gains `color` |

---

## DB Migration

```bash
dotnet ef database update --project TaskFlow.Infrastructure --startup-project TaskFlow.Api
```

New columns added:
- `WorkspaceTasks.Number` (int, not null)
- `WorkspaceTasks.UpdatedAt` (timestamp, not null)
- `WorkspaceColumns.Color` (varchar(7), not null)
- Unique index: `IX_WorkspaceTasks_WorkspaceId_Number`
