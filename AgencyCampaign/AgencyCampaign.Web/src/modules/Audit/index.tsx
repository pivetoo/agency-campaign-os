import { useEffect, useMemo, useState } from 'react'
import { Activity, FilePlus, FileX, Pencil, ShieldAlert } from 'lucide-react'
import {
  Badge,
  Card,
  CardContent,
  DataTable,
  Input,
  PageLayout,
  SearchableSelect,
  Sheet,
  SheetContent,
  SheetPreviewField,
  SheetPreviewGrid,
  SheetPreviewHeader,
  SheetPreviewSection,
  TableToolbar,
  useApi,
  usePermissions,
} from 'archon-ui'
import type { DataTableColumn, PaginatedResult } from 'archon-ui'
import { auditService } from '../../services/auditService'
import type { AuditAction, AuditEntry, AuditSearchFilters, AuditStats } from '../../types/audit'
import { AuditActionValue } from '../../types/audit'

const actionLabels: Record<AuditAction, string> = {
  [AuditActionValue.Insert]: 'Criado',
  [AuditActionValue.Update]: 'Atualizado',
  [AuditActionValue.Delete]: 'Excluído',
}

const actionVariants: Record<AuditAction, 'success' | 'secondary' | 'destructive'> = {
  [AuditActionValue.Insert]: 'success',
  [AuditActionValue.Update]: 'secondary',
  [AuditActionValue.Delete]: 'destructive',
}

const actionOptions = [
  { value: '', label: 'Todas as ações' },
  { value: String(AuditActionValue.Insert), label: 'Criado' },
  { value: String(AuditActionValue.Update), label: 'Atualizado' },
  { value: String(AuditActionValue.Delete), label: 'Excluído' },
]

function formatDate(value: string): string {
  return new Date(value).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'medium' })
}

function truncate(value: string | null | undefined, max = 80): string {
  if (!value) return '-'
  return value.length > max ? `${value.slice(0, max)}…` : value
}

export default function Audit() {
  const { isRoot } = usePermissions()

  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)
  const [entityName, setEntityName] = useState('')
  const [debouncedEntity, setDebouncedEntity] = useState('')
  const [changedBy, setChangedBy] = useState('')
  const [debouncedChangedBy, setDebouncedChangedBy] = useState('')
  const [actionFilter, setActionFilter] = useState<string>('')
  const [previewEntry, setPreviewEntry] = useState<AuditEntry | null>(null)
  const [selectedRows, setSelectedRows] = useState<AuditEntry[]>([])
  const [entries, setEntries] = useState<AuditEntry[]>([])
  const [stats, setStats] = useState<AuditStats | null>(null)

  const { execute: fetchEntries, loading, pagination } = useApi<PaginatedResult<AuditEntry>>({ showErrorMessage: true })
  const { execute: fetchStats } = useApi<AuditStats | null>({ showErrorMessage: false })

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedEntity(entityName), 300)
    return () => clearTimeout(timeout)
  }, [entityName])

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedChangedBy(changedBy), 300)
    return () => clearTimeout(timeout)
  }, [changedBy])

  useEffect(() => {
    setPage(1)
  }, [debouncedEntity, debouncedChangedBy, actionFilter])

  useEffect(() => {
    if (!isRoot) return
    const filters: AuditSearchFilters = {
      entityName: debouncedEntity || undefined,
      changedBy: debouncedChangedBy || undefined,
      action: actionFilter ? (Number(actionFilter) as AuditAction) : undefined,
    }
    void fetchEntries(() => auditService.recent(filters, { page, pageSize, orderBy: 'id' })).then((result) => {
      if (result) setEntries(result.data ?? [])
    })
  }, [isRoot, debouncedEntity, debouncedChangedBy, actionFilter, page, pageSize])

  useEffect(() => {
    if (!isRoot) return
    void fetchStats(() => auditService.stats()).then((result) => {
      if (result) setStats(result)
    })
  }, [isRoot])

  const actionCounts = useMemo(() => {
    if (!stats) return { Insert: 0, Update: 0, Delete: 0 }
    const map = { Insert: 0, Update: 0, Delete: 0 } as Record<string, number>
    for (const item of stats.actionDistribution) {
      if (item.action === AuditActionValue.Insert) map.Insert = item.count
      if (item.action === AuditActionValue.Update) map.Update = item.count
      if (item.action === AuditActionValue.Delete) map.Delete = item.count
    }
    return map
  }, [stats])

  if (!isRoot) {
    return (
      <PageLayout title="Auditoria" subtitle="Histórico de alterações no sistema" showDefaultActions={false}>
        <div className="flex flex-col items-center justify-center rounded-md border border-dashed bg-muted/20 p-10 text-center">
          <ShieldAlert className="mb-3 h-10 w-10 text-muted-foreground" />
          <h3 className="text-base font-semibold text-foreground">Acesso restrito</h3>
          <p className="mt-1 max-w-md text-sm text-muted-foreground">
            Apenas administradores do contrato podem consultar o histórico de auditoria.
          </p>
        </div>
      </PageLayout>
    )
  }

  const columns: DataTableColumn<AuditEntry>[] = [
    {
      key: 'changedAt',
      title: 'Data',
      dataIndex: 'changedAt',
      render: (value: string) => formatDate(value),
    },
    {
      key: 'action',
      title: 'Ação',
      dataIndex: 'action',
      render: (value: AuditAction) => <Badge variant={actionVariants[value]}>{actionLabels[value]}</Badge>,
    },
    {
      key: 'entityName',
      title: 'Entidade',
      dataIndex: 'entityName',
    },
    {
      key: 'entityId',
      title: 'ID',
      dataIndex: 'entityId',
    },
    {
      key: 'changedBy',
      title: 'Usuário',
      dataIndex: 'changedBy',
      render: (value?: string | null) => value ?? '—',
    },
    {
      key: 'propertyChanges',
      title: 'Mudanças',
      dataIndex: 'propertyChanges',
      render: (value: AuditEntry['propertyChanges']) => value?.length ?? 0,
    },
  ]

  return (
    <>
      <PageLayout title="Auditoria" subtitle="Histórico de alterações no contrato ativo" showDefaultActions={false}>
        {stats ? (
          <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Card>
              <CardContent className="flex items-center gap-3 py-4">
                <div className="rounded-md bg-primary/10 p-2 text-primary">
                  <Activity className="h-5 w-5" />
                </div>
                <div>
                  <div className="text-xs uppercase tracking-wide text-muted-foreground">Total</div>
                  <div className="text-lg font-semibold text-foreground">{stats.totalEntries}</div>
                </div>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="flex items-center gap-3 py-4">
                <div className="rounded-md bg-success/15 p-2 text-success">
                  <FilePlus className="h-5 w-5" />
                </div>
                <div>
                  <div className="text-xs uppercase tracking-wide text-muted-foreground">Criações</div>
                  <div className="text-lg font-semibold text-foreground">{actionCounts.Insert}</div>
                </div>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="flex items-center gap-3 py-4">
                <div className="rounded-md bg-secondary/15 p-2 text-secondary-foreground">
                  <Pencil className="h-5 w-5" />
                </div>
                <div>
                  <div className="text-xs uppercase tracking-wide text-muted-foreground">Atualizações</div>
                  <div className="text-lg font-semibold text-foreground">{actionCounts.Update}</div>
                </div>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="flex items-center gap-3 py-4">
                <div className="rounded-md bg-destructive/15 p-2 text-destructive">
                  <FileX className="h-5 w-5" />
                </div>
                <div>
                  <div className="text-xs uppercase tracking-wide text-muted-foreground">Exclusões</div>
                  <div className="text-lg font-semibold text-foreground">{actionCounts.Delete}</div>
                </div>
              </CardContent>
            </Card>
          </div>
        ) : null}

        <TableToolbar
          rightSlot={
            <div className="flex flex-wrap items-center gap-2">
              <Input
                value={entityName}
                onChange={(e) => setEntityName(e.target.value)}
                placeholder="Filtrar por entidade…"
                className="w-48"
              />
              <Input
                value={changedBy}
                onChange={(e) => setChangedBy(e.target.value)}
                placeholder="Filtrar por usuário…"
                className="w-48"
              />
              <SearchableSelect
                value={actionFilter}
                onValueChange={setActionFilter}
                options={actionOptions}
                placeholder="Todas as ações"
                className="w-44"
              />
            </div>
          }
          className="mb-3"
        />

        <DataTable
          columns={columns}
          data={entries}
          rowKey="id"
          loading={loading}
          emptyText="Nenhum registro encontrado."
          selectable
          selectedRows={selectedRows}
          onSelectionChange={setSelectedRows}
          onRowDoubleClick={setPreviewEntry}
          pageSize={pageSize}
          pageSizeOptions={[10, 25, 50, 100]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(s) => {
            setPageSize(s)
            setPage(1)
          }}
        />
      </PageLayout>

      <Sheet open={!!previewEntry} onOpenChange={(open) => !open && setPreviewEntry(null)}>
        <SheetContent side="right" className="w-full sm:max-w-2xl">
          {previewEntry ? (
            <div className="flex h-full flex-col">
              <SheetPreviewHeader
                title={`${previewEntry.entityName} #${previewEntry.entityId}`}
                meta={
                  <>
                    <Badge variant={actionVariants[previewEntry.action]}>{actionLabels[previewEntry.action]}</Badge>
                    <span className="text-xs text-muted-foreground">{formatDate(previewEntry.changedAt)}</span>
                  </>
                }
                description={previewEntry.changedBy ? `Alterado por ${previewEntry.changedBy}` : 'Origem do sistema'}
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title="Contexto">
                  <SheetPreviewGrid>
                    <SheetPreviewField label="Entidade" value={previewEntry.entityName} />
                    <SheetPreviewField label="ID" value={previewEntry.entityId} />
                    <SheetPreviewField label="Origem" value={previewEntry.source ?? '—'} />
                    <SheetPreviewField label="Correlation ID" value={previewEntry.correlationId ?? '—'} />
                    {previewEntry.parentEntityName ? (
                      <SheetPreviewField label="Entidade pai" value={`${previewEntry.parentEntityName} #${previewEntry.parentEntityId}`} />
                    ) : null}
                  </SheetPreviewGrid>
                </SheetPreviewSection>

                <SheetPreviewSection
                  title="Mudanças de campo"
                  description={`${previewEntry.propertyChanges.length} campo(s) alterado(s)`}
                >
                  {previewEntry.propertyChanges.length === 0 ? (
                    <p className="text-sm text-muted-foreground">Sem detalhes de propriedades para esta operação.</p>
                  ) : (
                    <div className="space-y-2">
                      {previewEntry.propertyChanges.map((change) => (
                        <div key={change.propertyName} className="rounded-lg border bg-muted/10 px-3 py-2">
                          <div className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                            {change.propertyName}
                          </div>
                          <div className="mt-2 grid gap-2 sm:grid-cols-2">
                            <div>
                              <div className="text-[10px] uppercase text-muted-foreground">Antes</div>
                              <div className="rounded border bg-background p-2 font-mono text-xs">
                                {truncate(change.oldValue, 200)}
                              </div>
                            </div>
                            <div>
                              <div className="text-[10px] uppercase text-muted-foreground">Depois</div>
                              <div className="rounded border bg-background p-2 font-mono text-xs">
                                {truncate(change.newValue, 200)}
                              </div>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </SheetPreviewSection>
              </div>
            </div>
          ) : null}
        </SheetContent>
      </Sheet>
    </>
  )
}
