import { Navigate } from 'react-router-dom'
import { Sidebar } from '../components/layout/Sidebar'
import { Spinner } from '../components/ui/Spinner'
import { useWorkspaces } from '../hooks/useWorkspaces'
import { getLastWorkspaceId } from '../lib/lastWorkspace'

export function WorkspaceRedirectPage() {
  const { data: workspaces, isLoading } = useWorkspaces()

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-50">
        <Spinner className="h-6 w-6 border-gray-300 border-t-brand-500" />
      </div>
    )
  }

  const lastId = getLastWorkspaceId()
  const target = workspaces?.find((w) => w.id === lastId) ?? workspaces?.[0]

  if (target) return <Navigate to={`/workspaces/${target.id}`} replace />

  return (
    <div className="flex min-h-screen bg-gray-50">
      <Sidebar />
      <main className="flex flex-1 items-center justify-center px-4">
        <div className="text-center">
          <h1 className="text-lg font-semibold text-gray-900">No workspaces yet</h1>
          <p className="mt-1 text-sm text-gray-500">Create one from the sidebar to get started.</p>
        </div>
      </main>
    </div>
  )
}
