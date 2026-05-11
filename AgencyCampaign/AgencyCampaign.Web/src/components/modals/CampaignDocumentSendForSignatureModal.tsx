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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  useApi,
  useI18n,
} from 'archon-ui'
import { Plus, Trash2 } from 'lucide-react'
import { campaignDocumentService, type CampaignDocumentSignerInput } from '../../services/campaignDocumentService'
import { integrationPlatformService } from '../../services/integrationPlatformService'
import {
  CampaignDocumentSignerRole,
  campaignDocumentSignerRoleLabels,
  type CampaignDocument,
  type CampaignDocumentSignerRoleValue,
} from '../../types/campaignDocument'
import type {
  Connector,
  IntegrationCategory,
  IntegrationPlatformIntegration,
  Pipeline,
} from '../../types/integrationPlatform'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  document: CampaignDocument | null
  onSuccess: () => void
}

const SIGNATURE_CATEGORY_HINTS = ['assinatura', 'signature']

export default function CampaignDocumentSendForSignatureModal({ open, onOpenChange, document, onSuccess }: Props) {
  const { t } = useI18n()
  const [categories, setCategories] = useState<IntegrationCategory[]>([])
  const [categoryId, setCategoryId] = useState<number | undefined>()
  const [integrations, setIntegrations] = useState<IntegrationPlatformIntegration[]>([])
  const [integrationId, setIntegrationId] = useState<number | undefined>()
  const [connectors, setConnectors] = useState<Connector[]>([])
  const [connectorId, setConnectorId] = useState<number | undefined>()
  const [pipelines, setPipelines] = useState<Pipeline[]>([])
  const [pipelineId, setPipelineId] = useState<number | undefined>()
  const [signers, setSigners] = useState<CampaignDocumentSignerInput[]>([])

  const { execute: loadCategories, loading: catLoading } = useApi<IntegrationCategory[]>({ showErrorMessage: true })
  const { execute: loadIntegrations, loading: intLoading } = useApi<IntegrationPlatformIntegration[]>({ showErrorMessage: true })
  const { execute: loadConnectors, loading: connLoading } = useApi<Connector[]>({ showErrorMessage: true })
  const { execute: loadPipelines, loading: pipeLoading } = useApi<Pipeline[]>({ showErrorMessage: true })
  const { execute: send, loading: sending } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open || !document) return

    setSigners(buildInitialSigners(document))

    void loadCategories(() => integrationPlatformService.getActiveIntegrationCategories()).then((result) => {
      if (!result) return
      setCategories(result)
      const signatureCategory = result.find((category) =>
        SIGNATURE_CATEGORY_HINTS.some((hint) => category.name.toLowerCase().includes(hint)),
      )
      setCategoryId(signatureCategory?.id ?? result[0]?.id)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, document?.id])

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
      setPipelineId(active[0]?.id)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [integrationId])

  const categoryOptions = useMemo(
    () => categories.map((c) => ({ value: String(c.id), label: c.name })),
    [categories],
  )
  const integrationOptions = useMemo(
    () => integrations.map((i) => ({ value: String(i.id), label: i.name })),
    [integrations],
  )
  const connectorOptions = useMemo(
    () => connectors.map((c) => ({ value: String(c.id), label: c.name })),
    [connectors],
  )
  const pipelineOptions = useMemo(
    () => pipelines.map((p) => ({ value: String(p.id), label: p.name })),
    [pipelines],
  )

  const isValid =
    !!connectorId &&
    !!pipelineId &&
    signers.length > 0 &&
    signers.every((s) => s.name.trim().length >= 2 && s.email.trim().length > 0)

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!document || !connectorId || !pipelineId) return

    const result = await send(() =>
      campaignDocumentService.sendForSignature(document.id, {
        connectorId,
        pipelineId,
        signers,
      }),
    )
    if (result !== null) onSuccess()
  }

  const updateSigner = (index: number, patch: Partial<CampaignDocumentSignerInput>) => {
    setSigners((prev) => prev.map((signer, i) => (i === index ? { ...signer, ...patch } : signer)))
  }

  const removeSigner = (index: number) => {
    setSigners((prev) => prev.filter((_, i) => i !== index))
  }

  const addSigner = () => {
    setSigners((prev) => [
      ...prev,
      { role: CampaignDocumentSignerRole.Other, name: '', email: '', documentNumber: '' },
    ])
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '880px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{t('modal.document.title.sendForSignature')}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.automation.field.integrationCategory')}</label>
              <SearchableSelect
                value={categoryId ? String(categoryId) : ''}
                onValueChange={(value) => setCategoryId(value ? Number(value) : undefined)}
                options={categoryOptions}
                placeholder={catLoading ? t('common.loading') : t('common.placeholder.select')}
                searchPlaceholder={t('common.placeholder.search')}
                disabled={catLoading}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.automation.field.integration')}</label>
              <SearchableSelect
                value={integrationId ? String(integrationId) : ''}
                onValueChange={(value) => setIntegrationId(value ? Number(value) : undefined)}
                options={integrationOptions}
                placeholder={intLoading ? t('common.loading') : integrationOptions.length === 0 ? t('modal.document.placeholder.noIntegration') : t('common.placeholder.select')}
                searchPlaceholder={t('common.placeholder.search')}
                disabled={intLoading || integrationOptions.length === 0}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Conector</label>
              <SearchableSelect
                value={connectorId ? String(connectorId) : ''}
                onValueChange={(value) => setConnectorId(value ? Number(value) : undefined)}
                options={connectorOptions}
                placeholder={connLoading ? t('common.loading') : connectorOptions.length === 0 ? t('modal.document.placeholder.configureConnector') : t('common.placeholder.select')}
                searchPlaceholder={t('common.placeholder.search')}
                disabled={connLoading || connectorOptions.length === 0}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.document.field.sendPipeline')}</label>
              <SearchableSelect
                value={pipelineId ? String(pipelineId) : ''}
                onValueChange={(value) => setPipelineId(value ? Number(value) : undefined)}
                options={pipelineOptions}
                placeholder={pipeLoading ? t('common.loading') : pipelineOptions.length === 0 ? t('modal.document.placeholder.noPipelines') : t('common.placeholder.select')}
                searchPlaceholder={t('common.placeholder.search')}
                disabled={pipeLoading || pipelineOptions.length === 0}
              />
            </div>
          </div>

          <div className="space-y-3 rounded-lg border bg-primary/5 p-4">
            <div className="flex items-center justify-between">
              <span className="text-sm font-medium">{t('modal.document.field.signers')}</span>
              <Button type="button" size="sm" variant="outline" onClick={addSigner}>
                <Plus className="mr-1 h-4 w-4" /> {t('common.action.add')}
              </Button>
            </div>
            {signers.length === 0 && (
              <p className="text-sm text-muted-foreground">{t('modal.document.addSigner')}</p>
            )}
            {signers.map((signer, index) => (
              <div key={index} className="grid grid-cols-1 gap-2 rounded-md border bg-background p-3 md:grid-cols-12">
                <div className="md:col-span-3">
                  <label className="text-xs text-muted-foreground">Papel</label>
                  <Select
                    value={String(signer.role)}
                    onValueChange={(value) => updateSigner(index, { role: Number(value) as CampaignDocumentSignerRoleValue })}
                  >
                    <SelectTrigger><SelectValue /></SelectTrigger>
                    <SelectContent>
                      {Object.values(CampaignDocumentSignerRole).map((roleValue) => (
                        <SelectItem key={roleValue} value={String(roleValue)}>
                          {campaignDocumentSignerRoleLabels[roleValue as CampaignDocumentSignerRoleValue]}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="md:col-span-3">
                  <label className="text-xs text-muted-foreground">Nome</label>
                  <Input value={signer.name} onChange={(e) => updateSigner(index, { name: e.target.value })} required />
                </div>
                <div className="md:col-span-3">
                  <label className="text-xs text-muted-foreground">Email</label>
                  <Input type="email" value={signer.email} onChange={(e) => updateSigner(index, { email: e.target.value })} required />
                </div>
                <div className="md:col-span-2">
                  <label className="text-xs text-muted-foreground">CPF/CNPJ</label>
                  <Input value={signer.documentNumber ?? ''} onChange={(e) => updateSigner(index, { documentNumber: e.target.value })} />
                </div>
                <div className="flex items-end justify-end md:col-span-1">
                  <Button type="button" size="icon" variant="ghost" onClick={() => removeSigner(index)}>
                    <Trash2 className="h-4 w-4 text-destructive" />
                  </Button>
                </div>
              </div>
            ))}
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              {t('common.action.cancel')}
            </Button>
            <Button type="submit" disabled={sending || !isValid}>
              {sending ? t('common.action.sending') : t('modal.document.action.sendForSignature')}
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}

function buildInitialSigners(document: CampaignDocument): CampaignDocumentSignerInput[] {
  if (document.signatures.length > 0) {
    return document.signatures.map((s) => ({
      role: s.role,
      name: s.signerName,
      email: s.signerEmail,
      documentNumber: s.signerDocumentNumber,
    }))
  }
  return [
    { role: CampaignDocumentSignerRole.Creator, name: '', email: '', documentNumber: '' },
    { role: CampaignDocumentSignerRole.Agency, name: '', email: '', documentNumber: '' },
  ]
}
