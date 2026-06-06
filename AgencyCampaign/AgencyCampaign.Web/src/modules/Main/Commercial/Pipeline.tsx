import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, ConfirmModal, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, PageLayout, SearchableSelect, Sheet, SheetContent, SheetTrigger, useApi, useI18n, usePermissions, useToast } from 'archon-ui'
import { AlertTriangle, ArrowRight, BarChart3, CalendarClock, DollarSign, Filter, LayoutGrid, Plus, Rows3, Search, Target, UserRound, X } from 'lucide-react'
import { opportunityService, type OpportunityBoardItem, type OpportunityBoardStage } from '../../../services/opportunityService'
import { opportunityWinReasonService, opportunityLossReasonService } from '../../../services/opportunityOutcomeReasonService'
import type { OpportunityWinReason, OpportunityLossReason } from '../../../types/opportunityOutcomeReason'
import OpportunityFormModal from '../../../components/modals/OpportunityFormModal'
import CommercialViewToggle from '../../../components/buttons/CommercialViewToggle'
import CommercialGoalsWidget from './CommercialGoalsWidget'
import CommercialForecastWidget from './CommercialForecastWidget'
import CommercialInsightsLists from './CommercialInsightsLists'
import { resolveAssetUrl } from '../../../lib/assetUrl'
import { formatDate } from '../../../lib/format'
import { formatCurrency } from '../../../lib/format'

const ALL_SENTINEL = '__all'

interface PendingFinalMove {
  item: OpportunityBoardItem
  targetStage: OpportunityBoardStage
  kind: 'won' | 'lost'
}

interface PendingReopen {
  item: OpportunityBoardItem
  targetStage: number
  targetColumn?: OpportunityBoardStage
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

function OpportunityCard({ item, isDragging, density = 'comfortable', onDragStart, onDragEnd }: { item: OpportunityBoardItem; isDragging: boolean; density?: 'comfortable' | 'compact'; onDragStart: () => void; onDragEnd: () => void }) {
  const navigate = useNavigate()
  const { t } = useI18n()
  const compact = density === 'compact'

  const slaClass =
    item.slaStatus === 'breached'
      ? 'border-destructive/60 bg-destructive/5'
      : item.slaStatus === 'warning'
        ? 'border-amber-400/60 bg-amber-50/50'
        : 'border-border bg-card'

  return (
    <button
      type="button"
      data-testid="opportunity-card"
      data-opportunity-id={item.id}
      draggable
      onDragStart={(event) => {
        event.dataTransfer.effectAllowed = 'move'
        event.dataTransfer.setData('text/plain', item.id.toString())
        onDragStart()
      }}
      onDragEnd={onDragEnd}
      onClick={() => navigate(`/comercial/oportunidades/${item.id}`)}
      className={`w-full cursor-grab rounded-xl border text-left shadow-sm transition active:cursor-grabbing hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md ${compact ? 'p-2.5' : 'p-4'} ${slaClass} ${isDragging ? 'scale-[0.98] opacity-50 ring-2 ring-primary/30' : ''}`}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 items-start gap-2.5">
          {item.brandLogoUrl && (
            <img
              src={resolveAssetUrl(item.brandLogoUrl)}
              alt=""
              className={`${compact ? 'h-6 w-6' : 'h-7 w-7'} shrink-0 rounded-md border bg-card object-contain p-0.5`}
              onError={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
            />
          )}
          <div className="min-w-0">
            <div className={`truncate font-semibold text-foreground ${compact ? 'text-[13px] leading-tight' : 'text-sm'}`}>{item.name}</div>
            <div className={`truncate text-xs text-muted-foreground ${compact ? '' : 'mt-1'}`}>{item.brandName}</div>
          </div>
        </div>
        <div className="flex shrink-0 flex-col items-end gap-1">
          {item.overdueFollowUpsCount > 0 && (
            <span className="rounded-full bg-destructive px-2 py-0.5 text-[11px] font-semibold text-destructive-foreground">{t('pipeline.card.overdueBadge')}</span>
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

      <div className={`flex items-center gap-2 font-semibold text-foreground ${compact ? 'mt-2 text-[13px]' : 'mt-4 text-sm'}`}>
        <DollarSign className={compact ? 'h-3.5 w-3.5 text-emerald-500' : 'h-4 w-4 text-emerald-500'} />
        {formatCurrency(item.estimatedValue)}
        {item.probability > 0 && (
          <span className="ml-auto rounded-full bg-primary/10 px-2 py-0.5 text-[11px] font-semibold text-primary" title={t('pipeline.card.probability')}>{Math.round(item.probability)}%</span>
        )}
      </div>

      {!compact && (
        <>
          <div className="mt-3 space-y-2 text-xs text-muted-foreground">
            <div className="flex items-center gap-2">
              <CalendarClock className="h-3.5 w-3.5" />
              <span>{formatDate(item.expectedCloseAt, t('pipeline.card.noForecast'))}</span>
            </div>

            <div className="flex items-center gap-2">
              <UserRound className="h-3.5 w-3.5" />
              <span className="truncate">{item.commercialResponsibleName || t('pipeline.card.noOwner')}</span>
            </div>
          </div>

          <div className="mt-4 grid grid-cols-3 gap-2 text-center text-xs">
            <div className="rounded-lg bg-muted px-2 py-2">
              <div className="font-semibold text-foreground">{item.proposalCount}</div>
              <div className="text-muted-foreground">{t('pipeline.card.proposals')}</div>
            </div>
            <div className="rounded-lg bg-muted px-2 py-2">
              <div className="font-semibold text-foreground">{item.pendingFollowUpsCount}</div>
              <div className="text-muted-foreground">{t('pipeline.card.pending')}</div>
            </div>
            <div className="rounded-lg bg-muted px-2 py-2">
              <div className={item.overdueFollowUpsCount > 0 ? 'font-semibold text-destructive' : 'font-semibold text-foreground'}>{item.overdueFollowUpsCount}</div>
              <div className="text-muted-foreground">{t('pipeline.card.overdue')}</div>
            </div>
          </div>
        </>
      )}
    </button>
  )
}

function DensityToggle({ value, onChange }: { value: 'comfortable' | 'compact'; onChange: (v: 'comfortable' | 'compact') => void }) {
  const { t } = useI18n()
  const base = 'inline-flex items-center justify-center rounded-full p-1.5 transition-colors'
  const active = 'bg-background text-foreground shadow-sm'
  const idle = 'bg-transparent text-muted-foreground hover:text-foreground'
  return (
    <div className="inline-flex rounded-full bg-muted p-[3px]">
      <button type="button" title={t('pipeline.toolbar.densityComfortable')} onClick={() => onChange('comfortable')} className={`${base} ${value === 'comfortable' ? active : idle}`}>
        <LayoutGrid className="h-4 w-4" />
      </button>
      <button type="button" title={t('pipeline.toolbar.densityCompact')} onClick={() => onChange('compact')} className={`${base} ${value === 'compact' ? active : idle}`}>
        <Rows3 className="h-4 w-4" />
      </button>
    </div>
  )
}

export default function CommercialPipeline() {
  const { t } = useI18n()
  const { toast } = useToast()
  const navigate = useNavigate()
  const [board, setBoard] = useState<OpportunityBoardStage[]>([])
  const [insightsOpen, setInsightsOpen] = useState<boolean>(() => localStorage.getItem('pipeline.insights.open') === 'true')
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [draggedItem, setDraggedItem] = useState<OpportunityBoardItem | null>(null)
  const [dragOverStage, setDragOverStage] = useState<number | null>(null)
  const [movingOpportunityId, setMovingOpportunityId] = useState<number | null>(null)
  const [pendingFinal, setPendingFinal] = useState<PendingFinalMove | null>(null)
  const [pendingReopen, setPendingReopen] = useState<PendingReopen | null>(null)
  const [finalNotes, setFinalNotes] = useState('')
  const [finalReasonId, setFinalReasonId] = useState<number | null>(null)
  const [winReasons, setWinReasons] = useState<OpportunityWinReason[]>([])
  const [lossReasons, setLossReasons] = useState<OpportunityLossReason[]>([])
  const [density, setDensity] = useState<'comfortable' | 'compact'>(() => (localStorage.getItem('pipeline.density') === 'compact' ? 'compact' : 'comfortable'))
  const [filtersOpen, setFiltersOpen] = useState(false)
  const [searchText, setSearchText] = useState('')
  const [responsibleFilter, setResponsibleFilter] = useState('')
  const [brandFilter, setBrandFilter] = useState('')
  const [riskOnly, setRiskOnly] = useState(false)
  const [mobileStageId, setMobileStageId] = useState<number | null>(null)
  const { execute: fetchBoard, loading } = useApi<OpportunityBoardStage[]>({ showErrorMessage: true })
  const { execute: runFinalClose, loading: closing } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { hasAnyPermission } = usePermissions()
  const canSeeAllBoard = hasAnyPermission(['opportunities.board'])

  const loadBoard = async () => {
    const result = await fetchBoard(() => (canSeeAllBoard ? opportunityService.getBoard() : opportunityService.getBoardMine()))
    if (result) {
      setBoard(result)
    }
  }

  useEffect(() => {
    void loadBoard()
  }, [])

  useEffect(() => {
    void opportunityWinReasonService.getAll({ pageSize: 200 }).then((result) => setWinReasons(result.data ?? [])).catch(() => setWinReasons([]))
    void opportunityLossReasonService.getAll({ pageSize: 200 }).then((result) => setLossReasons(result.data ?? [])).catch(() => setLossReasons([]))
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

  const changeDensity = (next: 'comfortable' | 'compact') => {
    setDensity(next)
    localStorage.setItem('pipeline.density', next)
  }

  const filterOptions = useMemo(() => {
    const responsibles = new Set<string>()
    const brands = new Set<string>()
    stages.forEach((stage) => stage.items.forEach((item) => {
      if (item.commercialResponsibleName) {
        responsibles.add(item.commercialResponsibleName)
      }
      if (item.brandName) {
        brands.add(item.brandName)
      }
    }))
    return {
      responsibles: [...responsibles].sort((a, b) => a.localeCompare(b)).map((name) => ({ value: name, label: name })),
      brands: [...brands].sort((a, b) => a.localeCompare(b)).map((name) => ({ value: name, label: name })),
    }
  }, [stages])

  const structuredFilterCount = (responsibleFilter ? 1 : 0) + (brandFilter ? 1 : 0) + (riskOnly ? 1 : 0)
  const activeFilterCount = (searchText.trim() ? 1 : 0) + structuredFilterCount

  const matchesFilters = (item: OpportunityBoardItem) => {
    const term = searchText.trim().toLowerCase()
    if (term) {
      const haystack = `${item.name} ${item.brandName} ${item.commercialResponsibleName ?? ''}`.toLowerCase()
      if (!haystack.includes(term)) {
        return false
      }
    }
    if (responsibleFilter && item.commercialResponsibleName !== responsibleFilter) {
      return false
    }
    if (brandFilter && item.brandName !== brandFilter) {
      return false
    }
    if (riskOnly && !(item.slaStatus === 'breached' || item.slaStatus === 'warning' || item.overdueFollowUpsCount > 0)) {
      return false
    }
    return true
  }

  const displayStages = useMemo(() => {
    if (activeFilterCount === 0) {
      return stages
    }
    return stages.map((stage) => {
      const items = stage.items.filter(matchesFilters)
      return {
        ...stage,
        items,
        opportunitiesCount: items.length,
        estimatedValueTotal: items.reduce((total, item) => total + item.estimatedValue, 0),
      }
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [stages, searchText, responsibleFilter, brandFilter, riskOnly, activeFilterCount])

  const visibleSummary = useMemo(() => {
    return displayStages.reduce(
      (acc, stage) => {
        acc.count += stage.opportunitiesCount
        acc.value += stage.estimatedValueTotal
        stage.items.forEach((item) => {
          acc.pendingFollowUps += item.pendingFollowUpsCount
          if (item.overdueFollowUpsCount > 0) {
            acc.overdueOpps += 1
          }
        })
        return acc
      },
      { count: 0, value: 0, pendingFollowUps: 0, overdueOpps: 0 }
    )
  }, [displayStages])

  const clearFilters = () => {
    setSearchText('')
    setResponsibleFilter('')
    setBrandFilter('')
    setRiskOnly(false)
  }

  const activeMobileStage = useMemo(() => {
    if (displayStages.length === 0) {
      return null
    }
    return displayStages.find((stage) => stage.commercialPipelineStageId === mobileStageId) ?? displayStages[0]
  }, [displayStages, mobileStageId])

  const moveOpportunity = async (movingItem: OpportunityBoardItem, targetStage: number) => {
    if (movingItem.commercialPipelineStageId === targetStage || movingOpportunityId) {
      setDragOverStage(null)
      return
    }

    const targetColumn = board.find((column) => column.commercialPipelineStageId === targetStage)

    if (targetColumn && targetColumn.finalBehavior === 1) {
      setPendingFinal({ item: movingItem, targetStage: targetColumn, kind: 'won' })
      setFinalNotes('')
      setFinalReasonId(null)
      setDragOverStage(null)
      setDraggedItem(null)
      return
    }

    if (targetColumn && targetColumn.finalBehavior === 2) {
      setPendingFinal({ item: movingItem, targetStage: targetColumn, kind: 'lost' })
      setFinalNotes('')
      setFinalReasonId(null)
      setDragOverStage(null)
      setDraggedItem(null)
      return
    }

    const sourceColumn = board.find((column) => column.commercialPipelineStageId === movingItem.commercialPipelineStageId)
    const isReopening = sourceColumn?.finalBehavior === 1 || sourceColumn?.finalBehavior === 2
    if (isReopening) {
      setPendingReopen({ item: movingItem, targetStage, targetColumn })
      setDragOverStage(null)
      setDraggedItem(null)
      return
    }

    await applyMove(movingItem, targetStage, targetColumn, false)
  }

  const applyMove = async (movingItem: OpportunityBoardItem, targetStage: number, targetColumn: OpportunityBoardStage | undefined, isReopening: boolean) => {
    const previousBoard = board
    const updatedItem = {
      ...movingItem,
      commercialPipelineStageId: targetStage,
      commercialPipelineStageName: targetColumn?.name || movingItem.commercialPipelineStageName,
      commercialPipelineStageColor: targetColumn?.color || movingItem.commercialPipelineStageColor,
    }

    setMovingOpportunityId(movingItem.id)
    setDragOverStage(null)
    setBoard((currentBoard) => {
      const boardByStage = new Map<number, OpportunityBoardItem[]>()

      currentBoard.forEach((stage) => {
        boardByStage.set(stage.commercialPipelineStageId, [])
      })

      currentBoard.forEach((stage) => {
        boardByStage.set(stage.commercialPipelineStageId, stage.items.filter((item) => item.id !== movingItem.id))
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
      await opportunityService.changeStage(movingItem.id, { commercialPipelineStageId: targetStage, allowReopen: isReopening, expectedVersion: movingItem.version })
    } catch (error) {
      setBoard(previousBoard)
      const status = (error as { status?: number } | null)?.status
      if (status === 409) {
        const message = (error as { message?: string } | null)?.message
        toast({ title: message || t('opportunities.move.conflict'), variant: 'destructive' })
        void loadBoard()
      } else {
        toast({ title: t('opportunities.move.failed'), variant: 'destructive' })
      }
    } finally {
      setMovingOpportunityId(null)
      setDraggedItem(null)
    }
  }

  const confirmReopen = async () => {
    if (!pendingReopen) {
      return
    }
    const { item, targetStage, targetColumn } = pendingReopen
    setPendingReopen(null)
    await applyMove(item, targetStage, targetColumn, true)
  }

  const confirmFinalMove = async () => {
    if (!pendingFinal) return
    const { item, kind } = pendingFinal
    const trimmedNotes = finalNotes.trim()

    if (kind === 'lost' && trimmedNotes.length === 0 && finalReasonId === null) return

    const result = await runFinalClose(() =>
      kind === 'won'
        ? opportunityService.closeAsWon(item.id, { wonNotes: trimmedNotes || undefined, winReasonId: finalReasonId ?? undefined })
        : opportunityService.closeAsLost(item.id, { lossReason: trimmedNotes || lossReasons.find((reason) => reason.id === finalReasonId)?.name || t('pipeline.finalMove.defaultLossReason'), lossReasonId: finalReasonId ?? undefined }),
    )
    if (result !== null) {
      setPendingFinal(null)
      setFinalNotes('')
      setFinalReasonId(null)
      void loadBoard()
    }
  }

  const cancelFinalMove = () => {
    setPendingFinal(null)
    setFinalNotes('')
    setFinalReasonId(null)
  }

  return (
    <PageLayout
      title={t('pipeline.title')}
      subtitle={t('pipeline.subtitle')}
      actionsSlot={(
        <div className="flex flex-wrap items-center gap-2">
          <CommercialViewToggle active="kanban" />
          {hasAnyPermission(['opportunities.analytics', 'opportunities.analyticsOwn']) && (
            <button
              type="button"
              onClick={() => navigate('/relatorios/comercial/funil')}
              className="inline-flex items-center gap-1.5 rounded-full border border-border bg-background px-3.5 py-1.5 text-[13px] font-semibold text-muted-foreground transition-colors hover:border-primary/40 hover:text-foreground"
            >
              <BarChart3 className="h-3.5 w-3.5" /> Analytics
            </button>
          )}
          <DensityToggle value={density} onChange={changeDensity} />
        </div>
      )}
      actions={[
        {
          key: 'new-lead',
          label: t('pipeline.action.newLead'),
          icon: <Plus className="h-4 w-4" />,
          variant: 'secondary',
          onClick: () => setIsFormOpen(true),
        },
      ]}
      onRefresh={() => void loadBoard()}
    >
      <Sheet open={insightsOpen} onOpenChange={(open) => {
          setInsightsOpen(open)
          localStorage.setItem('pipeline.insights.open', String(open))
        }}>
          <SheetTrigger asChild>
            <button
              type="button"
              aria-label={t('pipeline.insights.openAria')}
              title={t('pipeline.insights.title')}
              className="group fixed right-0 top-1/2 z-30 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-l-lg border border-r-0 border-border bg-card text-primary shadow-md transition-all hover:bg-muted hover:text-primary"
            >
              <BarChart3 className="h-5 w-5" />
              {summary.overdueFollowUps > 0 && (
                <span className="absolute -top-1.5 -left-1.5 inline-flex h-4 min-w-4 items-center justify-center rounded-full bg-destructive px-1 text-[10px] font-bold text-white">
                  {summary.overdueFollowUps}
                </span>
              )}
            </button>
          </SheetTrigger>
          <SheetContent side="right" className="w-full overflow-y-auto p-5 sm:w-[min(560px,95vw)] sm:max-w-none">
            <div className="mb-4">
              <h2 className="flex items-center gap-2 text-base font-semibold text-foreground">
                <Target className="h-4 w-4 text-primary" />
                {t('pipeline.insights.title')}
              </h2>
              <p className="mt-0.5 text-xs text-muted-foreground">
                <strong className="text-foreground">{summary.count}</strong> {summary.count === 1 ? t('pipeline.insights.openCount.one') : t('pipeline.insights.openCount.many')} · <span className="font-mono">{formatCurrency(summary.value)}</span>
                {summary.overdueFollowUps > 0 && <span className="ml-2 inline-flex items-center gap-1 rounded bg-destructive/10 px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wider text-destructive">{summary.overdueFollowUps} {summary.overdueFollowUps === 1 ? t('pipeline.insights.overdueBadge.one') : t('pipeline.insights.overdueBadge.many')}</span>}
              </p>
            </div>
            <div className="space-y-4">
              <CommercialGoalsWidget scope={canSeeAllBoard ? 'all' : 'mine'} onEmptyManage={() => { setInsightsOpen(false); navigate('/comercial/metas') }} />
              <CommercialForecastWidget scope={canSeeAllBoard ? 'all' : 'mine'} />
              <CommercialInsightsLists scope={canSeeAllBoard ? 'all' : 'mine'} onNavigate={() => setInsightsOpen(false)} />
            </div>
          </SheetContent>
        </Sheet>

      <div className="space-y-6">
        {summary.overdueFollowUps > 0 && (
          <div className="flex items-center gap-2 rounded-xl border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
            <AlertTriangle className="h-4 w-4" />
            {t('pipeline.alert.overdue')}
          </div>
        )}

        <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-border bg-card px-4 py-3 text-sm">
          <div className="flex flex-wrap items-center gap-x-6 gap-y-1">
            <span className="inline-flex items-baseline gap-2 text-muted-foreground">{t('pipeline.summary.value')}<strong className="font-semibold tracking-tight text-foreground">{formatCurrency(visibleSummary.value)}</strong></span>
            <span className="text-muted-foreground"><strong className="text-foreground">{visibleSummary.count}</strong> {t('pipeline.summary.opportunities').toLowerCase()}</span>
            {visibleSummary.overdueOpps > 0 && (
              <span className="font-semibold text-destructive">{visibleSummary.overdueOpps} {t('pipeline.summary.overdue').toLowerCase()}</span>
            )}
            {visibleSummary.pendingFollowUps > 0 && (
              <span className="font-semibold text-amber-600">{visibleSummary.pendingFollowUps} {t('pipeline.summary.pendingFollowUps').toLowerCase()}</span>
            )}
          </div>
          <div className="flex items-center gap-1.5">
            <div className="relative w-80">
              <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={searchText}
                onChange={(e) => setSearchText(e.target.value)}
                placeholder={t('pipeline.filter.searchPlaceholder')}
                className="h-8 pl-8 pr-8"
              />
              {searchText && (
                <button
                  type="button"
                  title={t('pipeline.filter.clear')}
                  onClick={() => setSearchText('')}
                  className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-0.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                >
                  <X className="h-3.5 w-3.5" />
                </button>
              )}
            </div>
            <button
              type="button"
              title={t('pipeline.toolbar.filters')}
              onClick={() => setFiltersOpen((open) => !open)}
              className={`relative inline-flex h-8 w-8 items-center justify-center rounded-lg border transition-colors ${filtersOpen || structuredFilterCount > 0 ? 'border-primary bg-primary/10 text-primary' : 'border-border text-muted-foreground hover:bg-muted hover:text-foreground'}`}
            >
              <Filter className="h-4 w-4" />
              {structuredFilterCount > 0 && (
                <span className="absolute -right-1.5 -top-1.5 inline-flex h-4 min-w-4 items-center justify-center rounded-full bg-primary px-1 text-[10px] font-bold text-primary-foreground">{structuredFilterCount}</span>
              )}
            </button>
          </div>
        </div>

        {filtersOpen && (
          <div className="flex flex-wrap items-center gap-2 rounded-xl border border-border bg-card px-3 py-2.5">
            <div className="w-48">
              <SearchableSelect
                value={responsibleFilter || ALL_SENTINEL}
                onValueChange={(value) => setResponsibleFilter(value === ALL_SENTINEL ? '' : value)}
                options={[{ value: ALL_SENTINEL, label: t('pipeline.filter.allResponsibles') }, ...filterOptions.responsibles]}
                placeholder={t('pipeline.filter.responsible')}
                searchPlaceholder={t('pipeline.filter.responsible')}
              />
            </div>
            <div className="w-48">
              <SearchableSelect
                value={brandFilter || ALL_SENTINEL}
                onValueChange={(value) => setBrandFilter(value === ALL_SENTINEL ? '' : value)}
                options={[{ value: ALL_SENTINEL, label: t('pipeline.filter.allBrands') }, ...filterOptions.brands]}
                placeholder={t('pipeline.filter.brand')}
                searchPlaceholder={t('pipeline.filter.brand')}
              />
            </div>
            <button
              type="button"
              onClick={() => setRiskOnly((value) => !value)}
              className={`inline-flex h-9 items-center gap-1.5 rounded-full border px-3 text-[13px] font-semibold transition-colors ${riskOnly ? 'border-destructive bg-destructive/10 text-destructive' : 'border-border bg-background text-muted-foreground hover:text-foreground'}`}
            >
              <AlertTriangle className="h-3.5 w-3.5" />
              {t('pipeline.filter.riskOnly')}
            </button>
            <div className="ml-auto flex items-center gap-3 text-xs text-muted-foreground">
              {structuredFilterCount > 0 && (
                <span>{t('pipeline.filter.activeLabel')}: <strong className="text-foreground">{structuredFilterCount}</strong></span>
              )}
              <button type="button" onClick={clearFilters} disabled={activeFilterCount === 0} className="font-semibold text-primary transition-colors hover:text-primary/80 disabled:cursor-not-allowed disabled:text-muted-foreground/50">
                {t('pipeline.filter.clear')}
              </button>
              <button type="button" onClick={() => setFiltersOpen(false)} title={t('pipeline.toolbar.filters')} className="rounded-md p-1 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground">
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>
        )}

        {/* Mobile: Kanban nao funciona no toque (colunas horizontais + drag). Seletor de etapa + lista vertical. */}
        <div className="md:hidden">
          {displayStages.length > 0 && (
            <div className="flex gap-2 overflow-x-auto pb-2">
              {displayStages.map((stage) => {
                const isActive = activeMobileStage?.commercialPipelineStageId === stage.commercialPipelineStageId
                return (
                  <button
                    key={stage.commercialPipelineStageId}
                    type="button"
                    onClick={() => setMobileStageId(stage.commercialPipelineStageId)}
                    className={`inline-flex shrink-0 items-center gap-2 rounded-full border px-3 py-1.5 text-[13px] font-semibold transition-colors ${isActive ? 'border-transparent' : 'border-border bg-card text-muted-foreground'}`}
                    style={isActive ? { backgroundColor: stage.color, color: getContrastColor(stage.color) } : undefined}
                  >
                    {!isActive && <span className="h-2 w-2 shrink-0 rounded-full" style={{ backgroundColor: stage.color }} />}
                    {stage.name}
                    <span className={`rounded-full px-1.5 text-[11px] ${isActive ? 'bg-black/15' : 'bg-muted'}`}>{stage.opportunitiesCount}</span>
                  </button>
                )
              })}
            </div>
          )}

          {activeMobileStage && (
            <div className="mt-3 space-y-3">
              <div className="flex items-center justify-between rounded-xl border border-border bg-muted/30 px-3 py-2 text-sm">
                <span className="font-semibold text-foreground">{formatCurrency(activeMobileStage.estimatedValueTotal)}</span>
                <span className="text-muted-foreground"><strong className="text-foreground">{activeMobileStage.opportunitiesCount}</strong> {t('pipeline.summary.opportunities').toLowerCase()}</span>
              </div>

              {activeMobileStage.items.map((item) => (
                <div key={item.id} className="space-y-1.5">
                  <OpportunityCard
                    item={item}
                    density="comfortable"
                    isDragging={movingOpportunityId === item.id}
                    onDragStart={() => {}}
                    onDragEnd={() => {}}
                  />
                  <div className="flex items-center gap-2 rounded-lg border border-border bg-card px-2.5 py-1.5">
                    <ArrowRight className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                    <select
                      value=""
                      disabled={movingOpportunityId != null}
                      onChange={(event) => {
                        const target = Number(event.target.value)
                        if (target) {
                          void moveOpportunity(item, target)
                        }
                      }}
                      className="w-full bg-transparent text-xs font-medium text-foreground focus:outline-none disabled:opacity-60"
                    >
                      <option value="">{t('pipeline.mobile.moveTo')}</option>
                      {displayStages
                        .filter((stage) => stage.commercialPipelineStageId !== item.commercialPipelineStageId)
                        .map((stage) => (
                          <option key={stage.commercialPipelineStageId} value={stage.commercialPipelineStageId}>{stage.name}</option>
                        ))}
                    </select>
                  </div>
                </div>
              ))}

              {!loading && activeMobileStage.items.length === 0 && (
                <div className="rounded-xl border border-dashed border-border px-3 py-10 text-center text-xs text-muted-foreground">
                  {activeFilterCount > 0 ? t('pipeline.filter.empty') : t('pipeline.stage.empty')}
                </div>
              )}
            </div>
          )}
        </div>

        <div className="hidden overflow-x-auto pb-4 md:block">
          <div className="grid gap-3" style={{ gridTemplateColumns: `repeat(${Math.max(displayStages.length, 1)}, minmax(220px, 1fr))` }}>
            {displayStages.map((stage) => (
              <section
                key={stage.commercialPipelineStageId}
                data-testid="opportunity-stage-column"
                data-stage-id={stage.commercialPipelineStageId}
                data-stage={stage.name}
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
                  if (draggedItem) {
                    void moveOpportunity(draggedItem, stage.commercialPipelineStageId)
                  }
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
                    <p className="mt-1 text-xs leading-relaxed text-muted-foreground">{stage.description || t('pipeline.stage.defaultDescription')}</p>
                  </div>
                </div>

                <div className={density === 'compact' ? 'space-y-2' : 'space-y-3'}>
                  {stage.items.map((item) => (
                    <OpportunityCard
                      key={item.id}
                      item={item}
                      density={density}
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
                      {activeFilterCount > 0 ? t('pipeline.filter.empty') : t('pipeline.stage.empty')}
                    </div>
                  )}
                </div>
              </section>
            ))}
          </div>
        </div>

        {!loading && summary.count === 0 && (
          <div className="flex flex-col items-center justify-center gap-3 py-10 text-center text-muted-foreground">
            <p className="text-sm">{t('pipeline.empty.description')}</p>
            <Button type="button" variant="secondary" size="sm" onClick={() => setIsFormOpen(true)}>
              {t('pipeline.action.registerLead')}
            </Button>
          </div>
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

      <ConfirmModal
        open={!!pendingReopen}
        onOpenChange={(open) => { if (!open) setPendingReopen(null) }}
        description={t('opportunity.reopen.confirmationRequired')}
        variant="warning"
        onConfirm={() => void confirmReopen()}
      />

      <Modal open={!!pendingFinal} onOpenChange={(open) => { if (!open) cancelFinalMove() }}>
        <ModalContent size="form">
          <ModalHeader>
            <ModalTitle>
              {pendingFinal?.kind === 'won' ? t('pipeline.finalMove.wonTitle') : t('pipeline.finalMove.lostTitle')}
            </ModalTitle>
          </ModalHeader>
          <div className="space-y-3">
            <p className="text-sm text-muted-foreground">
              {t('pipeline.finalMove.movingPrefix')}<strong>{pendingFinal?.item.name}</strong>{t('pipeline.finalMove.movingTo')}<strong>{pendingFinal?.targetStage.name}</strong>.
              {pendingFinal?.kind === 'lost' ? t('pipeline.finalMove.lostHint') : t('pipeline.finalMove.wonHint')}
            </p>
            {((pendingFinal?.kind === 'won' ? winReasons : lossReasons) as (OpportunityWinReason | OpportunityLossReason)[]).length > 0 && (
              <div className="space-y-2">
                <label className="text-sm font-medium">{t('pipeline.finalMove.reasonLabel')}</label>
                <div className="flex flex-wrap gap-1.5">
                  {((pendingFinal?.kind === 'won' ? winReasons : lossReasons) as (OpportunityWinReason | OpportunityLossReason)[]).map((reason) => {
                    const selected = finalReasonId === reason.id
                    return (
                      <button
                        key={reason.id}
                        type="button"
                        onClick={() => setFinalReasonId(selected ? null : reason.id)}
                        className="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-medium transition-colors"
                        style={selected ? { borderColor: reason.color, backgroundColor: `${reason.color}1a`, color: reason.color } : { borderColor: 'hsl(var(--border))', color: 'hsl(var(--muted-foreground))' }}
                      >
                        <span className="h-1.5 w-1.5 rounded-full" style={{ backgroundColor: reason.color }} />
                        {reason.name}
                      </button>
                    )
                  })}
                </div>
              </div>
            )}
            <div className="space-y-2">
              <label className="text-sm font-medium">
                {pendingFinal?.kind === 'won' ? t('pipeline.finalMove.wonNotesLabel') : t('pipeline.finalMove.lostReasonLabel')}
                {pendingFinal?.kind === 'lost' && <span className="text-destructive"> *</span>}
              </label>
              <textarea
                className="min-h-[100px] w-full rounded-md border bg-background p-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                value={finalNotes}
                onChange={(e) => setFinalNotes(e.target.value)}
                placeholder={pendingFinal?.kind === 'won' ? t('pipeline.finalMove.wonPlaceholder') : t('pipeline.finalMove.lostPlaceholder')}
                autoFocus
              />
            </div>
          </div>
          <ModalFooter>
            <Button type="button" variant="outline" onClick={cancelFinalMove} disabled={closing}>{t('common.action.cancel')}</Button>
            <Button
              type="button"
              variant={pendingFinal?.kind === 'lost' ? 'danger' : 'primary'}
              onClick={() => void confirmFinalMove()}
              disabled={closing || (pendingFinal?.kind === 'lost' && finalNotes.trim().length === 0 && finalReasonId === null)}
            >
              {closing ? t('common.action.saving') : t('common.action.confirm')}
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </PageLayout>
  )
}
