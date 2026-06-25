import type { TaskPriority } from '../types/api.types'

export const PRIORITY_BADGE: Record<TaskPriority, string> = {
  Low: 'bg-green-100 text-green-800',
  Medium: 'bg-amber-100 text-amber-800',
  High: 'bg-orange-100 text-orange-800',
}

export const COLOR_PALETTE = [
  '#6366F1',
  '#F59E0B',
  '#10B981',
  '#EF4444',
  '#3B82F6',
  '#8B5CF6',
  '#F97316',
  '#06B6D4',
  '#84CC16',
  '#EC4899',
] as const
