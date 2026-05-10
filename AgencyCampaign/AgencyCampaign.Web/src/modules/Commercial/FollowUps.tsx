import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Badge, Button, PageLayout, useApi } from 'archon-ui'
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

function relativeLabel(dueAt: string, isCompleted: boolean): { label: string; tone: 'overdue' | 'today' | 'upcoming' | 'completed' } {
  if (isCompleted) return { label: 'Concluído', tone: 'completed' }

  const due = new Date(dueAt)
  const now = new Date()
  const dueStart = new Date(due); dueStart.setHours(0, 0, 0, 0)
  const todayStart = new Date(now); todayStart.setHours(0, 0, 0, 0)

  const diffMs = dueStart.getTime() - todayStart.getTime()
  const diffDays = Math.round(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays < 0) {
    const days = Math.abs(diffDays)
    return { label: days === 1 ? 'Atrasado 1 dia' : `Atrasado ${days} dias`, tone: 'overdue' }
  }
  if (diffDays === 0) return { label: 'Hoje', tone: 'today' }
  if (diffDays === 1) return { label: 'Amanhã', tone: 'upcoming' }
  return { label: `Em ${diffDays} dias · ${formatDateBR(dueAt)}`, tone: 'upcoming' }
}

const TONE_BADGE: Record<'overdue' | 'today' | 'upcoming' | 'completed', { variant: 'destructive' | 'warning' | 'secondary' | 'success'; className: string }> = {
  overdue: { variant: 'destructive', className: '' },
  today: { variant: 'warning', className: '' },
  upcoming: { variant: 'secondary', className: '' },
  completed: { variant: 'success', className: '' },
}

const STATUS_TABS: Array<{ key: StatusKey; label: string; tone: 'destructive' | 'primary' | 'primary' | 'success' }> = [
  { key: 'overdue', label: 'Atrasadas', tone: 'destructive' },
  { key: 'today', label: 'Hoje', tone: 'primary' },
  { key: 'upcoming', label: 'Próximas', tone: 'primary' },
  { key: 'completed', label: 'Concluídas', tone: 'success' },
]

const EMPTY_STATES: Record<StatusKey, { icon: typeof Clock; title: string; subtitle: string }> = {
  overdue: {
    icon: CheckCircle2,
    title: 'Sem atividades atrasadas',
    subtitle: 'Bom trabalho! Você está em dia com tudo.',
  },
  today: {
    icon: Sparkles,
    title: 'Nada pra hoje',
    subtitle: 'Aproveita pra adiantar atividades futuras.',
  },
  upcoming: {
    icon: Clock,
    title: 'Sem próximas atividades',
    subtitle: 'Crie follow-ups dentro das oportunidades pra não perder o ritmo.',
  },
  completed: {
    icon: ClipboardCheck,
    title: 'Nenhuma atividade concluída ainda',
    subtitle: 'Quando marcar como concluída, ela aparece aqui.',
  },
}

export default function CommercialFollowUps() {
  const navigate = useNavigate()
  const [opportunities, setOpportunities] = useState<Opportunity[]>([])
  const [selectedStatus, setSelectedStatus] = useState<StatusKey>('overdue')
  const [completingId, setCompletingId] = useState<number | null>(null)
  const { execute: fetchOpportunities, loading } = useApi<Opportunity[]>({ showErrorMessage: true })
  const { execute: executeAction } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const loadData = async () => {
    const result = await fetchOpportunities(() => opportunityService.getAll())
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
      title="Atividades"
      subtitle="Agenda do comercial — atrasadas, de hoje, próximas e concluídas"
      onRefresh={() => void loadData()}
      showDefaultActions={false}
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
                <div className="text-sm text-muted-foreground">{tab.label}</div>
                <div className={`text-2xl font-bold ${valueClass}`}>{count}</div>
              </button>
            )
          })}
        </div>

        {loading ? (
          <div className="flex items-center justify-center rounded-xl border border-dashed py-16 text-sm text-muted-foreground">
            <Loader2 className="mr-2 h-4 w-4 animate-spin" /> Carregando atividades...
          </div>
        ) : visibleActivities.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-16 text-center text-muted-foreground">
            <EmptyIcon className="mb-3 h-10 w-10 opacity-50" />
            <p className="text-sm font-medium text-foreground">{emptyState.title}</p>
            <p className="mt-1 text-xs">{emptyState.subtitle}</p>
          </div>
        ) : (
          <ul className="space-y-2">
            {visibleActivities.map((activity) => {
              const rel = relativeLabel(activity.dueAt, activity.isCompleted)
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
                    title={activity.isCompleted ? 'Já concluída' : 'Marcar como concluída'}
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
                    title="Abrir oportunidade"
                  >
                    Abrir <ArrowRight className="ml-1 h-3.5 w-3.5" />
                  </Button>
                </li>
              )
            })}
          </ul>
        )}

        <div className="flex justify-end">
          <Button variant="outline" size="sm" onClick={() => navigate('/comercial/pipeline')}>
            <ExternalLink className="mr-1.5 h-3.5 w-3.5" /> Ir para o pipeline
          </Button>
        </div>
      </div>
    </PageLayout>
  )
}
