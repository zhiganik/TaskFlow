import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getErrorMessage } from '../api/errors'
import { Sidebar } from '../components/layout/Sidebar'
import { AddColumnModal } from '../components/workspaces/AddColumnModal'
import { DeleteColumnDialog } from '../components/workspaces/DeleteColumnDialog'
import { RenameColumnModal } from '../components/workspaces/RenameColumnModal'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import {
  ChevronLeftIcon,
  ChevronRightIcon,
  PencilIcon,
  PlusIcon,
  TrashIcon,
} from '../components/ui/Icons'
import { Spinner } from '../components/ui/Spinner'
import { useColumns, useReorderColumns } from '../hooks/useColumns'
import { useWorkspaces } from '../hooks/useWorkspaces'
import type { WorkspaceColumnDto } from '../types/api.types'

const MAX_COLUMNS = 7

export function ColumnsPage() {
  const { workspaceId: param } = useParams<{ workspaceId: string }>()
  const workspaceId = param ?? ''

  const { data: workspaces, isLoading: isLoadingWorkspace } = useWorkspaces()
  const workspace = workspaces?.find((w) => w.id === workspaceId) ?? null
  const canManage = workspace?.myRole === 'Owner' || workspace?.myRole === 'Admin'

  const { data: columns, isLoading: isLoadingColumns } = useColumns(workspaceId)
  const reorderMutation = useReorderColumns(workspaceId)

  const [addOpen, setAddOpen] = useState(false)
  const [renameTarget, setRenameTarget] = useState<WorkspaceColumnDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<WorkspaceColumnDto | null>(null)
  const [reorderError, setReorderError] = useState<string | null>(null)

  const sorted = columns ? [...columns].sort((a, b) => a.order - b.order) : []
  const atLimit = sorted.length >= MAX_COLUMNS

  const move = (index: number, direction: -1 | 1) => {
    const newOrder = [...sorted]
    const swapIndex = index + direction
    ;[newOrder[index], newOrder[swapIndex]] = [newOrder[swapIndex]!, newOrder[index]!]
    setReorderError(null)
    reorderMutation.mutate(
      { columnIds: newOrder.map((c) => c.id) },
      { onError: (err) => setReorderError(getErrorMessage(err)) },
    )
  }

  return (
    <div className="flex min-h-screen bg-gray-50">
      <Sidebar />

      <main className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-3">
          <span className="text-sm font-medium text-gray-900">
            {isLoadingWorkspace
              ? 'Loading…'
              : workspace
                ? `${workspace.name} — Columns`
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

        <div className="flex-1 overflow-y-auto px-6 py-5">
          {!isLoadingWorkspace && !workspace ? (
            <div className="flex justify-center py-10 text-center">
              <div>
                <h1 className="text-lg font-semibold text-gray-900">Workspace not found</h1>
                <p className="mt-1 text-sm text-gray-500">
                  <Link to="/" className="font-medium text-brand-700 hover:underline">
                    Go to your workspaces
                  </Link>
                </p>
              </div>
            </div>
          ) : (
            <>
              {reorderError && (
                <div className="mb-4">
                  <Alert variant="error">{reorderError}</Alert>
                </div>
              )}

              {isLoadingColumns ? (
                <div className="flex justify-center py-10">
                  <Spinner className="h-5 w-5 border-gray-300 border-t-brand-500" />
                </div>
              ) : (
                <>
                  {/* horizontal scroll track */}
                  <div className="overflow-x-auto pb-2">
                    <div className="flex min-w-max items-start gap-3">
                      {sorted.map((column, index) => (
                        <div
                          key={column.id}
                          className="flex w-60 shrink-0 flex-col rounded-lg border border-gray-200 bg-white shadow-sm"
                        >
                          {/* card header */}
                          <div className="flex items-center justify-between gap-2 border-b border-gray-100 px-4 py-3">
                            <span
                              className="min-w-0 flex-1 truncate text-sm font-semibold text-gray-900"
                              title={column.name}
                            >
                              {column.name}
                            </span>

                            {canManage && (
                              <div className="flex shrink-0 items-center gap-1">
                                <button
                                  type="button"
                                  aria-label={`Rename "${column.name}"`}
                                  onClick={() => setRenameTarget(column)}
                                  className="rounded p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-700"
                                >
                                  <PencilIcon className="h-3.5 w-3.5" />
                                </button>
                                <button
                                  type="button"
                                  aria-label={`Delete "${column.name}"`}
                                  onClick={() => setDeleteTarget(column)}
                                  className="rounded p-1 text-gray-400 hover:bg-red-50 hover:text-red-600"
                                >
                                  <TrashIcon className="h-3.5 w-3.5" />
                                </button>
                              </div>
                            )}
                          </div>

                          {/* card body */}
                          <div className="flex items-center justify-between px-4 py-3">
                            <span className="text-xs text-gray-400">Position {index + 1}</span>

                            {canManage && (
                              <div className="flex items-center gap-0.5">
                                <button
                                  type="button"
                                  aria-label="Move left"
                                  disabled={index === 0 || reorderMutation.isPending}
                                  onClick={() => move(index, -1)}
                                  className="rounded p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-700 disabled:cursor-not-allowed disabled:opacity-30"
                                >
                                  <ChevronLeftIcon className="h-4 w-4" />
                                </button>
                                <button
                                  type="button"
                                  aria-label="Move right"
                                  disabled={index === sorted.length - 1 || reorderMutation.isPending}
                                  onClick={() => move(index, 1)}
                                  className="rounded p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-700 disabled:cursor-not-allowed disabled:opacity-30"
                                >
                                  <ChevronRightIcon className="h-4 w-4" />
                                </button>
                              </div>
                            )}
                          </div>
                        </div>
                      ))}

                      {/* add-column placeholder card — visible for Owner/Admin when under limit */}
                      {canManage && !atLimit && (
                        <button
                          type="button"
                          onClick={() => setAddOpen(true)}
                          className="flex h-24 w-60 shrink-0 items-center justify-center gap-2 rounded-lg border-2 border-dashed border-gray-200 text-sm text-gray-400 transition-colors hover:border-brand-400 hover:text-brand-600"
                        >
                          <PlusIcon className="h-4 w-4" />
                          Add column
                        </button>
                      )}

                      {sorted.length === 0 && !canManage && (
                        <p className="text-sm text-gray-400">No columns yet.</p>
                      )}
                    </div>
                  </div>

                  {atLimit && canManage && (
                    <p className="mt-3 text-xs text-gray-400">
                      Maximum of {MAX_COLUMNS} columns reached.
                    </p>
                  )}
                </>
              )}
            </>
          )}
        </div>
      </main>

      {addOpen && <AddColumnModal workspaceId={workspaceId} onClose={() => setAddOpen(false)} />}

      {renameTarget && (
        <RenameColumnModal
          workspaceId={workspaceId}
          column={renameTarget}
          onClose={() => setRenameTarget(null)}
        />
      )}

      {deleteTarget && (
        <DeleteColumnDialog
          workspaceId={workspaceId}
          column={deleteTarget}
          onClose={() => setDeleteTarget(null)}
        />
      )}
    </div>
  )
}
