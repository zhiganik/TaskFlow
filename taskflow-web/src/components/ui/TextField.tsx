import { forwardRef, type InputHTMLAttributes } from 'react'

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string
  error?: string
}

export const TextField = forwardRef<HTMLInputElement, TextFieldProps>(
  ({ label, error, id, className = '', ...props }, ref) => {
    const inputId = id ?? props.name

    return (
      <div>
        <label htmlFor={inputId} className="mb-1.5 block text-sm font-medium text-gray-700">
          {label}
        </label>
        <input
          {...props}
          id={inputId}
          ref={ref}
          aria-invalid={!!error}
          className={`block w-full rounded-md border px-3 py-2.5 text-sm text-gray-900 placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-brand-500/30 ${
            error ? 'border-red-400 focus:border-red-400' : 'border-gray-200 focus:border-brand-500'
          } ${className}`}
        />
        {error && <p className="mt-1.5 text-xs text-[#a32d2d]">{error}</p>}
      </div>
    )
  },
)

TextField.displayName = 'TextField'
