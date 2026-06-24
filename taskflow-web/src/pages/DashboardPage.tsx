import { useEffect } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Sidebar } from '../components/layout/Sidebar'
import { useWorkspaces } from '../hooks/useWorkspaces'
import { setLastWorkspaceId } from '../lib/lastWorkspace'

export function DashboardPage() {
  const { workspaceId } = useParams<{ workspaceId: string }>()
  const { data: workspaces, isLoading } = useWorkspaces()
  const selected = workspaces?.find((w) => w.id === workspaceId) ?? null

  useEffect(() => {
    if (workspaceId) setLastWorkspaceId(workspaceId)
  }, [workspaceId])

  return (
    <div className="flex min-h-screen bg-gray-50">
      <Sidebar />

      <main className="flex flex-1 flex-col">
        <header className="border-b border-gray-200 bg-white px-6 py-3">
          <span className="text-sm font-medium text-gray-900">
            {isLoading ? 'Loading…' : selected ? selected.name : 'Workspace not found'}
          </span>
        </header>

        <div className="flex flex-1 items-center justify-center px-4">
          {!isLoading && !selected ? (
            <div className="text-center">
              <h1 className="text-lg font-semibold text-gray-900">Workspace not found</h1>
              <p className="mt-1 text-sm text-gray-500">
                <Link to="/" className="font-medium text-brand-700 hover:underline">
                  Go to your workspaces
                </Link>
              </p>
            </div>
          ) : (
            <div className="text-center">
              <h1 className="text-lg font-semibold text-gray-900">You&apos;re logged in</h1>
              <p className="mt-1 text-sm text-gray-500">Projects and tasks are coming soon.</p>
            </div>
          )}
        </div>
      </main>
    </div>
  )
}
