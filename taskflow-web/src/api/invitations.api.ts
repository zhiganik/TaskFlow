import { apiClient } from './client'
import type {
  AcceptInvitationRequest,
  AcceptInvitationResultDto,
  CreateInvitationRequest,
  InvitationDto,
  InvitationInfoDto,
} from '../types/api.types'

export const invitationsApi = {
  create: (workspaceId: string, data: CreateInvitationRequest) =>
    apiClient
      .post<InvitationDto>(`/workspaces/${workspaceId}/invitations`, data)
      .then((r) => r.data),

  list: (workspaceId: string) =>
    apiClient
      .get<InvitationDto[]>(`/workspaces/${workspaceId}/invitations`)
      .then((r) => r.data),

  cancel: (workspaceId: string, invitationId: string) =>
    apiClient
      .delete<void>(`/workspaces/${workspaceId}/invitations/${invitationId}`)
      .then((r) => r.data),

  getInfo: (token: string) =>
    apiClient.get<InvitationInfoDto>(`/invitations/${token}`).then((r) => r.data),

  accept: (token: string, data: AcceptInvitationRequest) =>
    apiClient
      .post<AcceptInvitationResultDto>(`/invitations/${token}/accept`, data)
      .then((r) => r.data),
}
