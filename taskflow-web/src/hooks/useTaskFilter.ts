import { useEffect, useMemo, useState } from 'react'
import type { TaskFilterParams, TaskPriority } from '../types/api.types'

export function useTaskFilter() {
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [assigneeId, setAssigneeId] = useState<string | undefined>()
  const [priorities, setPriorities] = useState<TaskPriority[]>([])

  useEffect(() => {
    const t = setTimeout(() => setSearch(searchInput), 300)
    return () => clearTimeout(t)
  }, [searchInput])

  const filter: TaskFilterParams = useMemo(
    () => ({
      search: search || undefined,
      assigneeId,
      priorities: priorities.length ? priorities : undefined,
    }),
    [search, assigneeId, priorities],
  )

  const hasActiveFilters = !!searchInput || !!assigneeId || priorities.length > 0

  const clearAll = () => {
    setSearchInput('')
    setSearch('')
    setAssigneeId(undefined)
    setPriorities([])
  }

  return {
    searchInput, setSearchInput,
    assigneeId, setAssigneeId,
    priorities, setPriorities,
    filter, hasActiveFilters, clearAll,
  }
}
