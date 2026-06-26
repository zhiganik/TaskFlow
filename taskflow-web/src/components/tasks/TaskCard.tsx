import type { DragEvent } from 'react'
import { CalendarIcon } from '../ui/Icons'
import { PRIORITY_BADGE } from '../../lib/priority'
import type { WorkspaceTaskDto } from '../../types/api.types'

interface TaskCardProps {
  task: WorkspaceTaskDto
  isSelected: boolean
  onClick: () => void
  onDragStart: (e: DragEvent<HTMLDivElement>) => void
  onDragEnd?: () => void
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

export function TaskCard({ task, isSelected, onClick, onDragStart, onDragEnd }: TaskCardProps) {
  const dueDateLabel = task.dueDate
    ? new Date(task.dueDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
    : null

  return (
    <div
      draggable
      onDragStart={onDragStart}
      onDragEnd={onDragEnd}
      onClick={onClick}
      className={[
        'cursor-pointer rounded-lg border bg-white px-3 py-2.5 shadow-sm transition-colors hover:border-gray-300',
        isSelected ? 'border-l-[3px] border-brand-500' : 'border-gray-200',
      ].join(' ')}
    >
      <div className="mb-1.5 flex items-start gap-1.5">
        <span className="mt-px shrink-0 text-[10px] font-medium text-gray-400">
          #{task.number}
        </span>
        <p className="line-clamp-2 text-sm font-medium leading-snug text-gray-900">{task.title}</p>
      </div>

      <div className="flex flex-wrap items-center gap-1.5">
        <span className={`inline-flex rounded-full px-2 py-0.5 text-[11px] font-medium ${PRIORITY_BADGE[task.priority]}`}>
          {task.priority}
        </span>

        {dueDateLabel && (
          <span
            className={`inline-flex items-center gap-0.5 text-[11px] ${
              task.isOverdue ? 'font-medium text-red-600' : 'text-gray-400'
            }`}
          >
            <CalendarIcon className="h-3 w-3" />
            {dueDateLabel}
          </span>
        )}

        {task.assigneeName && (
          <span
            title={task.assigneeName}
            className="ml-auto flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[9px] font-semibold text-white"
            style={{ backgroundColor: task.assigneeAvatarColor || '#818cf8' }}
          >
            {initials(task.assigneeName)}
          </span>
        )}
      </div>
    </div>
  )
}
