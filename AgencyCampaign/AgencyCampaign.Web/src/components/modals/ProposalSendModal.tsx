import { useEffect, useMemo, useState } from 'react'
import { Button, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, SearchableSelect, useApi, useI18n } from 'archon-ui'
import { proposalService, type SendProposalEmailRequest } from '../../services/proposalService'
import { integrationPlatformService } from '../../services/integrationPlatformService'
import { IntegrationCategoryIdentifier, type Connector, type IntegrationCategory, type IntegrationPlatformIntegration, type Pipeline } from '../../types/integrationPlatform'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  proposalId: number
  proposalName: string
  agencyName?: string
  defaultRecipientEmail?: string
  publicLinkUrl?: string
  onSuccess: () => void
}

const EMAIL_CATEGORY_IDENTIFIER = IntegrationCategoryIdentifier.Email

function buildDefaultSubject(proposalName: string): string {
  return `Proposta: ${proposalName}`
}

function buildDefaultBody(proposalName: string, agencyName: string | undefined, publicLinkUrl: string | undefined): string {
  const greeting = 'Olá,'
  const intro = `Segue a proposta "${proposalName}"${agencyName ? ` da ${agencyName}` : ''} para sua avaliação.`
  const link = publicLinkUrl ? `\n\nAcesse pelo link:\n${publicLinkUrl}` : ''
  const closing = '\n\nFico à disposição para quaisquer dúvidas.'
  return `${greeting}\n\n${intro}${link}${closing}`
}

export default function ProposalSendModal({ open, onOpenChange, proposalId, proposalName, agencyName, defaultRecipientEmail, publicLinkUrl, onSuccess }: Props) {
  const { t } = useI18n()
  const [categories, setCategories] = useState<IntegrationCategory[]>([])
  const [categoryId, setCategoryId] = useState<number | undefined>()
  const [integrations, setIntegrations] = useState<IntegrationPlatformIntegration[]>([])
  const [integrationId, setIntegrationId] = useState<number | undefined>()
  const [connectors, setConnectors] = useState<Connector[]>([])
  const [connectorId, setConnectorId] = useState<number | undefined>()
  const [pipelines, setPipelines] = useState<Pipeline[]>([])
  const [pipelineId, setPipelineId] = useState<number | undefined>()
  const [recipientEmail, setRecipientEmail] = useState('')
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')

  const { execute: loadCategories, loading: catLoading } = useApi<IntegrationCategory[]>({ showErrorMessage: true })
  const { execute: loadIntegrations, loading: intLoading } = useApi<IntegrationPlatformIntegration[]>({ showErrorMessage: true })
  const { execute: loadConnectors, loading: connLoading } = useApi<Connector[]>({ showErrorMessage: true })
  const { execute: loadPipelines, loading: pipeLoading } = useApi<Pipeline[]>({ showErrorMessage: true })
  const { execute: send, loading: sending } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return

    setRecipientEmail(defaultRecipientEmail ?? '')
    setSubject(buildDefaultSubject(proposalName))
    setBody(buildDefaultBody(proposalName, agencyName, publicLinkUrl))

    void loadCategories(() => integrationPlatformService.getActiveIntegrationCategories()).then((result) => {
      if (!result) return
      setCategories(result)
      const emailCategory = result.find((category) => category.identifier === EMAIL_CATEGORY_IDENTIFIER)
      setCategoryId(emailCategory?.id ?? result[0]?.id)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, proposalId])

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

  const categoryOptions = useMemo(() => categories.map((c) => ({ value: String(c.id), label: c.name })), [categories])
  const integrationOptions = useMemo(() => integrations.map((i) => ({ value: String(i.id), label: i.name })), [integrations])
  const connectorOptions = useMemo(() => connectors.map((c) => ({ value: String(c.id), label: c.name })), [connectors])
  const pipelineOptions = useMemo(() => pipelines.map((p) => ({ value: String(p.id), label: p.name })), [pipelines])

  const isValid = !!connectorId && !!pipelineId && recipientEmail.trim().length > 0 && subject.trim().length >= 2 && body.trim().length > 0

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!connectorId || !pipelineId) return

    const payload: SendProposalEmailRequest = {
      recipientEmail: recipientEmail.trim(),
      subject: subject.trim(),
      body: body.trim(),
      connectorId,
      pipelineId,
    }

    const result = await send(() => proposalService.sendByEmail(proposalId, payload))
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '760px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{t('modal.proposalSend.title')}</ModalTitle>
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
                placeholder={intLoading ? t('common.loading') : integrationOptions.length === 0 ? t('modal.proposalSend.placeholder.noIntegration') : t('common.placeholder.select')}
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
                placeholder={connLoading ? t('common.loading') : connectorOptions.length === 0 ? t('modal.proposalSend.placeholder.configureConnector') : t('common.placeholder.select')}
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
                placeholder={pipeLoading ? t('common.loading') : pipelineOptions.length === 0 ? t('modal.proposalSend.placeholder.noPipelines') : t('common.placeholder.select')}
                searchPlaceholder={t('common.placeholder.search')}
                disabled={pipeLoading || pipelineOptions.length === 0}
              />
            </div>
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">{t('modal.proposalSend.field.recipient')}</label>
            <Input type="email" value={recipientEmail} onChange={(e) => setRecipientEmail(e.target.value)} required />
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">{t('common.field.subject')}</label>
            <Input value={subject} onChange={(e) => setSubject(e.target.value)} required />
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">{t('common.field.message')}</label>
            <textarea
              className="min-h-[180px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              value={body}
              onChange={(e) => setBody(e.target.value)}
              required
            />
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={sending || !isValid}>{sending ? t('common.action.sending') : t('modal.proposalSend.action.send')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
