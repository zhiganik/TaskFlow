import { useEffect, useRef } from 'react'
import type { MemberDto } from '../../../types/api.types'

interface Props {
  members: MemberDto[]
  filter: string
  activeIndex: number
  onSelect: (member: MemberDto) => void
}

export function MentionAutocomplete({ members, filter, activeIndex, onSelect }: Props) {
  const filtered = members.filter((m) =>
    m.displayName.toLowerCase().includes(filter.toLowerCase()),
  )

  const listRef = useRef<HTMLUListElement>(null)

  useEffect(() => {
    const el = listRef.current?.children[activeIndex] as HTMLElement | undefined
    el?.scrollIntoView({ block: 'nearest' })
  }, [activeIndex])

  if (filtered.length === 0) return null

  return (
    <ul
      ref={listRef}
      className="absolute bottom-full mb-1 left-0 z-50 max-h-48 w-56 overflow-y-auto rounded-lg border border-gray-200 bg-white py-1 shadow-lg"
    >
      {filtered.map((m, i) => (
        <li key={m.userId}>
          <button
            type="button"
            onMouseDown={(e) => { e.preventDefault(); onSelect(m) }}
            className={[
              'flex w-full items-center gap-2 px-3 py-1.5 text-left text-sm',
              i === activeIndex % filtered.length
                ? 'bg-brand-50 text-brand-700'
                : 'text-gray-700 hover:bg-gray-50',
            ].join(' ')}
          >
            <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-brand-100 text-[9px] font-semibold text-brand-700">
              {m.displayName.slice(0, 2).toUpperCase()}
            </span>
            <span className="min-w-0 flex-1 truncate">{m.displayName}</span>
          </button>
        </li>
      ))}
    </ul>
  )
}
