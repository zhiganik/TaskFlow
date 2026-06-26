import { type DragEvent, useEffect, useMemo, useRef } from 'react'
import { useColumnTasks } from '../../hooks/useColumnTasks'
import { usePriorityConfig } from '../../hooks/usePriorityConfig'
import type { TaskFilterParams, TaskPriority, WorkspaceTaskDto } from '../../types/api.types'
import { Spinner } from '../ui/Spinner'
import { TaskCard } from './TaskCard'

const FALLBACK_PRIORITY_COLORS: Record<TaskPriority, string> = {
  Low: '#22c55e',
  Medium: '#f59e0b',
  High: '#ef4444',
}

interface Props {
  workspaceId: string
  columnId: string
  filter: TaskFilterParams
  selectedTaskId: string | null
  onTaskClick: (task: WorkspaceTaskDto) => void
  onDragStart: (e: DragEvent<HTMLDivElement>, taskId: string) => void
  onDragEnd: () => void
  onCountChange: (count: number) => void
}

export function ColumnTaskList({
  workspaceId, columnId, filter,
  selectedTaskId, onTaskClick,
  onDragStart, onDragEnd, onCountChange,
}: Props) {
  const { data, fetchNextPage, hasNextPage, isFetchingNextPage } = useColumnTasks(workspaceId, columnId, filter)
  const { data: priorityConfigs } = usePriorityConfig(workspaceId)
  const tasks = useMemo(() => data?.pages.flatMap((p) => p.items) ?? [], [data])

  const priorityColorMap = useMemo<Record<TaskPriority, string>>(() => {
    if (!priorityConfigs) return FALLBACK_PRIORITY_COLORS
    return Object.fromEntries(
      priorityConfigs.map((c) => [c.priority, c.color])
    ) as Record<TaskPriority, string>
  }, [priorityConfigs])

  const onCountChangeRef = useRef(onCountChange)
  onCountChangeRef.current = onCountChange
  useEffect(() => { onCountChangeRef.current(tasks.length) }, [tasks.length])

  if (tasks.length === 0 && !isFetchingNextPage) {
    return <p className="py-4 text-center text-xs text-gray-400">No tasks</p>
  }

  return (
    <>
      {tasks.map((task) => (
        <TaskCard
          key={task.id}
          task={task}
          priorityColor={priorityColorMap[task.priority]}
          isSelected={task.id === selectedTaskId}
          onClick={() => onTaskClick(task)}
          onDragStart={(e) => onDragStart(e, task.id)}
          onDragEnd={onDragEnd}
        />
      ))}
      {hasNextPage && (
        <button
          type="button"
          onClick={() => fetchNextPage()}
          disabled={isFetchingNextPage}
          className="mt-1 flex w-full items-center justify-center gap-1.5 rounded-lg py-1.5 text-xs text-gray-400 hover:bg-gray-200 hover:text-gray-600 disabled:opacity-50"
        >
          {isFetchingNextPage
            ? <><Spinner className="h-3 w-3 border-gray-300 border-t-gray-500" />Loading…</>
            : 'Show more'}
        </button>
      )}
    </>
  )
}
