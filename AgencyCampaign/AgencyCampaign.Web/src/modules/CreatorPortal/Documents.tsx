import { useEffect, useState } from 'react'
import { ExternalLink } from 'lucide-react'
import { creatorPortalService } from '../../services/creatorPortalService'
import {
  campaignDocumentStatusLabels,
  campaignDocumentTypeLabels,
  type CampaignDocument,
  type CampaignDocumentStatusValue,
} from '../../types/campaignDocument'
import { usePortalContext } from './hooks'

const STATUS_COLOR: Record<CampaignDocumentStatusValue, string> = {
  1: 'bg-muted text-muted-foreground',
  2: 'bg-muted text-muted-foreground',
  3: 'bg-amber-500/15 text-amber-600',
  4: 'bg-amber-500/15 text-amber-600',
  5: 'bg-emerald-500/15 text-emerald-600',
  6: 'bg-destructive/15 text-destructive',
  7: 'bg-destructive/15 text-destructive',
}

export default function CreatorPortalDocuments() {
  const { token } = usePortalContext()
  const [documents, setDocuments] = useState<CampaignDocument[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    creatorPortalService.getDocuments(token).then((res) => {
      if (!cancelled) {
        setDocuments(res)
        setLoading(false)
      }
    })
    return () => {
      cancelled = true
    }
  }, [token])

  if (loading) return <p className="text-sm text-muted-foreground">Carregando...</p>
  if (documents.length === 0) return <p className="text-sm text-muted-foreground">Nenhum contrato disponível.</p>

  return (
    <div className="space-y-3">
      <h2 className="text-base font-semibold">Seus contratos</h2>
      {documents.map((d) => (
        <div key={d.id} className="rounded-lg border bg-background p-3">
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0">
              <p className="font-medium">{d.title}</p>
              <p className="text-xs text-muted-foreground">{campaignDocumentTypeLabels[d.documentType]}</p>
            </div>
            <span className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-medium ${STATUS_COLOR[d.status]}`}>
              {campaignDocumentStatusLabels[d.status]}
            </span>
          </div>
          {d.signedDocumentUrl ? (
            <a
              href={d.signedDocumentUrl}
              target="_blank"
              rel="noreferrer"
              className="mt-2 inline-flex items-center gap-1 text-xs text-primary hover:underline"
            >
              <ExternalLink size={12} /> Baixar PDF assinado
            </a>
          ) : d.documentUrl ? (
            <a
              href={d.documentUrl}
              target="_blank"
              rel="noreferrer"
              className="mt-2 inline-flex items-center gap-1 text-xs text-primary hover:underline"
            >
              <ExternalLink size={12} /> Abrir documento original
            </a>
          ) : null}
          {d.signatures.length > 0 && (
            <div className="mt-2 space-y-1 border-t pt-2 text-xs">
              <p className="text-[10px] uppercase tracking-wide text-muted-foreground">Signatários</p>
              {d.signatures.map((s) => (
                <div key={s.id} className="flex items-center justify-between">
                  <span>{s.signerName}</span>
                  <span className={s.isSigned ? 'text-emerald-600' : 'text-muted-foreground'}>
                    {s.isSigned ? 'Assinou' : 'Aguardando'}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      ))}
    </div>
  )
}
