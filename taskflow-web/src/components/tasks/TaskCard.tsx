import type { DragEvent } from 'react'
import { CalendarIcon, CheckIcon } from '../ui/Icons'
import { UserAvatar } from '../ui/UserAvatar'
import type { WorkspaceTaskDto } from '../../types/api.types'

interface TaskCardProps {
  task: WorkspaceTaskDto
  priorityColor: string
  isSelected: boolean
  onClick: () => void
  onDragStart: (e: DragEvent<HTMLDivElement>) => void
  onDragEnd?: () => void
}


export function TaskCard({ task, priorityColor, isSelected, onClick, onDragStart, onDragEnd }: TaskCardProps) {
  const dueDateLabel = task.dueDate
    ? new Date(task.dueDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
    : null

  const visibleLabels = task.labels.slice(0, 3)
  const overflowCount = task.labels.length - 3

  const isDone = task.status === 'Done'

  return (
    <div
      draggable
      onDragStart={onDragStart}
      onDragEnd={onDragEnd}
      onClick={onClick}
      className={[
        'relative cursor-pointer rounded-lg border border-l-4 bg-white px-3 py-2.5 shadow-sm transition-colors hover:border-gray-300 hover:border-l-4',
        isSelected ? 'border-brand-500' : 'border-gray-200',
        isDone ? 'opacity-50' : '',
      ].join(' ')}
      style={{ borderLeftColor: priorityColor }}
    >
      {isDone && (
        <span className="absolute -right-1.5 -top-1.5 flex items-center gap-0.5 rounded-full bg-green-500 px-1.5 py-0.5 text-[9px] font-semibold text-white shadow-sm">
          <CheckIcon className="h-2.5 w-2.5" />
          Done
        </span>
      )}
      <div className="mb-1.5 flex items-start gap-1.5">
        <span className="mt-px shrink-0 text-[10px] font-medium text-gray-400">
          #{task.number}
        </span>
        <p className="line-clamp-2 text-sm font-medium leading-snug text-gray-900">{task.title}</p>
      </div>

      {task.labels.length > 0 && (
        <div className="mb-1.5 flex flex-wrap gap-1">
          {visibleLabels.map((label) => (
            <span
              key={label.id}
              className="rounded-full px-1.5 py-0.5 text-[10px] font-medium text-white"
              style={{ backgroundColor: label.color }}
            >
              {label.name}
            </span>
          ))}
          {overflowCount > 0 && (
            <span className="rounded-full bg-gray-100 px-1.5 py-0.5 text-[10px] text-gray-500">
              +{overflowCount}
            </span>
          )}
        </div>
      )}

      <div className="flex flex-wrap items-center gap-1.5">
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
          <span title={task.assigneeName} className="ml-auto shrink-0">
            <UserAvatar
              displayName={task.assigneeName}
              avatarColor={task.assigneeAvatarColor || '#818cf8'}
              avatarPath={task.assigneeAvatarPath}
              avatarStatus={task.assigneeAvatarStatus ?? 'None'}
              size="xs"
            />
          </span>
        )}
      </div>
    </div>
  )
}
