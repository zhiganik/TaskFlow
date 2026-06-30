import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { membersApi } from '../api/members.api'
import type { UpdateMemberRoleRequest } from '../types/api.types'

export const membersKey = (workspaceId: string) => ['members', workspaceId]

export const useMembers = (workspaceId: string) =>
  useQuery({
    queryKey: membersKey(workspaceId),
    queryFn: () => membersApi.list(workspaceId),
  })

export const useUpdateMemberRole = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ userId, data }: { userId: string; data: UpdateMemberRoleRequest }) =>
      membersApi.updateRole(workspaceId, userId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: membersKey(workspaceId) }),
  })
}

export const useRemoveMember = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (userId: string) => membersApi.remove(workspaceId, userId),
    onSuccess: () => qc.invalidateQueries({ queryKey: membersKey(workspaceId) }),
  })
}
