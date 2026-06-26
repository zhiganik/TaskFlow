import { apiClient } from './client'
import type { CreateLabelRequest, LabelDto, SetTaskLabelsRequest, UpdateLabelRequest, WorkspaceTaskDto } from '../types/api.types'

export const labelsApi = {
  list: (workspaceId: string) =>
    apiClient.get<LabelDto[]>(`/workspaces/${workspaceId}/labels`).then((r) => r.data),

  create: (workspaceId: string, data: CreateLabelRequest) =>
    apiClient.post<LabelDto>(`/workspaces/${workspaceId}/labels`, data).then((r) => r.data),

  update: (workspaceId: string, labelId: string, data: UpdateLabelRequest) =>
    apiClient.put<LabelDto>(`/workspaces/${workspaceId}/labels/${labelId}`, data).then((r) => r.data),

  remove: (workspaceId: string, labelId: string) =>
    apiClient.delete<void>(`/workspaces/${workspaceId}/labels/${labelId}`).then((r) => r.data),

  setTaskLabels: (workspaceId: string, taskId: string, data: SetTaskLabelsRequest) =>
    apiClient.put<WorkspaceTaskDto>(`/workspaces/${workspaceId}/tasks/${taskId}/labels`, data).then((r) => r.data),
}
