import { useMemo, useState } from 'react'
import { Button, useI18n } from 'archon-ui'
import { ChevronLeft, ChevronRight, CalendarDays, List } from 'lucide-react'
import type { CampaignDeliverable } from '../types/campaignDeliverable'

interface Props {
  deliverables: CampaignDeliverable[]
  onSelectDeliverable: (deliverable: CampaignDeliverable) => void
}

type StatusKind = 'onTime' | 'dueSoon' | 'overdue' | 'published' | 'cancelled'

const WEEKDAYS = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom']

const kindClasses: Record<StatusKind, string> = {
  onTime: 'bg-emerald-100 text-emerald-700 border border-emerald-200',
  dueSoon: 'bg-amber-100 text-amber-700 border border-amber-200',
  overdue: 'bg-red-100 text-red-700 border border-red-200',
  published: 'bg-blue-100 text-blue-700 border border-blue-200',
  cancelled: 'bg-muted text-muted-foreground border border-border',
}

const kindDot: Record<StatusKind, string> = {
  onTime: 'bg-emerald-500',
  dueSoon: 'bg-amber-500',
  overdue: 'bg-red-500',
  published: 'bg-blue-500',
  cancelled: 'bg-muted-foreground',
}

function resolveKind(deliverable: CampaignDeliverable): StatusKind {
  if (deliverable.status === 5) return 'cancelled'
  if (deliverable.status === 4) return 'published'
  const sla = deliverable.slaStatus ?? 0
  if (sla === 2) return 'overdue'
  if (sla === 1) return 'dueSoon'
  return 'onTime'
}

function dayKey(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

function capitalize(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1)
}

export default function CampaignDeliverableCalendar({ deliverables, onSelectDeliverable }: Props) {
  const { t } = useI18n()
  const [cursor, setCursor] = useState(() => { const now = new Date(); return new Date(now.getFullYear(), now.getMonth(), 1) })
  const [mode, setMode] = useState<'month' | 'list'>('month')

  function creatorName(deliverable: CampaignDeliverable): string {
    return deliverable.campaignCreator?.stageName || deliverable.campaignCreator?.creatorName || '-'
  }

  function kindLabel(kind: StatusKind): string {
    return t(`campaignCalendar.status.${kind}`)
  }

  const byDay = useMemo(() => {
    const map = new Map<string, CampaignDeliverable[]>()
    for (const deliverable of deliverables) {
      if (!deliverable.dueAt) continue
      const key = dayKey(new Date(deliverable.dueAt))
      const list = map.get(key) ?? []
      list.push(deliverable)
      map.set(key, list)
    }
    return map
  }, [deliverables])

  const cells = useMemo(() => {
    const year = cursor.getFullYear()
    const month = cursor.getMonth()
    const offset = (new Date(year, month, 1).getDay() + 6) % 7
    const daysInMonth = new Date(year, month + 1, 0).getDate()
    const total = Math.ceil((offset + daysInMonth) / 7) * 7
    const list: Array<Date | null> = []
    for (let index = 0; index < total; index++) {
      const dayNumber = index - offset + 1
      list.push(dayNumber >= 1 && dayNumber <= daysInMonth ? new Date(year, month, dayNumber) : null)
    }
    return list
  }, [cursor])

  const orderedForList = useMemo(() => {
    return deliverables
      .filter((item) => item.dueAt)
      .slice()
      .sort((a, b) => new Date(a.dueAt).getTime() - new Date(b.dueAt).getTime())
  }, [deliverables])

  const monthLabel = capitalize(cursor.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' }))
  const todayKey = dayKey(new Date())

  function goPrev() { setCursor((current) => new Date(current.getFullYear(), current.getMonth() - 1, 1)) }
  function goNext() { setCursor((current) => new Date(current.getFullYear(), current.getMonth() + 1, 1)) }
  function goToday() { const now = new Date(); setCursor(new Date(now.getFullYear(), now.getMonth(), 1)) }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <Button size="sm" variant="outline" onClick={goPrev}><ChevronLeft size={16} /></Button>
          <Button size="sm" variant="outline" onClick={goToday}>{t('campaignCalendar.today')}</Button>
          <Button size="sm" variant="outline" onClick={goNext}><ChevronRight size={16} /></Button>
          <span className="ml-2 text-sm font-semibold text-foreground">{monthLabel}</span>
        </div>
        <div className="flex items-center gap-1">
          <Button size="sm" variant={mode === 'month' ? 'primary' : 'outline'} onClick={() => setMode('month')}>
            <CalendarDays size={14} className="mr-1.5" />{t('campaignCalendar.mode.month')}
          </Button>
          <Button size="sm" variant={mode === 'list' ? 'primary' : 'outline'} onClick={() => setMode('list')}>
            <List size={14} className="mr-1.5" />{t('campaignCalendar.mode.list')}
          </Button>
        </div>
      </div>

      {mode === 'month' ? (
        <div className="overflow-x-auto">
          <div className="min-w-[680px]">
            <div className="grid grid-cols-7 gap-px border-b border-border pb-1">
              {WEEKDAYS.map((day) => (
                <div key={day} className="px-2 py-1 text-center text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">{day}</div>
              ))}
            </div>
            <div className="grid grid-cols-7 gap-px bg-border">
              {cells.map((date, index) => {
                const entries = date ? byDay.get(dayKey(date)) ?? [] : []
                const isToday = date && dayKey(date) === todayKey
                return (
                  <div key={index} className="min-h-[104px] bg-background p-1.5">
                    {date && (
                      <>
                        <div className={`mb-1 text-[11px] font-medium ${isToday ? 'flex h-5 w-5 items-center justify-center rounded-full bg-primary text-primary-foreground' : 'text-muted-foreground'}`}>
                          {date.getDate()}
                        </div>
                        <div className="space-y-1">
                          {entries.slice(0, 3).map((deliverable) => {
                            const kind = resolveKind(deliverable)
                            return (
                              <button
                                key={deliverable.id}
                                type="button"
                                onClick={() => onSelectDeliverable(deliverable)}
                                className={`block w-full truncate rounded px-1.5 py-0.5 text-left text-[11px] leading-tight ${kindClasses[kind]}`}
                                title={`${deliverable.title} - ${creatorName(deliverable)}`}
                              >
                                {deliverable.title}
                                <span className="opacity-70"> · {creatorName(deliverable)}</span>
                              </button>
                            )
                          })}
                          {entries.length > 3 && (
                            <span className="block px-1.5 text-[10px] text-muted-foreground">+{entries.length - 3}</span>
                          )}
                        </div>
                      </>
                    )}
                  </div>
                )
              })}
            </div>
          </div>
        </div>
      ) : (
        <div className="space-y-2">
          {orderedForList.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">{t('campaignCalendar.empty')}</p>
          ) : (
            orderedForList.map((deliverable) => {
              const kind = resolveKind(deliverable)
              return (
                <button
                  key={deliverable.id}
                  type="button"
                  onClick={() => onSelectDeliverable(deliverable)}
                  className="flex w-full items-center justify-between gap-3 rounded-lg border border-border bg-background px-3 py-2 text-left transition-colors hover:bg-accent/40"
                >
                  <div className="flex min-w-0 items-center gap-3">
                    <span className={`h-2 w-2 shrink-0 rounded-full ${kindDot[kind]}`} />
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium text-foreground">{deliverable.title}</p>
                      <p className="truncate text-xs text-muted-foreground">{creatorName(deliverable)} · {deliverable.platform?.name ?? '-'}</p>
                    </div>
                  </div>
                  <div className="shrink-0 text-right">
                    <p className="text-xs font-medium text-foreground">{new Date(deliverable.dueAt).toLocaleDateString('pt-BR')}</p>
                    <p className="text-[11px] text-muted-foreground">{kindLabel(kind)}</p>
                  </div>
                </button>
              )
            })
          )}
        </div>
      )}

      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 border-t border-border pt-3 text-[11px] text-muted-foreground">
        {(['onTime', 'dueSoon', 'overdue', 'published', 'cancelled'] as StatusKind[]).map((kind) => (
          <span key={kind} className="flex items-center gap-1.5">
            <span className={`h-2 w-2 rounded-full ${kindDot[kind]}`} />
            {kindLabel(kind)}
          </span>
        ))}
      </div>
    </div>
  )
}
