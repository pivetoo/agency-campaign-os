import { useEffect, useMemo, useState } from 'react'
import { Card, CardContent, Button, Input, Badge, useApi } from 'archon-ui'
import { Plus, Pencil, Trash2, Filter, RefreshCw, ChevronLeft, ChevronRight, ChevronDown, ChevronRight as ChevronRightSmall } from 'lucide-react'
import { auditService, type AuditPagedResult } from '../../../services/auditService'
import { AuditAction, auditActionLabels, type AuditActionValue, type AuditEntry } from '../../../types/audit'

const todayIso = () => new Date().toISOString().slice(0, 10)
const daysAgoIso = (days: number) => {
  const target = new Date()
  target.setDate(target.getDate() - days)
  return target.toISOString().slice(0, 10)
}

const actionIcon = (action: AuditActionValue) => {
  if (action === AuditAction.Insert) return <Plus className="h-3 w-3" />
  if (action === AuditAction.Delete) return <Trash2 className="h-3 w-3" />
  return <Pencil className="h-3 w-3" />
}

const actionTone = (action: AuditActionValue): 'success' | 'destructive' | 'outline' => {
  if (action === AuditAction.Insert) return 'success'
  if (action === AuditAction.Delete) return 'destructive'
  return 'outline'
}

const formatDateTime = (value: string) => {
  const date = new Date(value)
  return date.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}

const truncate = (value: string | null | undefined, max = 80) => {
  if (!value) return '—'
  return value.length > max ? `${value.slice(0, max)}…` : value
}

export default function AuditActivities() {
  const [from, setFrom] = useState(daysAgoIso(7))
  const [to, setTo] = useState(todayIso())
  const [entityName, setEntityName] = useState('')
  const [action, setAction] = useState<AuditActionValue | ''>('')
  const [changedBy, setChangedBy] = useState('')
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<AuditPagedResult | null>(null)
  const [expandedId, setExpandedId] = useState<number | null>(null)
  const [details, setDetails] = useState<Record<number, AuditEntry>>({})
  const { execute, loading } = useApi<AuditPagedResult>({ showErrorMessage: true })

  const load = async (targetPage = page) => {
    const data = await execute(() =>
      auditService.getRecent({
        from,
        to,
        entityName: entityName.trim() || undefined,
        action: action === '' ? undefined : action,
        changedBy: changedBy.trim() || undefined,
        page: targetPage,
        pageSize: 20,
      }),
    )
    if (data) {
      setResult(data)
      setPage(targetPage)
    }
  }

  useEffect(() => {
    void load(1)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const items = result?.items ?? []
  const pagination = result?.pagination

  const handleApply = () => {
    setExpandedId(null)
    setDetails({})
    void load(1)
  }

  const handleExpand = async (entry: AuditEntry) => {
    if (expandedId === entry.id) {
      setExpandedId(null)
      return
    }
    setExpandedId(entry.id)
    if (!details[entry.id]) {
      const detail = await auditService.getById(entry.id)
      if (detail) setDetails((prev) => ({ ...prev, [entry.id]: detail }))
    }
  }

  const totalPages = pagination?.totalPages ?? 0
  const hasFilters = useMemo(
    () => entityName.trim().length > 0 || action !== '' || changedBy.trim().length > 0,
    [entityName, action, changedBy],
  )

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div className="border-l-4 border-primary pl-5">
          <h1 className="text-3xl font-bold text-foreground tracking-tight">
            <strong className="text-primary">Atividades</strong>
          </h1>
          <p className="text-lg text-muted-foreground mt-3 leading-relaxed">
            Linha do tempo das alterações registradas pelo sistema
          </p>
        </div>
      </div>

      <Card>
        <CardContent className="pt-5">
          <div className="flex flex-wrap items-end gap-3">
            <div>
              <label className="block text-xs text-muted-foreground mb-1">De</label>
              <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="h-9 w-40" />
            </div>
            <div>
              <label className="block text-xs text-muted-foreground mb-1">Até</label>
              <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="h-9 w-40" />
            </div>
            <div>
              <label className="block text-xs text-muted-foreground mb-1">Entidade</label>
              <Input
                value={entityName}
                onChange={(e) => setEntityName(e.target.value)}
                placeholder="Ex.: Brand, Opportunity"
                className="h-9 w-48"
              />
            </div>
            <div>
              <label className="block text-xs text-muted-foreground mb-1">Ação</label>
              <select
                value={action}
                onChange={(e) => setAction(e.target.value === '' ? '' : (Number(e.target.value) as AuditActionValue))}
                className="h-9 rounded-md border bg-background px-2 text-sm"
              >
                <option value="">Todas</option>
                <option value={AuditAction.Insert}>Criação</option>
                <option value={AuditAction.Update}>Edição</option>
                <option value={AuditAction.Delete}>Exclusão</option>
              </select>
            </div>
            <div>
              <label className="block text-xs text-muted-foreground mb-1">Usuário</label>
              <Input
                value={changedBy}
                onChange={(e) => setChangedBy(e.target.value)}
                placeholder="email@empresa.com"
                className="h-9 w-56"
              />
            </div>
            <Button size="sm" onClick={handleApply} disabled={loading}>
              <Filter className="mr-1 h-3.5 w-3.5" /> Aplicar filtros
            </Button>
            {hasFilters && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  setEntityName('')
                  setAction('')
                  setChangedBy('')
                }}
                disabled={loading}
              >
                Limpar
              </Button>
            )}
            <Button variant="outline" size="sm" onClick={() => void load(page)} disabled={loading}>
              <RefreshCw className="mr-1 h-3.5 w-3.5" /> Atualizar
            </Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="pt-5">
          {items.length === 0 ? (
            <p className="py-12 text-center text-sm text-muted-foreground">
              {loading ? 'Carregando...' : 'Nenhum evento encontrado para os filtros aplicados.'}
            </p>
          ) : (
            <ul className="divide-y">
              {items.map((entry) => {
                const isExpanded = expandedId === entry.id
                const detail = details[entry.id]
                return (
                  <li key={entry.id}>
                    <button
                      type="button"
                      className="flex w-full items-start gap-3 px-2 py-3 text-left hover:bg-muted/30"
                      onClick={() => void handleExpand(entry)}
                    >
                      <span className="mt-0.5 text-muted-foreground">
                        {isExpanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRightSmall className="h-4 w-4" />}
                      </span>
                      <Badge variant={actionTone(entry.action)} className="gap-1">
                        {actionIcon(entry.action)} {auditActionLabels[entry.action]}
                      </Badge>
                      <div className="flex-1">
                        <p className="text-sm">
                          <span className="font-medium">{entry.changedBy ?? 'Sistema'}</span>{' '}
                          <span className="text-muted-foreground">em</span>{' '}
                          <span className="font-medium">{entry.entityName}</span>{' '}
                          <span className="font-mono text-xs text-muted-foreground">#{entry.entityId}</span>
                        </p>
                        {entry.parentEntityName && (
                          <p className="text-xs text-muted-foreground">
                            Origem: {entry.parentEntityName} #{entry.parentEntityId}
                          </p>
                        )}
                      </div>
                      <span className="text-xs text-muted-foreground whitespace-nowrap">
                        {formatDateTime(entry.changedAt)}
                      </span>
                    </button>
                    {isExpanded && (
                      <div className="ml-10 mb-3 rounded-md border bg-muted/20 p-3 text-xs">
                        {!detail && <p className="text-muted-foreground">Carregando detalhes...</p>}
                        {detail && detail.propertyChanges.length === 0 && (
                          <p className="text-muted-foreground">Sem alterações de propriedade registradas.</p>
                        )}
                        {detail && detail.propertyChanges.length > 0 && (
                          <table className="w-full">
                            <thead>
                              <tr className="text-left text-[10px] uppercase tracking-wider text-muted-foreground">
                                <th className="pb-1 pr-3">Propriedade</th>
                                <th className="pb-1 pr-3">Antes</th>
                                <th className="pb-1">Depois</th>
                              </tr>
                            </thead>
                            <tbody>
                              {detail.propertyChanges.map((change, idx) => (
                                <tr key={`${change.propertyName}-${idx}`} className="border-t border-border/40">
                                  <td className="py-1 pr-3 font-medium">{change.propertyName}</td>
                                  <td className="py-1 pr-3 text-muted-foreground" title={change.oldValue ?? ''}>
                                    {truncate(change.oldValue)}
                                  </td>
                                  <td className="py-1 text-foreground" title={change.newValue ?? ''}>
                                    {truncate(change.newValue)}
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        )}
                      </div>
                    )}
                  </li>
                )
              })}
            </ul>
          )}
        </CardContent>
      </Card>

      {pagination && pagination.totalPages > 1 && (
        <div className="flex items-center justify-between text-sm">
          <span className="text-muted-foreground">
            Página {pagination.page} de {totalPages} · {pagination.totalCount} eventos
          </span>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => void load(page - 1)}
              disabled={loading || page <= 1}
            >
              <ChevronLeft className="mr-1 h-3.5 w-3.5" /> Anterior
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => void load(page + 1)}
              disabled={loading || page >= totalPages}
            >
              Próxima <ChevronRight className="ml-1 h-3.5 w-3.5" />
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
