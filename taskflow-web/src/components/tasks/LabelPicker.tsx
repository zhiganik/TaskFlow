import { useRef, useState, useEffect } from 'react'
import { useLabels } from '../../hooks/useLabels'
import { TagIcon, XIcon } from '../ui/Icons'

interface Props {
  workspaceId: string
  selectedIds: string[]
  onChange: (ids: string[]) => void
  disabled?: boolean
}

export function LabelPicker({ workspaceId, selectedIds, onChange, disabled }: Props) {
  const { data: labels = [] } = useLabels(workspaceId)
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  const toggle = (id: string) => {
    onChange(selectedIds.includes(id) ? selectedIds.filter((x) => x !== id) : [...selectedIds, id])
  }

  const selectedLabels = labels.filter((l) => selectedIds.includes(l.id))

  return (
    <div ref={ref} className="relative">
      {/* selected chips */}
      {selectedLabels.length > 0 && (
        <div className="mb-1.5 flex flex-wrap gap-1">
          {selectedLabels.map((l) => (
            <span
              key={l.id}
              className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium text-white"
              style={{ backgroundColor: l.color }}
            >
              {l.name}
              {!disabled && (
                <button
                  type="button"
                  onClick={() => toggle(l.id)}
                  className="opacity-70 hover:opacity-100"
                >
                  <XIcon className="h-2.5 w-2.5" />
                </button>
              )}
            </span>
          ))}
        </div>
      )}

      {!disabled && (
        <button
          type="button"
          onClick={() => setOpen((o) => !o)}
          className="flex items-center gap-1.5 rounded-md border border-gray-200 px-2.5 py-1.5 text-xs text-gray-500 hover:border-gray-300 hover:bg-gray-50"
        >
          <TagIcon className="h-3.5 w-3.5" />
          {selectedLabels.length === 0 ? 'Add labels' : 'Edit labels'}
        </button>
      )}

      {open && (
        <div className="absolute left-0 top-full z-50 mt-1 w-48 rounded-lg border border-gray-200 bg-white py-1 shadow-lg">
          {labels.length === 0 ? (
            <p className="px-3 py-2 text-xs text-gray-400">No labels in this workspace</p>
          ) : (
            labels.map((l) => {
              const checked = selectedIds.includes(l.id)
              return (
                <button
                  key={l.id}
                  type="button"
                  onClick={() => toggle(l.id)}
                  className="flex w-full items-center gap-2 px-3 py-1.5 text-left text-sm hover:bg-gray-50"
                >
                  <span
                    className="h-3 w-3 shrink-0 rounded-full"
                    style={{ backgroundColor: l.color }}
                  />
                  <span className="flex-1 text-xs text-gray-700">{l.name}</span>
                  {checked && (
                    <span className="text-brand-600 text-xs font-bold">✓</span>
                  )}
                </button>
              )
            })
          )}
        </div>
      )}
    </div>
  )
}
