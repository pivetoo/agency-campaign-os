import { useEffect, useMemo, useState } from 'react'
import { Button, Card, CardContent, useApi } from 'archon-ui'
import { ArrowRight, MessageSquare, Pencil, Send, Trash2 } from 'lucide-react'
import {
  opportunityService,
  type OpportunityComment,
  type OpportunityStageHistoryItem,
} from '../../services/opportunityService'

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

function formatDateTime(value: string): string {
  const date = new Date(value)
  return `${date.toLocaleDateString('pt-BR')} ${date.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}`
}

function relativeTime(value: string): string {
  const date = new Date(value)
  const diffMs = Date.now() - date.getTime()
  const diffMin = Math.floor(diffMs / 60000)
  if (diffMin < 1) return 'agora'
  if (diffMin < 60) return `há ${diffMin}min`
  const diffHours = Math.floor(diffMin / 60)
  if (diffHours < 24) return `há ${diffHours}h`
  const diffDays = Math.floor(diffHours / 24)
  if (diffDays < 30) return `há ${diffDays}d`
  return date.toLocaleDateString('pt-BR')
}

export default function OpportunityActivityTab({ opportunityId, currentUserId }: OpportunityActivityTabProps) {
  const [comments, setComments] = useState<OpportunityComment[]>([])
  const [stageHistory, setStageHistory] = useState<OpportunityStageHistoryItem[]>([])
  const [body, setBody] = useState('')
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editingBody, setEditingBody] = useState('')

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

    const result = await runMutation(() => opportunityService.createComment(opportunityId, { body: trimmed }))
    if (result !== null) {
      setBody('')
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

  const removeComment = async (id: number) => {
    if (!window.confirm('Excluir este comentário?')) return
    const result = await runMutation(() => opportunityService.deleteComment(id))
    if (result !== null) {
      await reload()
    }
  }

  const canEdit = (comment: OpportunityComment) =>
    !comment.authorUserId || (currentUserId != null && comment.authorUserId === currentUserId)

  return (
    <Card>
      <CardContent className="space-y-6 p-6">
        <div className="space-y-3">
          <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
            <MessageSquare className="h-4 w-4 text-primary" />
            Adicionar comentário
          </div>
          <textarea
            value={body}
            onChange={(e) => setBody(e.target.value)}
            placeholder="Escreva um comentário interno sobre a oportunidade..."
            rows={3}
            className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            disabled={mutating}
          />
          <div className="flex justify-end">
            <Button
              size="sm"
              icon={<Send className="h-4 w-4" />}
              onClick={() => void submitComment()}
              disabled={mutating || !body.trim()}
            >
              Comentar
            </Button>
          </div>
        </div>

        <div className="space-y-4">
          <div className="text-sm font-semibold text-foreground">Linha do tempo</div>

          {loading && timeline.length === 0 ? (
            <div className="text-sm text-muted-foreground">Carregando atividades...</div>
          ) : timeline.length === 0 ? (
            <div className="text-sm text-muted-foreground">Nenhuma atividade registrada ainda.</div>
          ) : (
            <ol className="relative ml-3 space-y-4 border-l border-border pl-6">
              {timeline.map((item) => (
                <li key={item.id} className="relative">
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
                    <StageEvent entry={item.data as OpportunityStageHistoryItem} />
                  ) : (
                    <CommentEvent
                      comment={item.data as OpportunityComment}
                      isEditing={editingId === (item.data as OpportunityComment).id}
                      editingBody={editingBody}
                      canEdit={canEdit(item.data as OpportunityComment)}
                      mutating={mutating}
                      onStartEdit={startEditing}
                      onCancelEdit={cancelEditing}
                      onSaveEdit={saveEditing}
                      onChangeEdit={setEditingBody}
                      onDelete={removeComment}
                    />
                  )}
                </li>
              ))}
            </ol>
          )}
        </div>
      </CardContent>
    </Card>
  )
}

function StageEvent({ entry }: { entry: OpportunityStageHistoryItem }) {
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
          <span className="text-xs text-muted-foreground">por {entry.changedByUserName}</span>
        ) : null}
        <span className="text-xs text-muted-foreground" title={formatDateTime(entry.changedAt)}>
          {relativeTime(entry.changedAt)}
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
  onStartEdit: (comment: OpportunityComment) => void
  onCancelEdit: () => void
  onSaveEdit: () => void
  onChangeEdit: (value: string) => void
  onDelete: (id: number) => void
}

function CommentEvent(props: CommentEventProps) {
  const { comment, isEditing, editingBody, canEdit, mutating, onStartEdit, onCancelEdit, onSaveEdit, onChangeEdit, onDelete } = props

  return (
    <div className="space-y-1">
      <div className="flex flex-wrap items-center gap-2 text-sm">
        <span className="font-medium text-foreground">{comment.authorName}</span>
        <span className="text-xs text-muted-foreground" title={formatDateTime(comment.createdAt)}>
          {relativeTime(comment.createdAt)}
        </span>
        {comment.updatedAt && comment.updatedAt !== comment.createdAt ? (
          <span className="text-xs italic text-muted-foreground">(editado)</span>
        ) : null}
        {canEdit && !isEditing ? (
          <span className="ml-auto flex gap-1">
            <button
              type="button"
              className="inline-flex h-6 w-6 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-foreground"
              onClick={() => onStartEdit(comment)}
            >
              <Pencil className="h-3 w-3" />
            </button>
            <button
              type="button"
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
              Salvar
            </Button>
            <Button size="sm" variant="outline" onClick={onCancelEdit} disabled={mutating}>
              Cancelar
            </Button>
          </div>
        </div>
      ) : (
        <div className="whitespace-pre-wrap text-sm text-foreground">{comment.body}</div>
      )}
    </div>
  )
}
