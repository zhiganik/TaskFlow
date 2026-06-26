import { useRef, useState } from 'react'
import type { MemberDto } from '../../../types/api.types'
import { MentionAutocomplete } from './MentionAutocomplete'

interface Props {
  members: MemberDto[]
  initialValue?: string
  placeholder?: string
  submitLabel?: string
  isSubmitting?: boolean
  onSubmit: (content: string) => void
  onCancel?: () => void
}

export function CommentInput({
  members,
  initialValue = '',
  placeholder = 'Write a comment… (Markdown supported, @ to mention)',
  submitLabel = 'Comment',
  isSubmitting,
  onSubmit,
  onCancel,
}: Props) {
  const [value, setValue] = useState(initialValue)
  const [mentionStart, setMentionStart] = useState<number | null>(null)
  const [mentionFilter, setMentionFilter] = useState('')
  const [mentionActive, setMentionActive] = useState(0)
  const textareaRef = useRef<HTMLTextAreaElement>(null)

  const showMentions = mentionStart !== null && members.length > 0

  const filteredMembers = members.filter((m) =>
    m.displayName.toLowerCase().includes(mentionFilter.toLowerCase()),
  )

  const closeMention = () => {
    setMentionStart(null)
    setMentionFilter('')
    setMentionActive(0)
  }

  const handleChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const text = e.target.value
    const cursor = e.target.selectionStart ?? text.length
    setValue(text)

    const before = text.slice(0, cursor)
    const match = before.match(/(^|[\s\n])@(\S*)$/)
    if (match) {
      const atPos = before.lastIndexOf('@')
      setMentionStart(atPos)
      setMentionFilter(match[2] ?? '')
      setMentionActive(0)
    } else {
      closeMention()
    }
  }

  const insertMention = (member: MemberDto) => {
    if (mentionStart === null) return
    const cursor = textareaRef.current?.selectionStart ?? value.length
    const before = value.slice(0, mentionStart)
    const after = value.slice(cursor)
    const inserted = `@[${member.displayName}](${member.userId}) `
    const next = before + inserted + after
    setValue(next)
    closeMention()

    requestAnimationFrame(() => {
      const ta = textareaRef.current
      if (!ta) return
      const pos = before.length + inserted.length
      ta.focus()
      ta.setSelectionRange(pos, pos)
    })
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (showMentions && filteredMembers.length > 0) {
      if (e.key === 'ArrowDown') { e.preventDefault(); setMentionActive((i) => (i + 1) % filteredMembers.length); return }
      if (e.key === 'ArrowUp')   { e.preventDefault(); setMentionActive((i) => (i - 1 + filteredMembers.length) % filteredMembers.length); return }
      if (e.key === 'Enter' || e.key === 'Tab') {
        e.preventDefault()
        const m = filteredMembers[mentionActive % filteredMembers.length]
        if (m) insertMention(m)
        return
      }
      if (e.key === 'Escape') { closeMention(); return }
    }

    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      e.preventDefault()
      handleSubmit()
    }
  }

  const handleSubmit = () => {
    const trimmed = value.trim()
    if (!trimmed) return
    onSubmit(trimmed)
    if (!initialValue) setValue('')
  }

  return (
    <div className="relative">
      {showMentions && (
        <MentionAutocomplete
          members={filteredMembers}
          filter={mentionFilter}
          activeIndex={mentionActive}
          onSelect={insertMention}
        />
      )}
      <textarea
        ref={textareaRef}
        value={value}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        placeholder={placeholder}
        rows={3}
        className="block w-full resize-y rounded-md border border-gray-200 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30"
      />
      <div className="mt-1.5 flex items-center justify-end gap-1.5">
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            className="rounded px-2.5 py-1 text-xs text-gray-500 hover:bg-gray-100"
          >
            Cancel
          </button>
        )}
        <button
          type="button"
          disabled={!value.trim() || isSubmitting}
          onClick={handleSubmit}
          className="rounded bg-brand-500 px-3 py-1 text-xs font-medium text-white hover:bg-brand-600 disabled:opacity-50"
        >
          {isSubmitting ? 'Saving…' : submitLabel}
        </button>
      </div>
    </div>
  )
}
