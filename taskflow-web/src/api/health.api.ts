import { apiClient } from './client'
import type { HealthDto } from '../types/api.types'

export const healthApi = {
  check: () => apiClient.get<HealthDto>('/health').then((r) => r.data),
}
