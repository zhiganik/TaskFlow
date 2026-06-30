import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { Sidebar } from '../components/layout/Sidebar'
import { TaskDetailPanel } from '../components/tasks/TaskDetailPanel'
import { TaskFilterBar } from '../components/tasks/TaskFilterBar'
import { Spinner } from '../components/ui/Spinner'
import { MenuIcon } from '../components/ui/Icons'
import { useTaskFilter } from '../hooks/useTaskFilter'
import { useClosedTasks, useReopenTask } from '../hooks/useTasks'
import { useColumns } from '../hooks/useColumns'
import { useMembers } from '../hooks/useMembers'
import { usePriorityConfig } from '../hooks/usePriorityConfig'
import { useLabels } from '../hooks/useLabels'
import { useWorkspaces } from '../hooks/useWorkspaces'
import { useMobileSidebar } from '../store/mobileSidebarStore'
import type { WorkspaceTaskDto } from '../types/api.types'

type StatusFilter = 'All' | 'Closed' | 'Deleted'

export function ArchivePage() {
  const { workspaceId: param } = useParams<{ workspaceId: string }>()
  const workspaceId = param ?? ''

  const { data: workspaces } = useWorkspaces()
  const workspace = workspaces?.find((w) => w.id === workspaceId) ?? null

  const { data: columns = [] } = useColumns(workspaceId)
  const { data: members = [] } = useMembers(workspaceId)
  const { data: priorityConfigs = [] } = usePriorityConfig(workspaceId)
  const { data: labels = [] } = useLabels(workspaceId)

  const filterState = useTaskFilter()
  const { data: tasks, isLoading } = useClosedTasks(workspaceId, filterState.filter)

  const reopenMutation = useReopenTask(workspaceId)

  const [selectedTask, setSelectedTask] = useState<WorkspaceTaskDto | null>(null)
  const [panelWidth, setPanelWidth] = useState(380)
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('All')
  const { toggle: toggleSidebar } = useMobileSidebar()

  const visibleTasks = tasks?.filter((t) => {
    if (statusFilter === 'All') return true
    return t.status === statusFilter
  }) ?? []

  const getPriorityColor = (priority: string) =>
    priorityConfigs.find((c) => c.priority === priority)?.color ??
    { Low: '#22c55e', Medium: '#f59e0b', High: '#ef4444' }[priority as 'Low' | 'Medium' | 'High'] ??
    '#6b7280'

  const formatDate = (iso: string | null) => {
    if (!iso) return '—'
    return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
  }

  const handleResizeStart = (e: React.MouseEvent) => {
    const startX = e.clientX
    const startW = panelWidth
    const onMove = (me: MouseEvent) => setPanelWidth(Math.max(280, Math.min(720, startW - (me.clientX - startX))))
    const onUp = () => { window.removeEventListener('mousemove', onMove); window.removeEventListener('mouseup', onUp) }
    window.addEventListener('mousemove', onMove)
    window.addEventListener('mouseup', onUp)
  }

  const statusOptions: StatusFilter[] = ['All', 'Closed', 'Deleted']

  return (
    <div className="flex h-screen overflow-hidden bg-gray-50">
      <Sidebar />

      <div className="flex min-w-0 flex-1 overflow-hidden">
        <main className="flex min-w-0 flex-1 flex-col overflow-hidden">
          <header className="flex shrink-0 items-center gap-2 border-b border-gray-200 bg-white px-3 py-3 md:px-6 md:py-4">
            <button
              type="button"
              onClick={toggleSidebar}
              className="rounded-md p-2 text-gray-500 hover:bg-gray-100 md:hidden"
              aria-label="Open menu"
            >
              <MenuIcon className="h-5 w-5" />
            </button>
            <div>
              <h1 className="text-lg font-semibold text-gray-900 md:text-xl">Archive</h1>
              {workspace && <p className="mt-0.5 text-sm text-gray-500">{workspace.name}</p>}
            </div>
          </header>

          <div className="shrink-0 border-b border-gray-100 bg-white">
            <TaskFilterBar
              searchInput={filterState.searchInput}
              onSearchChange={filterState.setSearchInput}
              assigneeIds={filterState.assigneeIds}
              onToggleAssignee={filterState.toggleAssigneeId}
              priorities={filterState.priorities}
              onPriorityChange={filterState.setPriorities}
              priorityConfigs={priorityConfigs}
              labelIds={filterState.labelIds}
              onToggleLabel={filterState.toggleLabelId}
              labels={labels}
              members={members}
              hasActiveFilters={filterState.hasActiveFilters}
              onClear={filterState.clearAll}
            />
            <div className="flex items-center gap-1 px-3 py-2 md:px-4">
              <div className="flex items-center gap-1 rounded-md border border-gray-200 bg-gray-50 p-0.5">
                {statusOptions.map((s) => (
                  <button
                    key={s}
                    type="button"
                    onClick={() => setStatusFilter(s)}
                    className={[
                      'rounded px-2.5 py-1 text-xs font-medium transition-colors',
                      statusFilter === s
                        ? 'bg-white text-gray-900 shadow-sm'
                        : 'text-gray-500 hover:text-gray-700',
                    ].join(' ')}
                  >
                    {s}
                  </button>
                ))}
              </div>
            </div>
          </div>

          <div className="flex-1 overflow-y-auto px-3 py-3 md:px-6 md:py-4">
            {isLoading ? (
              <div className="flex justify-center py-12">
                <Spinner className="h-5 w-5 border-gray-300 border-t-brand-500" />
              </div>
            ) : !visibleTasks.length ? (
              <div className="flex flex-col items-center justify-center py-16 text-center">
                <p className="text-sm font-medium text-gray-500">No archived tasks</p>
                <p className="mt-1 text-xs text-gray-400">
                  Closed or deleted tasks will appear here.
                </p>
              </div>
            ) : (
              <>
                {/* Mobile: card list */}
                <div className="space-y-2 md:hidden">
                  {visibleTasks.map((task) => (
                    <div
                      key={task.id}
                      className="cursor-pointer rounded-xl border border-gray-200 bg-white p-3 shadow-sm transition-colors hover:bg-gray-50"
                      style={{ borderLeftWidth: 4, borderLeftColor: getPriorityColor(task.priority) }}
                      onClick={() => setSelectedTask(task)}
                    >
                      <div className="flex items-start justify-between gap-2">
                        <div className="min-w-0 flex-1">
                          <div className="flex items-center gap-1.5 mb-1">
                            <span className="text-xs text-gray-400">#{task.number}</span>
                            <span
                              className={[
                                'inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium',
                                task.status === 'Closed'
                                  ? 'bg-blue-50 text-blue-700'
                                  : 'bg-red-50 text-red-700',
                              ].join(' ')}
                            >
                              {task.status}
                            </span>
                          </div>
                          <p className="font-medium text-gray-900 text-sm leading-snug">{task.title}</p>
                          {task.labels.length > 0 && (
                            <div className="mt-1.5 flex flex-wrap gap-1">
                              {task.labels.slice(0, 3).map((l) => (
                                <span
                                  key={l.id}
                                  className="rounded-full px-1.5 py-0.5 text-[10px] font-medium text-white"
                                  style={{ backgroundColor: l.color }}
                                >
                                  {l.name}
                                </span>
                              ))}
                              {task.labels.length > 3 && (
                                <span className="text-[10px] text-gray-400">+{task.labels.length - 3}</span>
                              )}
                            </div>
                          )}
                          <div className="mt-2 flex flex-wrap gap-3 text-xs text-gray-500">
                            {task.assigneeName && <span>{task.assigneeName}</span>}
                            <span>{task.columnName}</span>
                            <span>{formatDate(task.closedAt)}</span>
                          </div>
                        </div>
                        <button
                          type="button"
                          disabled={reopenMutation.isPending}
                          onClick={(e) => { e.stopPropagation(); reopenMutation.mutate(task.id) }}
                          className="shrink-0 rounded border border-gray-200 px-2.5 py-1 text-xs text-gray-600 hover:border-green-300 hover:bg-green-50 hover:text-green-700 disabled:opacity-60"
                        >
                          Reopen
                        </button>
                      </div>
                    </div>
                  ))}
                </div>

                {/* Desktop: table */}
                <div className="hidden md:block overflow-x-auto rounded-xl border border-gray-200 bg-white shadow-sm">
                  <table className="min-w-full text-sm">
                    <thead>
                      <tr className="border-b border-gray-100 bg-gray-50 text-left text-[11px] font-medium uppercase tracking-wide text-gray-400">
                        <th className="px-4 py-2.5 w-12">#</th>
                        <th className="px-4 py-2.5">Title</th>
                        <th className="px-4 py-2.5 w-20">Priority</th>
                        <th className="px-4 py-2.5 w-32">Assignee</th>
                        <th className="px-4 py-2.5 w-32">Column</th>
                        <th className="px-4 py-2.5 w-24">Status</th>
                        <th className="px-4 py-2.5 w-32">Archived</th>
                        <th className="px-4 py-2.5 w-24" />
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                      {visibleTasks.map((task) => (
                        <tr
                          key={task.id}
                          className="cursor-pointer transition-colors hover:bg-gray-50"
                          onClick={() => setSelectedTask(task)}
                        >
                          <td className="px-4 py-2.5 text-xs text-gray-400">#{task.number}</td>
                          <td className="px-4 py-2.5">
                            <div className="flex min-w-0 flex-wrap items-center gap-1.5">
                              <span className="font-medium text-gray-900">{task.title}</span>
                              {task.labels.slice(0, 3).map((l) => (
                                <span
                                  key={l.id}
                                  className="shrink-0 rounded-full px-1.5 py-0.5 text-[10px] font-medium text-white"
                                  style={{ backgroundColor: l.color }}
                                >
                                  {l.name}
                                </span>
                              ))}
                              {task.labels.length > 3 && (
                                <span className="shrink-0 text-[10px] text-gray-400">+{task.labels.length - 3}</span>
                              )}
                            </div>
                          </td>
                          <td className="px-4 py-2.5">
                            <span
                              className="inline-block h-2 w-2 rounded-full"
                              style={{ backgroundColor: getPriorityColor(task.priority) }}
                            />
                            <span className="ml-1.5 text-xs text-gray-600">{task.priority}</span>
                          </td>
                          <td className="px-4 py-2.5 text-xs text-gray-600">
                            {task.assigneeName ?? <span className="text-gray-400">—</span>}
                          </td>
                          <td className="px-4 py-2.5 text-xs text-gray-600">{task.columnName}</td>
                          <td className="px-4 py-2.5">
                            <span
                              className={[
                                'inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium',
                                task.status === 'Closed'
                                  ? 'bg-blue-50 text-blue-700'
                                  : 'bg-red-50 text-red-700',
                              ].join(' ')}
                            >
                              {task.status}
                            </span>
                          </td>
                          <td className="px-4 py-2.5 text-xs text-gray-400">
                            {formatDate(task.closedAt)}
                          </td>
                          <td className="px-4 py-2.5" onClick={(e) => e.stopPropagation()}>
                            <button
                              type="button"
                              disabled={reopenMutation.isPending}
                              onClick={() => reopenMutation.mutate(task.id)}
                              className="rounded border border-gray-200 px-2.5 py-1 text-xs text-gray-600 hover:border-green-300 hover:bg-green-50 hover:text-green-700 disabled:opacity-60"
                            >
                              Reopen
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </>
            )}
          </div>
        </main>

        {selectedTask && (
          <TaskDetailPanel
            task={selectedTask}
            columns={columns}
            workspaceId={workspaceId}
            width={panelWidth}
            onResizeStart={handleResizeStart}
            onClose={() => setSelectedTask(null)}
            onTaskUpdated={(updated) => setSelectedTask(updated)}
          />
        )}
      </div>
    </div>
  )
}
