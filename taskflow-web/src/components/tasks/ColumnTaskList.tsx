import { type DragEvent, useEffect, useMemo } from 'react'
import { useColumnTasks } from '../../hooks/useColumnTasks'
import type { TaskFilterParams, WorkspaceTaskDto } from '../../types/api.types'
import { Spinner } from '../ui/Spinner'
import { TaskCard } from './TaskCard'

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
  const tasks = useMemo(() => data?.pages.flatMap((p) => p.items) ?? [], [data])

  useEffect(() => { onCountChange(tasks.length) }, [tasks.length, onCountChange])

  if (tasks.length === 0 && !isFetchingNextPage) {
    return <p className="py-4 text-center text-xs text-gray-400">No tasks</p>
  }

  return (
    <>
      {tasks.map((task) => (
        <TaskCard
          key={task.id}
          task={task}
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
