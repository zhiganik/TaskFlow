import { apiClient } from './client'
import type { InviteMemberRequest, MemberDto, UpdateMemberRoleRequest } from '../types/api.types'

export const membersApi = {
  list: (workspaceId: string) =>
    apiClient.get<MemberDto[]>(`/workspaces/${workspaceId}/members`).then((r) => r.data),

  add: (workspaceId: string, data: InviteMemberRequest) =>
    apiClient.post<MemberDto>(`/workspaces/${workspaceId}/members`, data).then((r) => r.data),

  updateRole: (workspaceId: string, userId: string, data: UpdateMemberRoleRequest) =>
    apiClient.put<MemberDto>(`/workspaces/${workspaceId}/members/${userId}`, data).then((r) => r.data),

  remove: (workspaceId: string, userId: string) =>
    apiClient.delete<void>(`/workspaces/${workspaceId}/members/${userId}`).then((r) => r.data),
}
