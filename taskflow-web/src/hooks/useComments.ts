import { useInfiniteQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { commentsApi } from '../api/comments.api'
import type { CreateCommentRequest, UpdateCommentRequest } from '../types/api.types'

const commentsKey = (workspaceId: string, taskId: string) => ['comments', workspaceId, taskId]

export const useComments = (workspaceId: string, taskId: string) =>
  useInfiniteQuery({
    queryKey: commentsKey(workspaceId, taskId),
    queryFn: ({ pageParam }) => commentsApi.list(workspaceId, taskId, pageParam),
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.nextCursor : undefined),
    enabled: !!taskId,
  })

export const useCreateComment = (workspaceId: string, taskId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateCommentRequest) => commentsApi.create(workspaceId, taskId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: commentsKey(workspaceId, taskId) }),
  })
}

export const useUpdateComment = (workspaceId: string, taskId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ commentId, data }: { commentId: string; data: UpdateCommentRequest }) =>
      commentsApi.update(workspaceId, taskId, commentId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: commentsKey(workspaceId, taskId) }),
  })
}

export const useDeleteComment = (workspaceId: string, taskId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (commentId: string) => commentsApi.remove(workspaceId, taskId, commentId),
    onSuccess: () => qc.invalidateQueries({ queryKey: commentsKey(workspaceId, taskId) }),
  })
}
