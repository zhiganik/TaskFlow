import { useRef, useState } from 'react'
import type { MemberDto } from '../../../types/api.types'
import { MentionAutocomplete } from './MentionAutocomplete'

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

interface Props {
  members: MemberDto[]
  initialValue?: string
  placeholder?: string
  submitLabel?: string
  isSubmitting?: boolean
  onSubmit: (content: string, stagedFiles: File[]) => void
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
  const [stagedFiles, setStagedFiles] = useState<File[]>([])
  const textareaRef = useRef<HTMLTextAreaElement>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

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

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files ?? [])
    if (files.length === 0) return
    setStagedFiles((prev) => [...prev, ...files])
    e.target.value = ''
  }

  const removeStaged = (index: number) => {
    setStagedFiles((prev) => prev.filter((_, i) => i !== index))
  }

  const handleSubmit = () => {
    const trimmed = value.trim()
    if (!trimmed && stagedFiles.length === 0) return
    onSubmit(trimmed, stagedFiles)
    if (!initialValue) {
      setValue('')
      setStagedFiles([])
    }
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

      {/* staged file chips */}
      {stagedFiles.length > 0 && (
        <div className="mt-1.5 flex flex-wrap gap-1.5">
          {stagedFiles.map((file, i) => (
            <div
              key={i}
              className="flex items-center gap-1.5 rounded-md border border-gray-200 bg-gray-50 px-2 py-1 text-[11px] text-gray-700"
            >
              <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" className="h-3 w-3 text-gray-400">
                <path d="M4 2h6l4 4v8a1 1 0 01-1 1H4a1 1 0 01-1-1V3a1 1 0 011-1z" />
                <polyline points="10 2 10 6 14 6" />
              </svg>
              <span className="max-w-[140px] truncate">{file.name}</span>
              <span className="text-gray-400">{formatBytes(file.size)}</span>
              <button
                type="button"
                onClick={() => removeStaged(i)}
                className="ml-0.5 flex h-3.5 w-3.5 items-center justify-center rounded-full text-gray-400 hover:bg-gray-200 hover:text-gray-600"
              >
                <svg viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="h-2.5 w-2.5">
                  <line x1="2" y1="2" x2="10" y2="10" />
                  <line x1="10" y1="2" x2="2" y2="10" />
                </svg>
              </button>
            </div>
          ))}
        </div>
      )}

      <div className="mt-1.5 flex items-center gap-1.5">
        {/* paperclip */}
        <button
          type="button"
          onClick={() => fileInputRef.current?.click()}
          className="flex items-center justify-center rounded p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
          title="Attach files"
        >
          <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" className="h-4 w-4">
            <path d="M17.5 11.5l-7.5 7.5a5 5 0 01-7-7l8-8a3 3 0 014 4l-8 8a1 1 0 01-1.5-1.5l7.5-7.5" />
          </svg>
        </button>
        <input
          ref={fileInputRef}
          type="file"
          multiple
          className="hidden"
          onChange={handleFileChange}
        />

        <div className="ml-auto flex items-center gap-1.5">
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
            disabled={(!value.trim() && stagedFiles.length === 0) || isSubmitting}
            onClick={handleSubmit}
            className="rounded bg-brand-500 px-3 py-1 text-xs font-medium text-white hover:bg-brand-600 disabled:opacity-50"
          >
            {isSubmitting ? 'Saving…' : submitLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
