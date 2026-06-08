import { useEffect, useState } from 'react'
import { Badge, DataTable, PageLayout, Modal, ModalContent, ModalHeader, ModalTitle, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { AlertCircle, Check, CheckCircle2, Clock, Copy, Info } from 'lucide-react'
import { integrationPlatformService } from '../../../services/integrationPlatformService'
import type { ExecutionListItem, ExecutionLogItem } from '../../../types/integrationPlatform'
import { formatDateTime } from '../../../lib/format'

const STATUS_VARIANT: Record<number, 'warning' | 'success' | 'destructive' | 'secondary'> = {
  1: 'warning',
  2: 'success',
  3: 'destructive',
  4: 'secondary',
}

const STATUS_LABEL: Record<number, string> = {
  1: 'Rodando',
  2: 'Sucesso',
  3: 'Erro',
  4: 'Parcial',
}

const TYPE_LABEL: Record<number, string> = {
  1: 'Manual',
  2: 'Webhook',
  3: 'Agendado',
  4: 'Serviço',
}

function formatDuration(ms?: number | null): string {
  if (ms == null) return '-'
  if (ms < 1000) return `${ms}ms`
  const s = Math.floor(ms / 1000)
  if (s < 60) return `${s}s`
  return `${Math.floor(s / 60)}m ${s % 60}s`
}

function tryPrettyJson(text: string): string {
  try { return JSON.stringify(JSON.parse(text), null, 2) } catch { return text }
}

function CopyButton({ text }: { text: string }) {
  const [copied, setCopied] = useState(false)
  const handle = async () => {
    await navigator.clipboard.writeText(text)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }
  return (
    <button type="button" onClick={handle} className="flex items-center gap-1 px-2 py-1 text-xs rounded hover:bg-accent transition-colors">
      {copied ? <><Check size={13} className="text-emerald-600" /><span>Copiado</span></> : <><Copy size={13} /><span>Copiar</span></>}
    </button>
  )
}

function getLogBadge(log: ExecutionLogItem) {
  if (log.level >= 3) return { variant: 'destructive' as const, icon: <AlertCircle size={13} />, label: 'Erro' }
  if (log.level === 2) return { variant: 'warning' as const, icon: <AlertCircle size={13} />, label: 'Aviso' }
  if (log.pipelineStep && log.duration != null) return { variant: 'success' as const, icon: <CheckCircle2 size={13} />, label: 'OK' }
  return { variant: 'secondary' as const, icon: <Info size={13} />, label: 'Info' }
}

function ExecutionDetailModal({ open, onOpenChange, execution }: { open: boolean; onOpenChange: (v: boolean) => void; execution: ExecutionListItem | null }) {
  const [logs, setLogs] = useState<ExecutionLogItem[]>([])
  const [selected, setSelected] = useState<ExecutionLogItem | null>(null)
  const { execute: fetchLogs, loading } = useApi<ExecutionLogItem[]>()

  useEffect(() => {
    if (!open || !execution) { setLogs([]); setSelected(null); return }
    fetchLogs(() => integrationPlatformService.getExecutionLogs(execution.id)).then((result) => {
      if (!result) return
      const sorted = [...result].sort((a, b) => a.id - b.id).filter((l) => !l.pipelineStep || l.duration != null)
      setLogs(sorted)
      const firstError = sorted.find((l) => l.level >= 3)
      const firstWithData = sorted.find((l) => l.request || l.response)
      setSelected(firstError ?? firstWithData ?? sorted[0] ?? null)
    })
  }, [open, execution?.id])

  if (!execution) return null

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="5xl" className="max-h-[90vh] flex flex-col">
        <ModalHeader>
          <ModalTitle>Execução #{execution.id}</ModalTitle>
        </ModalHeader>

        <div className="grid grid-cols-2 gap-4 text-sm mt-2 pb-3 border-b">
          <div><span className="font-medium">Conector:</span> {execution.connector?.name ?? '-'}</div>
          <div><span className="font-medium">Pipeline:</span> {execution.pipeline?.name ?? '-'}</div>
          <div>
            <span className="font-medium">Status:</span>{' '}
            <Badge variant={STATUS_VARIANT[execution.status] ?? 'secondary'}>{STATUS_LABEL[execution.status] ?? '-'}</Badge>
          </div>
          <div><span className="font-medium">Duração:</span> {formatDuration(execution.duration)}</div>
        </div>

        <div className="flex gap-4 flex-1 min-h-0 overflow-hidden mt-3">
          <div className="w-72 border rounded-lg overflow-y-auto flex-shrink-0">
            <div className="sticky top-0 bg-background border-b px-3 py-2 text-sm font-semibold">
              Steps ({logs.length})
            </div>
            {loading && <p className="p-4 text-sm text-muted-foreground text-center">Carregando...</p>}
            {!loading && logs.length === 0 && <p className="p-4 text-sm text-muted-foreground text-center">Nenhum log registrado.</p>}
            {!loading && logs.map((log) => {
              const badge = getLogBadge(log)
              return (
                <button
                  key={log.id}
                  type="button"
                  onClick={() => setSelected(log)}
                  className={['w-full text-left px-3 py-2.5 border-b last:border-0 hover:bg-accent transition-colors', selected?.id === log.id ? 'bg-accent' : ''].join(' ')}
                >
                  <div className="flex items-center justify-between gap-2 mb-0.5">
                    <span className="text-sm font-medium truncate">
                      {log.pipelineStep ? `${log.pipelineStep.order}. ${log.pipelineStep.name}` : 'Geral'}
                    </span>
                    <Badge variant={badge.variant} className="flex items-center gap-1 shrink-0 text-[10px]">
                      {badge.icon}{badge.label}
                    </Badge>
                  </div>
                  {log.duration != null && (
                    <div className="flex items-center gap-1 text-xs text-muted-foreground">
                      <Clock size={11} />{formatDuration(log.duration)}
                    </div>
                  )}
                </button>
              )
            })}
          </div>

          <div className="flex-1 border rounded-lg overflow-y-auto">
            {!selected && (
              <div className="flex items-center justify-center h-full text-sm text-muted-foreground">
                Selecione um step para ver os detalhes
              </div>
            )}
            {selected && (
              <div className="p-4 space-y-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground mb-1.5">Mensagem</p>
                  <p className="text-sm whitespace-pre-wrap bg-muted p-3 rounded">{selected.message}</p>
                </div>

                {selected.context && (
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground mb-1.5">Contexto</p>
                    <pre className="text-xs bg-muted p-3 rounded overflow-x-auto">{selected.context}</pre>
                  </div>
                )}

                {selected.request && (
                  <div>
                    <div className="flex items-center justify-between mb-1.5">
                      <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Requisição</p>
                      <CopyButton text={selected.request} />
                    </div>
                    <pre className="text-xs bg-muted p-3 rounded overflow-x-auto max-h-64 whitespace-pre-wrap break-all">{tryPrettyJson(selected.request)}</pre>
                  </div>
                )}

                {selected.response && (
                  <div>
                    <div className="flex items-center justify-between mb-1.5">
                      <div className="flex items-center gap-2">
                        <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Resposta</p>
                        {selected.httpStatusCode && (
                          <Badge variant={selected.httpStatusCode < 400 ? 'success' : 'destructive'} className="text-[10px]">
                            HTTP {selected.httpStatusCode}
                          </Badge>
                        )}
                      </div>
                      <CopyButton text={selected.response} />
                    </div>
                    <pre className="text-xs bg-muted p-3 rounded overflow-x-auto max-h-64 whitespace-pre-wrap break-all">{tryPrettyJson(selected.response)}</pre>
                  </div>
                )}

                {selected.duration != null && !selected.request && !selected.response && (
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground mb-1.5">Duração</p>
                    <p className="text-sm">{formatDuration(selected.duration)}</p>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </ModalContent>
    </Modal>
  )
}

const columns: DataTableColumn<ExecutionListItem>[] = [
  {
    key: 'type',
    title: 'Tipo',
    dataIndex: 'type',
    width: 110,
    render: (v: number) => TYPE_LABEL[v] ?? '-',
  },
  {
    key: 'connector',
    title: 'Conector',
    dataIndex: 'connector',
    render: (v: ExecutionListItem['connector']) => v?.name ?? '-',
  },
  {
    key: 'integration',
    title: 'Integração',
    dataIndex: 'connector',
    hiddenBelow: 'md',
    render: (v: ExecutionListItem['connector']) => v?.integration?.name ?? '-',
  },
  {
    key: 'pipeline',
    title: 'Pipeline',
    dataIndex: 'pipeline',
    hiddenBelow: 'lg',
    render: (v: ExecutionListItem['pipeline']) => v?.name ?? '-',
  },
  {
    key: 'status',
    title: 'Status',
    dataIndex: 'status',
    width: 110,
    render: (v: number) => <Badge variant={STATUS_VARIANT[v] ?? 'secondary'}>{STATUS_LABEL[v] ?? '-'}</Badge>,
  },
  {
    key: 'startedAt',
    title: 'Início',
    dataIndex: 'startedAt',
    hiddenBelow: 'lg',
    render: (v: string) => formatDateTime(v),
  },
  {
    key: 'duration',
    title: 'Duração',
    dataIndex: 'duration',
    width: 100,
    render: (v: number) => formatDuration(v),
  },
]

export default function IntegrationLogs() {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [totalCount, setTotalCount] = useState(0)
  const [executions, setExecutions] = useState<ExecutionListItem[]>([])
  const [selected, setSelected] = useState<ExecutionListItem | null>(null)
  const [modalOpen, setModalOpen] = useState(false)

  const { execute: fetchExecutions, loading } = useApi<{ items: ExecutionListItem[]; pagination: { page: number; pageSize: number; totalCount: number; totalPages: number } }>()

  const load = async (p: number, ps: number) => {
    const result = await fetchExecutions(() => integrationPlatformService.getExecutions(p, ps))
    if (result) {
      setExecutions(result.items)
      setTotalCount(Number(result.pagination?.totalCount ?? 0))
    }
  }

  useEffect(() => { void load(page, pageSize) }, [page, pageSize])

  const handleRowClick = (row: ExecutionListItem) => {
    setSelected(row)
    setModalOpen(true)
  }

  return (
    <PageLayout
      title="Logs de integração"
      subtitle="Histórico de execuções das integrações configuradas."
      onRefresh={() => void load(page, pageSize)}
    >
      <DataTable
        columns={columns}
        data={executions}
        rowKey="id"
        loading={loading}
        emptyText="Nenhuma execução registrada."
        onRowClick={handleRowClick}
        page={page}
        pageSize={pageSize}
        pageSizeOptions={[10, 20, 50]}
        totalCount={totalCount}
        onPageChange={(p) => setPage(p)}
        onPageSizeChange={(ps) => { setPageSize(ps); setPage(1) }}
      />

      <ExecutionDetailModal
        open={modalOpen}
        onOpenChange={(v) => { setModalOpen(v); if (!v) setSelected(null) }}
        execution={selected}
      />
    </PageLayout>
  )
}
