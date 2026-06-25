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

export type WorkspaceRole = 'Owner' | 'Admin' | 'Member'

export interface WorkspaceDto {
  id: string
  name: string
  ownerId: string
  createdAt: string
  myRole: WorkspaceRole
}

export interface CreateWorkspaceRequest {
  name: string
}

export interface UpdateWorkspaceRequest {
  name: string
}

export interface MemberDto {
  userId: string
  displayName: string
  email: string
  role: WorkspaceRole
  joinedAt: string
}

export interface InviteMemberRequest {
  email: string
  role: WorkspaceRole
}

export interface UpdateMemberRoleRequest {
  role: WorkspaceRole
}

export interface WorkspaceColumnDto {
  id: string
  workspaceId: string
  name: string
  order: number
  createdAt: string
}

export interface CreateColumnRequest {
  name: string
}

export interface RenameColumnRequest {
  name: string
}

export interface ReorderColumnsRequest {
  columnIds: string[]
}
