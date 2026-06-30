import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getErrorMessage } from '../api/errors'
import { Sidebar } from '../components/layout/Sidebar'
import { InviteMemberModal } from '../components/members/InviteMemberModal'
import { RemoveMemberDialog } from '../components/members/RemoveMemberDialog'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { MenuIcon, TrashIcon } from '../components/ui/Icons'
import { useMobileSidebar } from '../store/mobileSidebarStore'
import { Spinner } from '../components/ui/Spinner'
import { UserAvatar } from '../components/ui/UserAvatar'
import { useCancelInvitation, useInvitations } from '../hooks/useInvitations'
import { useMembers, useUpdateMemberRole } from '../hooks/useMembers'
import { useWorkspaces } from '../hooks/useWorkspaces'
import type { MemberDto } from '../types/api.types'

export function MembersPage() {
  const { workspaceId: param } = useParams<{ workspaceId: string }>()
  const workspaceId = param ?? ''

  const { data: workspaces, isLoading: isLoadingWorkspace } = useWorkspaces()
  const workspace = workspaces?.find((w) => w.id === workspaceId) ?? null
  const canManage = workspace?.myRole === 'Owner' || workspace?.myRole === 'Admin'

  const { data: members, isLoading: isLoadingMembers } = useMembers(workspaceId)
  const { data: invitations, isLoading: isLoadingInvitations } = useInvitations(workspaceId)
  const updateRoleMutation = useUpdateMemberRole(workspaceId)
  const cancelInvitationMutation = useCancelInvitation(workspaceId)

  const [inviteOpen, setInviteOpen] = useState(false)
  const [removeTarget, setRemoveTarget] = useState<MemberDto | null>(null)
  const [roleError, setRoleError] = useState<string | null>(null)
  const { toggle: toggleSidebar } = useMobileSidebar()

  const handleRoleChange = (member: MemberDto, role: 'Admin' | 'Member') => {
    setRoleError(null)
    updateRoleMutation.mutate(
      { userId: member.userId, data: { role } },
      { onError: (err) => setRoleError(getErrorMessage(err)) },
    )
  }

  return (
    <div className="flex h-screen overflow-hidden bg-gray-50">
      <Sidebar />

      <main className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <header className="flex shrink-0 items-center justify-between border-b border-gray-200 bg-white px-3 py-3 md:px-6 md:py-4">
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={toggleSidebar}
              className="rounded-md p-2 text-gray-500 hover:bg-gray-100 md:hidden"
              aria-label="Open menu"
            >
              <MenuIcon className="h-5 w-5" />
            </button>
            <div>
              <h1 className="text-lg font-semibold text-gray-900 md:text-xl">Members</h1>
              {workspace && <p className="mt-0.5 text-sm text-gray-500">{workspace.name}</p>}
            </div>
          </div>
          {canManage && (
            <Button type="button" onClick={() => setInviteOpen(true)}>
              Invite member
            </Button>
          )}
        </header>

        <div className="flex-1 overflow-y-auto px-3 py-4 space-y-6 md:px-6 md:py-5">
          {!isLoadingWorkspace && !workspace ? (
            <div className="flex justify-center py-10 text-center">
              <div>
                <h1 className="text-lg font-semibold text-gray-900">Workspace not found</h1>
                <p className="mt-1 text-sm text-gray-500">
                  <Link to="/" className="font-medium text-brand-700 hover:underline">
                    Go to your workspaces
                  </Link>
                </p>
              </div>
            </div>
          ) : (
            <>
              {roleError && <Alert variant="error">{roleError}</Alert>}

              {isLoadingMembers ? (
                <div className="flex justify-center py-10">
                  <Spinner className="h-5 w-5 border-gray-300 border-t-brand-500" />
                </div>
              ) : (
                <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
                  {members?.map((member) => {
                    const isOwner = member.role === 'Owner'
                    const isUpdatingThisRow =
                      updateRoleMutation.isPending &&
                      updateRoleMutation.variables?.userId === member.userId

                    return (
                      <div
                        key={member.userId}
                        className="flex items-center gap-3 border-b border-gray-100 px-4 py-3 last:border-b-0"
                      >
                        <UserAvatar
                          displayName={member.displayName}
                          avatarColor={member.avatarColor}
                          avatarPath={member.avatarPath}
                          avatarStatus={member.avatarStatus}
                          size="md"
                        />

                        <div className="min-w-0 flex-1">
                          <div className="truncate text-sm font-medium text-gray-900">
                            {member.displayName}
                          </div>
                          <div className="truncate text-xs text-gray-500">{member.email}</div>
                        </div>

                        <div className="hidden text-xs text-gray-400 sm:block">
                          Joined {new Date(member.joinedAt).toLocaleDateString()}
                        </div>

                        {isOwner ? (
                          <span className="text-xs font-medium text-gray-500">Owner</span>
                        ) : canManage ? (
                          <select
                            value={member.role}
                            disabled={isUpdatingThisRow}
                            onChange={(e) =>
                              handleRoleChange(member, e.target.value as 'Admin' | 'Member')
                            }
                            className="rounded-md border border-gray-200 bg-white px-2 py-1 text-xs text-gray-700 focus:outline-none focus:ring-2 focus:ring-brand-500/30 disabled:opacity-60"
                          >
                            <option value="Admin">Admin</option>
                            <option value="Member">Member</option>
                          </select>
                        ) : (
                          <span className="text-xs text-gray-500">{member.role}</span>
                        )}

                        {!isOwner && canManage && (
                          <button
                            type="button"
                            aria-label={`Remove ${member.displayName}`}
                            onClick={() => setRemoveTarget(member)}
                            className="rounded p-1.5 text-gray-400 hover:bg-red-50 hover:text-red-600"
                          >
                            <TrashIcon className="h-4 w-4" />
                          </button>
                        )}
                      </div>
                    )
                  })}

                  {members?.length === 0 && (
                    <p className="px-4 py-6 text-center text-sm text-gray-400">No members yet.</p>
                  )}
                </div>
              )}

              {canManage && (
                <div>
                  <h2 className="mb-2 text-sm font-medium text-gray-700">Pending invitations</h2>
                  {isLoadingInvitations ? (
                    <div className="flex justify-center py-6">
                      <Spinner className="h-4 w-4 border-gray-300 border-t-brand-500" />
                    </div>
                  ) : invitations && invitations.length > 0 ? (
                    <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
                      {invitations.map((inv) => (
                        <div
                          key={inv.id}
                          className="flex items-center gap-3 border-b border-gray-100 px-4 py-3 last:border-b-0"
                        >
                          <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-gray-100 text-xs font-medium text-gray-500">
                            {inv.email[0]?.toUpperCase() ?? '?'}
                          </div>

                          <div className="min-w-0 flex-1">
                            <div className="truncate text-sm text-gray-900">{inv.email}</div>
                            <div className="text-xs text-gray-400">
                              Invited as {inv.role} · expires{' '}
                              {new Date(inv.expiresAt).toLocaleDateString()}
                            </div>
                          </div>

                          <span className="rounded-full bg-yellow-50 px-2 py-0.5 text-xs font-medium text-yellow-700">
                            Pending
                          </span>

                          <button
                            type="button"
                            aria-label={`Cancel invitation for ${inv.email}`}
                            disabled={
                              cancelInvitationMutation.isPending &&
                              cancelInvitationMutation.variables === inv.id
                            }
                            onClick={() => cancelInvitationMutation.mutate(inv.id)}
                            className="rounded p-1.5 text-gray-400 hover:bg-red-50 hover:text-red-600 disabled:opacity-50"
                          >
                            <TrashIcon className="h-4 w-4" />
                          </button>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="text-sm text-gray-400">No pending invitations.</p>
                  )}
                </div>
              )}
            </>
          )}
        </div>
      </main>

      {inviteOpen && (
        <InviteMemberModal workspaceId={workspaceId} onClose={() => setInviteOpen(false)} />
      )}

      {removeTarget && (
        <RemoveMemberDialog
          workspaceId={workspaceId}
          member={removeTarget}
          onClose={() => setRemoveTarget(null)}
        />
      )}
    </div>
  )
}
