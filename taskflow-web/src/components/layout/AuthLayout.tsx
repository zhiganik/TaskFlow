import type { ReactNode } from 'react'

interface AuthLayoutProps {
  title: string
  subtitle?: string
  children: ReactNode
  footer?: ReactNode
}

export function AuthLayout({ title, subtitle, children, footer }: AuthLayoutProps) {
  return (
    <div className="flex min-h-dvh items-center justify-center bg-gray-50 px-4 py-12">
      <div className="w-full max-w-sm">
        <div className="mb-6 flex items-center justify-center gap-2">
          <div className="flex h-7 w-7 items-center justify-center rounded-md bg-brand-500">
            <svg viewBox="0 0 16 16" className="h-3.5 w-3.5 fill-white">
              <path d="M2 3h12v2H2zM2 7h8v2H2zM2 11h10v2H2z" />
            </svg>
          </div>
          <span className="text-base font-medium text-gray-900">TaskFlow</span>
        </div>

        <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
          <div className="mb-5 text-center">
            <h1 className="text-lg font-semibold text-gray-900">{title}</h1>
            {subtitle && <p className="mt-1 text-sm text-gray-500">{subtitle}</p>}
          </div>
          {children}
        </div>

        {footer && <div className="mt-5 text-center text-sm text-gray-500">{footer}</div>}
      </div>
    </div>
  )
}
