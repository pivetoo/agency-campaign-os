import { useEffect, useMemo, useState } from 'react'
import { PageLayout, useApi, useI18n, usePermissions } from 'archon-ui'
import { Activity, Award, Hourglass, Percent, ThumbsDown, ThumbsUp, TrendingDown, TrendingUp } from 'lucide-react'
import { ResponsivePie } from '@nivo/pie'
import { opportunityService } from '../../../services/opportunityService'
import type { CommercialAnalytics, ReasonAggregate, StageConversion } from '../../../types/commercialAnalytics'
import { formatCurrency } from '../../../lib/format'
import { todayDateInput } from '../../../lib/format'

function startOfMonth(): string {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-01`
}

function endOfMonth(): string {
  const d = new Date()
  const last = new Date(d.getFullYear(), d.getMonth() + 1, 0)
  return `${last.getFullYear()}-${String(last.getMonth() + 1).padStart(2, '0')}-${String(last.getDate()).padStart(2, '0')}`
}

export default function CommercialAnalytics() {
  const { t } = useI18n()
  const { hasAnyPermission } = usePermissions()
  const canSeeAll = hasAnyPermission(['opportunities.analytics'])
  const [periodStart, setPeriodStart] = useState<string>(startOfMonth())
  const [periodEnd, setPeriodEnd] = useState<string>(endOfMonth())
  const [analytics, setAnalytics] = useState<CommercialAnalytics | null>(null)
  const { execute: load, loading } = useApi<CommercialAnalytics | null>({ showErrorMessage: true })

  const fetchAnalytics = async () => {
    const startIso = `${periodStart}T00:00:00.000Z`
    const endIso = `${periodEnd}T23:59:59.000Z`
    const data = await load(() => (canSeeAll
      ? opportunityService.getAnalytics({ periodStart: startIso, periodEnd: endIso })
      : opportunityService.getAnalyticsMine({ periodStart: startIso, periodEnd: endIso })))
    setAnalytics(data ?? null)
  }

  useEffect(() => {
    void fetchAnalytics()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [periodStart, periodEnd, canSeeAll])

  return (
    <PageLayout title={t('commercialAnalytics.title')} subtitle={t('commercialAnalytics.subtitle')} showDefaultActions={false}>
      <div className="space-y-5">
        <div className="flex flex-wrap items-center gap-2 rounded-lg border border-border bg-card px-4 py-3">
          <label className="flex items-center gap-2 text-xs text-muted-foreground">
            <span>{t('commercialAnalytics.from')}</span>
            <input type="date" value={periodStart} onChange={(e) => setPeriodStart(e.target.value || todayDateInput())} className="rounded-md border border-input bg-background px-2 py-1 text-sm" />
          </label>
          <label className="flex items-center gap-2 text-xs text-muted-foreground">
            <span>{t('commercialAnalytics.to')}</span>
            <input type="date" value={periodEnd} onChange={(e) => setPeriodEnd(e.target.value || todayDateInput())} className="rounded-md border border-input bg-background px-2 py-1 text-sm" />
          </label>
        </div>

        {!loading && analytics && analytics.closedCount === 0 && analytics.conversionByStage.every((s) => s.entered === 0) && (
          <div className="rounded-lg border border-dashed border-border bg-muted/20 px-6 py-12 text-center text-sm text-muted-foreground">{t('commercialAnalytics.emptyPeriod')}</div>
        )}

        {analytics && (
          <>
            <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
              <KpiCard icon={<Award className="h-4 w-4 text-emerald-600" />} label={t('commercialAnalytics.kpiClosed')} value={String(analytics.closedCount)} sub={t('commercialAnalytics.kpiClosedSub').replace('{0}', String(analytics.wonCount)).replace('{1}', String(analytics.lostCount))} />
              <KpiCard icon={<Percent className="h-4 w-4 text-primary" />} label={t('commercialAnalytics.kpiWinRate')} value={`${analytics.winRate.toFixed(1)}%`} sub={analytics.closedCount === 0 ? t('commercialAnalytics.kpiWinRateNone') : t('commercialAnalytics.kpiWinRateSub')} />
              <KpiCard icon={<Hourglass className="h-4 w-4 text-amber-600" />} label={t('commercialAnalytics.kpiCycle')} value={t('commercialAnalytics.daysValue').replace('{0}', analytics.averageCycleDays.toFixed(1))} sub={t('commercialAnalytics.kpiCycleSub')} />
              <KpiCard icon={<Activity className="h-4 w-4 text-rose-600" />} label={t('commercialAnalytics.kpiInProgress')} value={String(analytics.conversionByStage.reduce((acc, s) => acc + s.stuck, 0))} sub={t('commercialAnalytics.kpiInProgressSub')} />
            </div>

            <Section title={t('commercialAnalytics.conversionByStage')}>
              <ConversionTable items={analytics.conversionByStage} />
            </Section>

            <div className="grid gap-5 lg:grid-cols-2">
              <Section title={t('commercialAnalytics.avgTimeByStage')}>
                <TimeBars items={analytics.averageTimeInStage} />
              </Section>
              <Section title={t('commercialAnalytics.topPerformers')}>
                <PerformersList items={analytics.topPerformers} />
              </Section>
            </div>

            <div className="grid gap-5 lg:grid-cols-2">
              <Section title={t('commercialAnalytics.winReasons')} icon={<ThumbsUp className="h-4 w-4 text-emerald-600" />}>
                <ReasonsBlock items={analytics.winReasons} emptyLabel={t('commercialAnalytics.noWins')} />
              </Section>
              <Section title={t('commercialAnalytics.lossReasons')} icon={<ThumbsDown className="h-4 w-4 text-rose-600" />}>
                <ReasonsBlock items={analytics.lossReasons} emptyLabel={t('commercialAnalytics.noLosses')} />
              </Section>
            </div>
          </>
        )}
      </div>
    </PageLayout>
  )
}

function KpiCard({ icon, label, value, sub }: { icon: React.ReactNode; label: string; value: string; sub?: string }) {
  return (
    <div className="rounded-lg border border-border bg-card p-4">
      <div className="flex items-center gap-1.5 text-[11px] font-bold uppercase tracking-wider text-muted-foreground">
        {icon} {label}
      </div>
      <p className="mt-1 truncate text-lg font-semibold text-foreground">{value}</p>
      {sub && <p className="mt-0.5 text-[11px] text-muted-foreground">{sub}</p>}
    </div>
  )
}

function Section({ title, icon, children }: { title: string; icon?: React.ReactNode; children: React.ReactNode }) {
  return (
    <div className="rounded-lg border border-border bg-card p-4">
      <h3 className="mb-3 flex items-center gap-2 text-sm font-semibold text-foreground">
        {icon} {title}
      </h3>
      {children}
    </div>
  )
}

function ConversionTable({ items }: { items: StageConversion[] }) {
  const { t } = useI18n()
  if (items.length === 0) {
    return <p className="text-xs text-muted-foreground">{t('commercialAnalytics.noData')}</p>
  }
  const maxEntered = Math.max(1, ...items.map((item) => item.entered))
  return (
    <div className="space-y-2">
      {items.map((item) => {
        const ratio = item.entered / maxEntered
        const advancedPct = item.entered > 0 ? (item.advanced / item.entered) * 100 : 0
        const stuckPct = item.entered > 0 ? (item.stuck / item.entered) * 100 : 0
        const lostPct = item.entered > 0 ? (item.lost / item.entered) * 100 : 0
        return (
          <div key={item.stageId} className="rounded-md border border-border/60 bg-muted/20 px-3 py-2">
            <div className="flex items-baseline justify-between gap-2 text-sm">
              <span className="flex items-center gap-2 font-semibold text-foreground">
                <span className="inline-block h-2 w-2 rounded-full" style={{ backgroundColor: item.stageColor ?? 'hsl(var(--primary))' }} />
                {item.stageName}
              </span>
              <span className="text-xs text-muted-foreground">{t('commercialAnalytics.entered').replace('{0}', String(item.entered))} · <strong className="text-foreground">{item.conversionRate.toFixed(0)}%</strong> {t('commercialAnalytics.advancedSuffix')}</span>
            </div>
            <div className="mt-1.5 h-2 overflow-hidden rounded-full bg-muted">
              <div className="h-full bg-primary/50" style={{ width: `${ratio * 100}%` }} />
            </div>
            <div className="mt-1.5 grid grid-cols-3 gap-1 text-[10.5px]">
              <span className="text-emerald-700">{t('commercialAnalytics.advancedCount').replace('{0}', String(item.advanced)).replace('{1}', advancedPct.toFixed(0))}</span>
              <span className="text-amber-700">{t('commercialAnalytics.stuckCount').replace('{0}', String(item.stuck)).replace('{1}', stuckPct.toFixed(0))}</span>
              <span className="text-rose-700">{t('commercialAnalytics.lostCount').replace('{0}', String(item.lost)).replace('{1}', lostPct.toFixed(0))}</span>
            </div>
          </div>
        )
      })}
    </div>
  )
}

function TimeBars({ items }: { items: { stageId: number; stageName: string; stageColor?: string; averageDays: number; samples: number }[] }) {
  const { t } = useI18n()
  if (items.length === 0) {
    return <p className="text-xs text-muted-foreground">{t('commercialAnalytics.noData')}</p>
  }
  const max = Math.max(1, ...items.map((item) => item.averageDays))
  return (
    <div className="space-y-2">
      {items.map((item) => (
        <div key={item.stageId}>
          <div className="flex items-baseline justify-between text-xs">
            <span className="flex items-center gap-2 text-foreground">
              <span className="inline-block h-2 w-2 rounded-full" style={{ backgroundColor: item.stageColor ?? 'hsl(var(--primary))' }} />
              {item.stageName}
            </span>
            <span className="text-muted-foreground">
              <strong className="text-foreground">{item.averageDays.toFixed(1)}</strong> {item.samples === 1 ? t('commercialAnalytics.daysSamples.one') : t('commercialAnalytics.daysSamples.many').replace('{0}', String(item.samples))}
            </span>
          </div>
          <div className="mt-1 h-1.5 overflow-hidden rounded-full bg-muted">
            <div className="h-full bg-amber-500" style={{ width: `${Math.min(100, (item.averageDays / max) * 100)}%` }} />
          </div>
        </div>
      ))}
    </div>
  )
}

function PerformersList({ items }: { items: { userId?: number; userName: string; wonCount: number; wonTotal: number }[] }) {
  const { t } = useI18n()
  if (items.length === 0) {
    return <p className="text-xs text-muted-foreground">{t('commercialAnalytics.noData')}</p>
  }
  const top = items[0]?.wonTotal ?? 1
  return (
    <div className="space-y-2">
      {items.map((item, index) => (
        <div key={`${item.userId ?? 'none'}-${index}`} className="flex items-center gap-3 rounded-md border border-border/60 bg-muted/20 px-3 py-2">
          <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-primary/10 text-xs font-bold text-primary">{index + 1}</span>
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-semibold text-foreground">{item.userName}</p>
            <p className="text-[11px] text-muted-foreground">{item.wonCount === 1 ? t('commercialAnalytics.deals.one') : t('commercialAnalytics.deals.many').replace('{0}', String(item.wonCount))}</p>
          </div>
          <div className="text-right">
            <p className="font-mono text-sm font-semibold text-foreground">{formatCurrency(item.wonTotal)}</p>
            <div className="mt-1 h-1 w-24 overflow-hidden rounded-full bg-muted">
              <div className="h-full bg-emerald-500" style={{ width: `${Math.min(100, (item.wonTotal / top) * 100)}%` }} />
            </div>
          </div>
        </div>
      ))}
    </div>
  )
}

function ReasonsBlock({ items, emptyLabel }: { items: ReasonAggregate[]; emptyLabel: string }) {
  const { t } = useI18n()
  const data = useMemo(() => items.map((item) => ({
    id: `${item.reasonId ?? 'none'}-${item.reasonName}`,
    label: item.reasonName,
    value: item.count,
    color: item.reasonColor ?? '#94A3B8',
  })), [items])

  if (items.length === 0) {
    return <p className="text-xs text-muted-foreground">{emptyLabel}</p>
  }
  const totalValue = items.reduce((acc, item) => acc + item.totalValue, 0)
  return (
    <div className="grid gap-3 md:grid-cols-[160px_1fr]">
      <div className="h-40">
        <ResponsivePie
          data={data}
          margin={{ top: 6, right: 6, bottom: 6, left: 6 }}
          innerRadius={0.5}
          padAngle={1}
          cornerRadius={2}
          activeOuterRadiusOffset={4}
          colors={(d) => (d.data as { color: string }).color}
          enableArcLabels={false}
          enableArcLinkLabels={false}
        />
      </div>
      <div className="space-y-1.5">
        {items.map((item) => {
          const share = totalValue > 0 ? (item.totalValue / totalValue) * 100 : 0
          return (
            <div key={`${item.reasonId ?? 'none'}-${item.reasonName}`} className="rounded-md border border-border/60 bg-muted/20 px-2.5 py-1.5">
              <div className="flex items-center justify-between gap-2 text-xs">
                <span className="flex items-center gap-2 truncate text-foreground">
                  <span className="inline-block h-2 w-2 rounded-full" style={{ backgroundColor: item.reasonColor ?? '#94A3B8' }} />
                  {item.reasonName}
                </span>
                <span className="font-mono text-muted-foreground">{item.count}</span>
              </div>
              <div className="mt-1 flex items-center justify-between text-[11px] text-muted-foreground">
                <span>{t('commercialAnalytics.shareOfValue').replace('{0}', share.toFixed(0))}</span>
                <span className="font-mono">{formatCurrency(item.totalValue)}</span>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}

// unused trend icons — kept for design alignment
void TrendingUp
void TrendingDown
