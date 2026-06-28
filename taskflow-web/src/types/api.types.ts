export interface HealthDto {
  status: string
  checkedAt: string
}

export interface UserDto {
  userId: string
  email: string
  displayName: string
  avatarColor: string
}

export interface UpdateProfileRequest {
  displayName: string
  email: string
  avatarColor: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
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
  avatarColor: string
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
  color: string
  order: number
  createdAt: string
}

export interface CreateColumnRequest {
  name: string
  color?: string
}

export interface UpdateColumnRequest {
  name: string
  color?: string
}

export interface ReorderColumnsRequest {
  columnIds: string[]
}

export type TaskPriority = 'Low' | 'Medium' | 'High'

export interface LabelDto {
  id: string
  name: string
  color: string
}

export interface PriorityConfigDto {
  priority: TaskPriority
  displayName: string
  color: string
}

export interface CreateLabelRequest {
  name: string
  color: string
}

export interface UpdateLabelRequest {
  name: string
  color: string
}

export interface UpdatePriorityConfigRequest {
  displayName: string
  color: string
}

export interface SetTaskLabelsRequest {
  labelIds: string[]
}

export interface WorkspaceTaskDto {
  id: string
  number: number
  workspaceId: string
  columnId: string
  columnName: string
  title: string
  description: string | null
  order: number
  priority: TaskPriority
  assigneeId: string | null
  assigneeName: string | null
  assigneeAvatarColor: string | null
  dueDate: string | null
  isOverdue: boolean
  createdById: string
  createdByName: string
  createdAt: string
  updatedAt: string
  labels: LabelDto[]
}

export interface CreateTaskRequest {
  title: string
  columnId: string
  description?: string | null
  priority?: TaskPriority
  assigneeId?: string | null
  dueDate?: string | null
  labelIds?: string[]
}

export interface UpdateTaskRequest {
  title: string
  description: string | null
  priority: TaskPriority
  assigneeId: string | null
  dueDate: string | null
}

export interface MoveTaskRequest {
  columnId: string
}

export interface TaskFilterParams {
  search?: string
  assigneeIds?: string[]
  priorities?: TaskPriority[]
  labelIds?: string[]
}

export interface PagedResult<T> {
  items: T[]
  nextCursor: string | null
  hasMore: boolean
}

export interface TaskCommentDto {
  id: string
  taskId: string
  content: string
  createdById: string
  createdByName: string
  createdByAvatarColor: string
  createdAt: string
  updatedAt: string
  isEdited: boolean
  mentionedUserIds: string[]
  attachments: AttachmentDto[]
}

export type AttachmentStatus = 'Pending' | 'Processing' | 'Ready' | 'Failed'

export interface AttachmentDto {
  id: string
  taskId: string
  commentId: string | null
  originalFileName: string
  contentType: string
  fileSizeBytes: number
  status: AttachmentStatus
  processingError: string | null
  uploadedById: string
  uploadedByName: string
  uploadedByAvatarColor: string
  uploadedAt: string
  processedAt: string | null
}

export interface CreateCommentRequest {
  content: string
}

export interface UpdateCommentRequest {
  content: string
}
