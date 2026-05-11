import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Button, Card, CardContent, CardHeader, CardTitle, Badge, useI18n } from 'archon-ui'
import { CheckCircle2, XCircle, ExternalLink, AlertTriangle } from 'lucide-react'
import { deliverablePublicService } from '../services/deliverableShareLinkService'
import type { DeliverablePublicView } from '../types/deliverableShareLink'

function formatDate(value?: string | null): string {
  if (!value) return '-'
  return new Date(value).toLocaleDateString('pt-BR')
}

const approvalStatusLabels: Record<number, string> = {
  1: 'Pendente',
  2: 'Aprovada',
  3: 'Rejeitada',
}

export default function PublicDeliverable() {
  const { t } = useI18n()
  const { token } = useParams<{ token: string }>()
  const [view, setView] = useState<DeliverablePublicView | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)
  const [reviewerName, setReviewerName] = useState('')
  const [comment, setComment] = useState('')
  const [submitting, setSubmitting] = useState<'approve' | 'reject' | null>(null)
  const [feedback, setFeedback] = useState<string | null>(null)

  useEffect(() => {
    if (!token) {
      setNotFound(true)
      setLoading(false)
      return
    }

    let mounted = true
    deliverablePublicService
      .getByToken(token)
      .then((result) => {
        if (!mounted) return
        if (!result) {
          setNotFound(true)
          return
        }
        setView(result)
      })
      .finally(() => {
        if (mounted) setLoading(false)
      })

    return () => {
      mounted = false
    }
  }, [token])

  const submit = async (decision: 'approve' | 'reject') => {
    if (!token) return
    if (reviewerName.trim().length < 2) {
      setFeedback('Informe seu nome para aprovar ou rejeitar.')
      return
    }
    setSubmitting(decision)
    setFeedback(null)
    try {
      const action = decision === 'approve' ? deliverablePublicService.approve : deliverablePublicService.reject
      const response = await action(token, { reviewerName: reviewerName.trim(), comment: comment.trim() || null })
      if (response.data) {
        setView(response.data)
        setFeedback(decision === 'approve' ? 'Entrega aprovada. Obrigado!' : 'Entrega marcada como rejeitada.')
      }
    } catch {
      setFeedback('Não foi possível registrar sua decisão. Tente novamente.')
    } finally {
      setSubmitting(null)
    }
  }

  if (loading) {
    return <div className="flex min-h-screen items-center justify-center text-sm text-muted-foreground">{t('public.loading')}</div>
  }

  if (notFound || !view) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-muted/40">
        <Card className="max-w-md">
          <CardContent className="flex flex-col items-center gap-3 py-8 text-center">
            <AlertTriangle size={28} className="text-amber-500" />
            <CardTitle>{t('public.deliverable.invalidLink.title')}</CardTitle>
            <p className="text-sm text-muted-foreground">{t('public.deliverable.invalidLink.message')}</p>
          </CardContent>
        </Card>
      </div>
    )
  }

  const decisionTaken = view.approvalStatus === 2 || view.approvalStatus === 3

  return (
    <div className="min-h-screen bg-muted/40 py-8">
      <div className="mx-auto max-w-2xl space-y-4 px-4">
        <Card>
          <CardHeader>
            <p className="text-xs uppercase tracking-wide text-muted-foreground">{view.brandName} · {view.campaignName}</p>
            <CardTitle className="mt-1">{view.title}</CardTitle>
            <p className="mt-1 text-sm text-muted-foreground">
              {view.creatorName ? t('public.deliverable.creator').replace('{0}', view.creatorName) : null}
              {view.platformName ? ` · ${view.platformName}` : null}
              {view.deliverableKindName ? ` · ${view.deliverableKindName}` : null}
            </p>
          </CardHeader>
          <CardContent className="space-y-3">
            {view.description && <p className="text-sm">{view.description}</p>}
            <div className="grid grid-cols-2 gap-3 text-sm">
              <div>
                <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('public.deliverable.field.deadline')}</p>
                <p className="font-medium">{formatDate(view.dueAt)}</p>
              </div>
              <div>
                <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('common.field.status')}</p>
                {view.approvalStatus ? (
                  <Badge variant={view.approvalStatus === 2 ? 'success' : view.approvalStatus === 3 ? 'destructive' : 'warning'}>
                    {approvalStatusLabels[view.approvalStatus]}
                  </Badge>
                ) : (
                  <Badge variant="warning">{t('public.deliverable.waitingDecision')}</Badge>
                )}
              </div>
            </div>

            {view.evidenceUrl && (
              <a href={view.evidenceUrl} target="_blank" rel="noopener noreferrer" className="inline-flex items-center gap-1 text-sm text-primary hover:underline">
                Ver prévia/evidência <ExternalLink size={12} />
              </a>
            )}
          </CardContent>
        </Card>

        {decisionTaken ? (
          <Card>
            <CardContent className="space-y-2 py-6">
              <p className="text-sm">
                Decisão registrada: <strong>{approvalStatusLabels[view.approvalStatus ?? 0]}</strong>.
              </p>
              {view.approvalComment && (
                <p className="text-sm text-muted-foreground">{t('public.deliverable.comment').replace('{0}', view.approvalComment)}</p>
              )}
            </CardContent>
          </Card>
        ) : (
          <Card>
            <CardHeader>
              <CardTitle className="text-base">{t('public.deliverable.approveTitle')}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="space-y-2">
                <label className="text-sm font-medium">{t('public.deliverable.field.name')}</label>
                <input
                  className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                  value={reviewerName}
                  onChange={(e) => setReviewerName(e.target.value)}
                  placeholder={t('public.deliverable.placeholder.name')}
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">{t('public.deliverable.field.comment')}</label>
                <textarea
                  className="min-h-[100px] w-full rounded-md border bg-background px-3 py-2 text-sm"
                  value={comment}
                  onChange={(e) => setComment(e.target.value)}
                  placeholder={t('public.deliverable.placeholder.comment')}
                />
              </div>
              {feedback && <p className="text-xs text-muted-foreground">{feedback}</p>}
              <div className="flex gap-2">
                <Button onClick={() => void submit('approve')} disabled={submitting !== null}>
                  <CheckCircle2 size={16} className="mr-1.5" />
                  {submitting === 'approve' ? 'Aprovando...' : 'Aprovar'}
                </Button>
                <Button variant="outline" onClick={() => void submit('reject')} disabled={submitting !== null}>
                  <XCircle size={16} className="mr-1.5" />
                  {submitting === 'reject' ? 'Rejeitando...' : 'Rejeitar'}
                </Button>
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  )
}
