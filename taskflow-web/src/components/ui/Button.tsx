import type { ButtonHTMLAttributes } from 'react'
import { Spinner } from './Spinner'

type ButtonVariant = 'primary' | 'secondary' | 'danger'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  loading?: boolean
}

const VARIANT_CLASSES: Record<ButtonVariant, string> = {
  primary: 'border-brand-500 bg-brand-500 text-white hover:border-brand-600 hover:bg-brand-600',
  secondary: 'border-gray-200 bg-white text-gray-900 hover:bg-gray-50',
  danger: 'border-red-600 bg-red-600 text-white hover:border-red-700 hover:bg-red-700',
}

const SPINNER_CLASSES: Record<ButtonVariant, string> = {
  primary: 'h-4 w-4 border-white/40 border-t-white',
  secondary: 'h-4 w-4 border-gray-300 border-t-gray-600',
  danger: 'h-4 w-4 border-white/40 border-t-white',
}

export function Button({
  variant = 'primary',
  loading = false,
  disabled,
  className = '',
  children,
  ...props
}: ButtonProps) {
  return (
    <button
      {...props}
      disabled={disabled || loading}
      className={`inline-flex items-center justify-center gap-2 rounded-md border px-4 py-2.5 text-sm font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-60 ${VARIANT_CLASSES[variant]} ${className}`}
    >
      {loading && <Spinner className={SPINNER_CLASSES[variant]} />}
      {children}
    </button>
  )
}
