import { useEffect, useState } from 'react'
import { AlertTriangle, Hourglass, Percent, TrendingUp } from 'lucide-react'
import { opportunityService } from '../../../services/opportunityService'
import type { CommercialAnalytics } from '../../../types/commercialAnalytics'
import { formatCurrency } from '../../../lib/format'

interface CommercialKpiStripProps {
  scope?: 'all' | 'mine'
  openCount: number
  openValue: number
  overdueFollowUps: number
}

function startOfMonth(): string {
  const d = new Date()
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString()
}

function endOfMonth(): string {
  const d = new Date()
  return new Date(d.getFullYear(), d.getMonth() + 1, 0, 23, 59, 59).toISOString()
}

export default function CommercialKpiStrip({ scope = 'all', openCount, openValue, overdueFollowUps }: CommercialKpiStripProps) {
  const [analytics, setAnalytics] = useState<CommercialAnalytics | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    const promise = scope === 'mine'
      ? opportunityService.getAnalyticsMine({ periodStart: startOfMonth(), periodEnd: endOfMonth() })
      : opportunityService.getAnalytics({ periodStart: startOfMonth(), periodEnd: endOfMonth() })
    promise
      .then((data) => { if (!cancelled) setAnalytics(data) })
      .catch(() => { if (!cancelled) setAnalytics(null) })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [scope])

  return (
    <div className="grid grid-cols-2 gap-2 md:grid-cols-4">
      <KpiTile
        icon={<TrendingUp className="h-3 w-3 text-primary" />}
        label="Em aberto"
        value={String(openCount)}
        sub={formatCurrency(openValue)}
      />
      <KpiTile
        icon={<Percent className="h-3 w-3 text-emerald-600" />}
        label="Win rate"
        value={loading ? '…' : analytics ? `${analytics.winRate.toFixed(0)}%` : '—'}
        sub={analytics ? `${analytics.wonCount}/${analytics.closedCount} no mês` : 'mês corrente'}
      />
      <KpiTile
        icon={<Hourglass className="h-3 w-3 text-amber-600" />}
        label="Ciclo médio"
        value={loading ? '…' : analytics ? `${analytics.averageCycleDays.toFixed(0)}d` : '—'}
        sub="criação → fechamento"
      />
      <KpiTile
        icon={<AlertTriangle className={`h-3 w-3 ${overdueFollowUps > 0 ? 'text-destructive' : 'text-muted-foreground'}`} />}
        label="Atrasados"
        value={String(overdueFollowUps)}
        sub={overdueFollowUps > 0 ? 'follow-ups vencidos' : 'tudo no prazo'}
        tone={overdueFollowUps > 0 ? 'destructive' : undefined}
      />
    </div>
  )
}

function KpiTile({ icon, label, value, sub, tone }: { icon: React.ReactNode; label: string; value: string; sub?: string; tone?: 'destructive' }) {
  return (
    <div className={`rounded-md border bg-card px-3 py-2 ${tone === 'destructive' ? 'border-destructive/30 bg-destructive/5' : 'border-border'}`}>
      <div className="flex items-center gap-1 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
        {icon} {label}
      </div>
      <p className={`mt-0.5 truncate text-base font-semibold ${tone === 'destructive' ? 'text-destructive' : 'text-foreground'}`}>{value}</p>
      {sub && <p className="truncate text-[10.5px] text-muted-foreground">{sub}</p>}
    </div>
  )
}
