import { useInfiniteQuery } from '@tanstack/react-query'
import { tasksApi } from '../api/tasks.api'
import type { TaskFilterParams } from '../types/api.types'

export const columnTasksKey = (workspaceId: string, columnId: string, filter: TaskFilterParams) =>
  ['tasks', workspaceId, 'column', columnId, filter]

export const useColumnTasks = (workspaceId: string, columnId: string, filter: TaskFilterParams) =>
  useInfiniteQuery({
    queryKey: columnTasksKey(workspaceId, columnId, filter),
    queryFn: ({ pageParam }) => tasksApi.listByColumn(workspaceId, columnId, filter, pageParam),
    initialPageParam: null as string | null,
    getNextPageParam: (page) => (page.hasMore ? page.nextCursor : undefined),
    enabled: !!workspaceId && !!columnId,
  })
