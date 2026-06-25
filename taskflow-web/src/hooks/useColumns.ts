import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { columnsApi } from '../api/columns.api'
import type { CreateColumnRequest, UpdateColumnRequest, ReorderColumnsRequest } from '../types/api.types'

const columnsKey = (workspaceId: string) => ['columns', workspaceId]

export const useColumns = (workspaceId: string) =>
  useQuery({
    queryKey: columnsKey(workspaceId),
    queryFn: () => columnsApi.list(workspaceId),
    enabled: !!workspaceId,
  })

export const useCreateColumn = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateColumnRequest) => columnsApi.create(workspaceId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: columnsKey(workspaceId) }),
  })
}

export const useUpdateColumn = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ columnId, data }: { columnId: string; data: UpdateColumnRequest }) =>
      columnsApi.update(workspaceId, columnId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: columnsKey(workspaceId) }),
  })
}

export const useReorderColumns = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: ReorderColumnsRequest) => columnsApi.reorder(workspaceId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: columnsKey(workspaceId) }),
  })
}

export const useDeleteColumn = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (columnId: string) => columnsApi.remove(workspaceId, columnId),
    onSuccess: () => qc.invalidateQueries({ queryKey: columnsKey(workspaceId) }),
  })
}
