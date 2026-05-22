import { useEffect, useState } from 'react'
import { Target, TrendingUp, Users } from 'lucide-react'
import { commercialGoalService } from '../../../services/commercialGoalService'
import { commercialGoalPeriodTypeLabels, type CommercialGoalProgress } from '../../../types/commercialGoal'
import { formatCurrency, formatDate } from '../../../lib/format'

interface CommercialGoalsWidgetProps {
  scope?: 'all' | 'mine'
  userId?: number
  onEmptyManage?: () => void
}

export default function CommercialGoalsWidget({ scope = 'all', userId, onEmptyManage }: CommercialGoalsWidgetProps) {
  const [items, setItems] = useState<CommercialGoalProgress[] | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    commercialGoalService.progress(scope === 'mine' && userId ? { userId, periodType: 1 } : { periodType: 1 })
      .then((data) => { if (!cancelled) setItems(data) })
      .catch(() => { if (!cancelled) setItems([]) })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [scope, userId])

  if (loading) {
    return (
      <div className="rounded-lg border border-dashed border-border bg-muted/20 px-4 py-3 text-sm text-muted-foreground">
        Carregando metas…
      </div>
    )
  }

  if (!items || items.length === 0) {
    return (
      <div className="flex flex-wrap items-center gap-3 rounded-lg border border-dashed border-border bg-muted/20 px-4 py-3 text-sm text-muted-foreground">
        <Target className="h-4 w-4" />
        <span>Sem meta para o período atual.</span>
        {onEmptyManage && (
          <button type="button" onClick={onEmptyManage} className="ml-auto text-xs font-semibold text-primary hover:underline">
            Definir meta →
          </button>
        )}
      </div>
    )
  }

  return (
    <div className="grid gap-2 md:grid-cols-2 lg:grid-cols-3">
      {items.map((goal) => (
        <GoalCard key={goal.id} goal={goal} />
      ))}
    </div>
  )
}

function GoalCard({ goal }: { goal: CommercialGoalProgress }) {
  const percent = Math.min(goal.percentAchieved, 100)
  const overshoot = goal.percentAchieved > 100
  const isAgencyGoal = !goal.userId
  const tone = goal.percentAchieved >= 100 ? 'emerald' : goal.percentAchieved >= 70 ? 'amber' : 'rose'
  const toneClasses: Record<typeof tone, { bar: string; text: string }> = {
    emerald: { bar: 'bg-emerald-500', text: 'text-emerald-700' },
    amber: { bar: 'bg-amber-500', text: 'text-amber-700' },
    rose: { bar: 'bg-rose-500', text: 'text-rose-700' },
  }

  return (
    <div className="rounded-lg border border-border bg-card p-3">
      <div className="flex items-center gap-2 text-[11px] font-bold uppercase tracking-wider text-muted-foreground">
        {isAgencyGoal ? <Users className="h-3 w-3" /> : <TrendingUp className="h-3 w-3" />}
        {isAgencyGoal ? 'Agência' : goal.userName ?? 'Vendedor'}
        <span className="ml-auto font-medium normal-case tracking-normal text-muted-foreground">
          {commercialGoalPeriodTypeLabels[goal.periodType as 1 | 2 | 3] ?? 'Período'} · {formatDate(goal.periodStart)}
        </span>
      </div>
      <div className="mt-2 flex items-baseline justify-between gap-2">
        <span className="font-mono text-base font-semibold text-foreground">{formatCurrency(goal.achievedAmount)}</span>
        <span className="text-xs text-muted-foreground">de <span className="font-mono">{formatCurrency(goal.targetAmount)}</span></span>
      </div>
      <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-muted">
        <div className={`h-full ${toneClasses[tone].bar} transition-all`} style={{ width: `${percent}%` }} />
      </div>
      <div className="mt-1.5 flex items-center justify-between text-[11px]">
        <span className={`font-semibold ${toneClasses[tone].text}`}>{goal.percentAchieved.toFixed(0)}%</span>
        <span className="text-muted-foreground">{goal.achievedDealsCount} negócio{goal.achievedDealsCount === 1 ? '' : 's'}{overshoot ? ' · meta batida' : ''}</span>
      </div>
    </div>
  )
}
