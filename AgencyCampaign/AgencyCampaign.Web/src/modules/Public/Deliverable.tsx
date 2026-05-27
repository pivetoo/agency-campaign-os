import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Button, Card, CardContent, CardHeader, CardTitle, Badge, useI18n } from 'archon-ui'
import { CheckCircle2, XCircle, ExternalLink, AlertTriangle, MessageSquare } from 'lucide-react'
import { deliverablePublicService } from '../../services/deliverableShareLinkService'
import type { DeliverablePublicView } from '../../types/deliverableShareLink'
import type { ContentVersion, ReviewComment } from '../../types/contentReview'
import { formatDate, formatDateTime } from '../../lib/format'
import { resolveUploadUrl } from '../../lib/uploadUrl'

const approvalStatusLabels: Record<number, string> = {
  1: 'Pendente',
  2: 'Aprovada',
  3: 'Rejeitada',
}

function latestVersion(versions: ContentVersion[]): ContentVersion | undefined {
  return versions.reduce<ContentVersion | undefined>((best, v) => {
    if (!best || v.roundNumber > best.roundNumber) return v
    return best
  }, undefined)
}

function authorLabel(role: number, t: (key: string) => string): string {
  if (role === 1) return t('contentReview.author.agency')
  if (role === 2) return t('contentReview.author.brand')
  return t('contentReview.author.creator')
}

function versionStatusLabel(status: number, t: (key: string) => string): string {
  if (status === 1) return t('contentReview.status.pendingInternal')
  if (status === 2) return t('contentReview.status.pendingBrand')
  if (status === 3) return t('contentReview.status.changesRequested')
  if (status === 4) return t('contentReview.status.approved')
  return ''
}

function versionStatusVariant(status: number): 'default' | 'warning' | 'success' | 'destructive' {
  if (status === 2) return 'warning'
  if (status === 3) return 'destructive'
  if (status === 4) return 'success'
  return 'default'
}

interface ContentReviewSectionProps {
  view: DeliverablePublicView
  token: string
  t: (key: string) => string
  onUpdate: (updated: DeliverablePublicView) => void
}

function ContentReviewSection({ view, token, t, onUpdate }: ContentReviewSectionProps) {
  const versions = view.versions ?? []
  const sharedComments = (view.comments ?? []).filter((c: ReviewComment) => c.visibility === 2)
  const version = latestVersion(versions)

  const [commentBody, setCommentBody] = useState('')
  const [submittingComment, setSubmittingComment] = useState(false)
  const [reviewerName, setReviewerName] = useState('')
  const [requestChangesComment, setRequestChangesComment] = useState('')
  const [showRequestChanges, setShowRequestChanges] = useState(false)
  const [submitting, setSubmitting] = useState<'approve' | 'requestChanges' | null>(null)
  const [feedback, setFeedback] = useState<string | null>(null)

  const versionClosed = version ? (version.status === 3 || version.status === 4) : false

  const handleApprove = async () => {
    if (reviewerName.trim().length < 2) {
      setFeedback('Informe seu nome para aprovar.')
      return
    }
    setSubmitting('approve')
    setFeedback(null)
    try {
      const response = await deliverablePublicService.approve(token, { reviewerName: reviewerName.trim(), comment: undefined })
      if (response.data) {
        onUpdate(response.data)
        setFeedback('Conteúdo aprovado. Obrigado!')
      }
    } catch {
      setFeedback('Não foi possível registrar sua decisão. Tente novamente.')
    } finally {
      setSubmitting(null)
    }
  }

  const handleRequestChanges = async () => {
    if (reviewerName.trim().length < 2) {
      setFeedback('Informe seu nome para solicitar alteração.')
      return
    }
    setSubmitting('requestChanges')
    setFeedback(null)
    try {
      const response = await deliverablePublicService.requestChanges(token, { reviewerName: reviewerName.trim(), comment: requestChangesComment.trim() || undefined })
      if (response.data) {
        onUpdate(response.data)
        setFeedback('Alteração solicitada. Obrigado!')
        setShowRequestChanges(false)
      }
    } catch {
      setFeedback('Não foi possível registrar sua decisão. Tente novamente.')
    } finally {
      setSubmitting(null)
    }
  }

  const handleAddComment = async () => {
    if (!commentBody.trim()) return
    setSubmittingComment(true)
    try {
      const response = await deliverablePublicService.addComment(token, commentBody.trim())
      if (response.data) {
        onUpdate(response.data)
        setCommentBody('')
      }
    } catch {
      // silent — comment failed
    } finally {
      setSubmittingComment(false)
    }
  }

  if (versions.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('contentReview.title')}</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">{t('contentReview.empty')}</p>
        </CardContent>
      </Card>
    )
  }

  return (
    <>
      {version && (
        <Card>
          <CardHeader className="flex flex-row items-center justify-between gap-2">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">
                {t('contentReview.round').replace('{0}', String(version.roundNumber))}
              </p>
              <CardTitle className="mt-1 text-base">
                {t('contentReview.submittedBy').replace('{0}', version.submittedByName)}
                {' '}
                <span className="font-normal text-muted-foreground">({authorLabel(version.submittedByRole, t)})</span>
              </CardTitle>
            </div>
            <Badge variant={versionStatusVariant(version.status)}>{versionStatusLabel(version.status, t)}</Badge>
          </CardHeader>
          <CardContent className="space-y-3">
            {version.note && <p className="text-sm text-muted-foreground">{version.note}</p>}
            {version.assets.length === 0 && (
              <p className="text-xs text-muted-foreground">{t('contentReview.empty')}</p>
            )}
            {version.assets.map((asset, idx) => (
              <div key={idx}>
                {asset.type === 1 ? (
                  <img src={resolveUploadUrl(asset.url)} alt={asset.fileName ?? ''} className="max-h-80 rounded-md border object-contain" />
                ) : (
                  <a href={asset.url} target="_blank" rel="noopener noreferrer" className="inline-flex items-center gap-1 text-sm text-primary hover:underline">
                    {t('contentReview.asset.externalLink')} <ExternalLink size={12} />
                  </a>
                )}
              </div>
            ))}
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <MessageSquare size={16} />
            {t('contentReview.comments.shared')}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {sharedComments.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('contentReview.comments.empty')}</p>
          ) : (
            <div className="space-y-3">
              {sharedComments.map((c: ReviewComment) => (
                <div key={c.id} className="rounded-md border bg-muted/30 px-3 py-2">
                  <div className="mb-1 flex items-center gap-2">
                    <span className="text-xs font-medium">{c.authorName}</span>
                    <span className="text-xs text-muted-foreground">({authorLabel(c.authorRole, t)})</span>
                    <span className="ml-auto text-xs text-muted-foreground">{formatDateTime(c.createdAt)}</span>
                  </div>
                  <p className="text-sm">{c.body}</p>
                </div>
              ))}
            </div>
          )}
          <div className="space-y-2">
            <textarea
              className="min-h-[80px] w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={commentBody}
              onChange={(e) => setCommentBody(e.target.value)}
              placeholder={t('contentReview.comments.placeholder')}
            />
            <Button variant="outline" size="sm" onClick={() => void handleAddComment()} disabled={submittingComment || !commentBody.trim()}>
              {t('contentReview.action.addComment')}
            </Button>
          </div>
        </CardContent>
      </Card>

      {!versionClosed && (
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
            {showRequestChanges && (
              <div className="space-y-2">
                <label className="text-sm font-medium">{t('public.deliverable.field.comment')}</label>
                <textarea
                  className="min-h-[80px] w-full rounded-md border bg-background px-3 py-2 text-sm"
                  value={requestChangesComment}
                  onChange={(e) => setRequestChangesComment(e.target.value)}
                  placeholder={t('public.deliverable.placeholder.comment')}
                />
              </div>
            )}
            {feedback && <p className="text-xs text-muted-foreground">{feedback}</p>}
            <div className="flex gap-2">
              <Button data-testid="public-deliverable-approve-button" onClick={() => void handleApprove()} disabled={submitting !== null}>
                <CheckCircle2 size={16} className="mr-1.5" />
                {submitting === 'approve' ? 'Aprovando...' : t('contentReview.action.approve')}
              </Button>
              {!showRequestChanges ? (
                <Button variant="outline" onClick={() => setShowRequestChanges(true)} disabled={submitting !== null}>
                  <XCircle size={16} className="mr-1.5" />
                  {t('contentReview.action.requestChanges')}
                </Button>
              ) : (
                <Button variant="outline" onClick={() => void handleRequestChanges()} disabled={submitting !== null}>
                  <XCircle size={16} className="mr-1.5" />
                  {submitting === 'requestChanges' ? 'Enviando...' : t('contentReview.action.requestChanges')}
                </Button>
              )}
            </div>
          </CardContent>
        </Card>
      )}
    </>
  )
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

  const hasVersions = (view?.versions?.length ?? 0) > 0

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

        {hasVersions ? (
          <ContentReviewSection
            view={view}
            token={token!}
            t={t}
            onUpdate={setView}
          />
        ) : decisionTaken ? (
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
                <Button data-testid="public-deliverable-approve-button" onClick={() => void submit('approve')} disabled={submitting !== null}>
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
