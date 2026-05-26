import { useEffect, useState } from 'react'
import { useI18n } from 'archon-ui'
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
  const { t } = useI18n()
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
        {t('commercialGoalsWidget.loading')}
      </div>
    )
  }

  if (!items || items.length === 0) {
    return (
      <div className="flex flex-wrap items-center gap-3 rounded-lg border border-dashed border-border bg-muted/20 px-4 py-3 text-sm text-muted-foreground">
        <Target className="h-4 w-4" />
        <span>{t('commercialGoalsWidget.empty')}</span>
        {onEmptyManage && (
          <button type="button" onClick={onEmptyManage} className="ml-auto text-xs font-semibold text-primary hover:underline">
            {t('commercialGoalsWidget.defineGoal')}
          </button>
        )}
      </div>
    )
  }

  return (
    <div className="grid gap-2 sm:grid-cols-2">
      {items.map((goal) => (
        <GoalCard key={goal.id} goal={goal} />
      ))}
    </div>
  )
}

function GoalCard({ goal }: { goal: CommercialGoalProgress }) {
  const { t } = useI18n()
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
        {isAgencyGoal ? t('commercialGoalsWidget.agency') : goal.userName ?? t('commercialGoalsWidget.seller')}
        <span className="ml-auto font-medium normal-case tracking-normal text-muted-foreground">
          {commercialGoalPeriodTypeLabels[goal.periodType as 1 | 2 | 3] ?? t('commercialGoalsWidget.period')} · {formatDate(goal.periodStart)}
        </span>
      </div>
      <div className="mt-2 flex flex-wrap items-baseline gap-x-2 gap-y-0.5">
        <span className="text-base font-semibold tracking-tight text-foreground">{formatCurrency(goal.achievedAmount)}</span>
        <span className="text-xs text-muted-foreground">{t('commercialGoalsWidget.ofTarget').replace('{0}', formatCurrency(goal.targetAmount))}</span>
      </div>
      <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-muted">
        <div className={`h-full ${toneClasses[tone].bar} transition-all`} style={{ width: `${percent}%` }} />
      </div>
      <div className="mt-1.5 flex items-center justify-between text-[11px]">
        <span className={`font-semibold ${toneClasses[tone].text}`}>{goal.percentAchieved.toFixed(0)}%</span>
        <span className="text-muted-foreground">{goal.achievedDealsCount === 1 ? t('commercialGoalsWidget.deals.one') : t('commercialGoalsWidget.deals.many').replace('{0}', String(goal.achievedDealsCount))}{overshoot ? ` · ${t('commercialGoalsWidget.goalReached')}` : ''}</span>
      </div>
    </div>
  )
}
