# Workspaces

A workspace is the top-level container for all work. Every user can own or be a member of
multiple workspaces. Creating a workspace automatically makes the creator the Owner and seeds
three default Kanban columns (Todo, In Progress, Done).

---

## Data Model

| Field       | Type       | Description                                    |
|-------------|------------|------------------------------------------------|
| `id`        | `guid`     | Primary key                                    |
| `name`      | `string`   | Workspace display name, max 100 characters     |
| `ownerId`   | `string`   | FK → `AspNetUsers.Id` (restrict delete)        |
| `createdAt` | `datetime` | UTC creation timestamp                         |

**Navigation properties:** `Owner` (AppUser), `Members` (WorkspaceMember[]), `Columns` (WorkspaceColumn[])

---

## Authorization

All endpoints require a valid JWT.

| Action              | Required policy    | Minimum role |
|---------------------|--------------------|--------------|
| List my workspaces  | none (JWT only)    | —            |
| Get single workspace| `WorkspaceMember`  | Member       |
| Create workspace    | none (JWT only)    | —            |
| Update workspace    | `WorkspaceOwner`   | Owner        |
| Delete workspace    | `WorkspaceOwner`   | Owner        |

The `WorkspaceMember` / `WorkspaceOwner` policies are enforced by `WorkspaceRoleHandler`, which
reads the caller's `WorkspaceMember` row for the `workspaceId` in the route.

---

## Endpoints

### List workspaces

```
GET /api/v1/workspaces
```

Returns all workspaces the authenticated user is a member of, along with the caller's role
in each.

**Responses**

| Status | Body                    |
|--------|-------------------------|
| 200    | `WorkspaceDto[]`        |
| 401    | —                       |

**Example response**
```json
[
  {
    "id": "...",
    "name": "My Workspace",
    "ownerId": "...",
    "createdAt": "2026-06-24T18:55:00Z",
    "myRole": "Owner"
  }
]
```

---

### Get workspace

```
GET /api/v1/workspaces/{workspaceId}
```

**Responses**

| Status | Body             |
|--------|------------------|
| 200    | `WorkspaceDto`   |
| 401    | —                |
| 403    | `ProblemDetails` |
| 404    | `ProblemDetails` |

---

### Create workspace

```
POST /api/v1/workspaces
```

Creates a workspace, adds the caller as Owner, and seeds three default columns
(Todo, In Progress, Done).

**Request body**

```json
{ "name": "Design Team" }
```

| Field  | Required | Rules               |
|--------|----------|---------------------|
| `name` | yes      | non-empty, max 100  |

**Responses**

| Status | Body             |
|--------|------------------|
| 201    | `WorkspaceDto`   |
| 400    | `ValidationProblemDetails` |
| 401    | —                |

`Location` header points to `GET /api/v1/workspaces/{id}`.

---

### Update workspace

```
PUT /api/v1/workspaces/{workspaceId}
```

Renames the workspace. Only the Owner can call this.

**Request body**

```json
{ "name": "Design Team v2" }
```

| Field  | Required | Rules               |
|--------|----------|---------------------|
| `name` | yes      | non-empty, max 100  |

**Responses**

| Status | Body             |
|--------|------------------|
| 200    | `WorkspaceDto`   |
| 400    | `ValidationProblemDetails` |
| 401    | —                |
| 403    | `ProblemDetails` |
| 404    | `ProblemDetails` |

---

### Delete workspace

```
DELETE /api/v1/workspaces/{workspaceId}
```

Permanently deletes the workspace and all its members and columns (cascade). Only the Owner
can call this.

**Responses**

| Status | Body             |
|--------|------------------|
| 204    | —                |
| 401    | —                |
| 403    | `ProblemDetails` |
| 404    | `ProblemDetails` |

---

## Business Rules

- The caller is automatically added as `Owner` on creation — no separate invite step.
- Three default columns are seeded on creation: see [Workspace Columns](workspace-columns.md).
- Deleting the workspace cascades to `WorkspaceMembers` and `WorkspaceColumns`.
- The Owner's `AspNetUsers` row cannot be deleted while they own a workspace (`Restrict` FK).
- Only one Owner per workspace. Role changes that would demote or reassign the Owner are
  handled through [Workspace Members](workspace-members.md).

---

## DTO Reference

```typescript
// WorkspaceDto
{
  id:        string   // guid
  name:      string
  ownerId:   string
  createdAt: string   // ISO 8601 UTC
  myRole:    "Owner" | "Admin" | "Member"
}
```
