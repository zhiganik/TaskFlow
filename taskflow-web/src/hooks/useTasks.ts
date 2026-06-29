import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { tasksApi } from '../api/tasks.api'
import type { CreateTaskRequest, MoveTaskRequest, TaskFilterParams, UpdateTaskRequest } from '../types/api.types'

const closedTasksKey = (workspaceId: string, filter?: TaskFilterParams) =>
  ['closed-tasks', workspaceId, filter ?? {}]

const tasksKey = (workspaceId: string, filter?: TaskFilterParams) =>
  ['tasks', workspaceId, filter ?? {}]

export const useTask = (workspaceId: string, taskId: string | null) =>
  useQuery({
    queryKey: ['task', workspaceId, taskId],
    queryFn: () => tasksApi.getById(workspaceId, taskId!),
    enabled: !!workspaceId && !!taskId,
  })

export const useTasks = (workspaceId: string, filter?: TaskFilterParams) =>
  useQuery({
    queryKey: tasksKey(workspaceId, filter),
    queryFn: () => tasksApi.list(workspaceId, filter),
    enabled: !!workspaceId,
    placeholderData: keepPreviousData,
  })

export const useCreateTask = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateTaskRequest) => tasksApi.create(workspaceId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tasks', workspaceId] }),
  })
}

export const useUpdateTask = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ taskId, data }: { taskId: string; data: UpdateTaskRequest }) =>
      tasksApi.update(workspaceId, taskId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tasks', workspaceId] }),
  })
}

export const useMoveTask = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ taskId, data }: { taskId: string; data: MoveTaskRequest }) =>
      tasksApi.move(workspaceId, taskId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tasks', workspaceId] }),
  })
}

export const useDeleteTask = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (taskId: string) => tasksApi.remove(workspaceId, taskId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tasks', workspaceId] })
      qc.invalidateQueries({ queryKey: ['closed-tasks', workspaceId] })
    },
  })
}

export const useClosedTasks = (workspaceId: string, filter?: TaskFilterParams) =>
  useQuery({
    queryKey: closedTasksKey(workspaceId, filter),
    queryFn: () => tasksApi.listClosed(workspaceId, filter),
    enabled: !!workspaceId,
    placeholderData: keepPreviousData,
  })

export const useCloseTask = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (taskId: string) => tasksApi.close(workspaceId, taskId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tasks', workspaceId] })
      qc.invalidateQueries({ queryKey: ['closed-tasks', workspaceId] })
    },
  })
}

export const useReopenTask = (workspaceId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (taskId: string) => tasksApi.reopen(workspaceId, taskId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tasks', workspaceId] })
      qc.invalidateQueries({ queryKey: ['closed-tasks', workspaceId] })
    },
  })
}
