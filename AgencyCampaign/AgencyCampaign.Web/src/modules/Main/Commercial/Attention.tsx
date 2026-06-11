import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, useI18n } from 'archon-ui'
import { AlertTriangle, ArrowRight, CalendarClock, CalendarX2, CheckCircle2, ClipboardCheck, Clock, Flame } from 'lucide-react'
import { opportunityService, type CommercialAlert, type FollowUpSummary, type ApprovalSummary, type CommercialDashboardSummary } from '../../../services/opportunityService'
import { formatDate } from '../../../lib/format'

interface Chip {
  key: string
  label: string
  value: number
  tone: 'rose' | 'amber' | 'muted'
  onClick?: () => void
}

const TONE_CLASS: Record<Chip['tone'], string> = {
  rose: 'border-rose-300 bg-rose-50 text-rose-700',
  amber: 'border-amber-300 bg-amber-50 text-amber-800',
  muted: 'border-border bg-muted/40 text-foreground',
}

function alertIcon(type: string) {
  if (type === 'stagesla') return Flame
  if (type === 'expectedclose') return CalendarX2
  return Clock
}

export default function CommercialAttention() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [alerts, setAlerts] = useState<CommercialAlert[]>([])
  const [followUps, setFollowUps] = useState<FollowUpSummary | null>(null)
  const [approvals, setApprovals] = useState<ApprovalSummary | null>(null)
  const [dashboard, setDashboard] = useState<CommercialDashboardSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  const load = async () => {
    setLoading(true)
    setError(false)
    try {
      const [alertsResult, followUpsResult, approvalsResult, dashboardResult] = await Promise.all([
        opportunityService.getAlerts(),
        opportunityService.getFollowUpsSummary(),
        opportunityService.getApprovalsSummary(),
        opportunityService.getDashboard(),
      ])
      setAlerts(alertsResult)
      setFollowUps(followUpsResult)
      setApprovals(approvalsResult)
      setDashboard(dashboardResult)
    } catch {
      setError(true)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [])

  const goToOpportunity = (alert: CommercialAlert) => {
    const tab = alert.followUpId ? '?tab=followups' : ''
    navigate(`/comercial/oportunidades/${alert.opportunityId}${tab}`)
  }

  // Sem receita/valores no painel (visivel a quem tem acesso comercial): so contagens e alertas acionaveis
  const chips: Chip[] = [
    { key: 'overdue', label: t('commercialAttention.chip.overdue'), value: followUps?.overdue ?? 0, tone: 'rose', onClick: () => navigate('/comercial/followups') },
    { key: 'today', label: t('commercialAttention.chip.today'), value: followUps?.today ?? 0, tone: 'amber', onClick: () => navigate('/comercial/followups') },
    { key: 'approvals', label: t('commercialAttention.chip.pendingApprovals'), value: approvals?.pending ?? 0, tone: 'amber', onClick: () => navigate('/comercial/aprovacoes') },
    { key: 'open', label: t('commercialAttention.chip.openOpportunities'), value: dashboard?.openOpportunities ?? 0, tone: 'muted', onClick: () => navigate('/comercial/oportunidades') },
  ]

  const highAlerts = alerts.filter((alert) => alert.severity === 'high')
  const otherAlerts = alerts.filter((alert) => alert.severity !== 'high')

  return (
    <PageLayout title={t('commercialAttention.title')} subtitle={t('commercialAttention.subtitle')}>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {chips.map((chip) => (
          <button
            key={chip.key}
            type="button"
            onClick={chip.onClick}
            className={`flex flex-col items-start rounded-lg border px-4 py-3 text-left transition-opacity hover:opacity-80 ${TONE_CLASS[chip.tone]}`}
          >
            <span className="text-2xl font-bold tabular-nums">{chip.value}</span>
            <span className="text-[11px] font-semibold uppercase tracking-wide">{chip.label}</span>
          </button>
        ))}
      </div>

      <div className="mt-6 rounded-lg border border-border bg-card">
        <div className="flex items-center gap-2 border-b border-border/60 px-4 py-3">
          <ClipboardCheck className="h-4 w-4 text-primary" />
          <h3 className="text-sm font-semibold text-foreground">{t('commercialAttention.alerts.title')}</h3>
          {!loading && !error && <span className="ml-auto text-[11px] text-muted-foreground">{alerts.length}</span>}
        </div>

        {loading ? (
          <p className="px-4 py-8 text-center text-sm text-muted-foreground">{t('commercialAttention.loading')}</p>
        ) : error ? (
          <p className="px-4 py-8 text-center text-sm text-rose-700">{t('commercialAttention.error')}</p>
        ) : alerts.length === 0 ? (
          <div className="px-4 py-10 text-center">
            <CheckCircle2 className="mx-auto h-8 w-8 text-emerald-500" />
            <p className="mt-2 text-sm font-semibold text-foreground">{t('commercialAttention.empty.title')}</p>
            <p className="text-xs text-muted-foreground">{t('commercialAttention.empty.subtitle')}</p>
          </div>
        ) : (
          <ul className="divide-y divide-border/60">
            {[...highAlerts, ...otherAlerts].map((alert, index) => {
              const Icon = alertIcon(alert.type)
              const high = alert.severity === 'high'
              return (
                <li key={`${alert.opportunityId}-${alert.followUpId ?? alert.type}-${index}`}>
                  <button
                    type="button"
                    onClick={() => goToOpportunity(alert)}
                    className="flex w-full items-center gap-3 px-4 py-3 text-left transition-colors hover:bg-muted/40"
                  >
                    <span className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full ${high ? 'bg-rose-50 text-rose-600' : 'bg-amber-50 text-amber-600'}`}>
                      {high ? <AlertTriangle className="h-4 w-4" /> : <Icon className="h-4 w-4" />}
                    </span>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className="truncate text-sm font-semibold text-foreground">{alert.title}</span>
                        {alert.dueAt && (
                          <span className="flex items-center gap-1 text-[11px] text-muted-foreground">
                            <CalendarClock className="h-3 w-3" /> {formatDate(alert.dueAt)}
                          </span>
                        )}
                      </div>
                      <p className="truncate text-xs text-muted-foreground">
                        {alert.opportunityName}{alert.description ? ` — ${alert.description}` : ''}
                      </p>
                    </div>
                    <ArrowRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                  </button>
                </li>
              )
            })}
          </ul>
        )}
      </div>
    </PageLayout>
  )
}
