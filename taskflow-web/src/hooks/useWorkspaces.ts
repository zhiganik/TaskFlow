import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { workspacesApi } from '../api/workspaces.api'
import type { CreateWorkspaceRequest, UpdateWorkspaceRequest } from '../types/api.types'

const WORKSPACES_KEY = ['workspaces']

export const useWorkspaces = () =>
  useQuery({
    queryKey: WORKSPACES_KEY,
    queryFn: workspacesApi.list,
  })

export const useCreateWorkspace = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateWorkspaceRequest) => workspacesApi.create(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: WORKSPACES_KEY }),
  })
}

export const useUpdateWorkspace = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateWorkspaceRequest }) =>
      workspacesApi.update(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: WORKSPACES_KEY }),
  })
}

export const useDeleteWorkspace = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => workspacesApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: WORKSPACES_KEY }),
  })
}
