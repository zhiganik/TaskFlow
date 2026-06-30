import { useRef, useState, useEffect } from 'react'
import type { LabelDto, MemberDto, PriorityConfigDto, TaskPriority } from '../../types/api.types'
import { SearchIcon, TagIcon, UserIcon, XIcon } from '../ui/Icons'
import { UserAvatar } from '../ui/UserAvatar'

interface Props {
  searchInput: string
  onSearchChange: (v: string) => void
  assigneeIds: string[]
  onToggleAssignee: (id: string) => void
  priorities: TaskPriority[]
  onPriorityChange: (p: TaskPriority[]) => void
  priorityConfigs: PriorityConfigDto[]
  labelIds: string[]
  onToggleLabel: (id: string) => void
  labels: LabelDto[]
  members: MemberDto[]
  hasActiveFilters: boolean
  onClear: () => void
}

const FALLBACK_PRIORITY_COLORS: Record<TaskPriority, string> = {
  Low: '#22c55e',
  Medium: '#f59e0b',
  High: '#ef4444',
}

export function TaskFilterBar({
  searchInput, onSearchChange,
  assigneeIds, onToggleAssignee,
  priorities, onPriorityChange,
  priorityConfigs,
  labelIds, onToggleLabel, labels,
  members, hasActiveFilters, onClear,
}: Props) {
  const sorted = [...members].sort((a, b) => a.displayName.localeCompare(b.displayName))
  const [labelDropdownOpen, setLabelDropdownOpen] = useState(false)
  const labelDropdownRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (labelDropdownRef.current && !labelDropdownRef.current.contains(e.target as Node))
        setLabelDropdownOpen(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  const getPriorityColor = (p: TaskPriority) =>
    priorityConfigs.find((c) => c.priority === p)?.color ?? FALLBACK_PRIORITY_COLORS[p]

  const getPriorityLabel = (p: TaskPriority) =>
    priorityConfigs.find((c) => c.priority === p)?.displayName ?? p

  const togglePriority = (p: TaskPriority) =>
    onPriorityChange(
      priorities.includes(p) ? priorities.filter((x) => x !== p) : [...priorities, p],
    )

  const allPriorities: TaskPriority[] = ['High', 'Medium', 'Low']

  return (
    <div className="flex flex-col gap-2 border-b border-gray-100 bg-white px-3 py-2 md:flex-row md:flex-wrap md:items-center md:gap-3 md:px-4">

      {/* search — full width on mobile */}
      <div className="relative flex items-center">
        <SearchIcon className="pointer-events-none absolute left-2.5 h-3.5 w-3.5 text-gray-400" />
        <input
          type="text"
          value={searchInput}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="Search by title or #"
          className="h-9 w-full rounded-md border border-gray-200 pl-8 pr-7 text-sm text-gray-900 placeholder-gray-400 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30 md:h-8 md:w-48"
        />
        {searchInput && (
          <button
            type="button"
            onClick={() => onSearchChange('')}
            className="absolute right-2 text-gray-400 hover:text-gray-600"
          >
            <XIcon className="h-3 w-3" />
          </button>
        )}
      </div>

      {/* filters row — horizontally scrollable on mobile */}
      <div className="flex items-center gap-3 overflow-x-auto scrollbar-hide pb-0.5 md:contents">

        {/* assignee avatars */}
        {sorted.length > 0 && (
          <div className="flex shrink-0 items-center gap-1.5">
            <button
              type="button"
              title="Unassigned"
              onClick={() => onToggleAssignee('unassigned')}
              className={[
                'flex h-8 w-8 items-center justify-center rounded-full border-2 transition-all md:h-7 md:w-7',
                assigneeIds.includes('unassigned')
                  ? 'border-brand-500 bg-brand-50 text-brand-600'
                  : 'border-gray-200 bg-gray-100 text-gray-400 hover:border-gray-300',
              ].join(' ')}
            >
              <UserIcon className="h-3.5 w-3.5" />
            </button>

            {sorted.map((m) => {
              const selected = assigneeIds.includes(m.userId)
              return (
                <button
                  key={m.userId}
                  type="button"
                  title={m.displayName}
                  onClick={() => onToggleAssignee(m.userId)}
                  className={[
                    'shrink-0 rounded-full border-2 transition-all',
                    selected ? 'border-brand-500' : 'border-transparent hover:opacity-80',
                  ].join(' ')}
                >
                  <UserAvatar
                    displayName={m.displayName}
                    avatarColor={m.avatarColor}
                    avatarPath={m.avatarPath}
                    avatarStatus={m.avatarStatus}
                    size="sm"
                  />
                </button>
              )
            })}
          </div>
        )}

        {sorted.length > 0 && <div className="hidden h-5 w-px shrink-0 bg-gray-200 md:block" />}

        {/* priority chips */}
        <div className="flex shrink-0 items-center gap-1">
          {allPriorities.map((p) => {
            const active = priorities.includes(p)
            const color = getPriorityColor(p)
            return (
              <button
                key={p}
                type="button"
                onClick={() => togglePriority(p)}
                className={[
                  'flex h-8 items-center gap-1.5 rounded-full border px-3 text-xs font-medium transition-colors md:h-7',
                  active
                    ? 'border-gray-300 bg-gray-100 text-gray-700'
                    : 'border-gray-200 text-gray-400 hover:border-gray-300 hover:bg-gray-50',
                ].join(' ')}
              >
                <span
                  className="h-2 w-2 shrink-0 rounded-full transition-opacity"
                  style={{ backgroundColor: color, opacity: active ? 1 : 0.35 }}
                />
                {getPriorityLabel(p)}
              </button>
            )
          })}
        </div>

        {/* label filter dropdown */}
        {labels.length > 0 && (
          <>
            <div className="hidden h-5 w-px shrink-0 bg-gray-200 md:block" />
            <div ref={labelDropdownRef} className="relative shrink-0">
              <button
                type="button"
                onClick={() => setLabelDropdownOpen((o) => !o)}
                className={[
                  'flex h-8 items-center gap-1.5 rounded-full border px-3 text-xs font-medium transition-colors md:h-7',
                  labelIds.length > 0
                    ? 'border-gray-300 bg-gray-100 text-gray-700'
                    : 'border-gray-200 text-gray-400 hover:border-gray-300 hover:bg-gray-50',
                ].join(' ')}
              >
                <TagIcon className="h-3 w-3 shrink-0" />
                Labels
                {labelIds.length > 0 && (
                  <span className="flex h-4 w-4 items-center justify-center rounded-full bg-brand-500 text-[10px] font-semibold text-white">
                    {labelIds.length}
                  </span>
                )}
              </button>

              {labelDropdownOpen && (
                <div className="absolute left-0 top-full z-50 mt-1.5 w-48 rounded-lg border border-gray-200 bg-white py-1 shadow-lg">
                  {labels.map((l) => {
                    const active = labelIds.includes(l.id)
                    return (
                      <button
                        key={l.id}
                        type="button"
                        onClick={() => onToggleLabel(l.id)}
                        className="flex w-full items-center gap-2 px-3 py-2 text-left hover:bg-gray-50"
                      >
                        <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ backgroundColor: l.color }} />
                        <span className="flex-1 text-xs text-gray-700">{l.name}</span>
                        {active && <span className="text-xs font-bold text-brand-600">✓</span>}
                      </button>
                    )
                  })}
                </div>
              )}
            </div>
          </>
        )}

        {/* clear */}
        {hasActiveFilters && (
          <button
            type="button"
            onClick={onClear}
            className="flex h-8 shrink-0 items-center gap-1 rounded-md px-2 text-xs text-gray-400 hover:bg-gray-100 hover:text-gray-700 md:h-7"
          >
            <XIcon className="h-3 w-3" />
            Clear
          </button>
        )}
      </div>
    </div>
  )
}
