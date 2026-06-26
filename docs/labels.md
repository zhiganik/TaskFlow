# Task Labels

Workspace-scoped colored tags that can be applied to tasks for categorization.

## Data Model

### WorkspaceLabel
| Column      | Type      | Notes                        |
|-------------|-----------|------------------------------|
| Id          | uuid      | PK                           |
| WorkspaceId | uuid      | FK → Workspaces(Id) CASCADE  |
| Name        | varchar(50) | Required                   |
| Color       | varchar(7) | Hex color (#rrggbb)          |
| CreatedAt   | timestamp | UTC                          |

### TaskLabel (join table)
| Column  | Type | Notes                          |
|---------|------|--------------------------------|
| TaskId  | uuid | FK → WorkspaceTasks(Id) CASCADE |
| LabelId | uuid | FK → WorkspaceLabels(Id) CASCADE |

Composite PK: `(TaskId, LabelId)`.

### WorkspacePriorityConfig
| Column      | Type      | Notes                                    |
|-------------|-----------|------------------------------------------|
| Id          | uuid      | PK                                       |
| WorkspaceId | uuid      | FK → Workspaces(Id) CASCADE              |
| Priority    | varchar   | Enum: Low / Medium / High (unique per workspace) |
| DisplayName | varchar(50) | Customizable display label             |
| Color       | varchar(7) | Hex color (#rrggbb)                     |

Unique index: `(WorkspaceId, Priority)`.

## Authorization

| Operation | Minimum Role |
|-----------|-------------|
| List labels | Member |
| Create label | Admin |
| Update label | Admin |
| Delete label | Admin |
| List priority configs | Member |
| Update priority config | Admin |
| Set task labels | Member |

## Endpoints

### Labels

#### `GET /api/v1/workspaces/{workspaceId}/labels`
Returns all labels for the workspace, ordered by name.

**Response 200:**
```json
[
  { "id": "...", "name": "Bug", "color": "#ef4444" },
  { "id": "...", "name": "Frontend", "color": "#60a5fa" }
]
```

#### `POST /api/v1/workspaces/{workspaceId}/labels`
Create a new label. Requires Admin role.

**Request:**
```json
{ "name": "Backend", "color": "#a78bfa" }
```

**Response 201:** `LabelDto`

#### `PUT /api/v1/workspaces/{workspaceId}/labels/{labelId}`
Update label name/color. Requires Admin role.

**Request:** `{ "name": "...", "color": "#rrggbb" }`

**Response 200:** `LabelDto`

#### `DELETE /api/v1/workspaces/{workspaceId}/labels/{labelId}`
Delete a label. Cascades off all tasks. Requires Admin role.

**Response 204**

### Priority Configs

#### `GET /api/v1/workspaces/{workspaceId}/priority-configs`
Returns 3 priority display configs ordered by enum value.

**Response 200:**
```json
[
  { "priority": "Low",    "displayName": "Low",    "color": "#22c55e" },
  { "priority": "Medium", "displayName": "Medium",  "color": "#f59e0b" },
  { "priority": "High",   "displayName": "High",   "color": "#ef4444" }
]
```

#### `PUT /api/v1/workspaces/{workspaceId}/priority-configs/{priority}`
Update display name and color for a priority level. Requires Admin role.

`priority` path segment is the enum value: `Low`, `Medium`, or `High`.

**Request:** `{ "displayName": "Critical", "color": "#dc2626" }`

**Response 200:** `PriorityConfigDto`

### Task Labels

#### `PUT /api/v1/workspaces/{workspaceId}/tasks/{taskId}/labels`
Replace the complete set of labels on a task. Requires Member role.

**Request:**
```json
{ "labelIds": ["uuid1", "uuid2"] }
```

Empty array removes all labels.

**Response 200:** `WorkspaceTaskDto` (includes updated `labels` array)

## Business Rules

- Label names are trimmed before save.
- Label IDs in `SetTaskLabels` and `CreateTask` must belong to the same workspace — returns 404 otherwise.
- Deleting a label cascades from all tasks via the join table (no orphan rows).
- Priority configs are seeded automatically on workspace creation; there is always exactly one config per priority level per workspace.
- The `labels` field in `WorkspaceTaskDto` is always present (empty array when no labels).

## DTO Reference

```csharp
record LabelDto(Guid Id, string Name, string Color);
record PriorityConfigDto(TaskPriority Priority, string DisplayName, string Color);
record CreateLabelRequest(string Name, string Color);
record UpdateLabelRequest(string Name, string Color);
record UpdatePriorityConfigRequest(string DisplayName, string Color);
record SetTaskLabelsRequest(IReadOnlyList<Guid> LabelIds);
```

`WorkspaceTaskDto` now includes `IReadOnlyList<LabelDto> Labels`.
`CreateTaskRequest` now includes optional `IReadOnlyList<Guid>? LabelIds`.
`TaskFilterQuery` now includes optional `IReadOnlyList<Guid>? LabelIds` (OR logic — tasks with any of the given labels).
