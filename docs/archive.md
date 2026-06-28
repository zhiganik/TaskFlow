# Archive Feature

Archived tasks are kept off the Kanban board but remain queryable. A task becomes archived by:
- Being closed explicitly via the "Close task" button in the task detail panel
- Being soft-deleted from the board (Status = Deleted)
- Being auto-closed by the background worker when it has sat in the Done column longer than `ArchiveAfterDays`

---

## Data Model

### WorkspaceTask — added fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Status` | `WorkspaceTaskStatus` (string) | `Active` | Current lifecycle status |
| `CompletedAt` | `DateTime?` | null | Set when task enters a Done column |
| `ClosedAt` | `DateTime?` | null | Set when task is closed or soft-deleted |

### WorkspaceTaskStatus enum

| Value | Meaning |
|-------|---------|
| `Active` | On the board, in a non-Done column |
| `Done` | On the board, in the Done column — visually dimmed + green badge |
| `Closed` | Archived via Close button or auto-close worker |
| `Deleted` | Soft-deleted from board; in archive, eligible for hard delete |

### WorkspaceColumn — added field

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `IsDoneColumn` | `bool` | `false` | At most one column per workspace may be the Done column |

Moving a task into the Done column sets `Status = Done`, `CompletedAt = UtcNow`. Moving it back out resets to `Active`.

### Workspace — added field

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `ArchiveAfterDays` | `int` | `1` | Days a Done task stays on the board before auto-close. 0 = disabled |

---

## Authorization

| Operation | Policy |
|-----------|--------|
| View archive | Member |
| Close task | Member |
| Reopen task | Member |
| Delete from archive (hard delete) | Member (via DELETE /tasks/{id}) |
| Set Done column | Admin / Owner |
| Update ArchiveAfterDays | Admin / Owner |

---

## Endpoints

### GET `/api/v1/workspaces/{workspaceId}/archive`
List closed and deleted tasks. Supports the same filter params as the board.

**Query params:** `search`, `assigneeIds[]`, `priorities[]`, `labelIds[]`

**Response `200`:** `WorkspaceTaskDto[]` ordered by `closedAt` descending.

---

### PUT `/api/v1/workspaces/{workspaceId}/archive/tasks/{taskId}/close`
Immediately move an Active or Done task to the archive.

**Errors:** `400` if already archived; `404` if not found.

**Response `200`:** Updated `WorkspaceTaskDto` with `Status = "Closed"`.

---

### PUT `/api/v1/workspaces/{workspaceId}/archive/tasks/{taskId}/reopen`
Return a Closed or Deleted task to the board.

Restores status to `Done` if the task's column is still a Done column, otherwise `Active`. Clears `ClosedAt`.

**Response `200`:** Updated `WorkspaceTaskDto`.

---

### PUT `/api/v1/workspaces/{workspaceId}/archive-settings`
Update `ArchiveAfterDays` for the workspace.

**Policy:** Admin  
**Body:** `{ "archiveAfterDays": 7 }`  
**Validation:** must be `>= 0`.

---

### DELETE `/api/v1/workspaces/{workspaceId}/tasks/{taskId}`
Dual-mode delete (on the task controller, not the archive controller):
- Task is `Active` or `Done` → **soft delete**: sets `Status = Deleted`, `ClosedAt = UtcNow`. Task moves to archive.
- Task is `Closed` or `Deleted` → **hard delete**: row permanently removed.

---

## Business Rules

1. A task with `Status = Closed` or `Deleted` is excluded from all board queries.
2. Only one column per workspace can have `IsDoneColumn = true`. Setting a new Done column automatically unsets the previous one.
3. Moving a Closed task throws `400 Bad Request`; the task must be reopened first.
4. The background worker (`TaskFlow.ArchiveWorker`) runs daily, finds all workspaces with `ArchiveAfterDays > 0`, and bulk-updates Done tasks whose `CompletedAt <= UtcNow - ArchiveAfterDays` using `ExecuteUpdateAsync` (no entity loading).
5. Setting `ArchiveAfterDays = 0` disables auto-close for that workspace.

---

## DTO Reference

### WorkspaceTaskDto (additions)

```json
{
  "status": "Active | Done | Closed | Deleted",
  "completedAt": "2026-06-28T12:00:00Z | null",
  "closedAt":    "2026-06-28T14:00:00Z | null"
}
```

### WorkspaceColumnDto (addition)

```json
{ "isDoneColumn": true }
```

### WorkspaceDto (addition)

```json
{ "archiveAfterDays": 7 }
```

### UpdateArchiveSettingsRequest

```json
{ "archiveAfterDays": 7 }
```
