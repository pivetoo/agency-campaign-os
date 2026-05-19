import { useEffect, useMemo, useRef, useState } from 'react'
import type { ChangeEvent, KeyboardEvent } from 'react'

export interface MentionableUser {
  userId: number
  name: string
  email: string
}

interface CommentInputWithMentionsProps {
  value: string
  onChange: (value: string) => void
  onMentionsChange: (userIds: number[]) => void
  mentionedUserIds: number[]
  users: MentionableUser[]
  placeholder?: string
  rows?: number
  disabled?: boolean
}

interface MentionState {
  open: boolean
  query: string
  startIndex: number
  selectedIndex: number
}

const initialMentionState: MentionState = { open: false, query: '', startIndex: -1, selectedIndex: 0 }

export default function CommentInputWithMentions({ value, onChange, onMentionsChange, mentionedUserIds, users, placeholder, rows = 3, disabled }: CommentInputWithMentionsProps) {
  const textareaRef = useRef<HTMLTextAreaElement | null>(null)
  const [mention, setMention] = useState<MentionState>(initialMentionState)

  const filtered = useMemo(() => {
    if (!mention.open) return []
    const term = mention.query.toLowerCase().trim()
    const base = term ? users.filter((u) => u.name.toLowerCase().includes(term) || u.email.toLowerCase().includes(term)) : users
    return base.slice(0, 8)
  }, [mention.open, mention.query, users])

  useEffect(() => {
    if (mention.open && mention.selectedIndex >= filtered.length) {
      setMention((prev) => ({ ...prev, selectedIndex: 0 }))
    }
  }, [mention.open, mention.selectedIndex, filtered.length])

  const closeMention = () => setMention(initialMentionState)

  const detectMention = (text: string, caret: number) => {
    const before = text.slice(0, caret)
    const atIndex = before.lastIndexOf('@')
    if (atIndex === -1) {
      closeMention()
      return
    }
    const charBeforeAt = atIndex === 0 ? ' ' : before[atIndex - 1]
    if (!/\s/.test(charBeforeAt)) {
      closeMention()
      return
    }
    const query = before.slice(atIndex + 1)
    if (/\s/.test(query)) {
      closeMention()
      return
    }
    setMention({ open: true, query, startIndex: atIndex, selectedIndex: 0 })
  }

  const handleChange = (event: ChangeEvent<HTMLTextAreaElement>) => {
    const next = event.target.value
    onChange(next)
    detectMention(next, event.target.selectionStart ?? next.length)
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (!mention.open || filtered.length === 0) return

    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setMention((prev) => ({ ...prev, selectedIndex: (prev.selectedIndex + 1) % filtered.length }))
      return
    }
    if (event.key === 'ArrowUp') {
      event.preventDefault()
      setMention((prev) => ({ ...prev, selectedIndex: (prev.selectedIndex - 1 + filtered.length) % filtered.length }))
      return
    }
    if (event.key === 'Enter' || event.key === 'Tab') {
      event.preventDefault()
      insertMention(filtered[mention.selectedIndex])
      return
    }
    if (event.key === 'Escape') {
      event.preventDefault()
      closeMention()
    }
  }

  const insertMention = (user: MentionableUser) => {
    if (mention.startIndex === -1) return

    const caret = textareaRef.current?.selectionStart ?? value.length
    const before = value.slice(0, mention.startIndex)
    const after = value.slice(caret)
    const insertion = `@${user.name} `
    const next = before + insertion + after

    onChange(next)
    if (!mentionedUserIds.includes(user.userId)) {
      onMentionsChange([...mentionedUserIds, user.userId])
    }
    closeMention()

    requestAnimationFrame(() => {
      const node = textareaRef.current
      if (!node) return
      const newCaret = before.length + insertion.length
      node.focus()
      node.setSelectionRange(newCaret, newCaret)
    })
  }

  return (
    <div className="relative">
      <textarea
        ref={textareaRef}
        value={value}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        onBlur={() => window.setTimeout(closeMention, 120)}
        placeholder={placeholder}
        rows={rows}
        disabled={disabled}
        className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      />
      {mention.open && filtered.length > 0 && (
        <ul className="absolute left-0 right-0 top-full z-20 mt-1 max-h-60 overflow-auto rounded-md border bg-popover shadow-md">
          {filtered.map((user, index) => (
            <li
              key={user.userId}
              onMouseDown={(e) => { e.preventDefault(); insertMention(user) }}
              className={`flex cursor-pointer flex-col gap-0.5 px-3 py-1.5 text-sm ${index === mention.selectedIndex ? 'bg-accent text-accent-foreground' : 'hover:bg-accent/50'}`}
            >
              <span className="font-medium">{user.name}</span>
              <span className="text-xs text-muted-foreground">{user.email}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
