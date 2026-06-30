import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { invitationsApi } from '../api/invitations.api'
import type { AcceptInvitationRequest, CreateInvitationRequest } from '../types/api.types'

export const invitationsKey = (workspaceId: string) => ['invitations', workspaceId]

export const useInvitations = (workspaceId: string) =>
  useQuery({
    queryKey: invitationsKey(workspaceId),
    queryFn: () => invitationsApi.list(workspaceId),
  })

export const useCreateInvitation = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateInvitationRequest) => invitationsApi.create(workspaceId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: invitationsKey(workspaceId) }),
  })
}

export const useCancelInvitation = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (invitationId: string) => invitationsApi.cancel(workspaceId, invitationId),
    onSuccess: () => qc.invalidateQueries({ queryKey: invitationsKey(workspaceId) }),
  })
}

export const useInvitationInfo = (token: string) =>
  useQuery({
    queryKey: ['invitation', token],
    queryFn: () => invitationsApi.getInfo(token),
    retry: false,
  })

export const useAcceptInvitation = (token: string) =>
  useMutation({
    mutationFn: (data: AcceptInvitationRequest) => invitationsApi.accept(token, data),
  })
