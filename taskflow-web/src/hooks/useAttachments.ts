import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { attachmentsApi } from '../api/attachments.api'
import type { AttachmentDto } from '../types/api.types'

const keys = {
  list: (workspaceId: string, taskId: string) =>
    ['attachments', workspaceId, taskId] as const,
}

export function useAttachments(workspaceId: string, taskId: string) {
  return useQuery({
    queryKey: keys.list(workspaceId, taskId),
    queryFn: () => attachmentsApi.list(workspaceId, taskId),
    // poll while any attachment is still being processed
    refetchInterval: (query) => {
      const data = query.state.data as AttachmentDto[] | undefined
      const hasPending = data?.some(
        (a) => a.status === 'Pending' || a.status === 'Processing',
      )
      return hasPending ? 3000 : false
    },
  })
}

export function useUploadAttachment(workspaceId: string, taskId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ file, commentId }: { file: File; commentId?: string }) =>
      attachmentsApi.upload(workspaceId, taskId, file, commentId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: keys.list(workspaceId, taskId) })
      // also invalidate comments so comment attachments refresh
      qc.invalidateQueries({ queryKey: ['comments', workspaceId, taskId] })
    },
  })
}

export function useDeleteAttachment(workspaceId: string, taskId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (attachmentId: string) =>
      attachmentsApi.remove(workspaceId, taskId, attachmentId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: keys.list(workspaceId, taskId) })
      qc.invalidateQueries({ queryKey: ['comments', workspaceId, taskId] })
    },
  })
}

export function useDownloadAttachment(workspaceId: string, taskId: string) {
  return useMutation({
    mutationFn: async ({
      attachmentId,
      fileName,
    }: {
      attachmentId: string
      fileName: string
    }) => {
      const blob = await attachmentsApi.download(workspaceId, taskId, attachmentId)
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = fileName
      a.click()
      URL.revokeObjectURL(url)
    },
  })
}
