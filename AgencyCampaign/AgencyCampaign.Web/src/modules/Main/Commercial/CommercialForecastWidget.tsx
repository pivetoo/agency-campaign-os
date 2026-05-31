import { useEffect, useState } from 'react'
import { useI18n } from 'archon-ui'
import { TrendingUp, CheckCircle2, XCircle, Hourglass } from 'lucide-react'
import { opportunityService } from '../../../services/opportunityService'
import type { CommercialForecast } from '../../../types/commercialForecast'
import { formatCurrency } from '../../../lib/format'

interface CommercialForecastWidgetProps {
  scope?: 'all' | 'mine'
}

export default function CommercialForecastWidget({ scope = 'all' }: CommercialForecastWidgetProps) {
  const { t } = useI18n()
  const [forecast, setForecast] = useState<CommercialForecast | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(false)
    const promise = scope === 'mine' ? opportunityService.getForecastMine() : opportunityService.getForecast()
    promise
      .then((data) => { if (!cancelled) setForecast(data) })
      .catch(() => { if (!cancelled) { setForecast(null); setError(true) } })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [scope])

  if (loading) {
    return (
      <div className="rounded-lg border border-dashed border-border bg-muted/20 px-4 py-3 text-sm text-muted-foreground">
        {t('commercialForecast.loading')}
      </div>
    )
  }

  if (error) {
    return (
      <div className="rounded-lg border border-dashed border-rose-300/60 bg-rose-50/50 px-4 py-3 text-sm text-rose-700 dark:border-rose-500/30 dark:bg-rose-500/10 dark:text-rose-300">
        {t('commercialForecast.error')}
      </div>
    )
  }

  if (!forecast) {
    return null
  }

  const monthLabel = new Date(forecast.periodStart).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
  const totalPipeline = forecast.weightedTotal + forecast.wonTotal
  const projected = forecast.weightedTotal
  const won = forecast.wonTotal
  const lost = forecast.lostTotal
  const wonShare = totalPipeline > 0 ? (won / totalPipeline) * 100 : 0
  const projectedShare = totalPipeline > 0 ? (projected / totalPipeline) * 100 : 0

  return (
    <div className="rounded-lg border border-border bg-card p-4">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <div className="flex items-center gap-2 text-[11px] font-bold uppercase tracking-wider text-muted-foreground">
          <TrendingUp className="h-3 w-3" /> {t('commercialForecast.header').replace('{0}', monthLabel)}
        </div>
        <span className="text-xs text-muted-foreground">{(forecast.openCount === 1 ? t('commercialForecast.openCount.one') : t('commercialForecast.openCount.many').replace('{0}', String(forecast.openCount)))} · {(forecast.wonCount === 1 ? t('commercialForecast.wonCount.one') : t('commercialForecast.wonCount.many').replace('{0}', String(forecast.wonCount)))}</span>
      </div>

      <div className="mt-2 flex items-baseline gap-3">
        <span className="font-mono text-xl font-bold text-foreground">{formatCurrency(won + projected)}</span>
        <span className="text-xs text-muted-foreground">{t('commercialForecast.weightedTotal')}</span>
      </div>

      <div className="mt-3 flex h-2 overflow-hidden rounded-full bg-muted">
        <div className="h-full bg-emerald-500" style={{ width: `${wonShare}%` }} title={t('commercialForecast.wonTitle').replace('{0}', formatCurrency(won))} />
        <div className="h-full bg-primary/70" style={{ width: `${projectedShare}%` }} title={t('commercialForecast.projectedTitle').replace('{0}', formatCurrency(projected))} />
      </div>

      <div className="mt-2 grid grid-cols-3 gap-2 text-[11px]">
        <Stat label={t('commercialForecast.statWon')} value={formatCurrency(won)} tone="emerald" icon={<CheckCircle2 className="h-3 w-3" />} />
        <Stat label={t('commercialForecast.statWeighted')} value={formatCurrency(projected)} tone="primary" icon={<Hourglass className="h-3 w-3" />} hint={t('commercialForecast.statWeightedHint').replace('{0}', formatCurrency(forecast.unweightedTotal))} />
        <Stat label={t('commercialForecast.statLost')} value={formatCurrency(lost)} tone="rose" icon={<XCircle className="h-3 w-3" />} />
      </div>
    </div>
  )
}

function Stat({ label, value, tone, icon, hint }: { label: string; value: string; tone: 'emerald' | 'primary' | 'rose'; icon: React.ReactNode; hint?: string }) {
  const toneClass = tone === 'emerald' ? 'text-emerald-700' : tone === 'rose' ? 'text-rose-700' : 'text-primary'
  return (
    <div className="rounded-md border border-border/60 bg-muted/30 px-2 py-1.5">
      <div className={`flex items-center gap-1 text-[10px] font-bold uppercase tracking-wider ${toneClass}`}>
        {icon} {label}
      </div>
      <p className="mt-0.5 truncate font-mono text-[12.5px] font-semibold text-foreground">{value}</p>
      {hint && <p className="truncate text-[10.5px] text-muted-foreground">{hint}</p>}
    </div>
  )
}
