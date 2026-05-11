import { useEffect, useMemo, useState } from 'react'
import {
  Badge,
  Button,
  ConfirmModal,
  Input,
  Modal,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  useApi,
  useI18n,
  useToast,
} from 'archon-ui'
import { Ban, Copy, Plus } from 'lucide-react'
import { creatorAccessTokenService } from '../../services/creatorAccessTokenService'
import type { CreatorAccessToken } from '../../types/creatorAccessToken'
import type { Creator } from '../../types/creator'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  creator: Creator | null
}

export default function CreatorAccessTokensModal({ open, onOpenChange, creator }: Props) {
  const { t } = useI18n()
  const [tokens, setTokens] = useState<CreatorAccessToken[]>([])
  const [expiresAt, setExpiresAt] = useState('')
  const [note, setNote] = useState('')
  const [tokenToRevoke, setTokenToRevoke] = useState<CreatorAccessToken | null>(null)
  const { toast } = useToast()

  const { execute: fetchTokens, loading } = useApi<CreatorAccessToken[]>({ showErrorMessage: true })
  const { execute: issueToken, loading: issuing } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: revokeToken, loading: revoking } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open || !creator) return
    void load()
    setExpiresAt(defaultExpiration())
    setNote('')
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, creator?.id])

  const load = async () => {
    if (!creator) return
    const result = await fetchTokens(() => creatorAccessTokenService.getByCreator(creator.id))
    if (result) setTokens(result)
  }

  const portalBase = useMemo(() => {
    const base = window.location.origin.replace(/\/$/, '')
    return base
  }, [])

  const handleIssue = async () => {
    if (!creator) return
    const result = await issueToken(() =>
      creatorAccessTokenService.issue({
        creatorId: creator.id,
        expiresAt: expiresAt ? new Date(expiresAt).toISOString() : undefined,
        note: note.trim() || undefined,
      }),
    )
    if (result !== null) {
      void load()
      setNote('')
    }
  }

  const confirmRevoke = async () => {
    if (!tokenToRevoke) return
    const result = await revokeToken(() => creatorAccessTokenService.revoke(tokenToRevoke.id))
    if (result !== null) {
      setTokenToRevoke(null)
      void load()
    }
  }

  const copyUrl = async (token: string) => {
    const url = `${portalBase}/portal/${token}`
    try {
      await navigator.clipboard.writeText(url)
      toast({ title: 'URL copiada', variant: 'success' })
    } catch {
      toast({ title: 'Não foi possível copiar', variant: 'destructive' })
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '760px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{t('modal.creatorTokens.title').replace('{0}', creator?.name ?? '')}</ModalTitle>
        </ModalHeader>

        <div className="space-y-4">
          <div className="rounded-lg border bg-primary/5 p-3">
            <h4 className="mb-3 text-sm font-medium">Gerar novo link</h4>
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
              <div className="space-y-1">
                <label className="text-xs text-muted-foreground">Expira em</label>
                <Input type="datetime-local" value={expiresAt} onChange={(e) => setExpiresAt(e.target.value)} />
              </div>
              <div className="space-y-1">
                <label className="text-xs text-muted-foreground">Nota (opcional)</label>
                <Input value={note} onChange={(e) => setNote(e.target.value)} placeholder="Ex: pra assinar contrato campanha X" />
              </div>
            </div>
            <div className="mt-3 flex justify-end">
              <Button size="sm" onClick={() => void handleIssue()} disabled={issuing}>
                <Plus size={14} className="mr-1" /> Gerar link
              </Button>
            </div>
          </div>

          <div className="space-y-2">
            <h4 className="text-sm font-medium">Links existentes</h4>
            {loading && <p className="text-sm text-muted-foreground">Carregando...</p>}
            {!loading && tokens.length === 0 && (
              <p className="text-sm text-muted-foreground">Nenhum link gerado ainda.</p>
            )}
            {tokens.map((tk) => (
              <div key={tk.id} className="rounded-md border bg-background p-3">
                <div className="flex flex-wrap items-start justify-between gap-2">
                  <div className="min-w-0 flex-1">
                    <code className="block truncate font-mono text-xs">
                      {portalBase}/portal/{tk.token}
                    </code>
                    <div className="mt-1 flex flex-wrap gap-2 text-xs text-muted-foreground">
                      {tk.revokedAt ? (
                        <Badge variant="destructive">Revogado</Badge>
                      ) : tk.expiresAt && new Date(tk.expiresAt) <= new Date() ? (
                        <Badge variant="destructive">Expirado</Badge>
                      ) : (
                        <Badge variant="success">Ativo</Badge>
                      )}
                      {tk.expiresAt && (
                        <span>Expira em {new Date(tk.expiresAt).toLocaleString('pt-BR')}</span>
                      )}
                      <span>Usos: {tk.usageCount}</span>
                      {tk.lastUsedAt && (
                        <span>Último: {new Date(tk.lastUsedAt).toLocaleString('pt-BR')}</span>
                      )}
                    </div>
                    {tk.note && <p className="mt-1 text-xs">{tk.note}</p>}
                  </div>
                  <div className="flex gap-1">
                    <Button size="sm" variant="outline" onClick={() => void copyUrl(tk.token)}>
                      <Copy size={14} />
                    </Button>
                    {!tk.revokedAt && (
                      <Button size="sm" variant="outline-danger" disabled={revoking} onClick={() => setTokenToRevoke(tk)}>
                        <Ban size={14} />
                      </Button>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        <ModalFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Fechar</Button>
        </ModalFooter>
      </ModalContent>

      <ConfirmModal
        open={tokenToRevoke !== null}
        onOpenChange={(value) => { if (!value) setTokenToRevoke(null) }}
        title="Revogar link de acesso"
        description={
          tokenToRevoke
            ? `Tem certeza que deseja revogar este link${tokenToRevoke.note ? ` (${tokenToRevoke.note})` : ''}? O creator perde acesso ao portal imediatamente e a ação não pode ser desfeita.`
            : ''
        }
        confirmText="Revogar"
        cancelText="Cancelar"
        variant="danger"
        loading={revoking}
        onConfirm={() => void confirmRevoke()}
      />
    </Modal>
  )
}

function defaultExpiration(): string {
  const date = new Date()
  date.setDate(date.getDate() + 30)
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
  return local.toISOString().slice(0, 16)
}
