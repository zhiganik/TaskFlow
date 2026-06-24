import { useEffect, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useWorkspaces } from '../../hooks/useWorkspaces'
import type { WorkspaceDto } from '../../types/api.types'
import { ChevronDownIcon, PencilIcon, PlusIcon, TrashIcon } from '../ui/Icons'
import { Spinner } from '../ui/Spinner'
import { DeleteWorkspaceDialog } from './DeleteWorkspaceDialog'
import { WorkspaceFormModal } from './WorkspaceFormModal'

type DialogState =
  | { type: 'create' }
  | { type: 'edit'; workspace: WorkspaceDto }
  | { type: 'delete'; workspace: WorkspaceDto }
  | null

export function WorkspaceSwitcher() {
  const { data: workspaces, isLoading } = useWorkspaces()
  const { workspaceId } = useParams<{ workspaceId: string }>()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [dialog, setDialog] = useState<DialogState>(null)
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    const onClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', onClickOutside)
    return () => document.removeEventListener('mousedown', onClickOutside)
  }, [open])

  const selected = workspaces?.find((w) => w.id === workspaceId) ?? null

  return (
    <div ref={containerRef} className="relative px-2">
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className="flex w-full items-center gap-2 rounded-md border border-gray-200 bg-gray-50 px-2.5 py-2 text-left hover:bg-gray-100"
      >
        <div className="min-w-0 flex-1">
          {isLoading ? (
            <Spinner className="h-3.5 w-3.5 border-gray-300 border-t-gray-600" />
          ) : (
            <>
              <div className="truncate text-xs font-medium text-gray-900">
                {selected?.name ?? 'No workspace'}
              </div>
              {selected && <div className="text-[11px] text-gray-400">{selected.myRole}</div>}
            </>
          )}
        </div>
        <ChevronDownIcon
          className={`h-3.5 w-3.5 shrink-0 text-gray-400 transition-transform ${open ? 'rotate-180' : ''}`}
        />
      </button>

      {open && (
        <div className="absolute left-2 right-2 top-full z-40 mt-1 rounded-md border border-gray-200 bg-white py-1 shadow-lg">
          <div className="max-h-56 overflow-y-auto px-1">
            {workspaces?.length === 0 && (
              <p className="px-2 py-2 text-xs text-gray-400">No workspaces yet.</p>
            )}
            {workspaces?.map((workspace) => (
              <div
                key={workspace.id}
                className={`group flex items-center gap-1 rounded-md px-2 py-1.5 ${
                  workspace.id === workspaceId ? 'bg-brand-50' : 'hover:bg-gray-50'
                }`}
              >
                <button
                  type="button"
                  onClick={() => {
                    navigate(`/workspaces/${workspace.id}`)
                    setOpen(false)
                  }}
                  className={`min-w-0 flex-1 truncate text-left text-xs font-medium ${
                    workspace.id === workspaceId ? 'text-brand-700' : 'text-gray-700'
                  }`}
                >
                  {workspace.name}
                </button>
                {workspace.myRole === 'Owner' && (
                  <>
                    <button
                      type="button"
                      aria-label={`Rename ${workspace.name}`}
                      onClick={() => {
                        setOpen(false)
                        setDialog({ type: 'edit', workspace })
                      }}
                      className="rounded p-1 text-gray-400 opacity-0 hover:bg-gray-200 hover:text-gray-700 group-hover:opacity-100"
                    >
                      <PencilIcon className="h-3.5 w-3.5" />
                    </button>
                    <button
                      type="button"
                      aria-label={`Delete ${workspace.name}`}
                      onClick={() => {
                        setOpen(false)
                        setDialog({ type: 'delete', workspace })
                      }}
                      className="rounded p-1 text-gray-400 opacity-0 hover:bg-red-50 hover:text-red-600 group-hover:opacity-100"
                    >
                      <TrashIcon className="h-3.5 w-3.5" />
                    </button>
                  </>
                )}
              </div>
            ))}
          </div>

          <div className="mt-1 border-t border-gray-100 px-1 pt-1">
            <button
              type="button"
              onClick={() => {
                setOpen(false)
                setDialog({ type: 'create' })
              }}
              className="flex w-full items-center gap-1.5 rounded-md px-2 py-1.5 text-left text-xs font-medium text-brand-700 hover:bg-brand-50"
            >
              <PlusIcon className="h-3.5 w-3.5" /> New workspace
            </button>
          </div>
        </div>
      )}

      {(dialog?.type === 'create' || dialog?.type === 'edit') && (
        <WorkspaceFormModal
          workspace={dialog.type === 'edit' ? dialog.workspace : undefined}
          onClose={() => setDialog(null)}
          onSaved={(workspace) => {
            navigate(`/workspaces/${workspace.id}`)
            setDialog(null)
          }}
        />
      )}

      {dialog?.type === 'delete' && (
        <DeleteWorkspaceDialog workspace={dialog.workspace} onClose={() => setDialog(null)} />
      )}
    </div>
  )
}
