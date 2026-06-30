import { type DragEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { getErrorMessage } from '../api/errors'
import { AddColumnModal } from '../components/workspaces/AddColumnModal'
import { DeleteColumnDialog } from '../components/workspaces/DeleteColumnDialog'
import { RenameColumnModal } from '../components/workspaces/RenameColumnModal'
import { CreateTaskModal } from '../components/tasks/CreateTaskModal'
import { ColumnTaskList } from '../components/tasks/ColumnTaskList'
import { TaskDetailPanel } from '../components/tasks/TaskDetailPanel'
import { TaskFilterBar } from '../components/tasks/TaskFilterBar'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { CheckIcon, PencilIcon, PlusIcon, TrashIcon } from '../components/ui/Icons'
import { Sidebar } from '../components/layout/Sidebar'
import { Spinner } from '../components/ui/Spinner'
import { useColumns, useReorderColumns } from '../hooks/useColumns'
import { useLabels } from '../hooks/useLabels'
import { useMembers } from '../hooks/useMembers'
import { usePriorityConfig } from '../hooks/usePriorityConfig'
import { useTaskFilter } from '../hooks/useTaskFilter'
import { useMoveTask, useTask } from '../hooks/useTasks'
import { useWorkspaces, useUpdateWorkspace } from '../hooks/useWorkspaces'
import { setLastWorkspaceId } from '../lib/lastWorkspace'
import type { WorkspaceColumnDto, WorkspaceTaskDto } from '../types/api.types'

const MAX_COLUMNS = 7

export function DashboardPage() {
  const { workspaceId } = useParams<{ workspaceId: string }>()
  const wsId = workspaceId ?? ''
  const [searchParams, setSearchParams] = useSearchParams()
  const linkedTaskId  = searchParams.get('taskId')
  const linkedCommentId = searchParams.get('commentId')

  useEffect(() => {
    if (workspaceId) setLastWorkspaceId(workspaceId)
  }, [workspaceId])

  const { data: workspaces, isLoading: isLoadingWorkspace } = useWorkspaces()
  const workspace = workspaces?.find((w) => w.id === wsId) ?? null
  const updateWorkspaceMutation = useUpdateWorkspace()

  const [editingWsName, setEditingWsName] = useState(false)
  const [wsNameDraft, setWsNameDraft] = useState('')

  const startEditWsName = () => {
    if (!workspace) return
    setWsNameDraft(workspace.name)
    setEditingWsName(true)
  }

  const saveWsName = () => {
    const trimmed = wsNameDraft.trim()
    if (!trimmed || trimmed === workspace?.name) { setEditingWsName(false); return }
    updateWorkspaceMutation.mutate(
      { id: wsId, data: { name: trimmed } },
      { onSuccess: () => setEditingWsName(false) },
    )
  }

  const { data: columns, isLoading: isLoadingColumns } = useColumns(wsId)
  const { data: members } = useMembers(wsId)
  const { data: labels = [] } = useLabels(wsId)
  const { data: priorityConfigs = [] } = usePriorityConfig(wsId)
  const {
    searchInput, setSearchInput,
    assigneeIds, toggleAssigneeId,
    priorities, setPriorities,
    labelIds, toggleLabelId,
    filter, hasActiveFilters, clearAll,
  } = useTaskFilter()
  const reorderMutation = useReorderColumns(wsId)
  const moveTaskMutation = useMoveTask(wsId)

  // board horizontal scroll ref (wheel → horizontal, middle mouse drag)
  const boardRef = useRef<HTMLDivElement>(null)
  useEffect(() => {
    const el = boardRef.current
    if (!el) return

    const onWheel = (e: WheelEvent) => {
      // let column task lists handle vertical scroll natively
      if ((e.target as HTMLElement).closest('[data-col-scroll]')) return
      if (Math.abs(e.deltaY) > Math.abs(e.deltaX)) {
        e.preventDefault()
        el.scrollLeft += e.deltaY
      }
    }

    const onMouseDown = (e: MouseEvent) => {
      if (e.button !== 1) return
      e.preventDefault()
      const startX = e.clientX
      const startLeft = el.scrollLeft
      el.style.cursor = 'grabbing'
      const onMouseMove = (ev: MouseEvent) => {
        el.scrollLeft = startLeft - (ev.clientX - startX)
      }
      const onMouseUp = () => {
        el.style.cursor = ''
        document.removeEventListener('mousemove', onMouseMove)
        document.removeEventListener('mouseup', onMouseUp)
      }
      document.addEventListener('mousemove', onMouseMove)
      document.addEventListener('mouseup', onMouseUp)
    }

    el.addEventListener('wheel', onWheel, { passive: false })
    el.addEventListener('mousedown', onMouseDown)
    return () => {
      el.removeEventListener('wheel', onWheel)
      el.removeEventListener('mousedown', onMouseDown)
    }
  }, [])

  // column drag state
  const colDraggedId = useRef<string | null>(null)
  const [colDragOverId, setColDragOverId] = useState<string | null>(null)
  const [colDraggingId, setColDraggingId] = useState<string | null>(null)

  // task drag state
  const taskDraggedId = useRef<string | null>(null)
  const [taskDragOverColId, setTaskDragOverColId] = useState<string | null>(null)

  // column management modals
  const [addColOpen, setAddColOpen] = useState(false)
  const [renameTarget, setRenameTarget] = useState<WorkspaceColumnDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<WorkspaceColumnDto | null>(null)
  const [colReorderError, setColReorderError] = useState<string | null>(null)

  // detail panel resize
  const [panelWidth, setPanelWidth] = useState(480)
  const startPanelResize = (e: React.MouseEvent) => {
    e.preventDefault()
    const startX = e.clientX
    const startWidth = panelWidth
    document.body.style.cursor = 'col-resize'
    document.body.style.userSelect = 'none'
    const onMouseMove = (ev: MouseEvent) => {
      setPanelWidth(Math.min(800, Math.max(320, startWidth + (startX - ev.clientX))))
    }
    const onMouseUp = () => {
      document.body.style.cursor = ''
      document.body.style.userSelect = ''
      window.removeEventListener('mousemove', onMouseMove)
      window.removeEventListener('mouseup', onMouseUp)
    }
    window.addEventListener('mousemove', onMouseMove)
    window.addEventListener('mouseup', onMouseUp)
  }

  // open task + optional comment from notification deep-link
  const { data: linkedTask } = useTask(wsId, linkedTaskId)
  useEffect(() => {
    if (!linkedTask) return
    setSelectedTask(linkedTask)
    setInitialCommentId(linkedCommentId)
    setSearchParams({}, { replace: true })
  }, [linkedTask, linkedCommentId, setSearchParams])

  // task state
  const [selectedTask, setSelectedTask] = useState<WorkspaceTaskDto | null>(null)
  const [initialCommentId, setInitialCommentId] = useState<string | null>(null)
  const [colCounts, setColCounts] = useState<Record<string, number>>({})
  const [createForColumnId, setCreateForColumnId] = useState<string | null>(null)
  const [createTaskOpen, setCreateTaskOpen] = useState(false)

  const sorted = useMemo(
    () => (columns ? [...columns].sort((a, b) => a.order - b.order) : []),
    [columns],
  )
  const atLimit = sorted.length >= MAX_COLUMNS

  const handleTaskClick = useCallback((task: WorkspaceTaskDto) => {
    setSelectedTask((prev) => (prev?.id === task.id ? null : task))
    setInitialCommentId(null)
  }, [])

  const handleColCount = useCallback((columnId: string, count: number) => {
    setColCounts((prev) => ({ ...prev, [columnId]: count }))
  }, [])

  // ── column DnD ────────────────────────────────────────────────────────────
  const handleColDragStart = (e: DragEvent<HTMLDivElement>, id: string) => {
    colDraggedId.current = id
    setColDraggingId(id)
    e.dataTransfer.effectAllowed = 'move'
  }
  const handleColDragOver = (e: DragEvent<HTMLDivElement>, id: string) => {
    e.preventDefault()
    e.dataTransfer.dropEffect = 'move'
    if (id !== colDraggedId.current) setColDragOverId(id)
  }
  const handleColDrop = (e: DragEvent<HTMLDivElement>, targetId: string) => {
    e.preventDefault()
    const sourceId = colDraggedId.current
    if (!sourceId || sourceId === targetId) return
    const newOrder = [...sorted]
    const from = newOrder.findIndex((c) => c.id === sourceId)
    const to = newOrder.findIndex((c) => c.id === targetId)
    const [item] = newOrder.splice(from, 1)
    newOrder.splice(to, 0, item!)
    setColReorderError(null)
    reorderMutation.mutate(
      { columnIds: newOrder.map((c) => c.id) },
      { onError: (err) => setColReorderError(getErrorMessage(err)) },
    )
    colDraggedId.current = null
    setColDraggingId(null)
    setColDragOverId(null)
  }
  const handleColDragEnd = () => {
    colDraggedId.current = null
    setColDraggingId(null)
    setColDragOverId(null)
  }

  // ── task DnD ──────────────────────────────────────────────────────────────
  const handleTaskDragStart = (e: DragEvent<HTMLDivElement>, taskId: string) => {
    e.stopPropagation() // prevent column drag from firing
    taskDraggedId.current = taskId
    e.dataTransfer.effectAllowed = 'move'
  }
  const handleTaskDragEnd = () => {
    taskDraggedId.current = null
    setTaskDragOverColId(null)
  }
  const handleColBodyDragOver = (e: DragEvent<HTMLDivElement>, columnId: string) => {
    if (!taskDraggedId.current) return
    e.preventDefault()
    e.stopPropagation()
    setTaskDragOverColId(columnId)
  }
  const handleColBodyDrop = (e: DragEvent<HTMLDivElement>, columnId: string) => {
    const tId = taskDraggedId.current
    if (!tId) return
    e.preventDefault()
    e.stopPropagation()
    moveTaskMutation.mutate({ taskId: tId, data: { columnId } })
    taskDraggedId.current = null
    setTaskDragOverColId(null)
  }

  const canManage = workspace?.myRole === 'Owner' || workspace?.myRole === 'Admin'
  const isLoading = isLoadingWorkspace || isLoadingColumns

  return (
    <div className="flex h-screen overflow-hidden bg-gray-50">
      <Sidebar />

      <main className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <header className="flex shrink-0 items-center justify-between border-b border-gray-200 bg-white px-6 py-4">
          <div className="flex min-w-0 items-center gap-2">
            {isLoadingWorkspace ? (
              <span className="text-xl font-semibold text-gray-400">Loading…</span>
            ) : workspace ? (
              editingWsName ? (
                <div className="flex items-center gap-2">
                  <input
                    autoFocus
                    value={wsNameDraft}
                    onChange={(e) => setWsNameDraft(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') saveWsName()
                      if (e.key === 'Escape') setEditingWsName(false)
                    }}
                    className="rounded-md border border-brand-400 px-2.5 py-1 text-xl font-semibold text-gray-900 focus:outline-none focus:ring-2 focus:ring-brand-500/30"
                    style={{ width: `${Math.max(wsNameDraft.length, 8)}ch` }}
                  />
                  <button
                    type="button"
                    onClick={saveWsName}
                    disabled={updateWorkspaceMutation.isPending}
                    className="rounded bg-brand-500 px-3 py-1.5 text-sm font-medium text-white hover:bg-brand-600 disabled:opacity-60"
                  >
                    Save
                  </button>
                  <button
                    type="button"
                    onClick={() => setEditingWsName(false)}
                    className="rounded px-2.5 py-1.5 text-sm text-gray-500 hover:bg-gray-100"
                  >
                    Cancel
                  </button>
                </div>
              ) : (
                <div className="group flex min-w-0 items-center gap-2">
                  <h1 className="truncate text-xl font-semibold text-gray-900">{workspace.name}</h1>
                  {canManage && (
                    <button
                      type="button"
                      onClick={startEditWsName}
                      className="shrink-0 rounded p-1 text-gray-300 opacity-0 transition-opacity group-hover:opacity-100 hover:bg-gray-100 hover:text-gray-600"
                      aria-label="Rename workspace"
                    >
                      <PencilIcon className="h-4 w-4" />
                    </button>
                  )}
                </div>
              )
            ) : (
              <span className="text-xl font-semibold text-gray-400">Workspace not found</span>
            )}
          </div>

          <div className="flex items-center gap-2">
            <Button
              type="button"
              onClick={() => {
                setCreateForColumnId(null)
                setCreateTaskOpen(true)
              }}
              disabled={sorted.length === 0}
              title={sorted.length === 0 ? 'Create a column first' : undefined}
            >
              <PlusIcon className="h-4 w-4" />
              New task
            </Button>

            {canManage && (
              <Button
                type="button"
                variant="secondary"
                onClick={() => setAddColOpen(true)}
                disabled={atLimit}
                title={atLimit ? 'Maximum 7 columns reached' : undefined}
              >
                Add column
              </Button>
            )}
          </div>
        </header>

        <div className="flex flex-1 overflow-hidden">
          {/* board track */}
          <div className="flex-1 overflow-hidden">
            {!isLoadingWorkspace && !workspace ? (
              <div className="flex h-full items-center justify-center">
                <div className="text-center">
                  <h1 className="text-lg font-semibold text-gray-900">Workspace not found</h1>
                  <p className="mt-1 text-sm text-gray-500">
                    <Link to="/" className="font-medium text-brand-700 hover:underline">
                      Go to your workspaces
                    </Link>
                  </p>
                </div>
              </div>
            ) : isLoading ? (
              <div className="flex h-full items-center justify-center">
                <Spinner className="h-5 w-5 border-gray-300 border-t-brand-500" />
              </div>
            ) : (
              <div className="flex h-full flex-col">
                <TaskFilterBar
                  searchInput={searchInput}
                  onSearchChange={setSearchInput}
                  assigneeIds={assigneeIds}
                  onToggleAssignee={toggleAssigneeId}
                  priorities={priorities}
                  onPriorityChange={setPriorities}
                  priorityConfigs={priorityConfigs}
                  labelIds={labelIds}
                  onToggleLabel={toggleLabelId}
                  labels={labels}
                  members={members ?? []}
                  hasActiveFilters={hasActiveFilters}
                  onClear={clearAll}
                />

                {colReorderError && (
                  <div className="shrink-0 px-4 pt-3">
                    <Alert variant="error">{colReorderError}</Alert>
                  </div>
                )}

                <div ref={boardRef} className="flex-1 overflow-x-auto overflow-y-hidden scrollbar-hide">
                  <div className="flex h-full min-w-max gap-3 p-4">
                    {sorted.map((column) => {
                      const isColDragging = colDraggingId === column.id
                      const isColDragOver = colDragOverId === column.id
                      const isTaskDragOver = taskDragOverColId === column.id

                      return (
                        <div
                          key={column.id}
                          draggable={canManage}
                          onDragStart={(e) => handleColDragStart(e, column.id)}
                          onDragOver={(e) => {
                            if (!taskDraggedId.current) handleColDragOver(e, column.id)
                          }}
                          onDrop={(e) => {
                            if (!taskDraggedId.current) handleColDrop(e, column.id)
                          }}
                          onDragEnd={handleColDragEnd}
                          className={[
                            'flex w-72 shrink-0 flex-col transition-opacity',
                            canManage ? 'cursor-grab active:cursor-grabbing' : '',
                            isColDragging ? 'opacity-40' : 'opacity-100',
                            isColDragOver ? 'rounded-xl ring-2 ring-brand-400 ring-offset-2' : '',
                          ].join(' ')}
                        >
                          {/* bordered card — header + body only */}
                          <div
                            className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-t-xl border border-b-0 border-gray-200 bg-gray-100"
                          >
                            {/* column header */}
                            <div className="flex items-center gap-2 px-3 py-2.5">
                              <div
                                className="h-2 w-2 shrink-0 rounded-full"
                                style={{ backgroundColor: column.color }}
                              />
                              <span
                                className="min-w-0 flex-1 truncate text-xs font-medium uppercase tracking-wide text-gray-600"
                                title={column.name}
                              >
                                {column.name}
                              </span>
                              <span className="shrink-0 rounded-full bg-gray-200 px-1.5 py-0.5 text-[10px] font-medium text-gray-600">
                                {colCounts[column.id] ?? 0}
                              </span>

                              {canManage && (
                                <div className="flex shrink-0 items-center gap-0.5">
                                  <button
                                    type="button"
                                    aria-label={`Edit "${column.name}"`}
                                    onMouseDown={(e) => e.stopPropagation()}
                                    onClick={(e) => { e.stopPropagation(); setRenameTarget(column) }}
                                    className="rounded p-1 text-gray-400 hover:bg-gray-200 hover:text-gray-700"
                                  >
                                    <PencilIcon className="h-3.5 w-3.5" />
                                  </button>
                                  <button
                                    type="button"
                                    aria-label={`Delete "${column.name}"`}
                                    onMouseDown={(e) => e.stopPropagation()}
                                    onClick={(e) => { e.stopPropagation(); setDeleteTarget(column) }}
                                    className="rounded p-1 text-gray-400 hover:bg-red-100 hover:text-red-600"
                                  >
                                    <TrashIcon className="h-3.5 w-3.5" />
                                  </button>
                                </div>
                              )}
                            </div>

                            {/* Done column indicator strip */}
                            {column.isDoneColumn && (
                              <div className="flex items-center gap-1 border-b border-green-100 bg-green-50 px-3 py-1">
                                <CheckIcon className="h-2.5 w-2.5 shrink-0 text-green-500" />
                                <span className="text-[10px] font-medium uppercase tracking-wide text-green-600">
                                  Done column
                                </span>
                              </div>
                            )}

                            {/* column body — task drop zone */}
                            <div
                              data-col-scroll
                              className={[
                                'flex flex-1 flex-col gap-2 overflow-y-auto scrollbar-hide px-2 pt-2 pb-2 transition-colors',
                                isTaskDragOver ? 'bg-brand-50/60' : '',
                              ].join(' ')}
                              onDragOver={(e) => handleColBodyDragOver(e, column.id)}
                              onDrop={(e) => handleColBodyDrop(e, column.id)}
                              onDragLeave={() => {
                                if (taskDragOverColId === column.id) setTaskDragOverColId(null)
                              }}
                            >
                              <ColumnTaskList
                                workspaceId={wsId}
                                columnId={column.id}
                                filter={filter}
                                selectedTaskId={selectedTask?.id ?? null}
                                onTaskClick={handleTaskClick}
                                onDragStart={handleTaskDragStart}
                                onDragEnd={handleTaskDragEnd}
                                onCountChange={(count) => handleColCount(column.id, count)}
                              />
                            </div>
                          </div>

                          {/* footer button — sits below the border, outside it */}
                          <button
                            type="button"
                            onClick={() => {
                              setCreateForColumnId(column.id)
                              setCreateTaskOpen(true)
                            }}
                            className="flex w-full items-center justify-center gap-1.5 rounded-b-xl bg-brand-500 py-2.5 text-sm font-medium text-white transition-colors hover:bg-brand-600 active:bg-brand-700"
                          >
                            <PlusIcon className="h-4 w-4" />
                            Add task
                          </button>
                        </div>
                      )
                    })}

                    {/* ghost card — add column */}
                    {canManage && !atLimit && (
                      <button
                        type="button"
                        onClick={() => setAddColOpen(true)}
                        className="flex h-20 w-72 shrink-0 items-center justify-center gap-2 self-start rounded-xl border-2 border-dashed border-gray-300 text-sm text-gray-400 transition-colors hover:border-brand-400 hover:bg-brand-50/50 hover:text-brand-600"
                      >
                        <PlusIcon className="h-4 w-4" />
                        Add column
                      </button>
                    )}

                    {sorted.length === 0 && (
                      <p className="py-10 text-sm text-gray-400">
                        {canManage ? 'Create your first column to get started.' : 'No columns yet.'}
                      </p>
                    )}
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* detail panel */}
          {selectedTask && (
            <TaskDetailPanel
              key={`${selectedTask.id}-${initialCommentId ?? ''}`}
              task={selectedTask}
              columns={sorted}
              workspaceId={wsId}
              width={panelWidth}
              initialCommentId={initialCommentId}
              onResizeStart={startPanelResize}
              onClose={() => { setSelectedTask(null); setInitialCommentId(null) }}
              onTaskUpdated={setSelectedTask}
            />
          )}
        </div>
      </main>

      {addColOpen && <AddColumnModal workspaceId={wsId} onClose={() => setAddColOpen(false)} />}

      {renameTarget && (
        <RenameColumnModal
          workspaceId={wsId}
          column={renameTarget}
          onClose={() => setRenameTarget(null)}
        />
      )}

      {deleteTarget && (
        <DeleteColumnDialog
          workspaceId={wsId}
          column={deleteTarget}
          onClose={() => setDeleteTarget(null)}
        />
      )}

      {createTaskOpen && sorted.length > 0 && (
        <CreateTaskModal
          workspaceId={wsId}
          columns={sorted}
          defaultColumnId={createForColumnId ?? undefined}
          onClose={() => {
            setCreateTaskOpen(false)
            setCreateForColumnId(null)
          }}
        />
      )}
    </div>
  )
}
