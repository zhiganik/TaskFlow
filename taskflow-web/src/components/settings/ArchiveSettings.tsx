import { useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { useUpdateArchiveSettings } from '../../hooks/useWorkspaces'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'

interface Props {
  workspaceId: string
  canManage: boolean
  currentDays: number
}

export function ArchiveSettings({ workspaceId, canManage, currentDays }: Props) {
  const [days, setDays] = useState(currentDays)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const mutation = useUpdateArchiveSettings()

  const handleSave = () => {
    setError(null)
    setSaved(false)
    mutation.mutate(
      { id: workspaceId, data: { archiveAfterDays: days } },
      {
        onSuccess: () => setSaved(true),
        onError: (err) => setError(getErrorMessage(err)),
      },
    )
  }

  return (
    <div>
      <h2 className="mb-1 text-base font-semibold text-gray-900">Archive Settings</h2>
      <p className="mb-4 text-sm text-gray-500">
        Tasks in the Done column are automatically closed after the configured number of days.
      </p>

      {error && <Alert variant="error">{error}</Alert>}
      {saved && <Alert variant="success">Archive settings saved.</Alert>}

      <div className="flex items-center gap-3">
        <label className="text-sm text-gray-700 shrink-0" htmlFor="archive-days">
          Auto-close after
        </label>
        <input
          id="archive-days"
          type="number"
          min={1}
          max={365}
          value={days}
          disabled={!canManage || mutation.isPending}
          onChange={(e) => { setSaved(false); setDays(Number(e.target.value)) }}
          className="w-24 rounded-md border border-gray-200 px-3 py-1.5 text-sm text-gray-900 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30 disabled:opacity-60"
        />
        <span className="text-sm text-gray-500">day(s)</span>

        {canManage && (
          <Button
            type="button"
            onClick={handleSave}
            loading={mutation.isPending}
            disabled={days === currentDays}
          >
            Save
          </Button>
        )}
      </div>
    </div>
  )
}
