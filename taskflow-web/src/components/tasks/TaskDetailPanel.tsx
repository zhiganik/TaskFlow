import { useEffect, useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { useSetTaskLabels } from '../../hooks/useLabels'
import { useCloseTask, useDeleteTask, useMoveTask, useReopenTask, useUpdateTask } from '../../hooks/useTasks'
import { useMembers } from '../../hooks/useMembers'
import { usePriorityConfig } from '../../hooks/usePriorityConfig'
import type { TaskPriority, WorkspaceColumnDto, WorkspaceTaskDto } from '../../types/api.types'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { ArchiveIcon, CalendarIcon, CheckIcon, PencilIcon, XIcon } from '../ui/Icons'
import { Spinner } from '../ui/Spinner'
import { UserAvatar } from '../ui/UserAvatar'
import { AttachmentSection } from './attachments/AttachmentSection'
import { CommentsList } from './comments/CommentsList'
import { LabelPicker } from './LabelPicker'

interface TaskDetailPanelProps {
  task: WorkspaceTaskDto
  columns: WorkspaceColumnDto[]
  workspaceId: string
  width: number
  onResizeStart: (e: React.MouseEvent<HTMLDivElement>) => void
  onClose: () => void
  onTaskUpdated: (task: WorkspaceTaskDto) => void
}

function initials(name: string) {
  return name
    .split(' ')
    .map((p) => p[0])
    .filter(Boolean)
    .slice(0, 2)
    .join('')
    .toUpperCase()
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

export function TaskDetailPanel({ task, columns, workspaceId, width, onResizeStart, onClose, onTaskUpdated }: TaskDetailPanelProps) {
  const updateMutation = useUpdateTask(workspaceId)
  const moveMutation = useMoveTask(workspaceId)
  const deleteMutation = useDeleteTask(workspaceId)
  const closeMutation = useCloseTask(workspaceId)
  const reopenMutation = useReopenTask(workspaceId)
  const setLabelsMutation = useSetTaskLabels(workspaceId)

  const isArchived = task.status === 'Closed' || task.status === 'Deleted'
  const { data: members } = useMembers(workspaceId)
  const { data: priorityConfigs = [] } = usePriorityConfig(workspaceId)

  const getPriorityColor = (p: TaskPriority) =>
    priorityConfigs.find((c) => c.priority === p)?.color ?? { Low: '#22c55e', Medium: '#f59e0b', High: '#ef4444' }[p]

  const getPriorityLabel = (p: TaskPriority) =>
    priorityConfigs.find((c) => c.priority === p)?.displayName ?? p

  const [editingTitle, setEditingTitle] = useState(false)
  const [titleDraft, setTitleDraft] = useState(task.title)
  const [editingDesc, setEditingDesc] = useState(false)
  const [descDraft, setDescDraft] = useState(task.description ?? '')
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [panelError, setPanelError] = useState<string | null>(null)

  // Reset editing state when the selected task changes
  useEffect(() => {
    setEditingTitle(false)
    setTitleDraft(task.title)
    setEditingDesc(false)
    setDescDraft(task.description ?? '')
    setConfirmDelete(false)
    setPanelError(null)
  }, [task.id])

  // local optimistic state for assignee — prevents the select snapping back to the old value
  // while the mutation is in-flight (controlled <select> re-renders with task.assigneeId otherwise)
  const [localAssigneeId, setLocalAssigneeId] = useState(task.assigneeId ?? '')
  useEffect(() => { setLocalAssigneeId(task.assigneeId ?? '') }, [task.assigneeId])

  const saveField = (patch: Partial<{
    title: string
    description: string | null
    priority: TaskPriority
    assigneeId: string | null
    dueDate: string | null
  }>) => {
    setPanelError(null)
    updateMutation.mutate(
      {
        taskId: task.id,
        data: {
          title: task.title,
          description: task.description,
          priority: task.priority,
          assigneeId: task.assigneeId,
          dueDate: task.dueDate,
          ...patch,
        },
      },
      {
        onSuccess: (updated) => onTaskUpdated(updated),
        onError: (err) => setPanelError(getErrorMessage(err)),
      },
    )
  }

  const saveTitle = () => {
    const trimmed = titleDraft.trim()
    if (trimmed && trimmed !== task.title) saveField({ title: trimmed })
    setEditingTitle(false)
  }

  const saveDesc = () => {
    const val = descDraft.trim() || null
    if (val !== task.description) saveField({ description: val })
    setEditingDesc(false)
  }

  const moveToColumn = (columnId: string) => {
    if (columnId === task.columnId) return
    setPanelError(null)
    moveMutation.mutate(
      { taskId: task.id, data: { columnId } },
      {
        onSuccess: (updated) => onTaskUpdated(updated),
        onError: (err) => setPanelError(getErrorMessage(err)),
      },
    )
  }

  const handleDelete = () => {
    setPanelError(null)
    deleteMutation.mutate(task.id, {
      onSuccess: onClose,
      onError: (err) => setPanelError(getErrorMessage(err)),
    })
  }

  const dueDateValue = task.dueDate ? task.dueDate.slice(0, 10) : ''

  const isSaving =
    updateMutation.isPending ||
    moveMutation.isPending ||
    closeMutation.isPending ||
    reopenMutation.isPending

  return (
    <div
      className="relative flex shrink-0 flex-col overflow-hidden border-l-4 bg-white"
      style={{ width, borderLeftColor: getPriorityColor(task.priority) }}
    >
      {/* resize handle */}
      <div
        onMouseDown={onResizeStart}
        className="absolute left-0 top-0 h-full w-1.5 cursor-col-resize hover:bg-brand-400/30 transition-colors z-10"
        title="Drag to resize"
      />
      {/* header */}
      <div className="shrink-0 border-b border-gray-100 px-4 py-3">
        <div className="mb-2 flex items-start gap-2">
          <span className="mt-0.5 shrink-0 text-xs font-medium text-gray-400">#{task.number}</span>

          {editingTitle ? (
            <div className="flex flex-1 flex-col gap-1.5">
              <input
                autoFocus
                value={titleDraft}
                onChange={(e) => setTitleDraft(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') saveTitle()
                  if (e.key === 'Escape') { setTitleDraft(task.title); setEditingTitle(false) }
                }}
                className="w-full rounded border border-brand-400 px-2 py-1 text-sm font-medium text-gray-900 focus:outline-none focus:ring-2 focus:ring-brand-500/30"
              />
              <div className="flex gap-1.5">
                <button
                  type="button"
                  onClick={saveTitle}
                  className="rounded bg-brand-500 px-2.5 py-1 text-xs font-medium text-white hover:bg-brand-600"
                >
                  Save
                </button>
                <button
                  type="button"
                  onClick={() => { setTitleDraft(task.title); setEditingTitle(false) }}
                  className="rounded px-2.5 py-1 text-xs text-gray-500 hover:bg-gray-100"
                >
                  Cancel
                </button>
              </div>
            </div>
          ) : (
            <p
              role="button"
              tabIndex={0}
              onClick={() => { setTitleDraft(task.title); setEditingTitle(true) }}
              onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { setTitleDraft(task.title); setEditingTitle(true) } }}
              className="min-w-0 flex-1 cursor-text rounded px-0.5 text-sm font-semibold leading-snug text-gray-900 hover:bg-gray-50"
              title="Click to edit title"
            >
              {task.title}
            </p>
          )}

          <button
            type="button"
            onClick={onClose}
            aria-label="Close panel"
            className="shrink-0 rounded p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-700"
          >
            <XIcon className="h-4 w-4" />
          </button>
        </div>

        <div className="flex flex-wrap items-center gap-1.5">
          {task.status === 'Done' && (
            <span className="inline-flex items-center gap-0.5 rounded-full bg-green-100 px-2 py-0.5 text-[11px] font-medium text-green-700">
              <CheckIcon className="h-2.5 w-2.5" />
              Done
            </span>
          )}
          {task.labels.map((l) => (
            <span
              key={l.id}
              className="inline-flex rounded-full px-2 py-0.5 text-[11px] font-medium text-white"
              style={{ backgroundColor: l.color }}
            >
              {l.name}
            </span>
          ))}
          {task.dueDate && (
            <span className={`inline-flex items-center gap-0.5 text-[11px] ${task.isOverdue ? 'font-medium text-red-600' : 'text-gray-400'}`}>
              <CalendarIcon className="h-3 w-3" />
              {new Date(task.dueDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
            </span>
          )}
        </div>
      </div>

      {/* scrollable body */}
      <div className="flex-1 overflow-x-hidden overflow-y-auto">
        {panelError && (
          <div className="px-4 pt-3">
            <Alert variant="error">{panelError}</Alert>
          </div>
        )}

        {/* archive banner */}
        {isArchived && (
          <div className="mx-4 mt-3 flex items-center gap-2 rounded-md bg-gray-100 px-3 py-2 text-xs text-gray-600">
            <ArchiveIcon className="h-3.5 w-3.5 shrink-0 text-gray-400" />
            <span>
              This task is archived
              {task.status === 'Deleted' ? ' (deleted from board)' : ''}.
            </span>
          </div>
        )}

        {/* ── Properties ─────────────────────────────────────── */}
        <div className="px-4 pt-4 pb-1">
          {!isArchived && (
            <PropRow label="Status">
              <div className="flex flex-wrap gap-1.5">
                {columns.map((col) => {
                  const isActive = col.id === task.columnId
                  return (
                    <button
                      key={col.id}
                      type="button"
                      disabled={isActive || moveMutation.isPending}
                      onClick={() => moveToColumn(col.id)}
                      className={[
                        'rounded-full border px-2.5 py-0.5 text-xs transition-colors',
                        isActive
                          ? 'border-brand-300 bg-brand-50 font-medium text-brand-700'
                          : 'border-gray-200 text-gray-600 hover:border-gray-300 hover:bg-gray-50',
                        moveMutation.isPending ? 'opacity-60' : '',
                      ].join(' ')}
                    >
                      {col.name}
                    </button>
                  )
                })}
              </div>
            </PropRow>
          )}

          <PropRow label="Priority">
            <select
              value={task.priority}
              disabled={isSaving}
              onChange={(e) => saveField({ priority: e.target.value as TaskPriority })}
              className="rounded-md border border-gray-200 bg-white px-2.5 py-1 text-sm text-gray-900 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30 disabled:opacity-60"
            >
              {(['Low', 'Medium', 'High'] as TaskPriority[]).map((p) => (
                <option key={p} value={p}>{getPriorityLabel(p)}</option>
              ))}
            </select>
          </PropRow>

          <PropRow label="Assignee">
            <div className="flex flex-col gap-1.5">
              {localAssigneeId && (() => {
                const member = members?.find((m) => m.userId === localAssigneeId)
                return (
                  <div className="flex items-center gap-2">
                    {member ? (
                      <UserAvatar
                        displayName={member.displayName}
                        avatarColor={member.avatarColor}
                        avatarPath={member.avatarPath}
                        avatarStatus={member.avatarStatus}
                        size="sm"
                      />
                    ) : (
                      <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-brand-50 text-[10px] font-semibold text-brand-700">
                        {initials(localAssigneeId)}
                      </span>
                    )}
                    <span className="min-w-0 flex-1 truncate text-sm text-gray-700">
                      {member?.displayName ?? '…'}
                    </span>
                    <button
                      type="button"
                      disabled={isSaving}
                      onClick={() => {
                        setLocalAssigneeId('')
                        updateMutation.mutate(
                          { taskId: task.id, data: { title: task.title, description: task.description, priority: task.priority, assigneeId: null, dueDate: task.dueDate } },
                          { onError: (err) => { setLocalAssigneeId(task.assigneeId ?? ''); setPanelError(getErrorMessage(err)) } },
                        )
                      }}
                      className="shrink-0 rounded p-0.5 text-gray-400 hover:bg-red-50 hover:text-red-500 disabled:opacity-40"
                      title="Unassign"
                      aria-label="Unassign"
                    >
                      <XIcon className="h-3.5 w-3.5" />
                    </button>
                  </div>
                )
              })()}
              <select
                value={localAssigneeId}
                disabled={isSaving}
                onChange={(e) => {
                  const val = e.target.value
                  setLocalAssigneeId(val)
                  saveField({ assigneeId: val || null })
                }}
                className="rounded-md border border-gray-200 bg-white px-2.5 py-1 text-sm text-gray-900 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30 disabled:opacity-60"
              >
                <option value="">Unassigned</option>
                {members?.map((m) => (
                  <option key={m.userId} value={m.userId}>{m.displayName}</option>
                ))}
              </select>
            </div>
          </PropRow>

          <PropRow label="Labels">
            <LabelPicker
              workspaceId={workspaceId}
              selectedIds={task.labels.map((l) => l.id)}
              disabled={setLabelsMutation.isPending}
              onChange={(ids) =>
                setLabelsMutation.mutate(
                  { taskId: task.id, data: { labelIds: ids } },
                  { onSuccess: (updated) => onTaskUpdated(updated) },
                )
              }
            />
          </PropRow>

          <PropRow label={task.isOverdue ? '⚠ Due date' : 'Due date'} labelRed={task.isOverdue} last>
            <input
              type="date"
              value={dueDateValue}
              disabled={isSaving}
              onChange={(e) =>
                saveField({
                  dueDate: e.target.value ? new Date(e.target.value + 'T00:00:00Z').toISOString() : null,
                })
              }
              className={[
                'rounded-md border px-2.5 py-1 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-brand-500/30 disabled:opacity-60',
                task.isOverdue ? 'border-red-300 bg-red-50 text-red-700' : 'border-gray-200 bg-white',
              ].join(' ')}
            />
          </PropRow>
        </div>

        {/* ── Rich sections ───────────────────────────────────── */}
        <RichSection label="Description">
          {editingDesc ? (
            <div className="space-y-1.5">
              <textarea
                autoFocus
                rows={4}
                value={descDraft}
                onChange={(e) => setDescDraft(e.target.value)}
                className="block w-full resize-none rounded-md border border-brand-400 px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-brand-500/30"
              />
              <div className="flex items-center gap-1.5">
                <button
                  type="button"
                  onClick={saveDesc}
                  className="rounded bg-brand-500 px-2.5 py-1 text-xs font-medium text-white hover:bg-brand-600"
                >
                  Save
                </button>
                <button
                  type="button"
                  onClick={() => { setDescDraft(task.description ?? ''); setEditingDesc(false) }}
                  className="rounded px-2.5 py-1 text-xs text-gray-500 hover:bg-gray-100"
                >
                  Cancel
                </button>
              </div>
            </div>
          ) : (
            <div
              className="group flex cursor-pointer items-start gap-1.5 rounded-md p-1 -mx-1 hover:bg-gray-50"
              onClick={() => { setDescDraft(task.description ?? ''); setEditingDesc(true) }}
            >
              <p className={`min-w-0 max-h-40 flex-1 overflow-y-auto break-words whitespace-pre-wrap text-sm leading-relaxed ${task.description ? 'text-gray-700' : 'italic text-gray-400'}`}>
                {task.description ?? 'Add a description…'}
              </p>
              <PencilIcon className="mt-0.5 h-3.5 w-3.5 shrink-0 text-gray-300 opacity-0 group-hover:opacity-100" />
            </div>
          )}
        </RichSection>

        <RichSection label="Attachments">
          <AttachmentSection workspaceId={workspaceId} taskId={task.id} canDelete={true} />
        </RichSection>

        <RichSection label="Comments">
          <CommentsList workspaceId={workspaceId} taskId={task.id} />
        </RichSection>

        {/* meta */}
        <div className="border-t border-gray-100 px-4 py-3 text-[11px] text-gray-400">
          Created by{' '}
          <span className="font-medium text-gray-500">{task.createdByName}</span>
          {' · '}
          Updated {formatRelative(task.updatedAt)}
        </div>
      </div>

      {/* footer */}
      <div className="shrink-0 border-t border-gray-100 px-4 py-3 space-y-2">
        {isSaving && (
          <div className="flex items-center gap-1.5 text-xs text-gray-400">
            <Spinner className="h-3.5 w-3.5 border-gray-300 border-t-gray-500" />
            Saving…
          </div>
        )}

        {isArchived ? (
          <>
            <button
              type="button"
              disabled={reopenMutation.isPending}
              onClick={() =>
                reopenMutation.mutate(task.id, {
                  onSuccess: (updated) => onTaskUpdated(updated),
                  onError: (err) => setPanelError(getErrorMessage(err)),
                })
              }
              className="flex w-full items-center justify-center gap-1.5 rounded-md border border-green-200 py-1.5 text-xs font-medium text-green-700 hover:bg-green-50 hover:border-green-300 disabled:opacity-60"
            >
              <CheckIcon className="h-3.5 w-3.5" />
              Reopen task
            </button>
            {confirmDelete ? (
              <div className="space-y-2">
                <p className="text-xs text-gray-600">Permanently delete? This cannot be undone.</p>
                <div className="flex gap-2">
                  <Button
                    type="button"
                    variant="danger"
                    className="flex-1 py-1.5 text-xs"
                    loading={deleteMutation.isPending}
                    onClick={handleDelete}
                  >
                    Delete permanently
                  </Button>
                  <Button
                    type="button"
                    variant="secondary"
                    className="flex-1 py-1.5 text-xs"
                    onClick={() => setConfirmDelete(false)}
                  >
                    Cancel
                  </Button>
                </div>
              </div>
            ) : (
              <button
                type="button"
                onClick={() => setConfirmDelete(true)}
                className="w-full rounded-md border border-red-200 py-1.5 text-xs font-medium text-red-600 hover:bg-red-50 hover:border-red-300"
              >
                Delete permanently
              </button>
            )}
          </>
        ) : (
          <>
            <button
              type="button"
              disabled={closeMutation.isPending}
              onClick={() =>
                closeMutation.mutate(task.id, {
                  onSuccess: onClose,
                  onError: (err) => setPanelError(getErrorMessage(err)),
                })
              }
              className="flex w-full items-center justify-center gap-1.5 rounded-md border border-gray-200 py-1.5 text-xs font-medium text-gray-600 hover:bg-gray-50 hover:border-gray-300 disabled:opacity-60"
            >
              <ArchiveIcon className="h-3.5 w-3.5" />
              Close task
            </button>
            {confirmDelete ? (
              <div className="space-y-2">
                <p className="text-xs text-gray-600">Move to archive? You can reopen it later.</p>
                <div className="flex gap-2">
                  <Button
                    type="button"
                    variant="danger"
                    className="flex-1 py-1.5 text-xs"
                    loading={deleteMutation.isPending}
                    onClick={handleDelete}
                  >
                    Delete
                  </Button>
                  <Button
                    type="button"
                    variant="secondary"
                    className="flex-1 py-1.5 text-xs"
                    onClick={() => setConfirmDelete(false)}
                  >
                    Cancel
                  </Button>
                </div>
              </div>
            ) : (
              <button
                type="button"
                onClick={() => setConfirmDelete(true)}
                className="w-full rounded-md border border-red-200 py-1.5 text-xs font-medium text-red-600 hover:bg-red-50 hover:border-red-300"
              >
                Delete task
              </button>
            )}
          </>
        )}
      </div>
    </div>
  )
}

function PropRow({
  label,
  labelRed,
  last,
  children,
}: {
  label: string
  labelRed?: boolean
  last?: boolean
  children: React.ReactNode
}) {
  return (
    <div className={`flex items-start gap-3 py-2.5 ${last ? '' : 'border-b border-gray-100'}`}>
      <span className={`w-20 shrink-0 pt-1 text-xs font-medium ${labelRed ? 'text-red-500' : 'text-gray-400'}`}>
        {label}
      </span>
      <div className="min-w-0 flex-1">{children}</div>
    </div>
  )
}

function RichSection({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="border-t-2 border-gray-100 px-4 pt-4 pb-4">
      <p className="mb-2.5 text-xs font-semibold uppercase tracking-wide text-gray-500">{label}</p>
      {children}
    </div>
  )
}
