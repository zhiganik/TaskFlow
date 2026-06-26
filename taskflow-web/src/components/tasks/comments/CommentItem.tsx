import { useState } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import type { MemberDto, TaskCommentDto } from '../../../types/api.types'
import { CommentInput } from './CommentInput'

interface Props {
  comment: TaskCommentDto
  currentUserId: string | undefined
  members: MemberDto[]
  onUpdate: (commentId: string, content: string) => Promise<void>
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

export function CommentItem({ comment, currentUserId, members, onUpdate, onDelete }: Props) {
  const [editing, setEditing] = useState(false)
  const [isUpdating, setIsUpdating] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const isOwn = comment.createdById === currentUserId

  const handleUpdate = async (content: string) => {
    setIsUpdating(true)
    try {
      await onUpdate(comment.id, content)
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

  return (
    <div className="group flex gap-2.5 py-3">
      <span className="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-brand-100 text-[10px] font-semibold text-brand-700">
        {comment.createdByName.slice(0, 2).toUpperCase()}
      </span>

      <div className="min-w-0 flex-1">
        <div className="mb-1 flex items-baseline gap-2">
          <span className="text-xs font-medium text-gray-800">{comment.createdByName}</span>
          <span className="text-[11px] text-gray-400">{formatRelative(comment.createdAt)}</span>
          {comment.isEdited && <span className="text-[11px] text-gray-400">(edited)</span>}

          {isOwn && !editing && (
            <span className="ml-auto hidden gap-1 group-hover:flex">
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
            </span>
          )}
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
          <div className="prose prose-sm max-w-none break-words text-gray-700 [&_.mention]:rounded [&_.mention]:bg-brand-50 [&_.mention]:px-1 [&_.mention]:text-brand-700 [&_.mention]:font-medium">
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
      </div>
    </div>
  )
}
