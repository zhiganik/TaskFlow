import { type DragEvent, useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getErrorMessage } from '../api/errors'
import { AddColumnModal } from '../components/workspaces/AddColumnModal'
import { DeleteColumnDialog } from '../components/workspaces/DeleteColumnDialog'
import { RenameColumnModal } from '../components/workspaces/RenameColumnModal'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { PencilIcon, PlusIcon, TrashIcon } from '../components/ui/Icons'
import { Sidebar } from '../components/layout/Sidebar'
import { Spinner } from '../components/ui/Spinner'
import { useColumns, useReorderColumns } from '../hooks/useColumns'
import { useWorkspaces } from '../hooks/useWorkspaces'
import { setLastWorkspaceId } from '../lib/lastWorkspace'
import type { WorkspaceColumnDto } from '../types/api.types'

const MAX_COLUMNS = 7

export function DashboardPage() {
  const { workspaceId } = useParams<{ workspaceId: string }>()
  const wsId = workspaceId ?? ''

  useEffect(() => {
    if (workspaceId) setLastWorkspaceId(workspaceId)
  }, [workspaceId])

  const { data: workspaces, isLoading: isLoadingWorkspace } = useWorkspaces()
  const workspace = workspaces?.find((w) => w.id === wsId) ?? null
  const canManage = workspace?.myRole === 'Owner' || workspace?.myRole === 'Admin'

  const { data: columns, isLoading: isLoadingColumns } = useColumns(wsId)
  const reorderMutation = useReorderColumns(wsId)

  const [addOpen, setAddOpen] = useState(false)
  const [renameTarget, setRenameTarget] = useState<WorkspaceColumnDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<WorkspaceColumnDto | null>(null)
  const [reorderError, setReorderError] = useState<string | null>(null)

  // drag state
  const draggedId = useRef<string | null>(null)
  const [dragOverId, setDragOverId] = useState<string | null>(null)
  const [draggingId, setDraggingId] = useState<string | null>(null)

  const sorted = columns ? [...columns].sort((a, b) => a.order - b.order) : []
  const atLimit = sorted.length >= MAX_COLUMNS

  const handleDragStart = (e: DragEvent<HTMLDivElement>, id: string) => {
    draggedId.current = id
    setDraggingId(id)
    e.dataTransfer.effectAllowed = 'move'
  }

  const handleDragOver = (e: DragEvent<HTMLDivElement>, id: string) => {
    e.preventDefault()
    e.dataTransfer.dropEffect = 'move'
    if (id !== draggedId.current) setDragOverId(id)
  }

  const handleDrop = (e: DragEvent<HTMLDivElement>, targetId: string) => {
    e.preventDefault()
    const sourceId = draggedId.current
    if (!sourceId || sourceId === targetId) return

    const newOrder = [...sorted]
    const fromIndex = newOrder.findIndex((c) => c.id === sourceId)
    const toIndex = newOrder.findIndex((c) => c.id === targetId)
    const [item] = newOrder.splice(fromIndex, 1)
    newOrder.splice(toIndex, 0, item!)

    setReorderError(null)
    reorderMutation.mutate(
      { columnIds: newOrder.map((c) => c.id) },
      { onError: (err) => setReorderError(getErrorMessage(err)) },
    )

    draggedId.current = null
    setDraggingId(null)
    setDragOverId(null)
  }

  const handleDragEnd = () => {
    draggedId.current = null
    setDraggingId(null)
    setDragOverId(null)
  }

  return (
    <div className="flex h-screen overflow-hidden bg-gray-50">
      <Sidebar />

      <main className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <header className="flex shrink-0 items-center justify-between border-b border-gray-200 bg-white px-6 py-3">
          <span className="text-sm font-medium text-gray-900">
            {isLoadingWorkspace
              ? 'Loading…'
              : workspace
                ? workspace.name
                : 'Workspace not found'}
          </span>

          {canManage && (
            <Button
              type="button"
              onClick={() => setAddOpen(true)}
              disabled={atLimit}
              title={atLimit ? 'Maximum 7 columns reached' : undefined}
            >
              <PlusIcon className="h-4 w-4" />
              Add column
            </Button>
          )}
        </header>

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
          ) : isLoadingColumns || isLoadingWorkspace ? (
            <div className="flex h-full items-center justify-center">
              <Spinner className="h-5 w-5 border-gray-300 border-t-brand-500" />
            </div>
          ) : (
            <div className="flex h-full flex-col">
              {reorderError && (
                <div className="shrink-0 px-4 pt-3">
                  <Alert variant="error">{reorderError}</Alert>
                </div>
              )}

              {/* board track — scrolls horizontally when columns overflow */}
              <div className="flex-1 overflow-x-auto overflow-y-hidden">
                <div className="flex h-full min-w-max gap-3 p-4">
                  {sorted.map((column) => {
                    const isDragging = draggingId === column.id
                    const isDragOver = dragOverId === column.id

                    return (
                      <div
                        key={column.id}
                        draggable={canManage}
                        onDragStart={(e) => handleDragStart(e, column.id)}
                        onDragOver={(e) => handleDragOver(e, column.id)}
                        onDrop={(e) => handleDrop(e, column.id)}
                        onDragEnd={handleDragEnd}
                        className={[
                          'flex w-72 shrink-0 flex-col rounded-xl bg-gray-100 transition-opacity',
                          canManage ? 'cursor-grab active:cursor-grabbing' : '',
                          isDragging ? 'opacity-40' : 'opacity-100',
                          isDragOver ? 'ring-2 ring-brand-400 ring-offset-2' : '',
                        ].join(' ')}
                      >
                        {/* column header */}
                        <div className="flex items-center gap-1.5 px-3 py-3">
                          <span
                            className="min-w-0 flex-1 truncate text-sm font-semibold text-gray-800"
                            title={column.name}
                          >
                            {column.name}
                          </span>

                          {canManage && (
                            <div className="flex shrink-0 items-center gap-0.5">
                              <button
                                type="button"
                                aria-label={`Rename "${column.name}"`}
                                onMouseDown={(e) => e.stopPropagation()}
                                onClick={(e) => {
                                  e.stopPropagation()
                                  setRenameTarget(column)
                                }}
                                className="rounded p-1 text-gray-400 hover:bg-gray-200 hover:text-gray-700"
                              >
                                <PencilIcon className="h-3.5 w-3.5" />
                              </button>
                              <button
                                type="button"
                                aria-label={`Delete "${column.name}"`}
                                onMouseDown={(e) => e.stopPropagation()}
                                onClick={(e) => {
                                  e.stopPropagation()
                                  setDeleteTarget(column)
                                }}
                                className="rounded p-1 text-gray-400 hover:bg-red-100 hover:text-red-600"
                              >
                                <TrashIcon className="h-3.5 w-3.5" />
                              </button>
                            </div>
                          )}
                        </div>

                        {/* column body — tasks will live here */}
                        <div className="flex-1 overflow-y-auto px-2 pb-2">
                          <div className="flex min-h-[60px] items-center justify-center rounded-lg border-2 border-dashed border-gray-200">
                            <span className="text-xs text-gray-400">No tasks yet</span>
                          </div>
                        </div>
                      </div>
                    )
                  })}

                  {/* ghost card — add a new column */}
                  {canManage && !atLimit && (
                    <button
                      type="button"
                      onClick={() => setAddOpen(true)}
                      className="flex h-20 w-72 shrink-0 items-center justify-center gap-2 self-start rounded-xl border-2 border-dashed border-gray-300 text-sm text-gray-400 transition-colors hover:border-brand-400 hover:bg-brand-50/50 hover:text-brand-600"
                    >
                      <PlusIcon className="h-4 w-4" />
                      Add column
                    </button>
                  )}

                  {sorted.length === 0 && !canManage && (
                    <p className="py-10 text-sm text-gray-400">No columns yet.</p>
                  )}
                </div>
              </div>
            </div>
          )}
        </div>
      </main>

      {addOpen && <AddColumnModal workspaceId={wsId} onClose={() => setAddOpen(false)} />}

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
    </div>
  )
}
