import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ArrowRight, CalendarClock, Flame, ListChecks } from 'lucide-react'
import { opportunityService } from '../../../services/opportunityService'
import type { CommercialOpportunityInsights, UpcomingClosingItem, AtRiskItem } from '../../../types/commercialInsights'
import { formatCurrency } from '../../../lib/format'

interface Props {
  scope?: 'all' | 'mine'
  onNavigate?: () => void
}

export default function CommercialInsightsLists({ scope = 'all', onNavigate }: Props) {
  const navigate = useNavigate()
  const [data, setData] = useState<CommercialOpportunityInsights | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    opportunityService.getInsights(scope, { agingThresholdDays: 14, take: 5 })
      .then((result) => { if (!cancelled) setData(result) })
      .catch(() => { if (!cancelled) setData(null) })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [scope])

  const goTo = (id: number) => {
    onNavigate?.()
    navigate(`/comercial/oportunidades/${id}`)
  }

  if (loading) {
    return (
      <div className="rounded-lg border border-dashed border-border bg-muted/20 px-4 py-6 text-center text-sm text-muted-foreground">Carregando insights…</div>
    )
  }

  return (
    <div className="space-y-4">
      <UpcomingSection items={data?.upcomingClosings ?? []} onClick={goTo} />
      <AtRiskSection items={data?.atRisk ?? []} onClick={goTo} />
    </div>
  )
}

function UpcomingSection({ items, onClick }: { items: UpcomingClosingItem[]; onClick: (id: number) => void }) {
  return (
    <div className="rounded-lg border border-border bg-card">
      <div className="flex items-center gap-2 border-b border-border/60 px-3 py-2">
        <CalendarClock className="h-3.5 w-3.5 text-primary" />
        <h4 className="text-sm font-semibold text-foreground">Próximos fechamentos</h4>
        <span className="ml-auto text-[11px] text-muted-foreground">top {items.length}</span>
      </div>
      {items.length === 0 ? (
        <p className="px-3 py-4 text-center text-xs text-muted-foreground">Nenhuma oportunidade com data de fechamento prevista.</p>
      ) : (
        <ul className="divide-y divide-border/60">
          {items.map((item) => {
            const dayTone = item.daysUntilClose <= 3 ? 'text-rose-700 bg-rose-100' : item.daysUntilClose <= 7 ? 'text-amber-800 bg-amber-100' : 'text-emerald-800 bg-emerald-100'
            return (
              <li key={item.id}>
                <button type="button" onClick={() => onClick(item.id)} className="group flex w-full items-center gap-3 px-3 py-2 text-left transition-colors hover:bg-muted/40">
                  <span className={`flex h-9 w-12 shrink-0 flex-col items-center justify-center rounded-md ${dayTone}`}>
                    <span className="text-sm font-bold leading-none">{item.daysUntilClose}</span>
                    <span className="text-[9px] font-semibold uppercase tracking-wider opacity-80">{item.daysUntilClose === 1 ? 'dia' : 'dias'}</span>
                  </span>
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-semibold text-foreground">{item.name}</p>
                    <p className="truncate text-[11px] text-muted-foreground">
                      {item.brandName ?? 'Sem marca'} · <span className="font-mono">{formatCurrency(item.estimatedValue)}</span> · {item.probability.toFixed(0)}%
                    </p>
                  </div>
                  <ArrowRight className="h-3.5 w-3.5 text-muted-foreground/50 transition-colors group-hover:text-primary" />
                </button>
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}

function AtRiskSection({ items, onClick }: { items: AtRiskItem[]; onClick: (id: number) => void }) {
  return (
    <div className="rounded-lg border border-border bg-card">
      <div className="flex items-center gap-2 border-b border-border/60 px-3 py-2">
        <Flame className="h-3.5 w-3.5 text-amber-600" />
        <h4 className="text-sm font-semibold text-foreground">Em risco</h4>
        <span className="ml-auto text-[11px] text-muted-foreground">paradas &gt; 14 dias</span>
      </div>
      {items.length === 0 ? (
        <p className="flex items-center justify-center gap-1.5 px-3 py-4 text-center text-xs text-muted-foreground">
          <ListChecks className="h-3.5 w-3.5 text-emerald-600" />
          Nenhuma oportunidade parada além do limite.
        </p>
      ) : (
        <ul className="divide-y divide-border/60">
          {items.map((item) => (
            <li key={item.id}>
              <button type="button" onClick={() => onClick(item.id)} className="group flex w-full items-center gap-3 px-3 py-2 text-left transition-colors hover:bg-muted/40">
                <span className="flex h-9 w-12 shrink-0 flex-col items-center justify-center rounded-md bg-amber-100 text-amber-800">
                  <span className="text-sm font-bold leading-none">{item.daysInStage}</span>
                  <span className="text-[9px] font-semibold uppercase tracking-wider opacity-80">dias</span>
                </span>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-semibold text-foreground">{item.name}</p>
                  <p className="flex items-center gap-1.5 truncate text-[11px] text-muted-foreground">
                    <span className="inline-block h-1.5 w-1.5 rounded-full" style={{ backgroundColor: item.stageColor ?? 'hsl(var(--muted-foreground))' }} />
                    {item.stageName} · {item.brandName ?? 'Sem marca'} · <span className="font-mono">{formatCurrency(item.estimatedValue)}</span>
                  </p>
                </div>
                <ArrowRight className="h-3.5 w-3.5 text-muted-foreground/50 transition-colors group-hover:text-primary" />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
