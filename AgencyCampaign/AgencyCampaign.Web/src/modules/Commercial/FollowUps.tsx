import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Badge, Button, PageLayout, useApi, useI18n } from 'archon-ui'
import { ArrowRight, Building2, Check, CheckCircle2, ClipboardCheck, Clock, ExternalLink, Loader2, Sparkles } from 'lucide-react'
import { opportunityService, type Opportunity, type OpportunityFollowUp } from '../../services/opportunityService'

type StatusKey = 'overdue' | 'today' | 'upcoming' | 'completed'

interface FollowUpRow extends OpportunityFollowUp {
  opportunityId: number
  opportunityName: string
  brandName: string
  estimatedValue: number
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
    maximumFractionDigits: 0,
  }).format(value)
}

function formatDateBR(value: string) {
  return new Date(value).toLocaleDateString('pt-BR')
}

function relativeLabel(dueAt: string, isCompleted: boolean, t: (key: string) => string): { label: string; tone: 'overdue' | 'today' | 'upcoming' | 'completed' } {
  if (isCompleted) return { label: t('followups.relative.completed'), tone: 'completed' }

  const due = new Date(dueAt)
  const now = new Date()
  const dueStart = new Date(due); dueStart.setHours(0, 0, 0, 0)
  const todayStart = new Date(now); todayStart.setHours(0, 0, 0, 0)

  const diffMs = dueStart.getTime() - todayStart.getTime()
  const diffDays = Math.round(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays < 0) {
    const days = Math.abs(diffDays)
    return { label: days === 1 ? t('followups.relative.overdueOne') : t('followups.relative.overdueMany').replace('{0}', String(days)), tone: 'overdue' }
  }
  if (diffDays === 0) return { label: t('followups.relative.today'), tone: 'today' }
  if (diffDays === 1) return { label: t('followups.relative.tomorrow'), tone: 'upcoming' }
  return { label: t('followups.relative.inDays').replace('{0}', String(diffDays)).replace('{1}', formatDateBR(dueAt)), tone: 'upcoming' }
}

const TONE_BADGE: Record<'overdue' | 'today' | 'upcoming' | 'completed', { variant: 'destructive' | 'warning' | 'secondary' | 'success'; className: string }> = {
  overdue: { variant: 'destructive', className: '' },
  today: { variant: 'warning', className: '' },
  upcoming: { variant: 'secondary', className: '' },
  completed: { variant: 'success', className: '' },
}

const STATUS_TABS: Array<{ key: StatusKey; labelKey: string; tone: 'destructive' | 'primary' | 'primary' | 'success' }> = [
  { key: 'overdue', labelKey: 'followups.tab.overdue', tone: 'destructive' },
  { key: 'today', labelKey: 'followups.tab.today', tone: 'primary' },
  { key: 'upcoming', labelKey: 'followups.tab.upcoming', tone: 'primary' },
  { key: 'completed', labelKey: 'followups.tab.completed', tone: 'success' },
]

const EMPTY_STATES: Record<StatusKey, { icon: typeof Clock; titleKey: string; subtitleKey: string }> = {
  overdue: {
    icon: CheckCircle2,
    titleKey: 'followups.empty.overdue.title',
    subtitleKey: 'followups.empty.overdue.subtitle',
  },
  today: {
    icon: Sparkles,
    titleKey: 'followups.empty.today.title',
    subtitleKey: 'followups.empty.today.subtitle',
  },
  upcoming: {
    icon: Clock,
    titleKey: 'followups.empty.upcoming.title',
    subtitleKey: 'followups.empty.upcoming.subtitle',
  },
  completed: {
    icon: ClipboardCheck,
    titleKey: 'followups.empty.completed.title',
    subtitleKey: 'followups.empty.completed.subtitle',
  },
}

export default function CommercialFollowUps() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [opportunities, setOpportunities] = useState<Opportunity[]>([])
  const [selectedStatus, setSelectedStatus] = useState<StatusKey>('overdue')
  const [completingId, setCompletingId] = useState<number | null>(null)
  const { execute: fetchOpportunities, loading } = useApi<Opportunity[]>({ showErrorMessage: true })
  const { execute: executeAction } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const loadData = async () => {
    const result = await fetchOpportunities(() => opportunityService.getAll({ pageSize: 200 }))
    if (result) setOpportunities(result)
  }

  useEffect(() => {
    void loadData()
  }, [])

  const followUps = useMemo<FollowUpRow[]>(() => (
    opportunities.flatMap((opportunity) =>
      opportunity.followUps.map((followUp) => ({
        ...followUp,
        opportunityId: opportunity.id,
        opportunityName: opportunity.name,
        brandName: opportunity.brand?.name || '—',
        estimatedValue: opportunity.estimatedValue ?? 0,
      })),
    ).sort((a, b) => new Date(a.dueAt).getTime() - new Date(b.dueAt).getTime())
  ), [opportunities])

  const buckets = useMemo(() => {
    const today = new Date(); today.setHours(0, 0, 0, 0)
    const tomorrow = new Date(today); tomorrow.setDate(today.getDate() + 1)

    return {
      overdue: followUps.filter((item) => !item.isCompleted && new Date(item.dueAt) < today),
      today: followUps.filter((item) => !item.isCompleted && new Date(item.dueAt) >= today && new Date(item.dueAt) < tomorrow),
      upcoming: followUps.filter((item) => !item.isCompleted && new Date(item.dueAt) >= tomorrow),
      completed: followUps.filter((item) => item.isCompleted),
    }
  }, [followUps])

  const visibleActivities = buckets[selectedStatus]
  const emptyState = EMPTY_STATES[selectedStatus]
  const EmptyIcon = emptyState.icon

  const completeActivity = async (activity: FollowUpRow) => {
    setCompletingId(activity.id)
    try {
      const result = await executeAction(() => opportunityService.completeFollowUp(activity.id))
      if (result !== null) await loadData()
    } finally {
      setCompletingId(null)
    }
  }

  return (
    <PageLayout
      title={t('followups.title')}
      subtitle={t('followups.subtitle')}
      onRefresh={() => void loadData()}
      showDefaultActions={false}
      actions={[
        {
          key: 'go-pipeline',
          label: t('followups.action.goToPipeline'),
          icon: <ExternalLink className="h-4 w-4" />,
          variant: 'outline',
          onClick: () => navigate('/comercial/pipeline'),
        },
      ]}
    >
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
          {STATUS_TABS.map((tab) => {
            const count = buckets[tab.key].length
            const isActive = selectedStatus === tab.key
            const colorClass = (() => {
              if (!isActive) return 'border-border bg-card hover:border-primary/40'
              if (tab.key === 'overdue') return 'border-destructive bg-destructive/5'
              if (tab.key === 'completed') return 'border-emerald-500 bg-emerald-500/5'
              return 'border-primary bg-primary/5'
            })()
            const valueClass = (() => {
              if (tab.key === 'overdue' && count > 0) return 'text-destructive'
              if (tab.key === 'completed') return 'text-emerald-600'
              return 'text-foreground'
            })()
            return (
              <button
                key={tab.key}
                type="button"
                onClick={() => setSelectedStatus(tab.key)}
                className={`rounded-xl border p-4 text-left transition-colors ${colorClass}`}
              >
                <div className="text-sm text-muted-foreground">{t(tab.labelKey)}</div>
                <div className={`text-2xl font-bold ${valueClass}`}>{count}</div>
              </button>
            )
          })}
        </div>

        {loading ? (
          <div className="flex items-center justify-center rounded-xl border border-dashed py-16 text-sm text-muted-foreground">
            <Loader2 className="mr-2 h-4 w-4 animate-spin" /> {t('followups.loading')}
          </div>
        ) : visibleActivities.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-16 text-center text-muted-foreground">
            <EmptyIcon className="mb-3 h-10 w-10 opacity-50" />
            <p className="text-sm font-medium text-foreground">{t(emptyState.titleKey)}</p>
            <p className="mt-1 text-xs">{t(emptyState.subtitleKey)}</p>
          </div>
        ) : (
          <ul className="space-y-2">
            {visibleActivities.map((activity) => {
              const rel = relativeLabel(activity.dueAt, activity.isCompleted, t)
              const badge = TONE_BADGE[rel.tone]
              const isCompleting = completingId === activity.id
              return (
                <li
                  key={activity.id}
                  className="group flex items-start gap-3 rounded-xl border border-border bg-card p-4 transition-colors hover:border-primary/40"
                >
                  <button
                    type="button"
                    disabled={activity.isCompleted || isCompleting}
                    onClick={(event) => {
                      event.stopPropagation()
                      void completeActivity(activity)
                    }}
                    title={activity.isCompleted ? t('followups.markComplete.alreadyDone') : t('followups.markComplete.title')}
                    className={[
                      'mt-0.5 flex h-6 w-6 flex-shrink-0 items-center justify-center rounded-full border-2 transition-colors',
                      activity.isCompleted
                        ? 'border-emerald-500 bg-emerald-500 text-white'
                        : 'border-muted-foreground/40 hover:border-primary hover:bg-primary/5',
                    ].join(' ')}
                  >
                    {isCompleting ? (
                      <Loader2 className="h-3.5 w-3.5 animate-spin" />
                    ) : activity.isCompleted ? (
                      <Check className="h-3.5 w-3.5" />
                    ) : null}
                  </button>

                  <button
                    type="button"
                    onClick={() => navigate(`/comercial/oportunidades/${activity.opportunityId}`)}
                    className="min-w-0 flex-1 cursor-pointer text-left"
                  >
                    <div className="flex flex-wrap items-center gap-2">
                      <span className={`truncate text-sm font-semibold ${activity.isCompleted ? 'text-muted-foreground line-through' : 'text-foreground'}`}>
                        {activity.subject}
                      </span>
                      <Badge variant={badge.variant} className="text-[10px]">
                        {rel.label}
                      </Badge>
                    </div>
                    <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground">
                      <span className="flex items-center gap-1">
                        <Building2 className="h-3 w-3" />
                        {activity.brandName}
                      </span>
                      <span>·</span>
                      <span className="truncate">{activity.opportunityName}</span>
                      {activity.estimatedValue > 0 && (
                        <>
                          <span>·</span>
                          <span className="font-medium text-foreground">{formatCurrency(activity.estimatedValue)}</span>
                        </>
                      )}
                    </div>
                    {activity.notes && (
                      <p className="mt-2 text-xs text-muted-foreground">{activity.notes}</p>
                    )}
                  </button>

                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={(event) => {
                      event.stopPropagation()
                      navigate(`/comercial/oportunidades/${activity.opportunityId}`)
                    }}
                    className="opacity-0 transition-opacity group-hover:opacity-100"
                    title={t('followups.action.openOpportunity')}
                  >
                    {t('common.action.open')} <ArrowRight className="ml-1 h-3.5 w-3.5" />
                  </Button>
                </li>
              )
            })}
          </ul>
        )}

      </div>
    </PageLayout>
  )
}
