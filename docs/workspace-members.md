# Workspace Members

Controls who has access to a workspace and at what privilege level. Membership is backed by
the `WorkspaceMembers` join table (composite PK: `WorkspaceId` + `UserId`).

---

## Data Model

| Field         | Type            | Description                                       |
|---------------|-----------------|---------------------------------------------------|
| `workspaceId` | `guid`          | Composite PK, FK → `Workspaces.Id` (cascade)     |
| `userId`      | `string`        | Composite PK, FK → `AspNetUsers.Id` (cascade)    |
| `role`        | `WorkspaceRole` | Member's privilege level (stored as string in DB) |
| `joinedAt`    | `datetime`      | UTC timestamp when membership was created         |

### Role hierarchy

| Role   | Value | Description                                        |
|--------|-------|----------------------------------------------------|
| Owner  | 0     | Full control; cannot be removed by Admins          |
| Admin  | 1     | Can invite, remove, and change roles of Members    |
| Member | 2     | Read access to workspace data; no management       |

The policy system uses `role <= minimumRole` for comparison — lower numeric value is more
privileged.

---

## Authorization

All endpoints require a valid JWT and live under `/api/v1/workspaces/{workspaceId}/members`.
The `WorkspaceRoleHandler` validates that the caller has a membership row for the given
`workspaceId` before any policy check succeeds.

| Action        | Required policy   | Minimum role |
|---------------|-------------------|--------------|
| List members  | `WorkspaceMember` | Member       |
| Add member    | `WorkspaceAdmin`  | Admin        |
| Change role   | `WorkspaceAdmin`  | Admin        |
| Remove member | `WorkspaceAdmin`  | Admin        |

---

## Endpoints

### List members

```
GET /api/v1/workspaces/{workspaceId}/members
```

Returns all members of the workspace.

**Responses**

| Status | Body             |
|--------|------------------|
| 200    | `MemberDto[]`    |
| 401    | —                |
| 403    | `ProblemDetails` |

**Example response**
```json
[
  {
    "userId": "...",
    "displayName": "Jane",
    "email": "jane@example.com",
    "role": "Owner",
    "joinedAt": "2026-06-24T18:55:00Z"
  }
]
```

---

### Add member

```
POST /api/v1/workspaces/{workspaceId}/members
```

Adds an existing registered user to the workspace by email. Returns 409 if already a member.

**Request body**

```json
{ "email": "jane@example.com", "role": "Member" }
```

| Field   | Required | Rules                                     |
|---------|----------|-------------------------------------------|
| `email` | yes      | must belong to an existing account        |
| `role`  | yes      | `"Admin"` or `"Member"` (not `"Owner"`)   |

**Responses**

| Status | Body             |
|--------|------------------|
| 201    | `MemberDto`      |
| 400    | `ValidationProblemDetails` |
| 401    | —                |
| 403    | `ProblemDetails` |
| 404    | `ProblemDetails` — email not found |
| 409    | `ProblemDetails` — already a member |

---

### Change role

```
PUT /api/v1/workspaces/{workspaceId}/members/{userId}
```

Updates the role of an existing member. Cannot be used to assign or change the `Owner` role.

**Request body**

```json
{ "role": "Admin" }
```

| Field  | Required | Rules                          |
|--------|----------|--------------------------------|
| `role` | yes      | `"Admin"` or `"Member"`        |

**Responses**

| Status | Body             |
|--------|------------------|
| 200    | `MemberDto`      |
| 400    | `ValidationProblemDetails` |
| 401    | —                |
| 403    | `ProblemDetails` |
| 404    | `ProblemDetails` — member not found |
| 409    | `ProblemDetails` — role change not permitted |

---

### Remove member

```
DELETE /api/v1/workspaces/{workspaceId}/members/{userId}
```

Removes a member from the workspace. The Owner cannot be removed.

**Responses**

| Status | Body             |
|--------|------------------|
| 204    | —                |
| 401    | —                |
| 403    | `ProblemDetails` |
| 404    | `ProblemDetails` — member not found |
| 409    | `ProblemDetails` — cannot remove Owner |

---

## Business Rules

- The workspace Owner is created automatically at workspace creation and cannot be removed
  through this API.
- Inviting by email means the user must already have a TaskFlow account; there is no
  pending-invite flow yet.
- Admins can manage Members but cannot manage other Admins or the Owner.
- A member can only hold one role at a time — updating the role replaces the existing one.

---

## DTO Reference

```typescript
// MemberDto
{
  userId:      string
  displayName: string
  email:       string
  role:        "Owner" | "Admin" | "Member"
  joinedAt:    string   // ISO 8601 UTC
}

// InviteMemberRequest
{ email: string; role: "Admin" | "Member" }

// UpdateMemberRoleRequest
{ role: "Admin" | "Member" }
```
