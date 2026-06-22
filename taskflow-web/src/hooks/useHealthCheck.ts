import { useMutation } from '@tanstack/react-query'
import { useRef, useState } from 'react'
import { healthApi } from '../api/health.api'

export const useHealthCheck = () => {
  const startedAt = useRef(0)
  const [latencyMs, setLatencyMs] = useState<number | null>(null)

  const mutation = useMutation({
    mutationFn: healthApi.check,
    onMutate: () => {
      console.log('[debug] onMutate')
      startedAt.current = performance.now()
    },
    onError: (e) => {
      console.log('[debug] onError', e)
    },
    onSuccess: (d) => {
      console.log('[debug] onSuccess', d)
    },
    onSettled: () => {
      console.log('[debug] onSettled')
      setLatencyMs(Math.round(performance.now() - startedAt.current))
    },
  })

  console.log('[debug] render', { status: mutation.status, isPending: mutation.isPending, error: mutation.error, data: mutation.data })

  return { ...mutation, latencyMs }
}
