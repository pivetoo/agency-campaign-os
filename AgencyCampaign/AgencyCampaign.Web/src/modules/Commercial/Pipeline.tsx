import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, Card, CardContent, PageLayout, useApi } from 'archon-ui'
import { AlertTriangle, CalendarClock, DollarSign, List, Plus, RefreshCcw, UserRound } from 'lucide-react'
import { opportunityService, type OpportunityBoardItem, type OpportunityBoardStage } from '../../services/opportunityService'
import OpportunityFormModal from '../../components/modals/OpportunityFormModal'
import { resolveAssetUrl } from '../../lib/assetUrl'

function formatCurrency(value: number) {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  }).format(value)
}

function formatDate(value?: string) {
  return value ? new Date(value).toLocaleDateString('pt-BR') : 'Sem previsão'
}

function getContrastColor(hexColor: string) {
  const normalized = hexColor.replace('#', '')
  if (normalized.length !== 6) {
    return '#ffffff'
  }

  const red = parseInt(normalized.slice(0, 2), 16)
  const green = parseInt(normalized.slice(2, 4), 16)
  const blue = parseInt(normalized.slice(4, 6), 16)
  const luminance = (0.299 * red) + (0.587 * green) + (0.114 * blue)

  return luminance > 186 ? '#111827' : '#ffffff'
}

function OpportunityCard({ item, isDragging, onDragStart, onDragEnd }: { item: OpportunityBoardItem; isDragging: boolean; onDragStart: () => void; onDragEnd: () => void }) {
  const navigate = useNavigate()

  const slaClass =
    item.slaStatus === 'breached'
      ? 'border-destructive/60 bg-destructive/5'
      : item.slaStatus === 'warning'
        ? 'border-amber-400/60 bg-amber-50/50'
        : 'border-border bg-card'

  return (
    <button
      type="button"
      draggable
      onDragStart={(event) => {
        event.dataTransfer.effectAllowed = 'move'
        event.dataTransfer.setData('text/plain', item.id.toString())
        onDragStart()
      }}
      onDragEnd={onDragEnd}
      onClick={() => navigate(`/comercial/oportunidades/${item.id}`)}
      className={`w-full cursor-grab rounded-xl border p-4 text-left shadow-sm transition active:cursor-grabbing hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md ${slaClass} ${isDragging ? 'scale-[0.98] opacity-50 ring-2 ring-primary/30' : ''}`}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 items-start gap-2.5">
          {item.brandLogoUrl && (
            <img
              src={resolveAssetUrl(item.brandLogoUrl)}
              alt=""
              className="h-7 w-7 shrink-0 rounded-md border bg-card object-contain p-0.5"
              onError={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
            />
          )}
          <div className="min-w-0">
            <div className="truncate text-sm font-semibold text-foreground">{item.name}</div>
            <div className="mt-1 truncate text-xs text-muted-foreground">{item.brandName}</div>
          </div>
        </div>
        <div className="flex shrink-0 flex-col items-end gap-1">
          {item.overdueFollowUpsCount > 0 && (
            <span className="rounded-full bg-destructive px-2 py-1 text-[11px] font-semibold text-destructive-foreground">Atraso</span>
          )}
          {item.slaStatus === 'breached' && item.daysInStage != null && item.stageSlaInDays != null && (
            <span className="rounded-full bg-destructive/15 px-2 py-0.5 text-[11px] font-semibold text-destructive">
              {item.daysInStage}d / SLA {item.stageSlaInDays}d
            </span>
          )}
          {item.slaStatus === 'warning' && item.daysInStage != null && item.stageSlaInDays != null && (
            <span className="rounded-full bg-amber-100 px-2 py-0.5 text-[11px] font-semibold text-amber-700">
              {item.daysInStage}d / SLA {item.stageSlaInDays}d
            </span>
          )}
        </div>
      </div>

      <div className="mt-4 flex items-center gap-2 text-sm font-semibold text-foreground">
        <DollarSign className="h-4 w-4 text-emerald-500" />
        {formatCurrency(item.estimatedValue)}
      </div>

      <div className="mt-3 space-y-2 text-xs text-muted-foreground">
        <div className="flex items-center gap-2">
          <CalendarClock className="h-3.5 w-3.5" />
          <span>{formatDate(item.expectedCloseAt)}</span>
        </div>

        <div className="flex items-center gap-2">
          <UserRound className="h-3.5 w-3.5" />
          <span className="truncate">{item.commercialResponsibleName || 'Sem responsável'}</span>
        </div>
      </div>

      <div className="mt-4 grid grid-cols-3 gap-2 text-center text-xs">
        <div className="rounded-lg bg-muted px-2 py-2">
          <div className="font-semibold text-foreground">{item.proposalCount}</div>
          <div className="text-muted-foreground">Propostas</div>
        </div>
        <div className="rounded-lg bg-muted px-2 py-2">
          <div className="font-semibold text-foreground">{item.pendingFollowUpsCount}</div>
          <div className="text-muted-foreground">Pendentes</div>
        </div>
        <div className="rounded-lg bg-muted px-2 py-2">
          <div className={item.overdueFollowUpsCount > 0 ? 'font-semibold text-destructive' : 'font-semibold text-foreground'}>{item.overdueFollowUpsCount}</div>
          <div className="text-muted-foreground">Atrasados</div>
        </div>
      </div>
    </button>
  )
}

export default function CommercialPipeline() {
  const navigate = useNavigate()
  const [board, setBoard] = useState<OpportunityBoardStage[]>([])
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [draggedItem, setDraggedItem] = useState<OpportunityBoardItem | null>(null)
  const [dragOverStage, setDragOverStage] = useState<number | null>(null)
  const [movingOpportunityId, setMovingOpportunityId] = useState<number | null>(null)
  const { execute: fetchBoard, loading } = useApi<OpportunityBoardStage[]>({ showErrorMessage: true })

  const loadBoard = async () => {
    const result = await fetchBoard(() => opportunityService.getBoard())
    if (result) {
      setBoard(result)
    }
  }

  useEffect(() => {
    void loadBoard()
  }, [])

  const stages = useMemo(() => {
    return [...board].sort((left, right) => left.displayOrder - right.displayOrder)
  }, [board])

  const summary = useMemo(() => {
    return stages.reduce(
      (acc, stage) => {
        acc.count += stage.opportunitiesCount
        acc.value += stage.estimatedValueTotal
        acc.pendingFollowUps += stage.items.reduce((total, item) => total + item.pendingFollowUpsCount, 0)
        acc.overdueFollowUps += stage.items.reduce((total, item) => total + item.overdueFollowUpsCount, 0)
        return acc
      },
      { count: 0, value: 0, pendingFollowUps: 0, overdueFollowUps: 0 }
    )
  }, [stages])

  const moveOpportunity = async (targetStage: number) => {
    if (!draggedItem || draggedItem.commercialPipelineStageId === targetStage || movingOpportunityId) {
      setDragOverStage(null)
      return
    }

    const previousBoard = board
    const targetColumn = board.find((column) => column.commercialPipelineStageId === targetStage)
    const updatedItem = {
      ...draggedItem,
      commercialPipelineStageId: targetStage,
      commercialPipelineStageName: targetColumn?.name || draggedItem.commercialPipelineStageName,
      commercialPipelineStageColor: targetColumn?.color || draggedItem.commercialPipelineStageColor,
    }

    setMovingOpportunityId(draggedItem.id)
    setDragOverStage(null)
    setBoard((currentBoard) => {
      const boardByStage = new Map<number, OpportunityBoardItem[]>()

      currentBoard.forEach((stage) => {
        boardByStage.set(stage.commercialPipelineStageId, [])
      })

      currentBoard.forEach((stage) => {
        boardByStage.set(stage.commercialPipelineStageId, stage.items.filter((item) => item.id !== draggedItem.id))
      })

      boardByStage.set(targetStage, [updatedItem, ...(boardByStage.get(targetStage) ?? [])])

      return currentBoard.map((stage) => {
        const items = boardByStage.get(stage.commercialPipelineStageId) ?? []
        return {
          ...stage,
          opportunitiesCount: items.length,
          estimatedValueTotal: items.reduce((total, item) => total + item.estimatedValue, 0),
          items,
        }
      })
    })

    try {
      await opportunityService.changeStage(draggedItem.id, { commercialPipelineStageId: targetStage })
    } catch {
      setBoard(previousBoard)
    } finally {
      setMovingOpportunityId(null)
      setDraggedItem(null)
    }
  }

  return (
    <PageLayout
      title="Pipeline Comercial"
      subtitle="Acompanhe leads e oportunidades por estágio, valor em funil e follow-ups críticos"
      actions={[
        {
          key: 'list-view',
          label: 'Ver lista',
          icon: <List className="h-4 w-4" />,
          variant: 'outline',
          onClick: () => navigate('/comercial/oportunidades'),
        },
        {
          key: 'new-lead',
          label: 'Novo Lead',
          icon: <Plus className="h-4 w-4" />,
          variant: 'secondary',
          onClick: () => setIsFormOpen(true),
        },
      ]}
      onRefresh={() => void loadBoard()}
    >
      <div className="space-y-6">
        <div className="flex flex-wrap items-center gap-x-6 gap-y-2 rounded-lg border bg-card px-4 py-2.5 text-sm">
          <div className="flex items-center gap-2">
            <UserRound className="h-4 w-4 text-muted-foreground" />
            <span className="text-muted-foreground">Oportunidades</span>
            <span className="font-semibold text-foreground">{summary.count}</span>
          </div>
          <div className="hidden h-4 w-px bg-border sm:block" />
          <div className="flex items-center gap-2">
            <DollarSign className="h-4 w-4 text-muted-foreground" />
            <span className="text-muted-foreground">Valor em funil</span>
            <span className="font-semibold text-foreground">{formatCurrency(summary.value)}</span>
          </div>
          <div className="hidden h-4 w-px bg-border sm:block" />
          <div className="flex items-center gap-2">
            <CalendarClock className="h-4 w-4 text-muted-foreground" />
            <span className="text-muted-foreground">Follow-ups pendentes</span>
            <span className="font-semibold text-foreground">{summary.pendingFollowUps}</span>
          </div>
          <div className="hidden h-4 w-px bg-border sm:block" />
          <div className="flex items-center gap-2">
            <AlertTriangle className={`h-4 w-4 ${summary.overdueFollowUps > 0 ? 'text-destructive' : 'text-muted-foreground'}`} />
            <span className="text-muted-foreground">Atrasados</span>
            <span className={`font-semibold ${summary.overdueFollowUps > 0 ? 'text-destructive' : 'text-foreground'}`}>{summary.overdueFollowUps}</span>
          </div>
        </div>

        {summary.overdueFollowUps > 0 && (
          <div className="flex items-center gap-2 rounded-xl border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
            <AlertTriangle className="h-4 w-4" />
            Existem follow-ups atrasados no pipeline. Priorize estes cards.
          </div>
        )}

        <div className="overflow-x-auto pb-4">
          <div className="grid min-w-[1180px] gap-4" style={{ gridTemplateColumns: `repeat(${Math.max(stages.length, 1)}, minmax(220px, 1fr))` }}>
            {stages.map((stage) => (
              <section
                key={stage.commercialPipelineStageId}
                onDragOver={(event) => {
                  event.preventDefault()
                  event.dataTransfer.dropEffect = 'move'
                  setDragOverStage(stage.commercialPipelineStageId)
                }}
                onDragLeave={(event) => {
                  if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
                    setDragOverStage(null)
                  }
                }}
                onDrop={(event) => {
                  event.preventDefault()
                  void moveOpportunity(stage.commercialPipelineStageId)
                }}
                className={`rounded-2xl border p-3 transition ${dragOverStage === stage.commercialPipelineStageId ? 'border-primary bg-primary/5 shadow-sm' : 'border-border bg-muted/30'}`}
                style={{ borderTopWidth: '4px', borderTopColor: stage.color }}
              >
                <div className="mb-4 space-y-3">
                  <div className="flex items-center justify-between gap-2">
                    <span
                      className="inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold"
                      style={{
                        backgroundColor: stage.color,
                        color: getContrastColor(stage.color),
                      }}
                    >
                      {stage.name}
                    </span>
                    <span className="text-sm font-semibold text-muted-foreground">{stage.opportunitiesCount}</span>
                  </div>
                  <div>
                    <div className="text-sm font-semibold">{formatCurrency(stage.estimatedValueTotal)}</div>
                    <p className="mt-1 text-xs leading-relaxed text-muted-foreground">{stage.description || 'Estágio configurável do funil comercial.'}</p>
                  </div>
                </div>

                <div className="space-y-3">
                  {stage.items.map((item) => (
                    <OpportunityCard
                      key={item.id}
                      item={item}
                      isDragging={draggedItem?.id === item.id || movingOpportunityId === item.id}
                      onDragStart={() => setDraggedItem(item)}
                      onDragEnd={() => {
                        setDraggedItem(null)
                        setDragOverStage(null)
                      }}
                    />
                  ))}

                  {!loading && stage.items.length === 0 && (
                    <div className="rounded-xl border border-dashed border-border px-3 py-8 text-center text-xs text-muted-foreground">
                      Nenhuma oportunidade neste estágio.
                    </div>
                  )}
                </div>
              </section>
            ))}
          </div>
        </div>

        {loading && (
          <div className="flex items-center justify-center gap-2 rounded-xl border border-border bg-card py-6 text-sm text-muted-foreground">
            <RefreshCcw className="h-4 w-4 animate-spin" />
            Carregando pipeline comercial...
          </div>
        )}

        {!loading && summary.count === 0 && (
          <Card>
            <CardContent className="flex flex-col items-center justify-center gap-3 py-10 text-center">
              <div className="text-lg font-semibold">Nenhuma oportunidade no pipeline</div>
              <p className="max-w-lg text-sm text-muted-foreground">
                Cadastre leads para visualizar o funil comercial por estágio, valor estimado e follow-ups.
              </p>
              <Button type="button" onClick={() => setIsFormOpen(true)}>
                Cadastrar lead
              </Button>
            </CardContent>
          </Card>
        )}
      </div>

      <OpportunityFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        opportunity={null}
        onSuccess={() => {
          setIsFormOpen(false)
          void loadBoard()
        }}
      />
    </PageLayout>
  )
}
