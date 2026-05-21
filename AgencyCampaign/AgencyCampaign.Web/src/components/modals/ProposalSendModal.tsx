import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, Tabs, TabsContent, TabsList, TabsTrigger, useApi, useI18n } from 'archon-ui'
import { AlertTriangle, ExternalLink, Mail, MessageCircle } from 'lucide-react'
import { proposalService, type SendProposalEmailRequest, type SendProposalWhatsappRequest } from '../../services/proposalService'
import { integrationCapabilityService } from '../../services/integrationCapabilityService'
import { IntegrationIntents } from '../../types/automation'
import type { IntegrationCapabilitySummary } from '../../types/integrationCapability'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  proposalId: number
  proposalName: string
  agencyName?: string
  defaultRecipientEmail?: string
  defaultRecipientPhone?: string
  publicLinkUrl?: string
  onSuccess: () => void
}

type Channel = 'email' | 'whatsapp'

interface ChannelBindingState {
  loading: boolean
  configured: boolean
  isActive: boolean
  connectorName?: string
}

const INITIAL_BINDING: ChannelBindingState = { loading: true, configured: false, isActive: false }

function buildDefaultSubject(proposalName: string): string {
  return `Proposta: ${proposalName}`
}

function buildDefaultEmailBody(proposalName: string, agencyName: string | undefined, publicLinkUrl: string | undefined): string {
  const greeting = 'Olá,'
  const intro = `Segue a proposta "${proposalName}"${agencyName ? ` da ${agencyName}` : ''} para sua avaliação.`
  const link = publicLinkUrl ? `\n\nAcesse pelo link:\n${publicLinkUrl}` : ''
  const closing = '\n\nFico à disposição para quaisquer dúvidas.'
  return `${greeting}\n\n${intro}${link}${closing}`
}

function buildDefaultWhatsappBody(proposalName: string, agencyName: string | undefined, publicLinkUrl: string | undefined): string {
  const intro = `Olá! Segue a proposta "${proposalName}"${agencyName ? ` da ${agencyName}` : ''} para sua avaliação.`
  const link = publicLinkUrl ? `\n\nLink: ${publicLinkUrl}` : ''
  return `${intro}${link}`
}

export default function ProposalSendModal({ open, onOpenChange, proposalId, proposalName, agencyName, defaultRecipientEmail, defaultRecipientPhone, publicLinkUrl, onSuccess }: Props) {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [channel, setChannel] = useState<Channel>('email')
  const [emailBinding, setEmailBinding] = useState<ChannelBindingState>(INITIAL_BINDING)
  const [whatsappBinding, setWhatsappBinding] = useState<ChannelBindingState>(INITIAL_BINDING)

  const [recipientEmail, setRecipientEmail] = useState('')
  const [subject, setSubject] = useState('')
  const [emailBody, setEmailBody] = useState('')
  const [recipientPhone, setRecipientPhone] = useState('')
  const [whatsappBody, setWhatsappBody] = useState('')

  const { execute: send, loading: sending } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: markSent, loading: markingSent } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const goToIntegrations = () => {
    onOpenChange(false)
    navigate('/configuracao/integracoes')
  }

  const handleMarkAsSent = async () => {
    const result = await markSent(() => proposalService.markAsSent(proposalId))
    if (result !== null) onSuccess()
  }

  useEffect(() => {
    if (!open) return

    setRecipientEmail(defaultRecipientEmail ?? '')
    setSubject(buildDefaultSubject(proposalName))
    setEmailBody(buildDefaultEmailBody(proposalName, agencyName, publicLinkUrl))
    setRecipientPhone(defaultRecipientPhone ?? '')
    setWhatsappBody(buildDefaultWhatsappBody(proposalName, agencyName, publicLinkUrl))
    setChannel('email')

    void loadBindings()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, proposalId])

  const loadBindings = async () => {
    setEmailBinding({ ...INITIAL_BINDING, loading: true })
    setWhatsappBinding({ ...INITIAL_BINDING, loading: true })
    try {
      const summary = await integrationCapabilityService.getSummary()
      setEmailBinding(buildBindingState(summary, IntegrationIntents.ProposalSendEmail))
      setWhatsappBinding(buildBindingState(summary, IntegrationIntents.ProposalSendWhatsapp))
    } catch {
      setEmailBinding({ loading: false, configured: false, isActive: false })
      setWhatsappBinding({ loading: false, configured: false, isActive: false })
    }
  }

  const currentBinding = channel === 'email' ? emailBinding : whatsappBinding

  const canSubmitEmail = emailBinding.configured && emailBinding.isActive && recipientEmail.trim().length > 0 && subject.trim().length >= 2 && emailBody.trim().length > 0
  const canSubmitWhatsapp = whatsappBinding.configured && whatsappBinding.isActive && recipientPhone.trim().length >= 8 && whatsappBody.trim().length > 0
  const canSubmit = channel === 'email' ? canSubmitEmail : canSubmitWhatsapp

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()

    if (channel === 'email') {
      const payload: SendProposalEmailRequest = {
        recipientEmail: recipientEmail.trim(),
        subject: subject.trim(),
        body: emailBody.trim(),
      }
      const result = await send(() => proposalService.sendByEmail(proposalId, payload))
      if (result !== null) onSuccess()
      return
    }

    const payload: SendProposalWhatsappRequest = {
      recipientPhone: recipientPhone.trim(),
      body: whatsappBody.trim(),
    }
    const result = await send(() => proposalService.sendByWhatsapp(proposalId, payload))
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '760px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{t('modal.proposalSend.title')}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <Tabs value={channel} onValueChange={(value) => setChannel(value as Channel)}>
            <TabsList className="w-full">
              <TabsTrigger value="email" className="flex-1 gap-2">
                <Mail className="h-4 w-4" /> Email
              </TabsTrigger>
              <TabsTrigger value="whatsapp" className="flex-1 gap-2">
                <MessageCircle className="h-4 w-4" /> WhatsApp
              </TabsTrigger>
            </TabsList>

            <TabsContent value="email" className="mt-4 space-y-4">
              {renderChannelHeader(emailBinding, goToIntegrations, handleMarkAsSent, markingSent)}
              {emailBinding.configured && emailBinding.isActive && (
                <>
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
                      value={emailBody}
                      onChange={(e) => setEmailBody(e.target.value)}
                      required
                    />
                  </div>
                </>
              )}
            </TabsContent>

            <TabsContent value="whatsapp" className="mt-4 space-y-4">
              {renderChannelHeader(whatsappBinding, goToIntegrations, handleMarkAsSent, markingSent)}
              {whatsappBinding.configured && whatsappBinding.isActive && (
                <>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">{t('modal.proposalSend.field.recipientPhone')}</label>
                    <Input type="tel" placeholder="+55 11 99999-9999" value={recipientPhone} onChange={(e) => setRecipientPhone(e.target.value)} required />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">{t('common.field.message')}</label>
                    <textarea
                      className="min-h-[140px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                      value={whatsappBody}
                      onChange={(e) => setWhatsappBody(e.target.value)}
                      required
                    />
                  </div>
                </>
              )}
            </TabsContent>
          </Tabs>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            {currentBinding.configured && currentBinding.isActive && (
              <Button type="submit" disabled={sending || !canSubmit}>{sending ? t('common.action.sending') : t('modal.proposalSend.action.send')}</Button>
            )}
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}

function buildBindingState(summary: IntegrationCapabilitySummary[], intentKey: string): ChannelBindingState {
  const item = summary.find((entry) => entry.intentKey === intentKey)
  if (!item || !item.configuredConnectorId) {
    return { loading: false, configured: false, isActive: false }
  }
  const connector = item.availableConnectors.find((option) => option.id === item.configuredConnectorId)
  return {
    loading: false,
    configured: true,
    isActive: item.isActive,
    connectorName: connector?.name ?? `Conta #${item.configuredConnectorId}`,
  }
}

function renderChannelHeader(binding: ChannelBindingState, goToIntegrations: () => void, handleMarkAsSent: () => void, markingSent: boolean) {
  if (binding.loading) {
    return <div className="rounded-md border border-dashed bg-muted/20 p-3 text-xs text-muted-foreground">Verificando configuração…</div>
  }

  if (!binding.configured) {
    return (
      <div className="space-y-3 rounded-md border border-amber-200 bg-amber-50 p-4">
        <div className="flex items-start gap-3">
          <AlertTriangle className="mt-0.5 h-5 w-5 flex-shrink-0 text-amber-600" />
          <div className="space-y-1">
            <div className="text-sm font-medium text-amber-900">Ação não configurada</div>
            <p className="text-xs text-amber-800">
              Você ainda não escolheu qual conta usar para esta ação. Configure em Configurações → Integrações → Ações, ou marque a proposta como enviada manualmente.
            </p>
          </div>
        </div>
        <div className="flex flex-wrap gap-2 pl-8">
          <Button type="button" size="sm" variant="outline" onClick={goToIntegrations}>
            <ExternalLink className="mr-1 h-3.5 w-3.5" /> Configurar Integrações
          </Button>
          <Button type="button" size="sm" variant="outline-primary" onClick={handleMarkAsSent} disabled={markingSent}>
            {markingSent ? 'Salvando…' : 'Marcar como enviada mesmo assim'}
          </Button>
        </div>
      </div>
    )
  }

  if (!binding.isActive) {
    return (
      <div className="space-y-3 rounded-md border border-amber-200 bg-amber-50 p-4">
        <div className="flex items-start gap-3">
          <AlertTriangle className="mt-0.5 h-5 w-5 flex-shrink-0 text-amber-600" />
          <div className="space-y-1">
            <div className="text-sm font-medium text-amber-900">Ação pausada</div>
            <p className="text-xs text-amber-800">
              A conta <span className="font-medium">{binding.connectorName}</span> está vinculada a esta ação, mas está pausada. Reative em Configurações → Integrações → Ações.
            </p>
          </div>
        </div>
        <div className="flex flex-wrap gap-2 pl-8">
          <Button type="button" size="sm" variant="outline" onClick={goToIntegrations}>
            <ExternalLink className="mr-1 h-3.5 w-3.5" /> Configurar Integrações
          </Button>
          <Button type="button" size="sm" variant="outline-primary" onClick={handleMarkAsSent} disabled={markingSent}>
            {markingSent ? 'Salvando…' : 'Marcar como enviada mesmo assim'}
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="rounded-md border bg-muted/20 p-3">
      <div className="space-y-1">
        <div className="text-xs uppercase tracking-wide text-muted-foreground">Enviando via</div>
        <div className="text-sm font-medium text-foreground">{binding.connectorName}</div>
      </div>
    </div>
  )
}
