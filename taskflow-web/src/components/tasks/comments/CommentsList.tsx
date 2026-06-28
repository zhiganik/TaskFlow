import { useEffect, useRef } from 'react'
import { useUploadAttachment } from '../../../hooks/useAttachments'
import { useComments, useCreateComment, useDeleteComment, useUpdateComment } from '../../../hooks/useComments'
import { useMembers } from '../../../hooks/useMembers'
import { useAuthStore } from '../../../store/authStore'
import { Spinner } from '../../ui/Spinner'
import { CommentInput } from './CommentInput'
import { CommentItem } from './CommentItem'

interface Props {
  workspaceId: string
  taskId: string
}

export function CommentsList({ workspaceId, taskId }: Props) {
  const currentUserId = useAuthStore((s) => s.user?.userId)
  const { data: members = [] } = useMembers(workspaceId)

  const { data, fetchNextPage, hasNextPage, isFetchingNextPage, isLoading } = useComments(
    workspaceId,
    taskId,
  )

  const createMutation = useCreateComment(workspaceId, taskId)
  const updateMutation = useUpdateComment(workspaceId, taskId)
  const deleteMutation = useDeleteComment(workspaceId, taskId)
  const uploadMutation = useUploadAttachment(workspaceId, taskId)

  const sentinelRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const sentinel = sentinelRef.current
    if (!sentinel) return

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0]?.isIntersecting && hasNextPage && !isFetchingNextPage) {
          fetchNextPage()
        }
      },
      { threshold: 0.1 },
    )

    observer.observe(sentinel)
    return () => observer.disconnect()
  }, [fetchNextPage, hasNextPage, isFetchingNextPage])

  const comments = data?.pages.flatMap((p) => p.items) ?? []

  const handleCreate = async (content: string, stagedFiles: File[]) => {
    const newComment = await createMutation.mutateAsync({ content })
    for (const file of stagedFiles) {
      uploadMutation.mutate({ file, commentId: newComment.id })
    }
  }

  const handleUpdate = async (commentId: string, content: string, stagedFiles: File[]) => {
    await updateMutation.mutateAsync({ commentId, data: { content } })
    for (const file of stagedFiles) {
      uploadMutation.mutate({ file, commentId })
    }
  }

  const handleDelete = async (commentId: string) => {
    await deleteMutation.mutateAsync(commentId)
  }

  return (
    <div>
      <div className="pb-4">
        <CommentInput
          members={members}
          isSubmitting={createMutation.isPending}
          onSubmit={handleCreate}
        />
      </div>

      {isLoading ? (
        <div className="flex justify-center py-4">
          <Spinner className="h-4 w-4 border-gray-200 border-t-gray-500" />
        </div>
      ) : (
        <div className="divide-y divide-gray-50">
          {comments.map((comment) => (
            <CommentItem
              key={comment.id}
              comment={comment}
              workspaceId={workspaceId}
              taskId={taskId}
              currentUserId={currentUserId}
              members={members}
              onUpdate={handleUpdate}
              onDelete={handleDelete}
            />
          ))}
        </div>
      )}

      {isFetchingNextPage && (
        <div className="flex justify-center py-2">
          <Spinner className="h-3.5 w-3.5 border-gray-200 border-t-gray-500" />
        </div>
      )}

      <div ref={sentinelRef} className="h-px" />
    </div>
  )
}
