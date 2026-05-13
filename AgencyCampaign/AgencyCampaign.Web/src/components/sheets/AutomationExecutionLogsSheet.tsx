import { useEffect, useState } from 'react'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  Badge,
  Button,
  useApi,
  useI18n,
} from 'archon-ui'
import { CheckCircle2, XCircle, ChevronDown, ChevronUp, ClipboardList } from 'lucide-react'
import { automationService } from '../../services/automationService'
import { automationTriggerLabels } from '../../types/automationTrigger'
import type { Automation, AutomationExecutionLog } from '../../types/automation'

interface Props {
  automation: Automation | null
  open: boolean
  onClose: () => void
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  }).format(new Date(value))
}

function tryFormatJson(raw?: string) {
  if (!raw) return null
  try {
    return JSON.stringify(JSON.parse(raw), null, 2)
  } catch {
    return raw
  }
}

export default function AutomationExecutionLogsSheet({ automation, open, onClose }: Props) {
  const { t } = useI18n()
  const [logs, setLogs] = useState<AutomationExecutionLog[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [expandedId, setExpandedId] = useState<number | null>(null)
  const { execute: fetchLogs, loading } = useApi<{ items: AutomationExecutionLog[]; pagination: { totalItems: number; totalPages: number; currentPage: number; pageSize: number } }>({ showErrorMessage: true })

  useEffect(() => {
    if (open && automation) {
      setPage(1)
      setExpandedId(null)
      void loadLogs(1)
    }
  }, [open, automation])

  useEffect(() => {
    if (open && automation) {
      void loadLogs(page)
    }
  }, [page])

  async function loadLogs(pageNumber: number) {
    if (!automation) return
    const result = await fetchLogs(() => automationService.getExecutionLogs(automation.id, pageNumber))
    if (result) {
      setLogs(result.items)
      setTotalPages(result.pagination.totalPages)
    }
  }

  function toggleExpand(id: number) {
    setExpandedId(prev => prev === id ? null : id)
  }

  return (
    <Sheet open={open} onOpenChange={(v) => { if (!v) onClose() }}>
      <SheetContent side="right" className="w-full sm:max-w-xl flex flex-col gap-0 p-0">
        <SheetHeader className="px-6 pt-6 pb-4 border-b">
          <div className="flex items-center gap-2">
            <ClipboardList size={18} className="text-muted-foreground" />
            <SheetTitle>{t('automations.logs.title')}</SheetTitle>
          </div>
          {automation && (
            <SheetDescription className="text-sm truncate">{automation.name}</SheetDescription>
          )}
        </SheetHeader>

        <div className="flex-1 overflow-y-auto">
          {loading && logs.length === 0 ? (
            <div className="flex items-center justify-center py-16 text-muted-foreground text-sm">
              {t('common.loading')}
            </div>
          ) : logs.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16 gap-2 text-muted-foreground">
              <ClipboardList size={36} />
              <p className="text-sm">{t('automations.logs.empty')}</p>
            </div>
          ) : (
            <ul className="divide-y">
              {logs.map((log) => {
                const isExpanded = expandedId === log.id
                const triggerLabel = automationTriggerLabels[log.trigger] ?? log.trigger
                const payloadFormatted = tryFormatJson(log.renderedPayload)

                return (
                  <li key={log.id} className="group">
                    <button
                      type="button"
                      onClick={() => toggleExpand(log.id)}
                      className="w-full flex items-center gap-3 px-6 py-4 hover:bg-muted/40 transition-colors text-left"
                    >
                      {log.succeeded ? (
                        <CheckCircle2 size={18} className="shrink-0 text-green-500" />
                      ) : (
                        <XCircle size={18} className="shrink-0 text-destructive" />
                      )}

                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <Badge variant="secondary" className="text-xs font-normal">
                            {triggerLabel}
                          </Badge>
                          <Badge variant={log.succeeded ? 'success' : 'destructive'} className="text-xs">
                            {log.succeeded ? t('automations.logs.status.success') : t('automations.logs.status.failed')}
                          </Badge>
                        </div>
                        <p className="text-xs text-muted-foreground mt-1">{formatDate(log.createdAt)}</p>
                        {!log.succeeded && log.errorMessage && !isExpanded && (
                          <p className="text-xs text-destructive mt-1 truncate">{log.errorMessage}</p>
                        )}
                      </div>

                      {isExpanded ? (
                        <ChevronUp size={16} className="shrink-0 text-muted-foreground" />
                      ) : (
                        <ChevronDown size={16} className="shrink-0 text-muted-foreground" />
                      )}
                    </button>

                    {isExpanded && (
                      <div className="px-6 pb-5 space-y-3 bg-muted/20">
                        {!log.succeeded && log.errorMessage && (
                          <div>
                            <p className="text-xs font-medium text-destructive mb-1">{t('automations.logs.detail.error')}</p>
                            <p className="text-xs bg-destructive/10 text-destructive rounded p-3 font-mono break-words">
                              {log.errorMessage}
                            </p>
                          </div>
                        )}
                        {payloadFormatted && (
                          <div>
                            <p className="text-xs font-medium text-muted-foreground mb-1">{t('automations.logs.detail.payload')}</p>
                            <pre className="text-xs bg-muted rounded p-3 overflow-x-auto whitespace-pre-wrap break-words font-mono">
                              {payloadFormatted}
                            </pre>
                          </div>
                        )}
                        {!payloadFormatted && log.succeeded && (
                          <p className="text-xs text-muted-foreground italic">{t('automations.logs.detail.noPayload')}</p>
                        )}
                      </div>
                    )}
                  </li>
                )
              })}
            </ul>
          )}
        </div>

        {totalPages > 1 && (
          <div className="flex items-center justify-between px-6 py-3 border-t bg-background">
            <Button
              variant="outline"
              size="sm"
              disabled={page <= 1 || loading}
              onClick={() => setPage(p => p - 1)}
            >
              {t('common.pagination.previous')}
            </Button>
            <span className="text-xs text-muted-foreground">
              {t('common.pagination.pageOf').replace('{0}', String(page)).replace('{1}', String(totalPages))}
            </span>
            <Button
              variant="outline"
              size="sm"
              disabled={page >= totalPages || loading}
              onClick={() => setPage(p => p + 1)}
            >
              {t('common.pagination.next')}
            </Button>
          </div>
        )}
      </SheetContent>
    </Sheet>
  )
}
