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
  CheckIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  MenuIcon,
  PencilIcon,
  PlusIcon,
  TrashIcon,
} from '../components/ui/Icons'
import { useMobileSidebar } from '../store/mobileSidebarStore'
import { Spinner } from '../components/ui/Spinner'
import { useColumns, useReorderColumns, useUpdateColumn } from '../hooks/useColumns'
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
  const updateColumnMutation = useUpdateColumn(workspaceId)

  const [addOpen, setAddOpen] = useState(false)
  const [renameTarget, setRenameTarget] = useState<WorkspaceColumnDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<WorkspaceColumnDto | null>(null)
  const [reorderError, setReorderError] = useState<string | null>(null)
  const { toggle: toggleSidebar } = useMobileSidebar()

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
        <header className="flex items-center justify-between border-b border-gray-200 bg-white px-3 py-3 md:px-6">
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={toggleSidebar}
              className="rounded-md p-2 text-gray-500 hover:bg-gray-100 md:hidden"
              aria-label="Open menu"
            >
              <MenuIcon className="h-5 w-5" />
            </button>
            <span className="text-sm font-medium text-gray-900">
              {isLoadingWorkspace
                ? 'Loading…'
                : workspace
                  ? `${workspace.name} — Columns`
                  : 'Workspace not found'}
            </span>
          </div>

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
                  {/* mobile: vertical stack; desktop: horizontal scroll */}
                  <div className="md:overflow-x-auto md:pb-2">
                    <div className="flex flex-col gap-3 md:flex-row md:min-w-max md:items-start">
                      {sorted.map((column, index) => (
                        <div
                          key={column.id}
                          className="flex w-full flex-col rounded-lg border border-gray-200 bg-white shadow-sm md:w-60 md:shrink-0"
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
                          <div className="px-4 py-3 space-y-3">
                            <div className="flex items-center justify-between">
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

                            {canManage && (
                              <button
                                type="button"
                                disabled={updateColumnMutation.isPending}
                                onClick={() =>
                                  updateColumnMutation.mutate({
                                    columnId: column.id,
                                    data: { name: column.name, color: column.color, isDoneColumn: !column.isDoneColumn },
                                  })
                                }
                                className={[
                                  'flex w-full items-center gap-2 rounded-md border px-3 py-1.5 text-xs transition-colors disabled:opacity-60',
                                  column.isDoneColumn
                                    ? 'border-green-300 bg-green-50 font-medium text-green-700'
                                    : 'border-gray-200 text-gray-500 hover:border-gray-300 hover:bg-gray-50',
                                ].join(' ')}
                              >
                                <CheckIcon className="h-3.5 w-3.5 shrink-0" />
                                {column.isDoneColumn ? 'Done column (active)' : 'Set as Done column'}
                              </button>
                            )}

                            {!canManage && column.isDoneColumn && (
                              <span className="flex items-center gap-1.5 text-xs text-green-600">
                                <CheckIcon className="h-3.5 w-3.5" />
                                Done column
                              </span>
                            )}
                          </div>
                        </div>
                      ))}

                      {/* add-column placeholder card — visible for Owner/Admin when under limit */}
                      {canManage && !atLimit && (
                        <button
                          type="button"
                          onClick={() => setAddOpen(true)}
                          className="flex h-16 w-full items-center justify-center gap-2 rounded-lg border-2 border-dashed border-gray-200 text-sm text-gray-400 transition-colors hover:border-brand-400 hover:text-brand-600 md:h-24 md:w-60 md:shrink-0"
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
