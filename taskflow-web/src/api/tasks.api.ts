import { apiClient } from './client'
import type {
  CreateTaskRequest,
  MoveTaskRequest,
  TaskFilterParams,
  UpdateTaskRequest,
  WorkspaceTaskDto,
} from '../types/api.types'

export const tasksApi = {
  list: (workspaceId: string, filter?: TaskFilterParams) =>
    apiClient
      .get<WorkspaceTaskDto[]>(`/workspaces/${workspaceId}/tasks`, {
        params: {
          search: filter?.search || undefined,
          assigneeId: filter?.assigneeId || undefined,
          priorities: filter?.priorities?.length ? filter.priorities : undefined,
        },
        paramsSerializer: { indexes: null },
      })
      .then((r) => r.data),

  create: (workspaceId: string, data: CreateTaskRequest) =>
    apiClient
      .post<WorkspaceTaskDto>(`/workspaces/${workspaceId}/tasks`, data)
      .then((r) => r.data),

  update: (workspaceId: string, taskId: string, data: UpdateTaskRequest) =>
    apiClient
      .put<WorkspaceTaskDto>(`/workspaces/${workspaceId}/tasks/${taskId}`, data)
      .then((r) => r.data),

  move: (workspaceId: string, taskId: string, data: MoveTaskRequest) =>
    apiClient
      .put<WorkspaceTaskDto>(`/workspaces/${workspaceId}/tasks/${taskId}/move`, data)
      .then((r) => r.data),

  remove: (workspaceId: string, taskId: string) =>
    apiClient
      .delete<void>(`/workspaces/${workspaceId}/tasks/${taskId}`)
      .then((r) => r.data),
}
