import { Sidebar } from '../components/layout/Sidebar'
import { useWorkspaces } from '../hooks/useWorkspaces'
import { useWorkspaceStore } from '../store/workspaceStore'

export function DashboardPage() {
  const { data: workspaces } = useWorkspaces()
  const selectedWorkspaceId = useWorkspaceStore((s) => s.selectedWorkspaceId)
  const selected = workspaces?.find((w) => w.id === selectedWorkspaceId) ?? null

  return (
    <div className="flex min-h-screen bg-gray-50">
      <Sidebar />

      <main className="flex flex-1 flex-col">
        <header className="border-b border-gray-200 bg-white px-6 py-3">
          <span className="text-sm font-medium text-gray-900">
            {selected ? selected.name : 'No workspace selected'}
          </span>
        </header>

        <div className="flex flex-1 items-center justify-center px-4">
          <div className="text-center">
            <h1 className="text-lg font-semibold text-gray-900">You&apos;re logged in</h1>
            <p className="mt-1 text-sm text-gray-500">Projects and tasks are coming soon.</p>
          </div>
        </div>
      </main>
    </div>
  )
}
