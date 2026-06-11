import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useI18n } from 'archon-ui'
import { AlertTriangle, ArrowRight, CalendarClock, CalendarX2, CheckCircle2, Clock, Flame } from 'lucide-react'
import { opportunityService, type CommercialAlert, type FollowUpSummary, type ApprovalSummary } from '../../../services/opportunityService'
import { formatDate } from '../../../lib/format'

interface Props {
  onNavigate?: () => void
}

interface Chip {
  key: string
  label: string
  value: number
  tone: 'rose' | 'amber'
  to: string
}

const TONE_CLASS: Record<Chip['tone'], string> = {
  rose: 'border-rose-300 bg-rose-50 text-rose-700',
  amber: 'border-amber-300 bg-amber-50 text-amber-800',
}

function alertIcon(type: string) {
  if (type === 'stagesla') return Flame
  if (type === 'expectedclose') return CalendarX2
  return Clock
}

// Painel "o que precisa de atencao hoje": follow-ups vencidos/hoje, aprovacoes pendentes e a lista de
// alertas acionaveis (GetAlerts). Sem receita/valores. Usado no sheet de insights do Funil.
export default function CommercialAttentionPanel({ onNavigate }: Props) {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [alerts, setAlerts] = useState<CommercialAlert[]>([])
  const [followUps, setFollowUps] = useState<FollowUpSummary | null>(null)
  const [approvals, setApprovals] = useState<ApprovalSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(false)
    Promise.all([
      opportunityService.getAlerts(),
      opportunityService.getFollowUpsSummary(),
      opportunityService.getApprovalsSummary(),
    ])
      .then(([alertsResult, followUpsResult, approvalsResult]) => {
        if (cancelled) return
        setAlerts(alertsResult)
        setFollowUps(followUpsResult)
        setApprovals(approvalsResult)
      })
      .catch(() => { if (!cancelled) setError(true) })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [])

  const goTo = (path: string) => {
    onNavigate?.()
    navigate(path)
  }

  const goToAlert = (alert: CommercialAlert) => {
    goTo(`/comercial/oportunidades/${alert.opportunityId}${alert.followUpId ? '?tab=followups' : ''}`)
  }

  const chips: Chip[] = [
    { key: 'overdue', label: t('commercialAttention.chip.overdue'), value: followUps?.overdue ?? 0, tone: 'rose', to: '/comercial/followups' },
    { key: 'today', label: t('commercialAttention.chip.today'), value: followUps?.today ?? 0, tone: 'amber', to: '/comercial/followups' },
    { key: 'approvals', label: t('commercialAttention.chip.pendingApprovals'), value: approvals?.pending ?? 0, tone: 'amber', to: '/comercial/aprovacoes' },
  ]

  const highAlerts = alerts.filter((alert) => alert.severity === 'high')
  const otherAlerts = alerts.filter((alert) => alert.severity !== 'high')

  return (
    <div className="rounded-lg border border-border bg-card">
      <div className="flex items-center gap-2 border-b border-border/60 px-3 py-2">
        <AlertTriangle className="h-3.5 w-3.5 text-primary" />
        <h4 className="text-sm font-semibold text-foreground">{t('commercialAttention.title')}</h4>
        {!loading && !error && <span className="ml-auto text-[11px] text-muted-foreground">{alerts.length}</span>}
      </div>

      <div className="grid grid-cols-3 gap-2 p-3">
        {chips.map((chip) => (
          <button
            key={chip.key}
            type="button"
            onClick={() => goTo(chip.to)}
            className={`flex flex-col items-start rounded-md border px-2.5 py-2 text-left transition-opacity hover:opacity-80 ${TONE_CLASS[chip.tone]}`}
          >
            <span className="text-lg font-bold tabular-nums leading-none">{chip.value}</span>
            <span className="mt-1 text-[10px] font-semibold uppercase leading-tight tracking-wide">{chip.label}</span>
          </button>
        ))}
      </div>

      {loading ? (
        <p className="px-3 pb-4 text-center text-xs text-muted-foreground">{t('commercialAttention.loading')}</p>
      ) : error ? (
        <p className="px-3 pb-4 text-center text-xs text-rose-700">{t('commercialAttention.error')}</p>
      ) : alerts.length === 0 ? (
        <div className="px-3 pb-4 text-center">
          <CheckCircle2 className="mx-auto h-6 w-6 text-emerald-500" />
          <p className="mt-1 text-xs font-semibold text-foreground">{t('commercialAttention.empty.title')}</p>
        </div>
      ) : (
        <ul className="divide-y divide-border/60 border-t border-border/60">
          {[...highAlerts, ...otherAlerts].map((alert, index) => {
            const Icon = alertIcon(alert.type)
            const high = alert.severity === 'high'
            return (
              <li key={`${alert.opportunityId}-${alert.followUpId ?? alert.type}-${index}`}>
                <button
                  type="button"
                  onClick={() => goToAlert(alert)}
                  className="flex w-full items-center gap-2.5 px-3 py-2 text-left transition-colors hover:bg-muted/40"
                >
                  <span className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-full ${high ? 'bg-rose-50 text-rose-600' : 'bg-amber-50 text-amber-600'}`}>
                    {high ? <AlertTriangle className="h-3.5 w-3.5" /> : <Icon className="h-3.5 w-3.5" />}
                  </span>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <span className="truncate text-xs font-semibold text-foreground">{alert.title}</span>
                      {alert.dueAt && (
                        <span className="flex items-center gap-1 text-[10px] text-muted-foreground">
                          <CalendarClock className="h-2.5 w-2.5" /> {formatDate(alert.dueAt)}
                        </span>
                      )}
                    </div>
                    <p className="truncate text-[11px] text-muted-foreground">{alert.opportunityName}</p>
                  </div>
                  <ArrowRight className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                </button>
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}
