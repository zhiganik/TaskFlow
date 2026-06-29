# Notifications

Real-time in-app notifications delivered via SignalR and persisted to PostgreSQL. A dedicated `TaskFlow.NotificationWorker` consumes domain events from RabbitMQ, writes `Notification` rows, and pushes them to connected browsers. The REST API lets clients poll, page, and mark notifications read.

---

## Data Model

### Notification

| Column        | Type         | Notes                                              |
|---------------|--------------|----------------------------------------------------|
| `Id`          | `uuid`       | PK, server-generated                               |
| `RecipientId` | `varchar(450)` | FK → `AspNetUsers.Id` (ON DELETE RESTRICT)       |
| `Type`        | `varchar`    | Stored as string enum (see below)                  |
| `Title`       | `varchar(200)` | Short heading shown in notification bell         |
| `Body`        | `varchar(500)` | Detail text                                      |
| `IsRead`      | `bool`       | Default `false`                                    |
| `CreatedAt`   | `timestamptz` | UTC, server-generated                             |
| `WorkspaceId` | `uuid?`      | Nullable context link                              |
| `TaskId`      | `uuid?`      | Nullable context link                              |
| `CommentId`   | `uuid?`      | Nullable context link                              |

**Indexes:** `(RecipientId, CreatedAt)` for pagination; `(RecipientId, IsRead)` for unread count.

### NotificationType enum

| Value                  | Integer | Trigger                                                         |
|------------------------|---------|-----------------------------------------------------------------|
| `MentionedInComment`   | 0       | Another user mentions `@[Name](userId)` in a comment           |
| `TaskAssigned`         | 1       | A task is assigned to you (not by yourself)                     |
| `TaskStatusChanged`    | 2       | Status of a task assigned to you changes (not by yourself)      |
| `MemberInvited`        | 3       | You are added to a workspace                                    |

---

## Authorization

All REST endpoints require a valid JWT (`[Authorize]`). No workspace policy — users can only access their own notifications.

The SignalR hub (`/hubs/notifications`) uses the same JWT, passed as the `access_token` query parameter because browsers cannot set Authorization headers on WebSocket upgrades.

---

## REST Endpoints

### GET `/api/v1/notifications`

Returns a cursor-paginated list of notifications for the authenticated user, newest first.

**Query parameters**

| Parameter | Type     | Default | Notes               |
|-----------|----------|---------|---------------------|
| `cursor`  | `string` | —       | Opaque continuation token from previous response |
| `limit`   | `int`    | `20`    | 1–100               |

**Response 200**

```json
{
  "items": [
    {
      "id": "uuid",
      "type": "TaskAssigned",
      "title": "Alice assigned you a task",
      "body": "Fix the login bug",
      "isRead": false,
      "createdAt": "2026-06-29T10:00:00Z",
      "workspaceId": "uuid",
      "taskId": "uuid",
      "commentId": null
    }
  ],
  "nextCursor": "base64string",
  "hasMore": true
}
```

---

### GET `/api/v1/notifications/unread-count`

**Response 200**

```json
{ "count": 3 }
```

---

### PUT `/api/v1/notifications/{notificationId}/read`

Marks a single notification as read. Returns 404 if not found or not owned by the caller.

**Response:** `204 No Content`

---

### PUT `/api/v1/notifications/read-all`

Marks all notifications for the authenticated user as read (bulk `ExecuteUpdateAsync`).

**Response:** `204 No Content`

---

## RabbitMQ Events → Notifications

Four events are published by the API and consumed by `TaskFlow.NotificationWorker`:

| Event                  | Publisher                      | Consumer creates notification for      |
|------------------------|--------------------------------|----------------------------------------|
| `CommentPostedEvent`   | `TaskCommentsService.CreateAsync` | Each mentioned user (≠ author)       |
| `TaskAssignedEvent`    | `WorkspaceTasksService`         | Assignee (when assignee ≠ updater)    |
| `TaskStatusChangedEvent` | `WorkspaceTasksService`       | Assignee (when assignee ≠ updater)    |
| `MemberInvitedEvent`   | `WorkspaceMembersService.AddAsync` | Invited user                        |

### CommentPostedConsumer extra work

In addition to creating notifications, this consumer:
1. Parses `@[DisplayName](userId)` mentions from the comment content using the same regex as the API
2. Writes `TaskCommentMention` rows to the shared DB (eventual consistency — there is a brief window between comment creation and mentions appearing in the comment DTO)

---

## SignalR Hub

**URL:** `/hubs/notifications`  
**Auth:** JWT passed as `access_token` query parameter  
**Server → client event:** `ReceiveNotification` with payload `NotificationDto`

The hub uses a **Redis backplane** (`AddSignalR().AddStackExchangeRedis(redisConn)`) so multiple worker replicas can all push to any user regardless of which replica holds the WebSocket connection.

### PresenceService

Tracks online users in a Redis SET at key `presence:online-users`. Used by the hub `OnConnected`/`OnDisconnected` lifecycle hooks. Currently informational; could gate mobile push notifications in future iterations.

---

## Business Rules

- Notifications are never deleted — they remain until a future cleanup job (not yet implemented).
- A user never receives a notification for their own actions (assignee ≠ actor, commenter ≠ mentioned).
- `MentionedInComment` notifications are created with eventual consistency: the API saves the comment immediately; the notification worker processes the event asynchronously.
- Soft-deleted tasks still generate `TaskStatusChanged` notifications (the status change itself is meaningful).

---

## DTO Reference

### NotificationDto

```csharp
record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Body,
    bool IsRead,
    DateTime CreatedAt,
    Guid? WorkspaceId,
    Guid? TaskId,
    Guid? CommentId
);
```

### UnreadCountDto

```csharp
record UnreadCountDto(int Count);
```
