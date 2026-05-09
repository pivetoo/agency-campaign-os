import { useEffect, useMemo, useState } from 'react'
import {
  Button,
  Input,
  Modal,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  SearchableSelect,
  useApi,
} from 'archon-ui'
import { creatorPaymentService } from '../../services/creatorPaymentService'
import { integrationPlatformService } from '../../services/integrationPlatformService'
import type { CreatorPayment } from '../../types/creatorPayment'
import type {
  Connector,
  IntegrationCategory,
  IntegrationPlatformIntegration,
  Pipeline,
} from '../../types/integrationPlatform'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  payments: CreatorPayment[]
  onSuccess: () => void
}

const PAYMENT_CATEGORY_HINTS = ['pagamento', 'payment', 'transfer', 'repasse']

export default function CreatorPaymentScheduleBatchModal({ open, onOpenChange, payments, onSuccess }: Props) {
  const [categories, setCategories] = useState<IntegrationCategory[]>([])
  const [categoryId, setCategoryId] = useState<number | undefined>()
  const [integrations, setIntegrations] = useState<IntegrationPlatformIntegration[]>([])
  const [integrationId, setIntegrationId] = useState<number | undefined>()
  const [connectors, setConnectors] = useState<Connector[]>([])
  const [connectorId, setConnectorId] = useState<number | undefined>()
  const [pipelines, setPipelines] = useState<Pipeline[]>([])
  const [pipelineId, setPipelineId] = useState<number | undefined>()
  const [scheduledFor, setScheduledFor] = useState('')

  const { execute: loadCategories, loading: catLoading } = useApi<IntegrationCategory[]>({ showErrorMessage: true })
  const { execute: loadIntegrations, loading: intLoading } = useApi<IntegrationPlatformIntegration[]>({ showErrorMessage: true })
  const { execute: loadConnectors, loading: connLoading } = useApi<Connector[]>({ showErrorMessage: true })
  const { execute: loadPipelines, loading: pipeLoading } = useApi<Pipeline[]>({ showErrorMessage: true })
  const { execute: send, loading: sending } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    setScheduledFor('')

    void loadCategories(() => integrationPlatformService.getActiveIntegrationCategories()).then((result) => {
      if (!result) return
      setCategories(result)
      const paymentCategory = result.find((c) =>
        PAYMENT_CATEGORY_HINTS.some((hint) => c.name.toLowerCase().includes(hint)),
      )
      setCategoryId(paymentCategory?.id ?? result[0]?.id)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  useEffect(() => {
    if (!categoryId) {
      setIntegrations([])
      setIntegrationId(undefined)
      return
    }
    void loadIntegrations(() => integrationPlatformService.getIntegrationsByCategory(categoryId)).then((result) => {
      if (!result) return
      setIntegrations(result)
      setIntegrationId(result[0]?.id)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [categoryId])

  useEffect(() => {
    if (!integrationId) {
      setConnectors([])
      setConnectorId(undefined)
      setPipelines([])
      setPipelineId(undefined)
      return
    }
    void loadConnectors(() => integrationPlatformService.getConnectorsByIntegration(integrationId)).then((result) => {
      if (!result) return
      const active = result.filter((c) => c.isActive)
      setConnectors(active)
      setConnectorId(active[0]?.id)
    })
    void loadPipelines(() => integrationPlatformService.getPipelinesByIntegration(integrationId)).then((result) => {
      if (!result) return
      const active = result.filter((p) => p.isActive)
      setPipelines(active)
      const sendPipeline = active.find((p) => p.identifier.includes('send') || p.identifier.includes('pix') || p.identifier.includes('transfer'))
      setPipelineId((sendPipeline ?? active[0])?.id)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [integrationId])

  const totals = useMemo(() => {
    const gross = payments.reduce((sum, p) => sum + (p.grossAmount || 0), 0)
    const net = payments.reduce((sum, p) => sum + (p.netAmount || 0), 0)
    const missingPix = payments.filter((p) => !p.creatorPixKey && !p.pixKey).length
    return { gross, net, missingPix }
  }, [payments])

  const isValid = !!connectorId && !!pipelineId && payments.length > 0

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!connectorId || !pipelineId) return
    const result = await send(() =>
      creatorPaymentService.scheduleBatch({
        connectorId,
        pipelineId,
        creatorPaymentIds: payments.map((p) => p.id),
        scheduledFor: scheduledFor ? new Date(scheduledFor).toISOString() : undefined,
      }),
    )
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '760px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>Agendar lote de pagamentos</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-3 gap-2 rounded-lg border bg-primary/5 p-3 text-sm">
            <div>
              <p className="text-xs text-muted-foreground">Selecionados</p>
              <p className="font-semibold">{payments.length}</p>
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Total bruto</p>
              <p className="font-semibold">R$ {totals.gross.toFixed(2)}</p>
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Total líquido</p>
              <p className="font-semibold">R$ {totals.net.toFixed(2)}</p>
            </div>
            {totals.missingPix > 0 && (
              <div className="col-span-3 rounded bg-destructive/10 p-2 text-xs text-destructive">
                {totals.missingPix} creator(s) sem chave PIX cadastrada — esses pagamentos serão pulados.
              </div>
            )}
          </div>

          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">Categoria</label>
              <SearchableSelect
                value={categoryId ? String(categoryId) : ''}
                onValueChange={(value) => setCategoryId(value ? Number(value) : undefined)}
                options={categories.map((c) => ({ value: String(c.id), label: c.name }))}
                placeholder={catLoading ? 'Carregando...' : 'Selecione'}
                searchPlaceholder="Buscar"
                disabled={catLoading}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Integração</label>
              <SearchableSelect
                value={integrationId ? String(integrationId) : ''}
                onValueChange={(value) => setIntegrationId(value ? Number(value) : undefined)}
                options={integrations.map((i) => ({ value: String(i.id), label: i.name }))}
                placeholder={intLoading ? 'Carregando...' : integrations.length === 0 ? 'Sem integrações' : 'Selecione'}
                searchPlaceholder="Buscar"
                disabled={intLoading || integrations.length === 0}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Conector</label>
              <SearchableSelect
                value={connectorId ? String(connectorId) : ''}
                onValueChange={(value) => setConnectorId(value ? Number(value) : undefined)}
                options={connectors.map((c) => ({ value: String(c.id), label: c.name }))}
                placeholder={connLoading ? 'Carregando...' : connectors.length === 0 ? 'Configure um conector primeiro' : 'Selecione'}
                searchPlaceholder="Buscar"
                disabled={connLoading || connectors.length === 0}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Pipeline de envio</label>
              <SearchableSelect
                value={pipelineId ? String(pipelineId) : ''}
                onValueChange={(value) => setPipelineId(value ? Number(value) : undefined)}
                options={pipelines.map((p) => ({ value: String(p.id), label: p.name }))}
                placeholder={pipeLoading ? 'Carregando...' : pipelines.length === 0 ? 'Sem pipelines' : 'Selecione'}
                searchPlaceholder="Buscar"
                disabled={pipeLoading || pipelines.length === 0}
              />
            </div>
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Data prevista de pagamento (opcional)</label>
              <Input type="datetime-local" value={scheduledFor} onChange={(e) => setScheduledFor(e.target.value)} />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={sending || !isValid}>
              {sending ? 'Enviando...' : `Enviar ${payments.length} pagamento(s)`}
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
