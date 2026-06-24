export interface HealthDto {
  status: string
  checkedAt: string
}

export interface UserDto {
  userId: string
  email: string
  displayName: string
}

export interface AuthResponseDto {
  accessToken: string
  expiresAt: string
  refreshToken: string
  user: UserDto
}

export interface RegisterRequest {
  email: string
  displayName: string
  password: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RefreshTokenRequest {
  refreshToken: string
}

// Mirrors Microsoft.AspNetCore.Mvc.ProblemDetails as serialized by GlobalExceptionMiddleware.
export interface ProblemDetails {
  type?: string | null
  title?: string | null
  status?: number
  detail?: string | null
  instance?: string | null
}

// Errors keys are the raw FluentValidation PropertyName (PascalCase, e.g. "Email"),
// not camelCased — System.Text.Json's naming policy doesn't rewrite dictionary keys.
export interface ValidationProblemDetails extends ProblemDetails {
  errors: Record<string, string[]>
}

export interface WorkspaceDto {
  id: string
  name: string
  ownerId: string
  createdAt: string
}

export interface CreateWorkspaceRequest {
  name: string
}

export interface UpdateWorkspaceRequest {
  name: string
}
