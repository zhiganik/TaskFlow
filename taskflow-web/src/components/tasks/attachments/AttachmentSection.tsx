import { useEffect, useRef } from 'react'
import { getErrorMessage } from '../../../api/errors'
import { useAttachments, useDeleteAttachment, useUploadAttachment } from '../../../hooks/useAttachments'
import { Spinner } from '../../ui/Spinner'
import { AttachmentChip } from './AttachmentChip'

interface Props {
  workspaceId: string
  taskId: string
  canDelete?: boolean
}

export function AttachmentSection({ workspaceId, taskId, canDelete = false }: Props) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const { data: attachments = [], isLoading } = useAttachments(workspaceId, taskId)
  const uploadMutation = useUploadAttachment(workspaceId, taskId)
  const deleteMutation = useDeleteAttachment(workspaceId, taskId)

  // clear error when switching to a different task
  useEffect(() => {
    uploadMutation.reset()
  }, [taskId]) // eslint-disable-line react-hooks/exhaustive-deps

  // auto-dismiss after 6 s
  useEffect(() => {
    if (!uploadMutation.isError) return
    const id = setTimeout(() => uploadMutation.reset(), 6000)
    return () => clearTimeout(id)
  }, [uploadMutation.isError]) // eslint-disable-line react-hooks/exhaustive-deps

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    uploadMutation.mutate({ file })
    e.target.value = ''
  }

  return (
    <div>
      <div className="mb-2.5 flex items-center justify-between">
        <span className="text-[11px] text-gray-400">
          {attachments.length > 0 ? `${attachments.length} file${attachments.length === 1 ? '' : 's'}` : ''}
        </span>
        <button
          type="button"
          disabled={uploadMutation.isPending}
          onClick={() => fileInputRef.current?.click()}
          className="flex items-center gap-1 rounded px-2 py-1 text-xs text-brand-600 hover:bg-brand-50 disabled:opacity-50"
        >
          {uploadMutation.isPending ? (
            <Spinner className="h-3.5 w-3.5 border-brand-200 border-t-brand-500" />
          ) : (
            <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" className="h-3.5 w-3.5">
              <line x1="8" y1="2" x2="8" y2="14" />
              <line x1="2" y1="8" x2="14" y2="8" />
            </svg>
          )}
          Attach file
        </button>
        <input
          ref={fileInputRef}
          type="file"
          className="hidden"
          onChange={handleFileChange}
        />
      </div>

      {/* upload error */}
      {uploadMutation.isError && (
        <div className="mb-2 flex items-start gap-1.5 rounded-md border border-red-200 bg-red-50 px-2.5 py-2 text-xs text-red-700">
          <svg viewBox="0 0 16 16" fill="currentColor" className="mt-px h-3.5 w-3.5 shrink-0 text-red-500">
            <path d="M8 1a7 7 0 100 14A7 7 0 008 1zm-.5 3.5h1v4h-1v-4zm0 5h1v1h-1v-1z" />
          </svg>
          <span className="flex-1">{getErrorMessage(uploadMutation.error)}</span>
          <button
            type="button"
            onClick={() => uploadMutation.reset()}
            className="ml-1 shrink-0 text-red-400 hover:text-red-600"
            aria-label="Dismiss"
          >
            <svg viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="h-3 w-3">
              <line x1="2" y1="2" x2="10" y2="10" />
              <line x1="10" y1="2" x2="2" y2="10" />
            </svg>
          </button>
        </div>
      )}

      {isLoading ? (
        <div className="flex justify-center py-3">
          <Spinner className="h-4 w-4 border-gray-200 border-t-gray-400" />
        </div>
      ) : attachments.length === 0 ? (
        <button
          type="button"
          onClick={() => fileInputRef.current?.click()}
          className="flex w-full items-center justify-center gap-2 rounded-lg border border-dashed border-gray-200 py-4 text-sm text-gray-400 transition-colors hover:border-brand-300 hover:text-brand-500"
        >
          <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" className="h-4 w-4">
            <path d="M2 10v3a1 1 0 001 1h10a1 1 0 001-1v-3" />
            <polyline points="5 6 8 3 11 6" />
            <line x1="8" y1="3" x2="8" y2="10" />
          </svg>
          Drop a file or click to attach
        </button>
      ) : (
        <div className="flex flex-wrap gap-2.5">
          {attachments.map((a) => (
            <AttachmentChip
              key={a.id}
              attachment={a}
              workspaceId={workspaceId}
              taskId={taskId}
              canDelete={canDelete}
              onDelete={(id) => deleteMutation.mutate(id)}
            />
          ))}
        </div>
      )}
    </div>
  )
}
