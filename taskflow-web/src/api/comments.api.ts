import { apiClient } from './client'
import type {
  CreateCommentRequest,
  PagedResult,
  TaskCommentDto,
  UpdateCommentRequest,
} from '../types/api.types'

export const commentsApi = {
  list: (workspaceId: string, taskId: string, cursor?: string | null, limit = 20) =>
    apiClient
      .get<PagedResult<TaskCommentDto>>(
        `/workspaces/${workspaceId}/tasks/${taskId}/comments`,
        { params: { cursor: cursor ?? undefined, limit } },
      )
      .then((r) => r.data),

  create: (workspaceId: string, taskId: string, data: CreateCommentRequest) =>
    apiClient
      .post<TaskCommentDto>(`/workspaces/${workspaceId}/tasks/${taskId}/comments`, data)
      .then((r) => r.data),

  update: (workspaceId: string, taskId: string, commentId: string, data: UpdateCommentRequest) =>
    apiClient
      .put<TaskCommentDto>(
        `/workspaces/${workspaceId}/tasks/${taskId}/comments/${commentId}`,
        data,
      )
      .then((r) => r.data),

  remove: (workspaceId: string, taskId: string, commentId: string) =>
    apiClient
      .delete<void>(`/workspaces/${workspaceId}/tasks/${taskId}/comments/${commentId}`)
      .then((r) => r.data),
}
