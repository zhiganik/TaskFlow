import { useEffect, useRef } from 'react'
import { useHealthCheck } from '../hooks/useHealthCheck'

type CheckState = 'checking' | 'healthy' | 'error'

const STATE_COPY: Record<CheckState, { title: string; subtitle: string; band: string; ring: string }> = {
  checking: {
    title: 'Checking status…',
    subtitle: 'Contacting the API',
    band: 'bg-gray-600 dark:bg-gray-700',
    ring: 'bg-white/15',
  },
  healthy: {
    title: 'All systems operational',
    subtitle: 'The API responded successfully',
    band: 'bg-emerald-600',
    ring: 'bg-white/15',
  },
  error: {
    title: 'Service unreachable',
    subtitle: 'The API did not respond',
    band: 'bg-red-600',
    ring: 'bg-white/15',
  },
}

function StatusIcon({ state }: { state: CheckState }) {
  if (state === 'checking') {
    return <div className="h-9 w-9 animate-spin rounded-full border-[3px] border-white/30 border-t-white" />
  }

  if (state === 'healthy') {
    return (
      <svg viewBox="0 0 24 24" fill="none" className="h-9 w-9 text-white">
        <path
          d="M5 13l4 4L19 7"
          stroke="currentColor"
          strokeWidth={2.5}
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    )
  }

  return (
    <svg viewBox="0 0 24 24" fill="none" className="h-9 w-9 text-white">
      <path d="M6 6l12 12M18 6L6 18" stroke="currentColor" strokeWidth={2.5} strokeLinecap="round" />
    </svg>
  )
}

function RefreshIcon({ spinning }: { spinning: boolean }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      className={`h-4 w-4 ${spinning ? 'animate-spin' : ''}`}
    >
      <path
        d="M4 4v5h5M20 20v-5h-5M19.5 9A8 8 0 0 0 5.6 6.6M4.5 15a8 8 0 0 0 13.9 2.4"
        stroke="currentColor"
        strokeWidth={2}
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

function DetailCell({ label, value }: { label: string; value: string }) {
  return (
    <div className="px-5 py-4">
      <dt className="text-xs font-medium tracking-wide text-gray-400 uppercase dark:text-gray-500">{label}</dt>
      <dd className="mt-1 truncate text-sm font-semibold text-gray-900 dark:text-gray-100">{value}</dd>
    </div>
  )
}

export function HealthCheckPage() {
  const { mutate, data, error, isPending, latencyMs } = useHealthCheck()
  const hasCheckedRef = useRef(false)

  useEffect(() => {
    if (hasCheckedRef.current) return
    hasCheckedRef.current = true
    mutate()
  }, [mutate])

  const state: CheckState = isPending ? 'checking' : error ? 'error' : data ? 'healthy' : 'checking'
  const copy = STATE_COPY[state]

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-b from-gray-50 to-gray-100 px-4 py-12 dark:from-gray-950 dark:to-gray-900">
      <div className="w-full max-w-md">
        <div className="mb-6 flex items-center justify-center gap-2">
          <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-blue-600 text-sm font-bold text-white">
            T
          </div>
          <span className="text-lg font-semibold text-gray-900 dark:text-gray-100">TaskFlow</span>
        </div>

        <div className="overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-xl dark:border-gray-800 dark:bg-gray-900">
          <div className={`flex flex-col items-center gap-3 px-6 py-8 text-center transition-colors ${copy.band}`}>
            <div className={`flex h-16 w-16 items-center justify-center rounded-full ${copy.ring}`}>
              <StatusIcon state={state} />
            </div>
            <div>
              <h1 className="flex items-center justify-center gap-2 text-xl font-semibold text-white">
                {state === 'healthy' && (
                  <span className="relative flex h-2.5 w-2.5">
                    <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-white/60" />
                    <span className="relative inline-flex h-2.5 w-2.5 rounded-full bg-white" />
                  </span>
                )}
                {copy.title}
              </h1>
              <p className="mt-1 text-sm text-white/80">{copy.subtitle}</p>
            </div>
          </div>

          <dl className="grid grid-cols-2 divide-x divide-y divide-gray-100 dark:divide-gray-800">
            <DetailCell label="Endpoint" value="GET /api/v1/health" />
            <DetailCell label="Status" value={data?.status ?? '—'} />
            <DetailCell label="Response time" value={latencyMs != null ? `${latencyMs} ms` : '—'} />
            <DetailCell
              label="Last checked"
              value={data ? new Date(data.checkedAt).toLocaleTimeString() : '—'}
            />
          </dl>

          {error && (
            <div className="mx-6 mb-6 rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-400">
              Could not reach the TaskFlow API. Make sure the backend is running and try again.
            </div>
          )}

          <div className="border-t border-gray-100 px-6 py-4 dark:border-gray-800">
            <button
              type="button"
              onClick={() => mutate()}
              disabled={isPending}
              className="flex w-full items-center justify-center gap-2 rounded-md bg-gray-900 px-4 py-2.5 text-sm font-medium text-white transition-colors hover:bg-gray-700 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-gray-100 dark:text-gray-900 dark:hover:bg-gray-300"
            >
              <RefreshIcon spinning={isPending} />
              {isPending ? 'Checking…' : 'Refresh'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
