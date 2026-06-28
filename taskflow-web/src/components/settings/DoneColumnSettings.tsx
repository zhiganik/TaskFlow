import { useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { useColumns, useUpdateColumn } from '../../hooks/useColumns'
import { Alert } from '../ui/Alert'

interface Props {
  workspaceId: string
  canManage: boolean
}

export function DoneColumnSettings({ workspaceId, canManage }: Props) {
  const { data: columns = [] } = useColumns(workspaceId)
  const updateMutation = useUpdateColumn(workspaceId)
  const [error, setError] = useState<string | null>(null)

  const sorted = [...columns].sort((a, b) => a.order - b.order)
  const doneColumn = sorted.find((c) => c.isDoneColumn)

  const handleChange = (columnId: string) => {
    setError(null)

    if (columnId === '') {
      if (!doneColumn) return
      updateMutation.mutate(
        { columnId: doneColumn.id, data: { name: doneColumn.name, color: doneColumn.color, isDoneColumn: false } },
        { onError: (err) => setError(getErrorMessage(err)) },
      )
      return
    }

    const col = sorted.find((c) => c.id === columnId)
    if (!col) return
    updateMutation.mutate(
      { columnId: col.id, data: { name: col.name, color: col.color, isDoneColumn: true } },
      { onError: (err) => setError(getErrorMessage(err)) },
    )
  }

  return (
    <div>
      <h2 className="mb-1 text-base font-semibold text-gray-900">Done Column</h2>
      <p className="mb-4 text-sm text-gray-500">
        Tasks moved into this column are marked as Done and will be auto-closed after the
        configured number of days.
      </p>

      {error && <Alert variant="error">{error}</Alert>}

      <div className="flex items-center gap-3">
        <select
          disabled={!canManage || updateMutation.isPending || sorted.length === 0}
          value={doneColumn?.id ?? ''}
          onChange={(e) => handleChange(e.target.value)}
          className="rounded-md border border-gray-200 px-3 py-1.5 text-sm text-gray-900 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30 disabled:opacity-60"
        >
          <option value="">None</option>
          {sorted.map((col) => (
            <option key={col.id} value={col.id}>
              {col.name}
            </option>
          ))}
        </select>

        {updateMutation.isPending && (
          <span className="text-xs text-gray-400">Saving…</span>
        )}
      </div>
    </div>
  )
}
