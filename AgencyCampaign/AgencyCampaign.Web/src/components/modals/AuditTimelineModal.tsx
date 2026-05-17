import { useEffect, useState } from 'react'
import {
  Modal,
  ModalContent,
  ModalHeader,
  ModalTitle,
  ModalDescription,
  Badge,
  useApi,
} from 'archon-ui'
import { ChevronDown, ChevronRight, FilePlus, FileX, Pencil, History, Loader2 } from 'lucide-react'
import { auditService } from '../../services/auditService'
import { AuditActionValue, type AuditAction, type AuditEntry, type AuditPropertyChange } from '../../types/audit'

interface AuditTimelineModalProps {
  entityName: string
  entityId: string | number | null
  entityLabel?: string
  open: boolean
  onClose: () => void
}

const actionMeta: Record<AuditAction, { label: string; variant: 'success' | 'secondary' | 'destructive'; icon: typeof FilePlus; iconClass: string }> = {
  [AuditActionValue.Insert]: { label: 'Criado', variant: 'success', icon: FilePlus, iconClass: 'bg-success/15 text-success' },
  [AuditActionValue.Update]: { label: 'Atualizado', variant: 'secondary', icon: Pencil, iconClass: 'bg-primary/15 text-primary' },
  [AuditActionValue.Delete]: { label: 'Excluído', variant: 'destructive', icon: FileX, iconClass: 'bg-destructive/15 text-destructive' },
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

function formatPropertyName(raw: string): string {
  return raw
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, (c) => c.toUpperCase())
    .trim()
}

function formatValue(value: string | null | undefined): string {
  if (value === null || value === undefined || value === '') return '—'
  return value
}

interface EntryRowProps {
  entry: AuditEntry
  expanded: boolean
  loadingDetail: boolean
  detail: AuditEntry | null
  onToggle: () => void
}

function EntryRow({ entry, expanded, loadingDetail, detail, onToggle }: EntryRowProps) {
  const meta = actionMeta[entry.action]
  const Icon = meta.icon
  const changes: AuditPropertyChange[] = detail?.propertyChanges ?? []

  return (
    <div className="rounded-lg border bg-background">
      <button
        type="button"
        onClick={onToggle}
        className="flex w-full items-start gap-3 px-4 py-3 text-left transition-colors hover:bg-muted/30"
      >
        <div className={`mt-0.5 rounded-md p-1.5 ${meta.iconClass}`}>
          <Icon className="h-3.5 w-3.5" />
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant={meta.variant}>{meta.label}</Badge>
            <span className="text-xs text-muted-foreground">{formatDate(entry.changedAt)}</span>
          </div>
          <div className="mt-1 truncate text-sm text-foreground">
            {entry.changedBy ?? 'Origem do sistema'}
          </div>
        </div>
        <div className="mt-1 text-muted-foreground">
          {expanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
        </div>
      </button>

      {expanded ? (
        <div className="border-t bg-muted/10 px-4 py-3">
          {loadingDetail ? (
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" /> Carregando detalhes…
            </div>
          ) : entry.action === AuditActionValue.Delete ? (
            <p className="text-sm text-muted-foreground">Registro excluído. Sem detalhes de propriedades.</p>
          ) : changes.length === 0 ? (
            <p className="text-sm text-muted-foreground">Nenhuma mudança de propriedade registrada para esta operação.</p>
          ) : (
            <div className="space-y-2">
              {changes.map((change) => (
                <div key={change.propertyName} className="rounded-md border bg-background px-3 py-2">
                  <div className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                    {formatPropertyName(change.propertyName)}
                  </div>
                  <div className="mt-2 grid gap-2 sm:grid-cols-2">
                    <div>
                      <div className="text-[10px] uppercase text-muted-foreground">Antes</div>
                      <div className="mt-0.5 break-words rounded border bg-muted/20 p-2 font-mono text-xs">
                        {formatValue(change.oldValue)}
                      </div>
                    </div>
                    <div>
                      <div className="text-[10px] uppercase text-muted-foreground">Depois</div>
                      <div className="mt-0.5 break-words rounded border bg-muted/20 p-2 font-mono text-xs">
                        {formatValue(change.newValue)}
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      ) : null}
    </div>
  )
}

export default function AuditTimelineModal({ entityName, entityId, entityLabel, open, onClose }: AuditTimelineModalProps) {
  const [entries, setEntries] = useState<AuditEntry[]>([])
  const [expandedId, setExpandedId] = useState<number | null>(null)
  const [detailsCache, setDetailsCache] = useState<Record<number, AuditEntry>>({})
  const [loadingDetailId, setLoadingDetailId] = useState<number | null>(null)

  const { execute: fetchEntries, loading } = useApi<AuditEntry[]>({ showErrorMessage: true })
  const { execute: fetchDetail } = useApi<AuditEntry | null>({ showErrorMessage: true })

  useEffect(() => {
    if (!open || entityId === null) return
    setEntries([])
    setExpandedId(null)
    setDetailsCache({})
    void fetchEntries(() => auditService.getByEntity(entityName, entityId)).then((result) => {
      if (result) setEntries(result)
    })
  }, [open, entityId, entityName])

  async function handleToggle(entry: AuditEntry) {
    if (expandedId === entry.id) {
      setExpandedId(null)
      return
    }
    setExpandedId(entry.id)
    if (detailsCache[entry.id] || entry.action === AuditActionValue.Delete) {
      return
    }
    setLoadingDetailId(entry.id)
    try {
      const detail = await fetchDetail(() => auditService.getById(entry.id))
      if (detail) {
        setDetailsCache((prev) => ({ ...prev, [entry.id]: detail }))
      }
    } finally {
      setLoadingDetailId(null)
    }
  }

  return (
    <Modal open={open} onOpenChange={(v) => { if (!v) onClose() }}>
      <ModalContent size="3xl" className="max-h-[85vh]">
        <ModalHeader>
          <ModalTitle className="flex items-center gap-2">
            <History className="h-4 w-4 text-primary" />
            Auditoria{entityLabel ? ` · ${entityLabel}` : ''}{entityId !== null ? ` #${entityId}` : ''}
          </ModalTitle>
          <ModalDescription>
            Histórico completo de alterações deste registro. Clique numa entrada para ver o diff campo a campo.
          </ModalDescription>
        </ModalHeader>

        <div className="-mx-1 mt-2 max-h-[60vh] space-y-2 overflow-y-auto px-1 pb-2">
          {loading ? (
            <div className="flex items-center justify-center gap-2 rounded-md border border-dashed p-8 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" /> Carregando histórico…
            </div>
          ) : entries.length === 0 ? (
            <div className="rounded-md border border-dashed p-8 text-center text-sm text-muted-foreground">
              Nenhum registro de auditoria encontrado para este item.
            </div>
          ) : (
            entries.map((entry) => (
              <EntryRow
                key={entry.id}
                entry={entry}
                expanded={expandedId === entry.id}
                loadingDetail={loadingDetailId === entry.id}
                detail={detailsCache[entry.id] ?? null}
                onToggle={() => void handleToggle(entry)}
              />
            ))
          )}
        </div>
      </ModalContent>
    </Modal>
  )
}
