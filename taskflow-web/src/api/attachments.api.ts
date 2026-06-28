import { apiClient } from './client'
import type { AttachmentDto } from '../types/api.types'

const base = (workspaceId: string, taskId: string) =>
  `/workspaces/${workspaceId}/tasks/${taskId}/attachments`

export const attachmentsApi = {
  list: (workspaceId: string, taskId: string) =>
    apiClient
      .get<AttachmentDto[]>(base(workspaceId, taskId))
      .then((r) => r.data),

  upload: (workspaceId: string, taskId: string, file: File, commentId?: string) => {
    const form = new FormData()
    form.append('file', file)
    return apiClient
      .post<AttachmentDto>(base(workspaceId, taskId), form, {
        headers: { 'Content-Type': 'multipart/form-data' },
        params: commentId ? { commentId } : undefined,
      })
      .then((r) => r.data)
  },

  download: (workspaceId: string, taskId: string, attachmentId: string) =>
    apiClient
      .get(`${base(workspaceId, taskId)}/${attachmentId}/download`, {
        responseType: 'blob',
      })
      .then((r) => r.data as Blob),

  remove: (workspaceId: string, taskId: string, attachmentId: string) =>
    apiClient
      .delete<void>(`${base(workspaceId, taskId)}/${attachmentId}`)
      .then((r) => r.data),
}
