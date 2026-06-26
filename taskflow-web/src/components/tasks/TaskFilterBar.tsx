import { PRIORITY_BADGE } from '../../lib/priority'
import type { MemberDto, TaskPriority } from '../../types/api.types'
import { SearchIcon, UserIcon, XIcon } from '../ui/Icons'

const PRIORITIES: TaskPriority[] = ['High', 'Medium', 'Low']

interface Props {
  searchInput: string
  onSearchChange: (v: string) => void
  assigneeId: string | undefined
  onAssigneeChange: (id: string | undefined) => void
  priorities: TaskPriority[]
  onPriorityChange: (p: TaskPriority[]) => void
  members: MemberDto[]
  hasActiveFilters: boolean
  onClear: () => void
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

export function TaskFilterBar({
  searchInput, onSearchChange,
  assigneeId, onAssigneeChange,
  priorities, onPriorityChange,
  members, hasActiveFilters, onClear,
}: Props) {
  const sorted = [...members].sort((a, b) => a.displayName.localeCompare(b.displayName))

  const togglePriority = (p: TaskPriority) =>
    onPriorityChange(
      priorities.includes(p) ? priorities.filter((x) => x !== p) : [...priorities, p],
    )

  const toggleAssignee = (id: string) =>
    onAssigneeChange(assigneeId === id ? undefined : id)

  return (
    <div className="flex flex-wrap items-center gap-3 border-b border-gray-100 bg-white px-4 py-2">

      {/* search */}
      <div className="relative flex items-center">
        <SearchIcon className="pointer-events-none absolute left-2.5 h-3.5 w-3.5 text-gray-400" />
        <input
          type="text"
          value={searchInput}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="Search by title or #"
          className="h-8 w-48 rounded-md border border-gray-200 pl-8 pr-7 text-sm text-gray-900 placeholder-gray-400 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30"
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

      {/* assignee avatars */}
      {sorted.length > 0 && (
        <div className="flex items-center gap-1.5">
          {/* unassigned */}
          <button
            type="button"
            title="Unassigned"
            onClick={() => toggleAssignee('unassigned')}
            className={[
              'flex h-7 w-7 items-center justify-center rounded-full border-2 transition-all',
              assigneeId === 'unassigned'
                ? 'border-brand-500 bg-brand-50 text-brand-600'
                : 'border-gray-200 bg-gray-100 text-gray-400 hover:border-gray-300',
            ].join(' ')}
          >
            <UserIcon className="h-3.5 w-3.5" />
          </button>

          {sorted.map((m) => {
            const selected = assigneeId === m.userId
            return (
              <button
                key={m.userId}
                type="button"
                title={m.displayName}
                onClick={() => toggleAssignee(m.userId)}
                className={[
                  'flex h-7 w-7 shrink-0 items-center justify-center rounded-full border-2 text-[10px] font-semibold transition-all',
                  selected
                    ? 'border-brand-500 bg-brand-500 text-white'
                    : 'border-transparent bg-brand-100 text-brand-700 hover:border-brand-300',
                ].join(' ')}
              >
                {initials(m.displayName)}
              </button>
            )
          })}
        </div>
      )}

      {/* divider */}
      {sorted.length > 0 && <div className="h-5 w-px bg-gray-200" />}

      {/* priority chips */}
      <div className="flex items-center gap-1">
        {PRIORITIES.map((p) => {
          const active = priorities.includes(p)
          return (
            <button
              key={p}
              type="button"
              onClick={() => togglePriority(p)}
              className={[
                'h-7 rounded-full border px-3 text-xs font-medium transition-colors',
                active
                  ? PRIORITY_BADGE[p] + ' border-transparent'
                  : 'border-gray-200 text-gray-500 hover:border-gray-300 hover:bg-gray-50',
              ].join(' ')}
            >
              {p}
            </button>
          )
        })}
      </div>

      {/* clear */}
      {hasActiveFilters && (
        <button
          type="button"
          onClick={onClear}
          className="flex h-7 items-center gap-1 rounded-md px-2 text-xs text-gray-400 hover:bg-gray-100 hover:text-gray-700"
        >
          <XIcon className="h-3 w-3" />
          Clear
        </button>
      )}
    </div>
  )
}
