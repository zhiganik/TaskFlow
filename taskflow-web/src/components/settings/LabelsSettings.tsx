import { useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { useCreateLabel, useDeleteLabel, useLabels, useUpdateLabel } from '../../hooks/useLabels'
import type { LabelDto } from '../../types/api.types'
import { COLOR_PALETTE } from '../../lib/priority'
import { PencilIcon, PlusIcon, TrashIcon, XIcon } from '../ui/Icons'

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

function LabelForm({
  initial,
  onSave,
  onCancel,
  isSaving,
}: {
  initial: { name: string; color: string }
  onSave: (name: string, color: string) => void
  onCancel: () => void
  isSaving: boolean
}) {
  const [name, setName] = useState(initial.name)
  const [color, setColor] = useState(initial.color)

  return (
    <div className="rounded-lg border border-gray-200 bg-gray-50 p-3 space-y-2">
      <input
        autoFocus
        value={name}
        onChange={(e) => setName(e.target.value)}
        placeholder="Label name"
        className="block w-full rounded-md border border-gray-200 px-2.5 py-1.5 text-sm text-gray-900 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30"
      />
      <div className="flex flex-wrap gap-2">
        {COLOR_PALETTE.map((c) => (
          <ColorSwatch key={c} color={c} selected={color === c} onClick={() => setColor(c)} />
        ))}
      </div>
      <div className="flex gap-2 pt-1">
        <span
          className="inline-flex items-center rounded-full px-3 py-0.5 text-xs font-medium text-white"
          style={{ backgroundColor: color }}
        >
          {name || 'Preview'}
        </span>
        <div className="ml-auto flex gap-1.5">
          <button
            type="button"
            onClick={onCancel}
            className="rounded px-2 py-1 text-xs text-gray-500 hover:bg-gray-200"
          >
            Cancel
          </button>
          <button
            type="button"
            disabled={!name.trim() || isSaving}
            onClick={() => onSave(name.trim(), color)}
            className="rounded bg-brand-500 px-2.5 py-1 text-xs font-medium text-white hover:bg-brand-600 disabled:opacity-50"
          >
            Save
          </button>
        </div>
      </div>
    </div>
  )
}

export function LabelsSettings({ workspaceId, canManage }: Props) {
  const { data: labels = [] } = useLabels(workspaceId)
  const createMutation = useCreateLabel(workspaceId)
  const updateMutation = useUpdateLabel(workspaceId)
  const deleteMutation = useDeleteLabel(workspaceId)

  const [creating, setCreating] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const handleCreate = (name: string, color: string) => {
    setError(null)
    createMutation.mutate(
      { name, color },
      {
        onSuccess: () => setCreating(false),
        onError: (e) => setError(getErrorMessage(e)),
      },
    )
  }

  const handleUpdate = (label: LabelDto, name: string, color: string) => {
    setError(null)
    updateMutation.mutate(
      { labelId: label.id, data: { name, color } },
      {
        onSuccess: () => setEditingId(null),
        onError: (e) => setError(getErrorMessage(e)),
      },
    )
  }

  const handleDelete = (labelId: string) => {
    setError(null)
    deleteMutation.mutate(labelId, {
      onSuccess: () => setDeleteConfirmId(null),
      onError: (e) => setError(getErrorMessage(e)),
    })
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-sm font-semibold text-gray-900">Labels</h3>
          <p className="mt-0.5 text-xs text-gray-500">Colored tags for categorizing tasks</p>
        </div>
        {canManage && !creating && (
          <button
            type="button"
            onClick={() => setCreating(true)}
            className="flex items-center gap-1 rounded-md bg-brand-500 px-2.5 py-1.5 text-xs font-medium text-white hover:bg-brand-600"
          >
            <PlusIcon className="h-3.5 w-3.5" />
            New label
          </button>
        )}
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-md bg-red-50 px-3 py-2 text-xs text-red-700">
          {error}
          <button type="button" onClick={() => setError(null)} className="ml-auto">
            <XIcon className="h-3 w-3" />
          </button>
        </div>
      )}

      {creating && canManage && (
        <LabelForm
          initial={{ name: '', color: COLOR_PALETTE[0] }}
          onSave={handleCreate}
          onCancel={() => setCreating(false)}
          isSaving={createMutation.isPending}
        />
      )}

      <div className="space-y-1">
        {labels.map((label) =>
          editingId === label.id ? (
            <LabelForm
              key={label.id}
              initial={{ name: label.name, color: label.color }}
              onSave={(name, color) => handleUpdate(label, name, color)}
              onCancel={() => setEditingId(null)}
              isSaving={updateMutation.isPending}
            />
          ) : (
            <div
              key={label.id}
              className="flex items-center gap-2.5 rounded-lg border border-gray-100 bg-white px-3 py-2"
            >
              <span
                className="h-3 w-3 shrink-0 rounded-full"
                style={{ backgroundColor: label.color }}
              />
              <span
                className="inline-flex rounded-full px-2 py-0.5 text-xs font-medium text-white"
                style={{ backgroundColor: label.color }}
              >
                {label.name}
              </span>
              {canManage && (
                <div className="ml-auto flex items-center gap-1">
                  {deleteConfirmId === label.id ? (
                    <>
                      <span className="text-xs text-gray-500">Delete?</span>
                      <button
                        type="button"
                        onClick={() => handleDelete(label.id)}
                        disabled={deleteMutation.isPending}
                        className="rounded px-2 py-0.5 text-xs font-medium text-red-600 hover:bg-red-50 disabled:opacity-50"
                      >
                        Yes
                      </button>
                      <button
                        type="button"
                        onClick={() => setDeleteConfirmId(null)}
                        className="rounded px-2 py-0.5 text-xs text-gray-500 hover:bg-gray-100"
                      >
                        No
                      </button>
                    </>
                  ) : (
                    <>
                      <button
                        type="button"
                        onClick={() => setEditingId(label.id)}
                        className="rounded p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
                      >
                        <PencilIcon className="h-3.5 w-3.5" />
                      </button>
                      <button
                        type="button"
                        onClick={() => setDeleteConfirmId(label.id)}
                        className="rounded p-1 text-gray-400 hover:bg-red-50 hover:text-red-500"
                      >
                        <TrashIcon className="h-3.5 w-3.5" />
                      </button>
                    </>
                  )}
                </div>
              )}
            </div>
          ),
        )}
        {labels.length === 0 && !creating && (
          <p className="py-4 text-center text-xs text-gray-400">
            No labels yet. {canManage ? 'Create one to get started.' : ''}
          </p>
        )}
      </div>
    </div>
  )
}
