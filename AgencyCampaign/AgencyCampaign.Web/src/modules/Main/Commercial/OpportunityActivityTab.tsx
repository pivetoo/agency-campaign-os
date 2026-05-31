import { useEffect, useMemo, useState } from 'react'
import type React from 'react'
import { Button, Card, CardContent, ConfirmModal, useApi, useI18n, UsersManagementService } from 'archon-ui'
import { ArrowRight, MessageSquare, Pencil, Send, Trash2 } from 'lucide-react'
import { opportunityService, type OpportunityComment, type OpportunityStageHistoryItem } from '../../../services/opportunityService'
import { formatDateTime } from '../../../lib/format'
import CommentInputWithMentions, { type MentionableUser } from '../../../components/comments/CommentInputWithMentions'

interface OpportunityActivityTabProps {
  opportunityId: number
  currentUserId?: number | null
}

interface TimelineItem {
  id: string
  kind: 'stage' | 'comment'
  occurredAt: string
  data: OpportunityStageHistoryItem | OpportunityComment
}

function renderBodyWithMentions(body: string, users: MentionableUser[]): Array<string | React.ReactElement> {
  const sorted = [...users].sort((a, b) => b.name.length - a.name.length)
  const parts: Array<string | React.ReactElement> = []
  let cursor = 0
  let nodeIndex = 0
  while (cursor < body.length) {
    const atIdx = body.indexOf('@', cursor)
    if (atIdx === -1) {
      parts.push(body.slice(cursor))
      break
    }
    if (atIdx > cursor) parts.push(body.slice(cursor, atIdx))
    const remaining = body.slice(atIdx + 1)
    const found = sorted.find((u) => u.name && remaining.startsWith(u.name))
    if (found) {
      parts.push(
        <span key={`m-${nodeIndex++}`} className="rounded bg-primary/15 px-1 font-medium text-primary">@{found.name}</span>,
      )
      cursor = atIdx + 1 + found.name.length
    } else {
      parts.push('@')
      cursor = atIdx + 1
    }
  }
  return parts
}

function relativeTime(value: string, t: (key: string) => string): string {
  const date = new Date(value)
  const diffMs = Date.now() - date.getTime()
  const diffMin = Math.floor(diffMs / 60000)
  if (diffMin < 1) return t('common.relative.now')
  if (diffMin < 60) return t('common.relative.minutesAgo').replace('{0}', String(diffMin))
  const diffHours = Math.floor(diffMin / 60)
  if (diffHours < 24) return t('common.relative.hoursAgo').replace('{0}', String(diffHours))
  const diffDays = Math.floor(diffHours / 24)
  if (diffDays < 30) return t('common.relative.daysAgo').replace('{0}', String(diffDays))
  return date.toLocaleDateString('pt-BR')
}

export default function OpportunityActivityTab({ opportunityId, currentUserId }: OpportunityActivityTabProps) {
  const { t } = useI18n()
  const [comments, setComments] = useState<OpportunityComment[]>([])
  const [stageHistory, setStageHistory] = useState<OpportunityStageHistoryItem[]>([])
  const [body, setBody] = useState('')
  const [mentionedUserIds, setMentionedUserIds] = useState<number[]>([])
  const [users, setUsers] = useState<MentionableUser[]>([])
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editingBody, setEditingBody] = useState('')
  const [commentToDeleteId, setCommentToDeleteId] = useState<number | null>(null)

  useEffect(() => {
    void UsersManagementService.listInCurrentContract().then((list) => {
      setUsers(list.filter((u) => u.isActive).map((u) => ({ userId: u.userId, name: u.name, email: u.email })))
    })
  }, [])

  const { execute: load, loading } = useApi<unknown>({ showErrorMessage: true })
  const { execute: runMutation, loading: mutating } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    void load(async () => {
      const [commentList, history] = await Promise.all([
        opportunityService.getComments(opportunityId),
        opportunityService.getStageHistory(opportunityId),
      ])
      setComments(commentList)
      setStageHistory(history)
      return null
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [opportunityId])

  useEffect(() => {
    const hash = window.location.hash
    if (!hash.startsWith('#comment-') || comments.length === 0) return
    const target = document.getElementById(hash.slice(1))
    if (target) {
      target.scrollIntoView({ behavior: 'smooth', block: 'center' })
      target.classList.add('ring-2', 'ring-primary')
      window.setTimeout(() => target.classList.remove('ring-2', 'ring-primary'), 2000)
    }
  }, [comments])

  const reload = async () => {
    const [commentList, history] = await Promise.all([
      opportunityService.getComments(opportunityId),
      opportunityService.getStageHistory(opportunityId),
    ])
    setComments(commentList)
    setStageHistory(history)
  }

  const timeline = useMemo<TimelineItem[]>(() => {
    const items: TimelineItem[] = [
      ...stageHistory.map((entry) => ({
        id: `stage-${entry.id}`,
        kind: 'stage' as const,
        occurredAt: entry.changedAt,
        data: entry,
      })),
      ...comments.map((entry) => ({
        id: `comment-${entry.id}`,
        kind: 'comment' as const,
        occurredAt: entry.createdAt,
        data: entry,
      })),
    ]
    return items.sort((a, b) => new Date(b.occurredAt).getTime() - new Date(a.occurredAt).getTime())
  }, [comments, stageHistory])

  const submitComment = async () => {
    const trimmed = body.trim()
    if (!trimmed) return

    const presentMentions = mentionedUserIds.filter((id) => trimmed.includes('@' + (users.find((u) => u.userId === id)?.name ?? '')))
    const result = await runMutation(() => opportunityService.createComment(opportunityId, { body: trimmed, mentionedUserIds: presentMentions }))
    if (result !== null) {
      setBody('')
      setMentionedUserIds([])
      await reload()
    }
  }

  const startEditing = (comment: OpportunityComment) => {
    setEditingId(comment.id)
    setEditingBody(comment.body)
  }

  const cancelEditing = () => {
    setEditingId(null)
    setEditingBody('')
  }

  const saveEditing = async () => {
    if (!editingId) return
    const trimmed = editingBody.trim()
    if (!trimmed) return

    const result = await runMutation(() => opportunityService.updateComment(editingId, { body: trimmed }))
    if (result !== null) {
      cancelEditing()
      await reload()
    }
  }

  const removeComment = async () => {
    if (!commentToDeleteId) return
    const result = await runMutation(() => opportunityService.deleteComment(commentToDeleteId))
    if (result !== null) {
      setCommentToDeleteId(null)
      await reload()
    }
  }

  const canEdit = (comment: OpportunityComment) =>
    !comment.isDeleted && (!comment.authorUserId || (currentUserId != null && comment.authorUserId === currentUserId))

  return (
    <>
    <ConfirmModal
      open={commentToDeleteId !== null}
      onOpenChange={(open) => { if (!open) setCommentToDeleteId(null) }}
      description={t('opportunityActivity.confirm.delete')}
      variant="danger"
      onConfirm={() => void removeComment()}
      loading={mutating}
    />
    <Card>
      <CardContent className="space-y-6 p-6">
        <div className="space-y-3">
          <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
            <MessageSquare className="h-4 w-4 text-primary" />
            {t('opportunityActivity.title')}
          </div>
          <CommentInputWithMentions
            value={body}
            onChange={setBody}
            mentionedUserIds={mentionedUserIds}
            onMentionsChange={setMentionedUserIds}
            users={users}
            placeholder={t('opportunityActivity.placeholder')}
            rows={3}
            disabled={mutating}
          />
          <div className="flex justify-end">
            <Button
              size="sm"
              icon={<Send className="h-4 w-4" />}
              onClick={() => void submitComment()}
              disabled={mutating || !body.trim()}
            >
              {t('opportunityActivity.submit')}
            </Button>
          </div>
        </div>

        <div className="space-y-4">
          <div className="text-sm font-semibold text-foreground">{t('opportunityActivity.timeline.title')}</div>

          {loading && timeline.length === 0 ? (
            <div className="text-sm text-muted-foreground">{t('opportunityActivity.loading')}</div>
          ) : timeline.length === 0 ? (
            <div className="text-sm text-muted-foreground">{t('opportunityActivity.empty')}</div>
          ) : (
            <ol className="relative ml-3 space-y-4 border-l border-border pl-6">
              {timeline.map((item) => (
                <li key={item.id} id={item.kind === 'comment' ? item.id : undefined} className="relative scroll-mt-24">
                  <span
                    className={`absolute -left-[1.6rem] top-1 flex h-4 w-4 items-center justify-center rounded-full ring-2 ring-background ${
                      item.kind === 'stage' ? 'bg-primary/15' : 'bg-emerald-500/20'
                    }`}
                  >
                    {item.kind === 'stage' ? (
                      <ArrowRight className="h-2.5 w-2.5 text-primary" />
                    ) : (
                      <MessageSquare className="h-2.5 w-2.5 text-emerald-700" />
                    )}
                  </span>

                  {item.kind === 'stage' ? (
                    <StageEvent entry={item.data as OpportunityStageHistoryItem} t={t} />
                  ) : (
                    <CommentEvent
                      comment={item.data as OpportunityComment}
                      isEditing={editingId === (item.data as OpportunityComment).id}
                      editingBody={editingBody}
                      canEdit={canEdit(item.data as OpportunityComment)}
                      mutating={mutating}
                      users={users}
                      onStartEdit={startEditing}
                      onCancelEdit={cancelEditing}
                      onSaveEdit={saveEditing}
                      onChangeEdit={setEditingBody}
                      onDelete={(id) => setCommentToDeleteId(id)}
                      t={t}
                    />
                  )}
                </li>
              ))}
            </ol>
          )}
        </div>
      </CardContent>
    </Card>
    </>
  )
}

function StageEvent({ entry, t }: { entry: OpportunityStageHistoryItem; t: (key: string) => string }) {
  return (
    <div className="space-y-1">
      <div className="flex flex-wrap items-center gap-2 text-sm">
        <span className="font-medium text-foreground">
          {entry.fromStageName ? (
            <>
              <span style={{ color: entry.fromStageColor ?? undefined }}>{entry.fromStageName}</span>
              {' → '}
              <span style={{ color: entry.toStageColor ?? undefined }}>{entry.toStageName}</span>
            </>
          ) : (
            <span style={{ color: entry.toStageColor ?? undefined }}>{entry.toStageName}</span>
          )}
        </span>
        {entry.changedByUserName ? (
          <span className="text-xs text-muted-foreground">{t('common.byUser').replace('{0}', entry.changedByUserName)}</span>
        ) : null}
        <span className="text-xs text-muted-foreground" title={formatDateTime(entry.changedAt)}>
          {relativeTime(entry.changedAt, t)}
        </span>
      </div>
      {entry.reason ? (
        <div className="text-sm text-muted-foreground">{entry.reason}</div>
      ) : null}
    </div>
  )
}

interface CommentEventProps {
  comment: OpportunityComment
  isEditing: boolean
  editingBody: string
  canEdit: boolean
  mutating: boolean
  users: MentionableUser[]
  onStartEdit: (comment: OpportunityComment) => void
  onCancelEdit: () => void
  onSaveEdit: () => void
  onChangeEdit: (value: string) => void
  onDelete: (id: number) => void
  t: (key: string) => string
}

function CommentEvent(props: CommentEventProps) {
  const { comment, isEditing, editingBody, canEdit, mutating, users, onStartEdit, onCancelEdit, onSaveEdit, onChangeEdit, onDelete, t } = props

  return (
    <div className="space-y-1">
      <div className="flex flex-wrap items-center gap-2 text-sm">
        <span className="font-medium text-foreground">{comment.authorName}</span>
        <span className="text-xs text-muted-foreground" title={formatDateTime(comment.createdAt)}>
          {relativeTime(comment.createdAt, t)}
        </span>
        {comment.updatedAt && comment.updatedAt !== comment.createdAt && !comment.isDeleted ? (
          <span className="text-xs italic text-muted-foreground">{t('opportunityActivity.editedBadge')}</span>
        ) : null}
        {canEdit && !isEditing ? (
          <span className="ml-auto flex gap-1">
            <button
              type="button"
              aria-label={t('common.action.edit')}
              title={t('common.action.edit')}
              className="inline-flex h-6 w-6 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-foreground"
              onClick={() => onStartEdit(comment)}
            >
              <Pencil className="h-3 w-3" />
            </button>
            <button
              type="button"
              aria-label={t('common.action.delete')}
              title={t('common.action.delete')}
              className="inline-flex h-6 w-6 items-center justify-center rounded-md text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
              onClick={() => onDelete(comment.id)}
            >
              <Trash2 className="h-3 w-3" />
            </button>
          </span>
        ) : null}
      </div>
      {isEditing ? (
        <div className="space-y-2">
          <textarea
            value={editingBody}
            onChange={(e) => onChangeEdit(e.target.value)}
            rows={3}
            className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            disabled={mutating}
          />
          <div className="flex gap-2">
            <Button size="sm" onClick={onSaveEdit} disabled={mutating || !editingBody.trim()}>
              {t('common.action.save')}
            </Button>
            <Button size="sm" variant="outline" onClick={onCancelEdit} disabled={mutating}>
              {t('common.action.cancel')}
            </Button>
          </div>
        </div>
      ) : comment.isDeleted ? (
        <div className="text-sm italic text-muted-foreground">{t('opportunityActivity.deletedBadge')}</div>
      ) : (
        <div className="whitespace-pre-wrap text-sm text-foreground">{renderBodyWithMentions(comment.body, users)}</div>
      )}
    </div>
  )
}
