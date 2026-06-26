# Profile Management

Allows authenticated users to view and update their own profile: display name, email, avatar color, and password.

---

## Data Model

Changes to `AppUser` (ASP.NET Identity):

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `AvatarColor` | `string` | `"#818cf8"` | Hex color string, e.g. `#818cf8` |

All other fields (`DisplayName`, `Email`, `PasswordHash`, etc.) are inherited from `IdentityUser`.

---

## Authorization

All endpoints require a valid JWT (`[Authorize]`). Users may only read and modify their own profile — the user id is always taken from the JWT claim (`User.GetUserId()`), never from the request body.

---

## Endpoints

### GET /api/v1/me

Returns the authenticated user's profile.

**Response 200 OK**

```json
{
  "userId": "abc123",
  "email": "user@example.com",
  "displayName": "Alice Smith",
  "avatarColor": "#818cf8"
}
```

---

### PUT /api/v1/me

Update display name, email, and/or avatar color.

**Request body**

| Field | Type | Validation |
|-------|------|------------|
| `displayName` | string | Required, ≤ 100 chars |
| `email` | string | Required, valid email, ≤ 200 chars |
| `avatarColor` | string | Required, matches `^#[0-9a-fA-F]{6}$` |

**Response 200 OK** — returns updated `UserDto`.

**Response 400 Bad Request** — validation failure.

**Response 409 Conflict** — `email` is already in use by another account.

---

### PUT /api/v1/me/password

Change the user's password. The current password must be provided for verification.

**Request body**

| Field | Type | Validation |
|-------|------|------------|
| `currentPassword` | string | Required |
| `newPassword` | string | Required, ≥ 8 chars, must contain a digit and an uppercase letter |

**Response 204 No Content** — password changed.

**Response 400 Bad Request** — current password incorrect, or new password doesn't meet ASP.NET Identity requirements.

---

## Business Rules

- Email uniqueness is checked before updating; a `409 Conflict` is returned if the email belongs to another user.
- When the email changes, `NormalizedEmail`, `UserName`, and `NormalizedUserName` are updated atomically via `UserManager.UpdateAsync`.
- Password change delegates to `UserManager.ChangePasswordAsync`, which enforces the same rules as registration (RequireDigit, RequiredLength = 8, RequireUppercase).
- The `AvatarColor` must be a 6-digit hex color with `#` prefix; the backend validates with a regex; the frontend exposes 8 preset swatches.

---

## DTO Reference

```csharp
// Response
record UserDto(string UserId, string Email, string DisplayName, string AvatarColor);

// Requests
record UpdateProfileRequest(string DisplayName, string Email, string AvatarColor);
record ChangePasswordRequest(string CurrentPassword, string NewPassword);
```

---

## Frontend

- Clicking the user avatar/name in the bottom-left of the sidebar opens `ProfileModal`.
- The modal has three sections: profile info (name, email, color picker), password change, and logout.
- On successful profile update, `authStore.updateUser(user)` is called so the sidebar reflects the new name, email, and color immediately.
- The color picker shows 8 preset swatches; the selected color is previewed on the avatar in real time.
