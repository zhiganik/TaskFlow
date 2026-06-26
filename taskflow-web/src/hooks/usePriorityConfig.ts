import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { priorityConfigApi } from '../api/priorityConfig.api'
import type { TaskPriority, UpdatePriorityConfigRequest } from '../types/api.types'

const priorityConfigKey = (workspaceId: string) => ['priorityConfigs', workspaceId]

export const usePriorityConfig = (workspaceId: string) =>
  useQuery({
    queryKey: priorityConfigKey(workspaceId),
    queryFn: () => priorityConfigApi.list(workspaceId),
    enabled: !!workspaceId,
    staleTime: 5 * 60 * 1000,
  })

export const useUpdatePriorityConfig = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ priority, data }: { priority: TaskPriority; data: UpdatePriorityConfigRequest }) =>
      priorityConfigApi.update(workspaceId, priority, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: priorityConfigKey(workspaceId) }),
  })
}
