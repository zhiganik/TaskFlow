import type { ReactNode } from 'react'

type AlertVariant = 'error' | 'success'

interface AlertProps {
  variant?: AlertVariant
  children: ReactNode
}

const VARIANT_CLASSES: Record<AlertVariant, string> = {
  error: 'border-[#f5d5d5] bg-[#fcebeb] text-[#a32d2d]',
  success: 'border-[#d9e8c4] bg-[#eaf3de] text-[#3b6d11]',
}

export function Alert({ variant = 'error', children }: AlertProps) {
  return (
    <div
      role={variant === 'error' ? 'alert' : 'status'}
      className={`rounded-md border px-3.5 py-2.5 text-sm ${VARIANT_CLASSES[variant]}`}
    >
      {children}
    </div>
  )
}
