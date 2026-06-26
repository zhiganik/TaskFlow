import { apiClient } from './client'
import type { PriorityConfigDto, TaskPriority, UpdatePriorityConfigRequest } from '../types/api.types'

export const priorityConfigApi = {
  list: (workspaceId: string) =>
    apiClient.get<PriorityConfigDto[]>(`/workspaces/${workspaceId}/priority-configs`).then((r) => r.data),

  update: (workspaceId: string, priority: TaskPriority, data: UpdatePriorityConfigRequest) =>
    apiClient
      .put<PriorityConfigDto>(`/workspaces/${workspaceId}/priority-configs/${priority}`, data)
      .then((r) => r.data),
}
