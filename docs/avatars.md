# User Avatars

## Overview

Users can personalize their avatar with either a **color + initials** display (always available as a fallback) or a **custom photo** uploaded via an interactive crop interface. The photo is processed asynchronously by a dedicated `AvatarWorker` service and served as a static image without authentication.

---

## Data Model

`AppUser` (extends `IdentityUser`) — two new columns on `AspNetUsers`:

| Column | Type | Description |
|--------|------|-------------|
| `AvatarPath` | `varchar` nullable | Relative storage path, e.g. `"avatars/uuid.jpg"`. `null` = color mode. |
| `AvatarStatus` | `int` | Enum: `None=0`, `Pending=1`, `Ready=2`, `Failed=3` |

`AvatarPath` is only meaningful when `AvatarStatus == Ready`. During `Pending` it holds the future path (the file does not exist yet on disk).

---

## Authorization

| Action | Who |
|--------|-----|
| Upload avatar | Authenticated user (own profile only) |
| Remove avatar | Authenticated user (own profile only) |
| Read avatar image | Anyone (AllowAnonymous) — filenames are unguessable GUIDs |

---

## Endpoints

### Upload avatar
```
POST /api/v1/me/avatar
Content-Type: multipart/form-data

form field: file (image/*, max 10 MB)
```

**Response `200 OK`** — returns updated `UserDto` with `avatarStatus: "Pending"`.

**Errors:**
- `400` — non-image MIME type or file exceeds 10 MB
- `401` — not authenticated

### Remove avatar
```
DELETE /api/v1/me/avatar
```

**Response `200 OK`** — returns updated `UserDto` with `avatarPath: null`, `avatarStatus: "None"`.

### Serve avatar image
```
GET /api/v1/files/avatars/{fileName}
```

**Response `200 OK`** — `image/jpeg` stream. `304 Not Found` if worker hasn't saved the file yet or the file was deleted.

No authentication required.

---

## Business Rules

1. Uploading a new photo deletes the old file immediately (if `AvatarStatus == Ready`), then sets `AvatarPath` to the new future path and `AvatarStatus = Pending`.
2. Removing a photo deletes the file (if `AvatarStatus == Ready`) and resets `AvatarPath = null`, `AvatarStatus = None`. The `AvatarColor` is unaffected and re-appears as the display.
3. If the worker fails to process the image, `AvatarStatus` is set to `Failed`. The frontend shows an error and the user can retry by uploading again.
4. File validation (image type, 10 MB limit) happens in `ProfileService` before the file reaches Redis.
5. The avatar `AvatarColor` is always stored independently and used as the fallback display when no photo is active.

---

## Worker Pipeline

```
User uploads photo (cropped to square by frontend)
  → API validates (image/*, ≤ 10 MB)
  → Store bytes in Redis: key = avatar:temp:{userId}, TTL = 30 min
  → Update user: AvatarPath = "avatars/{uuid}.jpg", AvatarStatus = Pending
  → Publish AvatarUploadMessage → RabbitMQ

AvatarWorker consumes AvatarUploadMessage
  → Read bytes from Redis
  → If null (TTL expired): set AvatarStatus = Failed, stop
  → Center-crop to square, resize to 256×256, encode JPEG (quality 85)
  → Save to /app/uploads/avatars/{uuid}.jpg via IBlobService
  → Delete Redis key
  → Update user: AvatarStatus = Ready
```

The worker is `TaskFlow.AvatarWorker` — a separate MassTransit consumer project distinct from `TaskFlow.FileLoader.Worker`.

### Image processing (`SixLabors.ImageSharp`)

Input: any image format the browser produces (JPEG, PNG, WebP). The frontend already crops to a square (via `react-easy-crop` canvas export) so server-side crop is a safety net only.

Output: 256×256 JPEG at quality 85, metadata stripped.

---

## Frontend

### Avatar display — `UserAvatar` component

```tsx
<UserAvatar
  displayName={user.displayName}
  avatarColor={user.avatarColor}
  avatarPath={user.avatarPath}   // null = color mode
  avatarStatus={user.avatarStatus}
  size="md"                       // xs | sm | md | lg
/>
```

Renders:
- `Ready` + `avatarPath` → `<img src="/api/v1/files/{avatarPath}" />`
- `Pending` → spinner overlay on the colored initials
- Anything else → colored initials

### Crop UI — `AvatarCropModal`

Uses `react-easy-crop` with a circular viewport. User drags/zooms, then clicks **Apply** which runs `getCroppedImg()` (canvas-based crop → JPEG `File` object). The resulting `File` is uploaded immediately.

### Profile modal

The **Profile settings** modal has an **Avatar** section at the top:
- Color picker (always visible — sets the fallback color, saved with profile form)
- **Upload photo** / **Change photo** buttons
- **Remove photo** button (only when a photo is active)
- Processing/error state feedback

After upload, `useProfile` polls `GET /me` every 2 s while `avatarStatus === "Pending"` and updates the auth store on each successful fetch, so the sidebar and comment avatars update automatically.

---

## DTO Reference

### `UserDto`
```json
{
  "userId": "string",
  "email": "string",
  "displayName": "string",
  "avatarColor": "#818cf8",
  "avatarPath": "uuid.jpg",
  "avatarStatus": "Ready"
}
```

`avatarPath` is the **filename only** (Path.GetFileName). Prepend `/api/v1/files/avatars/` on the client to build the full URL.

### `MemberDto`
```json
{
  "userId": "string",
  "displayName": "string",
  "email": "string",
  "avatarColor": "#818cf8",
  "avatarPath": "uuid.jpg",
  "role": "Member",
  "joinedAt": "2026-01-01T00:00:00Z"
}
```

### `TaskCommentDto` (avatar fields)
```json
{
  "createdByAvatarColor": "#818cf8",
  "createdByAvatarPath": "uuid.jpg"
}
```

### `AvatarUploadMessage` (RabbitMQ contract)
```json
{
  "userId": "string",
  "redisKey": "avatar:temp:{userId}",
  "permanentPath": "/app/uploads/avatars/uuid.jpg"
}
```
