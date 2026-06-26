import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { labelsApi } from '../api/labels.api'
import type { CreateLabelRequest, SetTaskLabelsRequest, UpdateLabelRequest } from '../types/api.types'

const labelsKey = (workspaceId: string) => ['labels', workspaceId]

export const useLabels = (workspaceId: string) =>
  useQuery({
    queryKey: labelsKey(workspaceId),
    queryFn: () => labelsApi.list(workspaceId),
    enabled: !!workspaceId,
  })

export const useCreateLabel = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateLabelRequest) => labelsApi.create(workspaceId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: labelsKey(workspaceId) }),
  })
}

export const useUpdateLabel = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ labelId, data }: { labelId: string; data: UpdateLabelRequest }) =>
      labelsApi.update(workspaceId, labelId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: labelsKey(workspaceId) }),
  })
}

export const useDeleteLabel = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (labelId: string) => labelsApi.remove(workspaceId, labelId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: labelsKey(workspaceId) })
      qc.invalidateQueries({ queryKey: ['tasks', workspaceId] })
    },
  })
}

export const useSetTaskLabels = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ taskId, data }: { taskId: string; data: SetTaskLabelsRequest }) =>
      labelsApi.setTaskLabels(workspaceId, taskId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tasks', workspaceId] }),
  })
}
