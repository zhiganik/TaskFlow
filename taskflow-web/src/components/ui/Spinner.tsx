interface SpinnerProps {
  className?: string
}

export function Spinner({ className = 'h-4 w-4 border-white/40 border-t-white' }: SpinnerProps) {
  return <div className={`animate-spin rounded-full border-2 ${className}`} />
}
