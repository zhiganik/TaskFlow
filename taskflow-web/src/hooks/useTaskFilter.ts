import { useEffect, useMemo, useState } from 'react'
import type { TaskFilterParams, TaskPriority } from '../types/api.types'

export function useTaskFilter() {
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [assigneeIds, setAssigneeIds] = useState<string[]>([])
  const [priorities, setPriorities] = useState<TaskPriority[]>([])
  const [labelIds, setLabelIds] = useState<string[]>([])

  useEffect(() => {
    const t = setTimeout(() => setSearch(searchInput), 300)
    return () => clearTimeout(t)
  }, [searchInput])

  const toggleAssigneeId = (id: string) =>
    setAssigneeIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id],
    )

  const toggleLabelId = (id: string) =>
    setLabelIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id],
    )

  const filter: TaskFilterParams = useMemo(
    () => ({
      search: search || undefined,
      assigneeIds: assigneeIds.length ? assigneeIds : undefined,
      priorities: priorities.length ? priorities : undefined,
      labelIds: labelIds.length ? labelIds : undefined,
    }),
    [search, assigneeIds, priorities, labelIds],
  )

  const hasActiveFilters =
    !!searchInput || assigneeIds.length > 0 || priorities.length > 0 || labelIds.length > 0

  const clearAll = () => {
    setSearchInput('')
    setSearch('')
    setAssigneeIds([])
    setPriorities([])
    setLabelIds([])
  }

  return {
    searchInput, setSearchInput,
    assigneeIds, toggleAssigneeId,
    priorities, setPriorities,
    labelIds, toggleLabelId,
    filter, hasActiveFilters, clearAll,
  }
}
