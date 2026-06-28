import { apiClient } from './client'
import type {
  CreateWorkspaceRequest,
  UpdateArchiveSettingsRequest,
  UpdateWorkspaceRequest,
  WorkspaceDto,
} from '../types/api.types'

export const workspacesApi = {
  list: () => apiClient.get<WorkspaceDto[]>('/workspaces').then((r) => r.data),

  getById: (id: string) => apiClient.get<WorkspaceDto>(`/workspaces/${id}`).then((r) => r.data),

  create: (data: CreateWorkspaceRequest) =>
    apiClient.post<WorkspaceDto>('/workspaces', data).then((r) => r.data),

  update: (id: string, data: UpdateWorkspaceRequest) =>
    apiClient.put<WorkspaceDto>(`/workspaces/${id}`, data).then((r) => r.data),

  remove: (id: string) => apiClient.delete<void>(`/workspaces/${id}`).then((r) => r.data),

  updateArchiveSettings: (id: string, data: UpdateArchiveSettingsRequest) =>
    apiClient.put<WorkspaceDto>(`/workspaces/${id}/archive-settings`, data).then((r) => r.data),
}
