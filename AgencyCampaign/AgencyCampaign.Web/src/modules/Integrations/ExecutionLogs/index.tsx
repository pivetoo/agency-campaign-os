import { useEffect, useState } from 'react'
import { Badge, Button, PageLayout, useApi } from 'archon-ui'
import { ChevronDown, ChevronRight, CheckCircle2, XCircle, AlertTriangle, Clock, RefreshCw } from 'lucide-react'
import { integrationPlatformService } from '../../../services/integrationPlatformService'
import type { ExecutionListItem, ExecutionLogItem } from '../../../types/integrationPlatform'
import { formatDateTime } from '../../../lib/format'

function statusLabel(status: number) {
  if (status === 2) return { label: 'Sucesso', variant: 'success' as const, icon: <CheckCircle2 size={12} /> }
  if (status === 3) return { label: 'Erro', variant: 'error' as const, icon: <XCircle size={12} /> }
  if (status === 4) return { label: 'Parcial', variant: 'warning' as const, icon: <AlertTriangle size={12} /> }
  return { label: 'Rodando', variant: 'info' as const, icon: <Clock size={12} /> }
}

function logLevelLabel(level: number) {
  if (level === 3) return { label: 'Erro', variant: 'error' as const }
  if (level === 2) return { label: 'Aviso', variant: 'warning' as const }
  return { label: 'Info', variant: 'info' as const }
}

function stepTypeLabel(type: number) {
  if (type === 1) return 'HTTP'
  if (type === 2) return 'JavaScript'
  if (type === 4) return 'Email'
  if (type === 5) return 'SQL'
  return `Tipo ${type}`
}

function JsonBlock({ raw }: { raw: string }) {
  let formatted = raw
  try {
    formatted = JSON.stringify(JSON.parse(raw), null, 2)
  } catch {
    // não é JSON — exibe como texto
  }
  return (
    <pre className="overflow-x-auto rounded bg-muted/60 p-3 text-[11px] leading-relaxed text-foreground/80 whitespace-pre-wrap break-all max-h-48">
      {formatted}
    </pre>
  )
}

function ExecutionLogRow({ log }: { log: ExecutionLogItem }) {
  const [open, setOpen] = useState(false)
  const hasDetail = !!(log.request || log.response)
  const level = logLevelLabel(log.level)

  return (
    <div className="border-b last:border-0">
      <button
        type="button"
        onClick={() => hasDetail && setOpen((v) => !v)}
        className={['flex w-full items-start gap-3 px-4 py-2.5 text-left text-sm transition-colors', hasDetail ? 'hover:bg-muted/40 cursor-pointer' : 'cursor-default'].join(' ')}
      >
        {hasDetail ? (
          open ? <ChevronDown size={14} className="mt-0.5 flex-shrink-0 text-muted-foreground" /> : <ChevronRight size={14} className="mt-0.5 flex-shrink-0 text-muted-foreground" />
        ) : (
          <span className="w-3.5 flex-shrink-0" />
        )}
        <div className="flex flex-1 flex-wrap items-start gap-x-3 gap-y-1 min-w-0">
          <Badge variant={level.variant} size="sm">{level.label}</Badge>
          {log.pipelineStep && (
            <span className="text-xs text-muted-foreground font-medium">
              {log.pipelineStep.order}. {log.pipelineStep.name}
              <span className="ml-1 opacity-60">({stepTypeLabel(log.pipelineStep.type)})</span>
            </span>
          )}
          {log.httpStatusCode && (
            <span className={['text-xs font-mono font-medium', log.httpStatusCode >= 400 ? 'text-destructive' : 'text-emerald-600 dark:text-emerald-400'].join(' ')}>
              HTTP {log.httpStatusCode}
            </span>
          )}
          {log.duration && <span className="text-xs text-muted-foreground">{log.duration}ms</span>}
          <span className="flex-1 min-w-0 text-xs text-foreground/80 break-words">{log.message}</span>
        </div>
      </button>

      {open && hasDetail && (
        <div className="px-10 pb-3 space-y-2">
          {log.request && (
            <div>
              <p className="text-[10px] font-semibold uppercase tracking-wide text-muted-foreground mb-1">Requisição</p>
              <JsonBlock raw={log.request} />
            </div>
          )}
          {log.response && (
            <div>
              <p className="text-[10px] font-semibold uppercase tracking-wide text-muted-foreground mb-1">Resposta</p>
              <JsonBlock raw={log.response} />
            </div>
          )}
        </div>
      )}
    </div>
  )
}

function ExecutionRow({ execution }: { execution: ExecutionListItem }) {
  const [open, setOpen] = useState(false)
  const { data: logs, loading: loadingLogs, execute: fetchLogs } = useApi<ExecutionLogItem[]>()
  const status = statusLabel(execution.status)

  const toggle = async () => {
    if (!open && !logs) {
      await fetchLogs(() => integrationPlatformService.getExecutionLogs(execution.id))
    }
    setOpen((v) => !v)
  }

  return (
    <div className="border rounded-lg overflow-hidden">
      <button
        type="button"
        onClick={toggle}
        className="flex w-full items-center gap-3 px-4 py-3 text-left text-sm hover:bg-muted/30 transition-colors"
      >
        {open ? <ChevronDown size={15} className="flex-shrink-0 text-muted-foreground" /> : <ChevronRight size={15} className="flex-shrink-0 text-muted-foreground" />}
        <div className="flex flex-1 flex-wrap items-center gap-x-4 gap-y-1 min-w-0">
          <span className="font-medium text-sm min-w-0 truncate">
            {execution.connector?.name ?? `Conector #${execution.connectorId}`}
          </span>
          {execution.connector?.integration?.name && (
            <span className="text-xs text-muted-foreground">{execution.connector.integration.name}</span>
          )}
          {execution.pipeline?.name && (
            <span className="text-xs text-muted-foreground italic">{execution.pipeline.name}</span>
          )}
        </div>
        <div className="flex items-center gap-3 flex-shrink-0">
          <Badge variant={status.variant} size="sm">
            <span className="flex items-center gap-1">{status.icon}{status.label}</span>
          </Badge>
          {execution.duration && <span className="text-xs text-muted-foreground">{execution.duration}ms</span>}
          <span className="text-xs text-muted-foreground hidden sm:block">{formatDateTime(execution.startedAt)}</span>
        </div>
      </button>

      {open && (
        <div className="border-t bg-muted/10">
          {loadingLogs && (
            <p className="px-4 py-3 text-sm text-muted-foreground">Carregando logs...</p>
          )}
          {!loadingLogs && logs && logs.length === 0 && (
            <p className="px-4 py-3 text-sm text-muted-foreground">Nenhum log registrado para esta execução.</p>
          )}
          {!loadingLogs && logs && logs.length > 0 && (
            <div>
              {logs.map((log) => (
                <ExecutionLogRow key={log.id} log={log} />
              ))}
            </div>
          )}
          {execution.errors && (
            <div className="px-4 pb-3 pt-2">
              <p className="text-[10px] font-semibold uppercase tracking-wide text-muted-foreground mb-1">Erro final</p>
              <p className="text-xs text-destructive font-mono break-all">{execution.errors}</p>
            </div>
          )}
        </div>
      )}
    </div>
  )
}

export default function IntegrationLogs() {
  const [page, setPage] = useState(1)
  const pageSize = 20

  const { data, loading, execute: fetchExecutions } = useApi<{ items: ExecutionListItem[]; pagination: { page: number; pageSize: number; total: number; totalPages: number } }>()

  const load = (p: number) => {
    setPage(p)
    fetchExecutions(() => integrationPlatformService.getExecutions(p, pageSize))
  }

  useEffect(() => { load(1) }, [])

  const executions = data?.items ?? []
  const pagination = data?.pagination

  return (
    <PageLayout
      title="Logs de integração"
      subtitle="Histórico de execuções e detalhes de cada passo realizado."
      actions={[
        {
          key: 'refresh',
          label: 'Atualizar',
          icon: <RefreshCw size={16} />,
          onClick: () => load(page),
          variant: 'outline',
        },
      ]}
    >
      {loading && (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-12 rounded-lg bg-muted/40 animate-pulse" />
          ))}
        </div>
      )}

      {!loading && executions.length === 0 && (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <Clock size={36} className="text-muted-foreground/40 mb-3" />
          <p className="text-sm font-medium text-muted-foreground">Nenhuma execução registrada</p>
          <p className="text-xs text-muted-foreground/70 mt-1">As execuções aparecerão aqui conforme as integrações forem utilizadas.</p>
        </div>
      )}

      {!loading && executions.length > 0 && (
        <div className="space-y-2">
          {executions.map((execution) => (
            <ExecutionRow key={execution.id} execution={execution} />
          ))}
        </div>
      )}

      {pagination && pagination.totalPages > 1 && (
        <div className="flex items-center justify-between pt-4">
          <p className="text-xs text-muted-foreground">
            {pagination.total} execuções · página {pagination.page} de {pagination.totalPages}
          </p>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => load(page - 1)}>Anterior</Button>
            <Button variant="outline" size="sm" disabled={page >= pagination.totalPages} onClick={() => load(page + 1)}>Próxima</Button>
          </div>
        </div>
      )}
    </PageLayout>
  )
}
