# Task Comments

Text comments on tasks with Markdown formatting and @mentions. File attachments are planned for a future iteration.

## Data Model

### TaskComment
| Column | Type | Notes |
|---|---|---|
| Id | uuid PK | |
| TaskId | uuid FK → WorkspaceTasks | cascade delete |
| Content | varchar(5000) | Markdown; @mentions stored inline as `@[Name](userId)` |
| CreatedById | varchar FK → AspNetUsers | restrict delete |
| CreatedAt | timestamptz | set on insert |
| UpdatedAt | timestamptz | set on insert, updated on edit |

### TaskCommentMention
| Column | Type | Notes |
|---|---|---|
| CommentId | uuid FK → TaskComments | PK, cascade delete |
| UserId | varchar FK → AspNetUsers | PK, cascade delete |

Extracted from `Content` by the service on every create/update using regex `@\[([^\]]+)\]\(([^)]+)\)`.

**Indexes:** `TaskComments(TaskId)`, `TaskComments(TaskId, CreatedAt)` for pagination.

## Authorization

All endpoints require `WorkspacePolicies.Member` (the authenticated user must be a member of the workspace).

Edit and Delete additionally enforce author-only access at the service layer — `ForbiddenException` is thrown if `comment.CreatedById != userId`.

## Endpoints

### GET `api/v1/workspaces/{workspaceId}/tasks/{taskId}/comments`

Cursor-based pagination, chronological order (oldest first).

**Query params**
| Param | Type | Default | Notes |
|---|---|---|---|
| cursor | string? | — | Opaque base64 cursor from previous response |
| limit | int | 20 | Number of comments per page |

**Response 200**
```json
{
  "items": [
    {
      "id": "uuid",
      "taskId": "uuid",
      "content": "**Hello** @[Alice](uuid)",
      "createdById": "string",
      "createdByName": "Alice",
      "createdAt": "ISO 8601",
      "updatedAt": "ISO 8601",
      "isEdited": false,
      "mentionedUserIds": ["uuid"]
    }
  ],
  "nextCursor": "base64string | null",
  "hasMore": false
}
```

### POST `api/v1/workspaces/{workspaceId}/tasks/{taskId}/comments`

**Body** `CreateCommentRequest`
```json
{ "content": "string (1–5000 chars)" }
```

**Response 201** — `TaskCommentDto` with `Location` header.

### PUT `api/v1/workspaces/{workspaceId}/tasks/{taskId}/comments/{commentId}`

Author only. Updates `Content`, `UpdatedAt`, and rebuilds `Mentions`.

**Body** `UpdateCommentRequest`
```json
{ "content": "string (1–5000 chars)" }
```

**Response 200** — updated `TaskCommentDto`.

### DELETE `api/v1/workspaces/{workspaceId}/tasks/{taskId}/comments/{commentId}`

Author only. Cascade deletes `TaskCommentMentions`.

**Response 204** No Content.

## Business Rules

- `IsEdited` is computed: `UpdatedAt > CreatedAt + 1 second`.
- `MentionedUserIds` is derived from the `TaskCommentMentions` join table at read time; mentions are rebuilt from `Content` on every write.
- Cursor encodes `{CreatedAt.Ticks}_{Id}` as base64 for stable ordering on ties.
- Only the comment author can edit or delete; any workspace member can read and create.

## DTO Reference

### TaskCommentDto
```csharp
record TaskCommentDto(
    Guid Id, Guid TaskId, string Content,
    string CreatedById, string CreatedByName,
    DateTime CreatedAt, DateTime UpdatedAt,
    bool IsEdited,
    IReadOnlyList<string> MentionedUserIds)
```

### PagedResult\<T\>
```csharp
record PagedResult<T>(IReadOnlyList<T> Items, string? NextCursor, bool HasMore)
```

## Frontend

- **API client** — `src/api/comments.api.ts`
- **Hooks** — `src/hooks/useComments.ts` (`useComments`, `useCreateComment`, `useUpdateComment`, `useDeleteComment`)
- **Components** — `src/components/tasks/comments/`
  - `CommentsList.tsx` — infinite scroll via `IntersectionObserver` + `useInfiniteQuery`
  - `CommentItem.tsx` — renders Markdown via `react-markdown` + `remark-gfm`; @mentions are rendered as styled chips (the `@[Name](uuid)` syntax creates a Markdown link with a UUID href, which the custom `a` component intercepts)
  - `CommentInput.tsx` — textarea with @ autocomplete trigger
  - `MentionAutocomplete.tsx` — dropdown filtering workspace members

### Mention format
Stored in content as `@[DisplayName](userId)`. In Markdown, `[Name](uuid)` is a valid link; `react-markdown` intercepts links whose `href` matches a UUID pattern and renders them as `<span class="mention">@Name</span>`.
