# Auth

JWT-based authentication built on ASP.NET Identity. Access tokens are short-lived; refresh
tokens are single-use and rotate on every exchange. The frontend persists both tokens in
`localStorage` via the `authStore` zustand middleware.

---

## Data Model

ASP.NET Identity manages the `AspNetUsers` table. Two custom fields are added via `AppUser`:

| Field         | Type       | Description                     |
|---------------|------------|---------------------------------|
| `id`          | `string`   | Identity PK (GUID string)       |
| `email`       | `string`   | Unique, used as username        |
| `displayName` | `string`   | Human-readable name             |
| `createdAt`   | `datetime` | UTC account creation timestamp  |

Refresh tokens are stored in Redis with a TTL equal to the configured refresh token lifetime.

---

## Authorization

All auth endpoints are **public** (no `[Authorize]` attribute). The tokens they issue are used
by every other endpoint in the API.

---

## Endpoints

### Register

```
POST /api/v1/auth/register
```

Creates a new account.

**Request body**

```json
{ "email": "user@example.com", "displayName": "Jane", "password": "Secret1!" }
```

| Field         | Required | Rules                                          |
|---------------|----------|------------------------------------------------|
| `email`       | yes      | valid email format, unique                     |
| `displayName` | yes      | non-empty                                      |
| `password`    | yes      | min 8 chars, at least 1 digit, 1 uppercase     |

**Responses**

| Status | Body             |
|--------|------------------|
| 201    | `UserDto`        |
| 400    | `ValidationProblemDetails` |

**Example response**
```json
{ "userId": "...", "email": "user@example.com", "displayName": "Jane" }
```

---

### Login

```
POST /api/v1/auth/login
```

Validates credentials and issues a token pair.

**Request body**

```json
{ "email": "user@example.com", "password": "Secret1!" }
```

**Responses**

| Status | Body             |
|--------|------------------|
| 200    | `AuthResponseDto`|
| 400    | `ValidationProblemDetails` |
| 401    | `ProblemDetails` — wrong credentials |

**Example response**
```json
{
  "accessToken":  "eyJ...",
  "expiresAt":    "2026-06-25T20:00:00Z",
  "refreshToken": "opaque-random-string",
  "user": { "userId": "...", "email": "user@example.com", "displayName": "Jane" }
}
```

---

### Refresh

```
POST /api/v1/auth/refresh
```

Exchanges a refresh token for a **new** access token and refresh token. The old refresh token
is invalidated immediately (single-use rotation).

**Request body**

```json
{ "refreshToken": "opaque-random-string" }
```

**Responses**

| Status | Body             |
|--------|------------------|
| 200    | `AuthResponseDto`|
| 401    | `ProblemDetails` — token unknown or expired |

---

### Logout

```
POST /api/v1/auth/logout
```

Revokes the provided refresh token. The access token expires naturally (no blocklist).

**Request body**

```json
{ "refreshToken": "opaque-random-string" }
```

**Responses**

| Status | Body |
|--------|------|
| 204    | —    |
| 400    | `ValidationProblemDetails` |

---

## Token Details

| Token        | Storage | Lifetime         | Notes                                  |
|--------------|---------|------------------|----------------------------------------|
| Access token | JWT     | Short (minutes)  | Bearer header on every protected route |
| Refresh token| Redis   | Longer (days)    | Single-use; rotates on each refresh    |

The `Authorization` header format for protected routes:

```
Authorization: Bearer <accessToken>
```

---

## DTO Reference

```typescript
// UserDto
{ userId: string; email: string; displayName: string }

// AuthResponseDto
{
  accessToken:  string
  expiresAt:    string   // ISO 8601 UTC
  refreshToken: string
  user:         UserDto
}
```
