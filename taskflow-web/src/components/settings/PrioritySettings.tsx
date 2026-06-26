import { useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { usePriorityConfig, useUpdatePriorityConfig } from '../../hooks/usePriorityConfig'
import type { PriorityConfigDto, TaskPriority } from '../../types/api.types'
import { COLOR_PALETTE } from '../../lib/priority'
import { PencilIcon, XIcon } from '../ui/Icons'

interface Props {
  workspaceId: string
  canManage: boolean
}

function ColorSwatch({ color, selected, onClick }: { color: string; selected: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={[
        'h-6 w-6 rounded-full transition-all',
        selected ? 'ring-2 ring-offset-1 ring-gray-700' : 'hover:scale-110',
      ].join(' ')}
      style={{ backgroundColor: color }}
    />
  )
}

function PriorityRow({
  config,
  canManage,
  onSave,
  isSaving,
}: {
  config: PriorityConfigDto
  canManage: boolean
  onSave: (displayName: string, color: string) => void
  isSaving: boolean
}) {
  const [editing, setEditing] = useState(false)
  const [displayName, setDisplayName] = useState(config.displayName)
  const [color, setColor] = useState(config.color)

  const save = () => {
    if (displayName.trim()) {
      onSave(displayName.trim(), color)
      setEditing(false)
    }
  }

  if (!editing) {
    return (
      <div className="flex items-center gap-3 rounded-lg border border-gray-100 bg-white px-3 py-2.5">
        <span
          className="inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium text-white"
          style={{ backgroundColor: config.color }}
        >
          {config.displayName}
        </span>
        <span className="text-xs text-gray-400">{config.priority}</span>
        {canManage && (
          <button
            type="button"
            onClick={() => { setDisplayName(config.displayName); setColor(config.color); setEditing(true) }}
            className="ml-auto rounded p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
          >
            <PencilIcon className="h-3.5 w-3.5" />
          </button>
        )}
      </div>
    )
  }

  return (
    <div className="rounded-lg border border-gray-200 bg-gray-50 p-3 space-y-2">
      <input
        autoFocus
        value={displayName}
        onChange={(e) => setDisplayName(e.target.value)}
        placeholder="Display name"
        className="block w-full rounded-md border border-gray-200 px-2.5 py-1.5 text-sm text-gray-900 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30"
      />
      <div className="flex flex-wrap gap-2">
        {COLOR_PALETTE.map((c) => (
          <ColorSwatch key={c} color={c} selected={color === c} onClick={() => setColor(c)} />
        ))}
      </div>
      <div className="flex items-center gap-2 pt-1">
        <span
          className="inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium text-white"
          style={{ backgroundColor: color }}
        >
          {displayName || 'Preview'}
        </span>
        <div className="ml-auto flex gap-1.5">
          <button
            type="button"
            onClick={() => setEditing(false)}
            className="rounded px-2 py-1 text-xs text-gray-500 hover:bg-gray-200"
          >
            Cancel
          </button>
          <button
            type="button"
            disabled={!displayName.trim() || isSaving}
            onClick={save}
            className="rounded bg-brand-500 px-2.5 py-1 text-xs font-medium text-white hover:bg-brand-600 disabled:opacity-50"
          >
            Save
          </button>
        </div>
      </div>
    </div>
  )
}

const PRIORITY_ORDER: TaskPriority[] = ['High', 'Medium', 'Low']

export function PrioritySettings({ workspaceId, canManage }: Props) {
  const { data: configs = [] } = usePriorityConfig(workspaceId)
  const updateMutation = useUpdatePriorityConfig(workspaceId)
  const [error, setError] = useState<string | null>(null)

  const sorted = PRIORITY_ORDER.map((p) => configs.find((c) => c.priority === p)).filter(Boolean) as PriorityConfigDto[]

  const handleSave = (priority: TaskPriority, displayName: string, color: string) => {
    setError(null)
    updateMutation.mutate(
      { priority, data: { displayName, color } },
      { onError: (e) => setError(getErrorMessage(e)) },
    )
  }

  return (
    <div className="space-y-3">
      <div>
        <h3 className="text-sm font-semibold text-gray-900">Priority Display</h3>
        <p className="mt-0.5 text-xs text-gray-500">Customize how each priority level is labeled and colored</p>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-md bg-red-50 px-3 py-2 text-xs text-red-700">
          {error}
          <button type="button" onClick={() => setError(null)} className="ml-auto">
            <XIcon className="h-3 w-3" />
          </button>
        </div>
      )}

      <div className="space-y-1">
        {sorted.map((config) => (
          <PriorityRow
            key={config.priority}
            config={config}
            canManage={canManage}
            onSave={(name, color) => handleSave(config.priority, name, color)}
            isSaving={updateMutation.isPending}
          />
        ))}
        {sorted.length === 0 && (
          <p className="py-4 text-center text-xs text-gray-400">Loading…</p>
        )}
      </div>
    </div>
  )
}
