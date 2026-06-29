import { useParams } from 'react-router-dom'
import { Sidebar } from '../components/layout/Sidebar'
import { DoneColumnSettings } from '../components/settings/DoneColumnSettings'
import { LabelsSettings } from '../components/settings/LabelsSettings'
import { PrioritySettings } from '../components/settings/PrioritySettings'
import { useWorkspaces } from '../hooks/useWorkspaces'

export function SettingsPage() {
  const { workspaceId } = useParams<{ workspaceId: string }>()
  const wsId = workspaceId ?? ''

  const { data: workspaces } = useWorkspaces()
  const workspace = workspaces?.find((w) => w.id === wsId) ?? null
  const canManage = workspace?.myRole === 'Owner' || workspace?.myRole === 'Admin'

  return (
    <div className="flex h-screen overflow-hidden bg-gray-50">
      <Sidebar />

      <main className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <header className="shrink-0 border-b border-gray-200 bg-white px-6 py-4">
          <h1 className="text-xl font-semibold text-gray-900">Workspace Settings</h1>
          {workspace && (
            <p className="mt-0.5 text-sm text-gray-500">{workspace.name}</p>
          )}
        </header>

        <div className="flex-1 overflow-y-auto px-6 py-6">
          <div className="mx-auto max-w-2xl space-y-8">
            <section className="rounded-xl border border-gray-200 bg-white p-5">
              <LabelsSettings workspaceId={wsId} canManage={canManage} />
            </section>

            <section className="rounded-xl border border-gray-200 bg-white p-5">
              <PrioritySettings workspaceId={wsId} canManage={canManage} />
            </section>

            <section className="rounded-xl border border-gray-200 bg-white p-5">
              <DoneColumnSettings workspaceId={wsId} canManage={canManage} />
            </section>

          </div>
        </div>
      </main>
    </div>
  )
}
