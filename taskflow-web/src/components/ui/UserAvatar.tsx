import type { AvatarStatus } from '../../types/api.types'

const API_BASE = `${import.meta.env.VITE_API_URL ?? ''}/api/v1`

interface Props {
  displayName: string
  avatarColor: string
  avatarPath?: string | null
  avatarStatus?: AvatarStatus | null
  size?: 'xs' | 'sm' | 'md' | 'lg'
  className?: string
}

const SIZE = {
  xs: 'h-5 w-5 text-[9px]',
  sm: 'h-6 w-6 text-[10px]',
  md: 'h-8 w-8 text-xs',
  lg: 'h-12 w-12 text-sm',
}

function initials(name: string) {
  return name
    .split(' ')
    .map((p) => p[0])
    .filter(Boolean)
    .slice(0, 2)
    .join('')
    .toUpperCase()
}

export function UserAvatar({ displayName, avatarColor, avatarPath, avatarStatus, size = 'sm', className = '' }: Props) {
  const sizeClass = SIZE[size]
  const base = `shrink-0 rounded-full ${sizeClass} ${className}`

  const imgUrl = (avatarStatus === 'Ready' && avatarPath)
    ? `${API_BASE}/me/avatar/${avatarPath}`
    : null

  if (imgUrl) {
    return (
      <img
        src={imgUrl}
        alt={displayName}
        className={`${base} object-cover`}
      />
    )
  }

  if (avatarStatus === 'Pending') {
    return (
      <span
        className={`${base} flex items-center justify-center font-semibold text-white`}
        style={{ backgroundColor: avatarColor }}
      >
        <svg className="h-3 w-3 animate-spin" viewBox="0 0 24 24" fill="none">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
        </svg>
      </span>
    )
  }

  return (
    <span
      className={`${base} flex items-center justify-center font-semibold text-white`}
      style={{ backgroundColor: avatarColor }}
    >
      {initials(displayName)}
    </span>
  )
}
