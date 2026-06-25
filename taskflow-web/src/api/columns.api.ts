import { apiClient } from './client'
import type {
  CreateColumnRequest,
  RenameColumnRequest,
  ReorderColumnsRequest,
  WorkspaceColumnDto,
} from '../types/api.types'

export const columnsApi = {
  list: (workspaceId: string) =>
    apiClient
      .get<WorkspaceColumnDto[]>(`/workspaces/${workspaceId}/columns`)
      .then((r) => r.data),

  create: (workspaceId: string, data: CreateColumnRequest) =>
    apiClient
      .post<WorkspaceColumnDto>(`/workspaces/${workspaceId}/columns`, data)
      .then((r) => r.data),

  rename: (workspaceId: string, columnId: string, data: RenameColumnRequest) =>
    apiClient
      .put<WorkspaceColumnDto>(`/workspaces/${workspaceId}/columns/${columnId}`, data)
      .then((r) => r.data),

  reorder: (workspaceId: string, data: ReorderColumnsRequest) =>
    apiClient
      .put<void>(`/workspaces/${workspaceId}/columns/reorder`, data)
      .then((r) => r.data),

  remove: (workspaceId: string, columnId: string) =>
    apiClient
      .delete<void>(`/workspaces/${workspaceId}/columns/${columnId}`)
      .then((r) => r.data),
}
