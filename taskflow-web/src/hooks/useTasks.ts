import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { tasksApi } from '../api/tasks.api'
import type { CreateTaskRequest, MoveTaskRequest, UpdateTaskRequest } from '../types/api.types'

const tasksKey = (workspaceId: string) => ['tasks', workspaceId]

export const useTasks = (workspaceId: string) =>
  useQuery({
    queryKey: tasksKey(workspaceId),
    queryFn: () => tasksApi.list(workspaceId),
    enabled: !!workspaceId,
  })

export const useCreateTask = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateTaskRequest) => tasksApi.create(workspaceId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: tasksKey(workspaceId) }),
  })
}

export const useUpdateTask = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ taskId, data }: { taskId: string; data: UpdateTaskRequest }) =>
      tasksApi.update(workspaceId, taskId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: tasksKey(workspaceId) }),
  })
}

export const useMoveTask = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ taskId, data }: { taskId: string; data: MoveTaskRequest }) =>
      tasksApi.move(workspaceId, taskId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: tasksKey(workspaceId) }),
  })
}

export const useDeleteTask = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (taskId: string) => tasksApi.remove(workspaceId, taskId),
    onSuccess: () => qc.invalidateQueries({ queryKey: tasksKey(workspaceId) }),
  })
}
