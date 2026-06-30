import { useEffect, type MouseEvent, type ReactNode } from 'react'

interface ModalProps {
  title: string
  onClose: () => void
  children: ReactNode
}

export function Modal({ title, onClose, children }: ModalProps) {
  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  const stopPropagation = (e: MouseEvent) => e.stopPropagation()

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/30 sm:items-center sm:px-4"
      onClick={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className="w-full max-h-[90vh] overflow-y-auto rounded-t-2xl border border-gray-200 bg-white p-5 shadow-lg sm:max-w-sm sm:rounded-lg"
        onClick={stopPropagation}
      >
        <h2 className="mb-4 text-base font-semibold text-gray-900">{title}</h2>
        {children}
      </div>
    </div>
  )
}
