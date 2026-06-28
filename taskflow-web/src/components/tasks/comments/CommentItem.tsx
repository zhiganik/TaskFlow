import { useEffect, useRef, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { getErrorMessage } from '../../../api/errors'
import { useAttachments, useDeleteAttachment, useUploadAttachment } from '../../../hooks/useAttachments'
import type { MemberDto, TaskCommentDto } from '../../../types/api.types'
import { AttachmentChip } from '../attachments/AttachmentChip'
import { CommentInput } from './CommentInput'

interface Props {
  comment: TaskCommentDto
  workspaceId: string
  taskId: string
  currentUserId: string | undefined
  members: MemberDto[]
  onUpdate: (commentId: string, content: string, stagedFiles: File[]) => Promise<void>
  onDelete: (commentId: string) => Promise<void>
}

function formatRelative(iso: string) {
  const diff = Date.now() - new Date(iso).getTime()
  const mins = Math.floor(diff / 60_000)
  if (mins < 1) return 'just now'
  if (mins < 60) return `${mins}m ago`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24) return `${hrs}h ago`
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

export function CommentItem({ comment, workspaceId, taskId, currentUserId, members, onUpdate, onDelete }: Props) {
  const [editing, setEditing] = useState(false)
  const [isUpdating, setIsUpdating] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const isOwn = comment.createdById === currentUserId

  // use the shared polling query and filter by this comment's id — updates at the same rate as the task section
  const { data: allAttachments = [] } = useAttachments(workspaceId, taskId)
  const commentAttachments = allAttachments.filter((a) => a.commentId === comment.id)

  const uploadMutation = useUploadAttachment(workspaceId, taskId)
  const deleteMutation = useDeleteAttachment(workspaceId, taskId)

  useEffect(() => {
    if (!uploadMutation.isError) return
    const id = setTimeout(() => uploadMutation.reset(), 6000)
    return () => clearTimeout(id)
  }, [uploadMutation.isError]) // eslint-disable-line react-hooks/exhaustive-deps

  const handleUpdate = async (content: string, stagedFiles: File[]) => {
    setIsUpdating(true)
    try {
      await onUpdate(comment.id, content, stagedFiles)
      setEditing(false)
    } finally {
      setIsUpdating(false)
    }
  }

  const handleDelete = async () => {
    setIsDeleting(true)
    try {
      await onDelete(comment.id)
    } finally {
      setIsDeleting(false)
    }
  }

  const handleAttach = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    uploadMutation.mutate({ file, commentId: comment.id })
    e.target.value = ''
  }

  return (
    <div className="group flex gap-2.5 py-3">
      <span
        className="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full text-[10px] font-semibold text-white"
        style={{ backgroundColor: comment.createdByAvatarColor }}
      >
        {comment.createdByName.slice(0, 2).toUpperCase()}
      </span>

      <div className="min-w-0 flex-1">
        <div className="mb-1 flex items-baseline gap-2">
          <span className="text-xs font-medium text-gray-800">{comment.createdByName}</span>
          <span className="text-[11px] text-gray-400">{formatRelative(comment.createdAt)}</span>
          {comment.isEdited && <span className="text-[11px] text-gray-400">(edited)</span>}

          {!editing && (
            <span className="ml-auto hidden items-center gap-1 group-hover:flex">
              <button
                type="button"
                disabled={uploadMutation.isPending}
                onClick={() => fileInputRef.current?.click()}
                className="rounded px-1.5 py-0.5 text-[11px] text-gray-400 hover:bg-gray-100 hover:text-gray-600 disabled:opacity-50"
                title="Attach file to this comment"
              >
                <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" className="inline h-3.5 w-3.5">
                  <path d="M17.5 11.5l-7.5 7.5a5 5 0 01-7-7l8-8a3 3 0 014 4l-8 8a1 1 0 01-1.5-1.5l7.5-7.5" />
                </svg>
              </button>
              {isOwn && (
                <>
                  <button
                    type="button"
                    onClick={() => setEditing(true)}
                    className="rounded px-1.5 py-0.5 text-[11px] text-gray-400 hover:bg-gray-100 hover:text-gray-600"
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    disabled={isDeleting}
                    onClick={handleDelete}
                    className="rounded px-1.5 py-0.5 text-[11px] text-gray-400 hover:bg-red-50 hover:text-red-500 disabled:opacity-50"
                  >
                    {isDeleting ? '…' : 'Delete'}
                  </button>
                </>
              )}
            </span>
          )}
          <input ref={fileInputRef} type="file" className="hidden" onChange={handleAttach} />
        </div>

        {editing ? (
          <CommentInput
            members={members}
            initialValue={comment.content}
            submitLabel="Save"
            isSubmitting={isUpdating}
            onSubmit={handleUpdate}
            onCancel={() => setEditing(false)}
          />
        ) : (
          <div className="prose prose-sm max-w-none break-words text-gray-700 [&_.mention]:rounded [&_.mention]:bg-brand-50 [&_.mention]:px-1 [&_.mention]:font-medium [&_.mention]:text-brand-700">
            <ReactMarkdown
              remarkPlugins={[remarkGfm]}
              components={{
                a: ({ href, children }) => {
                  if (href && /^[0-9a-f-]{36}$/i.test(href)) {
                    return <span className="mention">{children}</span>
                  }
                  return (
                    <a href={href} target="_blank" rel="noopener noreferrer" className="text-brand-600 underline">
                      {children}
                    </a>
                  )
                },
              }}
            >
              {comment.content.replace(/@\[([^\]]+)\]\(([^)]+)\)/g, '[@$1]($2)')}
            </ReactMarkdown>
          </div>
        )}

        {/* upload error for the hover Attach button */}
        {uploadMutation.isError && (
          <div className="mt-1.5 flex items-start gap-1.5 rounded-md border border-red-200 bg-red-50 px-2.5 py-1.5 text-[11px] text-red-700">
            <svg viewBox="0 0 16 16" fill="currentColor" className="mt-px h-3 w-3 shrink-0 text-red-500">
              <path d="M8 1a7 7 0 100 14A7 7 0 008 1zm-.5 3.5h1v4h-1v-4zm0 5h1v1h-1v-1z" />
            </svg>
            <span className="flex-1">{getErrorMessage(uploadMutation.error)}</span>
            <button
              type="button"
              onClick={() => uploadMutation.reset()}
              className="ml-1 shrink-0 text-red-400 hover:text-red-600"
              aria-label="Dismiss"
            >
              <svg viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="h-2.5 w-2.5">
                <line x1="2" y1="2" x2="10" y2="10" />
                <line x1="10" y1="2" x2="2" y2="10" />
              </svg>
            </button>
          </div>
        )}

        {/* comment-level attachments — driven by the shared polling query so status updates instantly */}
        {commentAttachments.length > 0 && (
          <div className="mt-2 flex flex-wrap gap-2">
            {commentAttachments.map((a) => (
              <AttachmentChip
                key={a.id}
                attachment={a}
                workspaceId={workspaceId}
                taskId={taskId}
                canDelete={isOwn}
                onDelete={(id) => deleteMutation.mutate(id)}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
