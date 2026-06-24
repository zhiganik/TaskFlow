import { forwardRef, type SelectHTMLAttributes } from 'react'

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label: string
  error?: string
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ label, error, id, className = '', children, ...props }, ref) => {
    const selectId = id ?? props.name

    return (
      <div>
        <label htmlFor={selectId} className="mb-1.5 block text-sm font-medium text-gray-700">
          {label}
        </label>
        <select
          {...props}
          id={selectId}
          ref={ref}
          aria-invalid={!!error}
          className={`block w-full rounded-md border bg-white px-3 py-2.5 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-brand-500/30 ${
            error ? 'border-red-400 focus:border-red-400' : 'border-gray-200 focus:border-brand-500'
          } ${className}`}
        >
          {children}
        </select>
        {error && <p className="mt-1.5 text-xs text-[#a32d2d]">{error}</p>}
      </div>
    )
  },
)

Select.displayName = 'Select'
