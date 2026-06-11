import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Card, CardContent, CardHeader, CardTitle, useI18n } from 'archon-ui'
import { AlertTriangle, CalendarClock, CheckCircle2, FileDown, FileText, Sparkles } from 'lucide-react'
import { proposalPublicService, type ProposalPublicSnapshot, type ProposalPublicView } from '../../services/proposalPublicService'
import { formatDate } from '../../lib/format'
import { formatCurrency } from '../../lib/format'
import { resolveUploadUrl } from '../../lib/uploadUrl'

export default function PublicProposal() {
  const { t } = useI18n()
  const { token } = useParams<{ token: string }>()
  const [view, setView] = useState<ProposalPublicView | null>(null)
  const [snapshot, setSnapshot] = useState<ProposalPublicSnapshot | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)
  const [downloading, setDownloading] = useState(false)
  const [decisionName, setDecisionName] = useState('')
  const [decisionEmail, setDecisionEmail] = useState('')
  const [decisionNotes, setDecisionNotes] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [decisionError, setDecisionError] = useState<string | null>(null)
  const [localDecision, setLocalDecision] = useState<'accepted' | 'rejected' | null>(null)

  const submitDecision = async (accept: boolean) => {
    if (!token) return
    if (!decisionName.trim()) {
      setDecisionError(t('public.proposal.decision.nameRequired'))
      return
    }
    setSubmitting(true)
    setDecisionError(null)
    try {
      const input = { name: decisionName.trim(), email: decisionEmail.trim() || undefined, notes: decisionNotes.trim() || undefined }
      if (accept) {
        await proposalPublicService.accept(token, input)
      } else {
        await proposalPublicService.reject(token, input)
      }
      setLocalDecision(accept ? 'accepted' : 'rejected')
    } catch {
      setDecisionError(t('public.proposal.decision.failed'))
    } finally {
      setSubmitting(false)
    }
  }

  const handleDownloadPdf = async () => {
    if (!token || downloading) return
    setDownloading(true)
    try {
      await proposalPublicService.downloadPdf(token)
    } finally {
      setDownloading(false)
    }
  }

  useEffect(() => {
    if (!token) {
      setNotFound(true)
      setLoading(false)
      return
    }

    let isMounted = true
    proposalPublicService
      .getByToken(token)
      .then((result) => {
        if (!isMounted) return
        if (!result) {
          setNotFound(true)
          return
        }
        setView(result)
        setSnapshot(proposalPublicService.parseSnapshot(result.snapshotJson))
      })
      .finally(() => {
        if (isMounted) setLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [token])

  const expired = useMemo(() => {
    if (!view?.validityUntil) return false
    return new Date(view.validityUntil) < new Date()
  }, [view])

  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-muted/40">
        <div className="text-sm text-muted-foreground">{t('public.proposal.loading')}</div>
      </div>
    )
  }

  if (notFound || !view) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-muted/40 px-4">
        <div className="max-w-md rounded-lg border border-border bg-background p-8 text-center shadow-sm">
          <AlertTriangle className="mx-auto h-10 w-10 text-amber-500" />
          <h1 className="mt-4 text-lg font-semibold text-foreground">{t('public.proposal.invalidLink.title')}</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            {t('public.proposal.invalidLink.body')}
          </p>
        </div>
      </div>
    )
  }

  return (
    <div data-testid="public-proposal-page" className="min-h-screen bg-muted/40 py-12">
      <div className="mx-auto w-full max-w-4xl px-4">
        <div className="mb-6 flex items-center gap-3">
          {resolveUploadUrl(view.brandLogoUrl) ? (
            <img src={resolveUploadUrl(view.brandLogoUrl)} alt={view.brandName} className="h-10 w-10 rounded-lg border border-border/60 bg-white object-contain" />
          ) : (
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/15 text-primary">
              <Sparkles className="h-5 w-5" />
            </div>
          )}
          <div>
            <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('public.proposal.title')}</p>
            <h1 className="text-xl font-bold text-foreground">{view.brandName || t('public.proposal.fallbackName')}</h1>
          </div>
          <span className="ml-auto rounded-full bg-primary/10 px-3 py-1 text-xs font-medium text-primary">
            v{view.versionNumber}
          </span>
          {token && (
            <button
              type="button"
              onClick={handleDownloadPdf}
              disabled={downloading}
              className="inline-flex items-center gap-1.5 rounded-md border bg-background px-3 py-1.5 text-xs font-medium text-foreground hover:border-primary/40 disabled:opacity-60"
            >
              <FileDown className="h-3.5 w-3.5" />
              {downloading ? t('public.proposal.downloading') : t('public.proposal.downloadPdf')}
            </button>
          )}
        </div>

        <Card className="overflow-hidden border-border/70 shadow-sm">
          <CardHeader className="space-y-1 border-b bg-background pb-5">
            <CardTitle className="text-2xl">{view.name}</CardTitle>
            {view.description ? (
              <p className="text-sm leading-relaxed text-muted-foreground">{view.description}</p>
            ) : null}
            <div className="flex flex-wrap gap-4 pt-3 text-xs text-muted-foreground">
              <span className="flex items-center gap-1.5">
                <CalendarClock className="h-3.5 w-3.5" />
                {t('public.proposal.sentAt').replace('{0}', formatDate(view.sentAt))}
              </span>
              {view.validityUntil ? (
                <span className={`flex items-center gap-1.5 ${expired ? 'text-destructive' : ''}`}>
                  <FileText className="h-3.5 w-3.5" />
                  {t('public.proposal.validUntil').replace('{0}', formatDate(view.validityUntil))}
                  {expired ? ` ${t('public.proposal.expiredSuffix')}` : ''}
                </span>
              ) : null}
            </div>
          </CardHeader>
          <CardContent className="space-y-6 p-6">
            {snapshot && snapshot.items.length > 0 ? (
              <div className="overflow-x-auto rounded-md border border-border/70">
                <table className="w-full text-sm">
                  <thead className="bg-muted/40 text-left text-xs uppercase tracking-wide text-muted-foreground">
                    <tr>
                      <th className="px-4 py-3">{t('public.proposal.field.creatorItem')}</th>
                      <th className="px-4 py-3">{t('public.proposal.field.description')}</th>
                      <th className="px-4 py-3 text-right">{t('public.proposal.field.qty')}</th>
                      <th className="px-4 py-3 text-right">{t('public.proposal.field.unitPrice')}</th>
                      <th className="px-4 py-3 text-right">{t('public.proposal.field.total')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border/60">
                    {snapshot.items.map((item) => (
                      <tr key={item.id}>
                        <td className="px-4 py-3 font-medium text-foreground">
                          {item.creatorName || '—'}
                        </td>
                        <td className="px-4 py-3 text-foreground">
                          <div className="flex flex-wrap items-center gap-2">
                            <span>{item.description}</span>
                            {item.kind === 1 && (
                              <span className="inline-flex items-center gap-1 rounded-full bg-primary/10 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-primary">
                                {t('public.proposal.usageRights')}
                                {' · '}
                                {item.usageDurationMonths ? t('public.proposal.usageMonths').replace('{0}', String(item.usageDurationMonths)) : t('public.proposal.usagePerpetual')}
                                {item.usageScope ? ` · ${item.usageScope}` : ''}
                              </span>
                            )}
                            {item.isVariable && (
                              <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-amber-700">
                                {item.variableRate != null ? `${item.variableRate}% · ` : ''}{t('public.proposal.variablePricing')}
                              </span>
                            )}
                          </div>
                          {item.deliveryDeadline ? (
                            <div className="text-xs text-muted-foreground">
                              {t('public.proposal.deliveryBy').replace('{0}', formatDate(item.deliveryDeadline))}
                            </div>
                          ) : null}
                          {item.observations ? (
                            <div className="text-xs italic text-muted-foreground">{item.observations}</div>
                          ) : null}
                        </td>
                        <td className="px-4 py-3 text-right text-foreground">{item.isVariable ? '—' : item.quantity}</td>
                        <td className="px-4 py-3 text-right text-foreground">{item.isVariable ? '—' : formatCurrency(item.unitPrice)}</td>
                        <td className="px-4 py-3 text-right font-semibold text-foreground">
                          {formatCurrency(item.total)}
                          {item.isVariable ? <div className="text-[10px] font-normal text-muted-foreground">{t('public.proposal.estimate')}</div> : null}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot className="bg-muted/30">
                    {view.discountPercent != null && view.discountPercent > 0 ? (
                      <>
                        <tr>
                          <td colSpan={4} className="px-4 py-2 text-right text-sm text-muted-foreground">
                            {t('public.proposal.grossTotal')}
                          </td>
                          <td className="px-4 py-2 text-right text-sm text-foreground">
                            {formatCurrency(view.totalValue)}
                          </td>
                        </tr>
                        <tr>
                          <td colSpan={4} className="px-4 py-2 text-right text-sm text-muted-foreground">
                            {t('public.proposal.discount').replace('{0}', String(view.discountPercent))}
                          </td>
                          <td className="px-4 py-2 text-right text-sm text-destructive">
                            - {formatCurrency(view.discountValue)}
                          </td>
                        </tr>
                      </>
                    ) : null}
                    <tr>
                      <td colSpan={4} className="px-4 py-3 text-right text-sm font-medium text-muted-foreground">
                        {t('public.proposal.netTotal')}
                      </td>
                      <td className="px-4 py-3 text-right text-lg font-bold text-foreground">
                        {formatCurrency(view.netTotalValue)}
                      </td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            ) : (
              <div className="rounded-md border border-dashed border-border/70 p-6 text-center text-sm text-muted-foreground">
                {t('public.proposal.empty')}
              </div>
            )}

            {snapshot?.notes ? (
              <div className="rounded-md border border-border/70 bg-muted/20 p-4">
                <h3 className="mb-2 text-sm font-semibold text-foreground">{t('public.proposal.notes')}</h3>
                <p className="whitespace-pre-wrap text-sm leading-relaxed text-muted-foreground">
                  {snapshot.notes}
                </p>
              </div>
            ) : null}
          </CardContent>
        </Card>

        {(localDecision === 'accepted' || (!localDecision && view.decision === 'accepted')) ? (
          <div data-testid="public-proposal-accepted" className="mt-6 rounded-lg border border-emerald-300 bg-emerald-50 p-5 text-center">
            <CheckCircle2 className="mx-auto h-8 w-8 text-emerald-600" />
            <p className="mt-2 text-sm font-semibold text-emerald-800">{t('public.proposal.decision.acceptedTitle')}</p>
            <p className="text-xs text-emerald-700">{t('public.proposal.decision.acceptedHint')}</p>
          </div>
        ) : localDecision === 'rejected' ? (
          <div className="mt-6 rounded-lg border border-border bg-muted/30 p-5 text-center">
            <p className="text-sm font-semibold text-foreground">{t('public.proposal.decision.rejectedTitle')}</p>
            <p className="text-xs text-muted-foreground">{t('public.proposal.decision.rejectedHint')}</p>
          </div>
        ) : view.canDecide && !expired ? (
          <div className="mt-6 rounded-lg border border-border bg-card p-5 shadow-sm">
            <p className="text-sm font-semibold text-foreground">{t('public.proposal.decision.prompt')}</p>
            <div className="mt-3 grid gap-3 sm:grid-cols-2">
              <input data-testid="public-proposal-name" className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring" placeholder={t('public.proposal.decision.namePlaceholder')} value={decisionName} onChange={(e) => setDecisionName(e.target.value)} />
              <input className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring" placeholder={t('public.proposal.decision.emailPlaceholder')} value={decisionEmail} onChange={(e) => setDecisionEmail(e.target.value)} />
            </div>
            <textarea className="mt-3 min-h-[70px] w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring" placeholder={t('public.proposal.decision.notesPlaceholder')} value={decisionNotes} onChange={(e) => setDecisionNotes(e.target.value)} />
            {decisionError && <p className="mt-2 text-xs text-destructive">{decisionError}</p>}
            <div className="mt-3 flex flex-wrap gap-2">
              <button type="button" data-testid="public-proposal-accept" disabled={submitting} onClick={() => void submitDecision(true)} className="inline-flex items-center gap-1.5 rounded-md bg-emerald-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-emerald-700 disabled:opacity-60">
                <CheckCircle2 className="h-4 w-4" /> {t('public.proposal.decision.accept')}
              </button>
              <button type="button" data-testid="public-proposal-reject" disabled={submitting} onClick={() => void submitDecision(false)} className="inline-flex items-center rounded-md border border-border px-4 py-2 text-sm font-medium text-muted-foreground transition-colors hover:border-destructive/40 hover:text-destructive disabled:opacity-60">
                {t('public.proposal.decision.reject')}
              </button>
            </div>
          </div>
        ) : null}

        <p className="mt-6 text-center text-xs text-muted-foreground">
          {t('public.proposal.footer')}
        </p>
        <p className="mt-2 text-center text-[11px] text-muted-foreground/70">
          {t('public.proposal.privacyNote')}
        </p>
      </div>
    </div>
  )
}
