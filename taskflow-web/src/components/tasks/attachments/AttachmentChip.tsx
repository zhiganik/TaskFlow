import { useState } from 'react'
import { useDownloadAttachment } from '../../../hooks/useAttachments'
import type { AttachmentDto } from '../../../types/api.types'

interface Props {
  attachment: AttachmentDto
  workspaceId: string
  taskId: string
  onDelete?: (id: string) => void
  canDelete?: boolean
}

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

function FileTypeIcon({ contentType, size = 22 }: { contentType: string; size?: number }) {
  const style = { width: size, height: size }

  if (contentType.startsWith('image/'))
    return (
      <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" style={style}>
        <rect x="2" y="3" width="16" height="14" rx="2" />
        <circle cx="7" cy="8" r="1.5" />
        <polyline points="2 15 7 10 10 13 13 10 18 15" />
      </svg>
    )

  if (contentType === 'application/pdf')
    return (
      <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" style={style}>
        <path d="M4 2h8l4 4v12a1 1 0 01-1 1H4a1 1 0 01-1-1V3a1 1 0 011-1z" />
        <polyline points="12 2 12 6 16 6" />
        <line x1="7" y1="11" x2="13" y2="11" />
        <line x1="7" y1="14" x2="10" y2="14" />
      </svg>
    )

  if (contentType.startsWith('text/') || contentType.includes('json') || contentType.includes('xml'))
    return (
      <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" style={style}>
        <path d="M4 2h8l4 4v12a1 1 0 01-1 1H4a1 1 0 01-1-1V3a1 1 0 011-1z" />
        <polyline points="12 2 12 6 16 6" />
        <line x1="7" y1="11" x2="13" y2="11" />
        <line x1="7" y1="14" x2="13" y2="14" />
      </svg>
    )

  return (
    <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" style={style}>
      <path d="M4 2h8l4 4v12a1 1 0 01-1 1H4a1 1 0 01-1-1V3a1 1 0 011-1z" />
      <polyline points="12 2 12 6 16 6" />
    </svg>
  )
}

export function AttachmentChip({ attachment, workspaceId, taskId, onDelete, canDelete }: Props) {
  const [hovered, setHovered] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const downloadMutation = useDownloadAttachment(workspaceId, taskId)

  const isReady = attachment.status === 'Ready'
  const isFailed = attachment.status === 'Failed'
  const isPending = attachment.status === 'Pending' || attachment.status === 'Processing'

  const handleBodyClick = () => {
    if (confirmDelete) return
    if (!isReady || downloadMutation.isPending) return
    downloadMutation.mutate({ attachmentId: attachment.id, fileName: attachment.originalFileName })
  }

  const handleDeleteClick = (e: React.MouseEvent) => {
    e.stopPropagation()
    setConfirmDelete(true)
  }

  const handleConfirmYes = (e: React.MouseEvent) => {
    e.stopPropagation()
    onDelete?.(attachment.id)
    setConfirmDelete(false)
  }

  const handleConfirmNo = (e: React.MouseEvent) => {
    e.stopPropagation()
    setConfirmDelete(false)
  }

  const cardBg = isFailed
    ? 'border-red-200 bg-red-50'
    : confirmDelete
      ? 'border-red-300 bg-red-50'
      : 'border-gray-200 bg-white hover:border-gray-300 hover:bg-gray-50'

  return (
    <div
      className="relative"
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => { setHovered(false); setConfirmDelete(false) }}
    >
      {/* card */}
      <div
        className={[
          'relative flex w-36 flex-col rounded-lg border p-2.5 transition-colors',
          cardBg,
          isReady && !confirmDelete ? 'cursor-pointer' : 'cursor-default',
          downloadMutation.isPending ? 'opacity-60' : '',
        ].join(' ')}
        onClick={handleBodyClick}
      >
        {/* × delete — absolute top-right, shown on hover or always when Failed */}
        {canDelete && (hovered || isFailed) && !confirmDelete && (
          <button
            type="button"
            onClick={handleDeleteClick}
            className="absolute right-1 top-1 flex h-4 w-4 items-center justify-center rounded-full bg-gray-200 text-gray-500 hover:bg-red-100 hover:text-red-600"
            title="Remove"
          >
            <svg viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="h-2.5 w-2.5">
              <line x1="2" y1="2" x2="10" y2="10" />
              <line x1="10" y1="2" x2="2" y2="10" />
            </svg>
          </button>
        )}

        {/* confirm state */}
        {confirmDelete ? (
          <div className="flex flex-col items-center gap-2 py-1 text-center">
            <p className="text-xs font-medium text-red-700">Remove file?</p>
            <div className="flex gap-1.5">
              <button
                type="button"
                onClick={handleConfirmYes}
                className="rounded bg-red-600 px-2.5 py-1 text-xs font-medium text-white hover:bg-red-700"
              >
                Remove
              </button>
              <button
                type="button"
                onClick={handleConfirmNo}
                className="rounded border border-gray-200 px-2.5 py-1 text-xs text-gray-600 hover:bg-gray-100"
              >
                Cancel
              </button>
            </div>
          </div>
        ) : (
          <>
            {/* file icon — alone in the top section, no status here */}
            <div className="mb-1.5">
              <span className={isFailed ? 'text-red-400' : isPending ? 'text-gray-300' : 'text-brand-500'}>
                <FileTypeIcon contentType={attachment.contentType} />
              </span>
            </div>

            {/* filename */}
            <p className={[
              'line-clamp-2 break-all text-[11px] font-medium leading-tight',
              isFailed ? 'text-red-600' : isPending ? 'text-gray-400' : 'text-gray-800',
            ].join(' ')}>
              {attachment.originalFileName}
            </p>

            {/* size + status badge — bottom row, no overlap with × */}
            <div className="mt-1 flex items-center gap-1">
              <p className="text-[10px] text-gray-400">{formatBytes(attachment.fileSizeBytes)}</p>
              {isPending && (
                <span className="h-2.5 w-2.5 animate-spin rounded-full border border-gray-200 border-t-brand-400 shrink-0" title={attachment.status} />
              )}
              {isFailed && (
                <span className="flex h-3.5 w-3.5 shrink-0 items-center justify-center rounded-full bg-red-100" title="Processing failed">
                  <svg viewBox="0 0 12 12" fill="currentColor" className="h-2.5 w-2.5 text-red-500">
                    <path d="M6 1a5 5 0 100 10A5 5 0 006 1zm-.5 2.5h1v3h-1v-3zm0 4h1v1h-1v-1z" />
                  </svg>
                </span>
              )}
            </div>
          </>
        )}
      </div>

      {/* metadata tooltip */}
      {hovered && !confirmDelete && (
        <div className="pointer-events-none absolute bottom-full left-0 z-50 mb-1.5 w-52 rounded-lg border border-gray-200 bg-white p-2.5 shadow-lg">
          <p className="mb-1 break-all text-[11px] font-medium text-gray-900">{attachment.originalFileName}</p>
          <div className="space-y-0.5 text-[10px] text-gray-500">
            <p>{formatBytes(attachment.fileSizeBytes)}</p>
            <p>Added by <span className="font-medium text-gray-700">{attachment.uploadedByName}</span></p>
            <p>{formatDate(attachment.uploadedAt)}</p>
            {isFailed && (
              <p className="font-medium text-red-500">{attachment.processingError ?? 'Processing failed'}</p>
            )}
            {isPending && <p className="text-amber-500">{attachment.status}…</p>}
          </div>
        </div>
      )}
    </div>
  )
}
