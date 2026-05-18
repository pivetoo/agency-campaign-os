import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Card, CardContent, CardHeader, CardTitle, useI18n } from 'archon-ui'
import { AlertTriangle, CalendarClock, FileDown, FileText, Sparkles } from 'lucide-react'
import {
  proposalPublicService,
  type ProposalPublicSnapshot,
  type ProposalPublicView,
} from '../services/proposalPublicService'

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)
}

function formatDate(value?: string): string {
  if (!value) return '-'
  return new Date(value).toLocaleDateString('pt-BR')
}

export default function PublicProposal() {
  const { t } = useI18n()
  const { token } = useParams<{ token: string }>()
  const [view, setView] = useState<ProposalPublicView | null>(null)
  const [snapshot, setSnapshot] = useState<ProposalPublicSnapshot | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)

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
            Esta proposta pode ter sido revogada ou o link já expirou. Entre em contato com a agência para receber um novo acesso.
          </p>
        </div>
      </div>
    )
  }

  return (
    <div data-testid="public-proposal-page" className="min-h-screen bg-muted/40 py-12">
      <div className="mx-auto w-full max-w-4xl px-4">
        <div className="mb-6 flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/15 text-primary">
            <Sparkles className="h-5 w-5" />
          </div>
          <div>
            <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('public.proposal.title')}</p>
            <h1 className="text-xl font-bold text-foreground">{view.brandName || 'Proposta'}</h1>
          </div>
          <span className="ml-auto rounded-full bg-primary/10 px-3 py-1 text-xs font-medium text-primary">
            v{view.versionNumber}
          </span>
          {token && (
            <a
              href={`/api/proposal-public/${encodeURIComponent(token)}/pdf`}
              className="inline-flex items-center gap-1.5 rounded-md border bg-background px-3 py-1.5 text-xs font-medium text-foreground hover:border-primary/40"
              download
            >
              <FileDown className="h-3.5 w-3.5" />
              Baixar PDF
            </a>
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
                Enviada em {formatDate(view.sentAt)}
              </span>
              {view.validityUntil ? (
                <span className={`flex items-center gap-1.5 ${expired ? 'text-destructive' : ''}`}>
                  <FileText className="h-3.5 w-3.5" />
                  Válida até {formatDate(view.validityUntil)}
                  {expired ? ' (expirada)' : ''}
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
                      <th className="px-4 py-3">Descrição</th>
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
                          <div>{item.description}</div>
                          {item.deliveryDeadline ? (
                            <div className="text-xs text-muted-foreground">
                              Entrega até {formatDate(item.deliveryDeadline)}
                            </div>
                          ) : null}
                          {item.observations ? (
                            <div className="text-xs italic text-muted-foreground">{item.observations}</div>
                          ) : null}
                        </td>
                        <td className="px-4 py-3 text-right text-foreground">{item.quantity}</td>
                        <td className="px-4 py-3 text-right text-foreground">{formatCurrency(item.unitPrice)}</td>
                        <td className="px-4 py-3 text-right font-semibold text-foreground">{formatCurrency(item.total)}</td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot className="bg-muted/30">
                    <tr>
                      <td colSpan={4} className="px-4 py-3 text-right text-sm font-medium text-muted-foreground">
                        Total da proposta
                      </td>
                      <td className="px-4 py-3 text-right text-lg font-bold text-foreground">
                        {formatCurrency(view.totalValue)}
                      </td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            ) : (
              <div className="rounded-md border border-dashed border-border/70 p-6 text-center text-sm text-muted-foreground">
                Esta proposta não tem itens cadastrados.
              </div>
            )}

            {snapshot?.notes ? (
              <div className="rounded-md border border-border/70 bg-muted/20 p-4">
                <h3 className="mb-2 text-sm font-semibold text-foreground">Observações</h3>
                <p className="whitespace-pre-wrap text-sm leading-relaxed text-muted-foreground">
                  {snapshot.notes}
                </p>
              </div>
            ) : null}
          </CardContent>
        </Card>

        <p className="mt-6 text-center text-xs text-muted-foreground">
          Documento gerado automaticamente. Para dúvidas ou aprovação, fale com a agência.
        </p>
      </div>
    </div>
  )
}
