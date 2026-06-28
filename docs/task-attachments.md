# Task Attachments

File attachments for workspace tasks. Upload any file type; the API validates size and extension, stores bytes in Redis with a TTL, then a background worker (`file-worker`) moves the file to permanent disk storage. Downloads are served with `Content-Disposition: attachment` (force-download).

---

## Data Model

### `TaskAttachment`

| Column            | Type               | Notes                                               |
|-------------------|--------------------|-----------------------------------------------------|
| `Id`              | `uuid`             | PK, server-generated                                |
| `TaskId`          | `uuid`             | FK → `WorkspaceTask` (cascade delete)               |
| `UploadedById`    | `string`           | FK → `AspNetUsers` (restrict delete)                |
| `OriginalFileName`| `text`             | Client-supplied display name                        |
| `ContentType`     | `text`             | MIME type from multipart upload                     |
| `FileSizeBytes`   | `bigint`           | Validated before Redis storage                      |
| `StoredFileName`  | `text`             | Server-generated (`{uuid}.{ext}`) — never the original name |
| `StoragePath`     | `text`             | Empty while Pending/Processing; permanent disk path when Ready |
| `Status`          | `int` (enum)       | `Pending=0`, `Processing=1`, `Ready=2`, `Failed=3`  |
| `ProcessingError` | `text?`            | Populated by worker on failure                      |
| `UploadedAt`      | `timestamp`        | Set at upload time                                  |
| `ProcessedAt`     | `timestamp?`       | Set by worker when status → Ready                   |

**Indexes:** `IX_TaskAttachments_TaskId`, `IX_TaskAttachments_Status`

### `AttachmentStatus` Enum

```
Pending     — saved to DB, file bytes in Redis, message published
Processing  — worker received message, reading from Redis
Ready       — file on permanent disk, StoragePath set
Failed      — worker error; ProcessingError describes reason
```

---

## Processing Pipeline

```
API                              Redis (TTL: 30 min)           file-worker          Disk
 │                                    │                             │                 │
 │─── validate size + extension ──────┤                             │                 │
 │─── INSERT attachment (Pending) ────┤                             │                 │
 │─── STORE bytes ───────────────────▶│ key: file:temp:{id}        │                 │
 │─── PUBLISH FileUploadMessage ──────┼────────────────────────────▶│                 │
 │─── return 202 + AttachmentDto ─────┤                             │                 │
 │                                    │                             │─── UPDATE Processing
 │                                    │◀── GET bytes ───────────────│                 │
 │                                    │                             │─── SaveAsync ──▶│ /processed/{storedFileName}
 │                                    │◀── DELETE key ──────────────│                 │
 │                                    │                             │─── UPDATE Ready ─│
```

**Temp file lifetime:** If the worker is down and does not process the message before the Redis TTL expires, the attachment is permanently Failed on the next processing attempt. Increase `CacheKeys.Ttl.TempFile` (default 30 min) for large files or slow workers.

**Download:** API reads from permanent disk path via `IBlobService`. The shared Docker volume `uploads_data` (mounted at `/app/uploads`) is accessible to both the API (read) and `file-worker` (write).

---

## Authorization

All endpoints require `[Authorize(Policy = WorkspacePolicies.Member)]`. The workspace RBAC `WorkspaceRoleHandler` resolves the caller's role from DB per-request using the `{workspaceId}` route parameter.

---

## Endpoints

Base route: `GET /api/v1/workspaces/{workspaceId}/tasks/{taskId}/attachments`

### List attachments

```
GET /api/v1/workspaces/{workspaceId}/tasks/{taskId}/attachments
```

**Response 200**
```json
[
  {
    "id": "uuid",
    "taskId": "uuid",
    "originalFileName": "design.pdf",
    "contentType": "application/pdf",
    "fileSizeBytes": 102400,
    "status": "Ready",
    "processingError": null,
    "uploadedById": "string",
    "uploadedByName": "Alice",
    "uploadedByAvatarColor": "#4f46e5",
    "uploadedAt": "2026-06-28T12:00:00Z",
    "processedAt": "2026-06-28T12:00:05Z"
  }
]
```

---

### Get attachment by ID

```
GET /api/v1/workspaces/{workspaceId}/tasks/{taskId}/attachments/{id}
```

**Responses:** `200 AttachmentDto` | `404`

---

### Upload file

```
POST /api/v1/workspaces/{workspaceId}/tasks/{taskId}/attachments
Content-Type: multipart/form-data
```

**Form field:** `file` (binary)

**Validation:**
- Max size: 20 MB (configurable via `Storage:MaxFileSizeBytes`)
- Blocked extensions: `.exe .bat .cmd .ps1 .sh .msi .dll .com .scr .vbs .jar .app .dmg .bin .run`
- All other types accepted (Jira-like behavior)

**Response 202** — file is accepted and queued; status will be `Pending`
```json
{
  "id": "uuid",
  "status": "Pending",
  ...
}
```

**Responses:** `202 AttachmentDto` | `400 blocked extension or size exceeded` | `404 task not found`

---

### Download file

```
GET /api/v1/workspaces/{workspaceId}/tasks/{taskId}/attachments/{id}/download
```

Returns the file as an octet-stream with `Content-Disposition: attachment; filename="<originalFileName>"` — browsers will always prompt a save dialog rather than opening the file inline (security: prevents HTML/SVG execution).

**Responses:** `200 file stream` | `404` | `409 status != Ready`

---

### Delete attachment

```
DELETE /api/v1/workspaces/{workspaceId}/tasks/{taskId}/attachments/{id}
```

- If `Status == Ready`: deletes the file from disk, then removes the DB record.
- If `Status == Pending/Processing`: removes the DB record; the Redis key expires via TTL automatically.
- If `Status == Failed`: removes the DB record only.

**Responses:** `204` | `404`

---

## Security Controls

| Threat | Control |
|--------|---------|
| Executable uploads | Extension blocklist (`.exe`, `.bat`, `.ps1`, `.sh`, etc.) |
| Path traversal | Server-generated stored filename (`{uuid}.{ext}`) — original name never used as path |
| Inline execution (HTML/SVG) | `Content-Disposition: attachment` on all downloads |
| Orphaned temp files | Redis TTL (30 min) auto-expiry |

---

## Business Rules

1. A task can have unlimited attachments.
2. Uploading returns `202 Accepted` immediately — the file is not yet on disk.
3. Downloading an attachment that is not `Ready` returns `409 Conflict`.
4. Deleting a task deletes all its attachments (EF cascade).
5. Deleting a user does **not** delete their attachments (FK restrict; the user record must be retained).
6. `StoragePath` is always empty while the attachment is not `Ready`.

---

## DTO Reference

### `AttachmentDto`

| Field              | Type     | Notes                             |
|--------------------|----------|-----------------------------------|
| `Id`               | `Guid`   |                                   |
| `TaskId`           | `Guid`   |                                   |
| `OriginalFileName` | `string` | Display name                      |
| `ContentType`      | `string` | MIME type                         |
| `FileSizeBytes`    | `long`   |                                   |
| `Status`           | `string` | Enum as string (JSON)             |
| `ProcessingError`  | `string?`|                                   |
| `UploadedById`     | `string` |                                   |
| `UploadedByName`   | `string` | From `AppUser.DisplayName`        |
| `UploadedByAvatarColor` | `string` | From `AppUser.AvatarColor`   |
| `UploadedAt`       | `DateTime`|                                  |
| `ProcessedAt`      | `DateTime?`|                                 |

---

## Configuration

`appsettings.json` (`Storage` section):

```json
{
  "Storage": {
    "BasePath": "/app/uploads",
    "MaxFileSizeBytes": 20971520,
    "BlockedExtensions": [".exe", ".bat", ".cmd", ".ps1", ".sh", ".msi",
                          ".dll", ".com", ".scr", ".vbs", ".jar", ".app",
                          ".dmg", ".bin", ".run"]
  }
}
```

Redis temp file TTL is controlled by `CacheKeys.Ttl.TempFile` (`TaskFlow.Application/Caching/CacheKeys.cs`).

---

## Infrastructure

- `TaskFlow.FileLoader.Worker` — .NET Worker Service consuming `FileUploadMessage` via MassTransit/RabbitMQ
- `IBlobService` — disk read/write/delete (API reads, Worker writes)
- `ITemporaryFileStore` — Redis binary store with TTL (API writes, Worker reads)
- `IMessagePublisher` → `MassTransitPublisher` — publishes to RabbitMQ
- Docker volume: `uploads_data` mounted at `/app/uploads` in `api` (read) and `file-worker` (write)
