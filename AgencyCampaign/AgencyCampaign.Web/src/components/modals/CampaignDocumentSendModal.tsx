import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, useApi, useI18n } from 'archon-ui'
import { AlertTriangle, CheckCircle2, ExternalLink, Mail, MessageCircle } from 'lucide-react'
import { campaignDocumentService, type SendCampaignDocumentEmailRequest, type SendCampaignDocumentWhatsappRequest } from '../../services/campaignDocumentService'
import { integrationCapabilityService } from '../../services/integrationCapabilityService'
import { IntegrationIntents } from '../../types/automation'
import type { CampaignDocument } from '../../types/campaignDocument'
import type { IntegrationCapabilitySummary } from '../../types/integrationCapability'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  document: CampaignDocument | null
  defaultRecipientEmail?: string
  defaultRecipientPhone?: string
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

export default function CampaignDocumentSendModal({ open, onOpenChange, document, defaultRecipientEmail, defaultRecipientPhone, onSuccess }: Props) {
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

  const goToIntegrations = () => {
    onOpenChange(false)
    navigate('/configuracao/integracoes')
  }

  useEffect(() => {
    if (!open || !document) return

    setRecipientEmail(defaultRecipientEmail ?? document.recipientEmail ?? '')
    setSubject(document.emailSubject ?? `Documento: ${document.title}`)
    setEmailBody(document.emailBody ?? `Olá, segue o documento "${document.title}" para sua análise.`)
    setRecipientPhone(defaultRecipientPhone ?? '')
    setWhatsappBody(`Olá! Segue o documento "${document.title}" para sua análise.`)
    setChannel('email')

    void loadBindings()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, document])

  const loadBindings = async () => {
    setEmailBinding({ ...INITIAL_BINDING, loading: true })
    setWhatsappBinding({ ...INITIAL_BINDING, loading: true })
    try {
      const summary = await integrationCapabilityService.getSummary()
      setEmailBinding(buildBindingState(summary, IntegrationIntents.CampaignDocumentSendEmail))
      setWhatsappBinding(buildBindingState(summary, IntegrationIntents.CampaignDocumentSendWhatsapp))
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
    if (!document) return

    if (channel === 'email') {
      const payload: SendCampaignDocumentEmailRequest = {
        recipientEmail: recipientEmail.trim(),
        subject: subject.trim(),
        body: emailBody.trim(),
      }
      const result = await send(() => campaignDocumentService.sendEmail(document.id, payload))
      if (result !== null) onSuccess()
      return
    }

    const payload: SendCampaignDocumentWhatsappRequest = {
      recipientPhone: recipientPhone.trim(),
      body: whatsappBody.trim(),
    }
    const result = await send(() => campaignDocumentService.sendWhatsapp(document.id, payload))
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '760px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>Enviar documento</ModalTitle>
          <p className="mt-1 text-sm text-muted-foreground">Escolha o canal de envio. Você pode mandar por email ou WhatsApp.</p>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-5">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <ChannelCard
              icon={<Mail className="h-5 w-5" />}
              label="Email"
              iconClass="bg-primary/10 text-primary"
              selectedRingClass="ring-primary/30 border-primary bg-primary/5"
              binding={emailBinding}
              selected={channel === 'email'}
              onSelect={() => setChannel('email')}
            />
            <ChannelCard
              icon={<MessageCircle className="h-5 w-5" />}
              label="WhatsApp"
              iconClass="bg-[#25D366]/15 text-[#128C7E]"
              selectedRingClass="ring-[#25D366]/30 border-[#25D366] bg-[#25D366]/5"
              binding={whatsappBinding}
              selected={channel === 'whatsapp'}
              onSelect={() => setChannel('whatsapp')}
            />
          </div>

          {channel === 'email' && (
            <div className="space-y-4">
              {renderChannelHeader(emailBinding, goToIntegrations)}
              {emailBinding.configured && emailBinding.isActive && (
                <>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">Destinatário</label>
                    <Input type="email" value={recipientEmail} onChange={(e) => setRecipientEmail(e.target.value)} required />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">Assunto</label>
                    <Input value={subject} onChange={(e) => setSubject(e.target.value)} required />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">Mensagem</label>
                    <textarea
                      className="min-h-[180px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                      value={emailBody}
                      onChange={(e) => setEmailBody(e.target.value)}
                      required
                    />
                  </div>
                </>
              )}
            </div>
          )}

          {channel === 'whatsapp' && (
            <div className="space-y-4">
              {renderChannelHeader(whatsappBinding, goToIntegrations)}
              {whatsappBinding.configured && whatsappBinding.isActive && (
                <>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">Telefone do destinatário</label>
                    <Input type="tel" placeholder="+55 11 99999-9999" value={recipientPhone} onChange={(e) => setRecipientPhone(e.target.value)} required />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">Mensagem</label>
                    <textarea
                      className="min-h-[140px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                      value={whatsappBody}
                      onChange={(e) => setWhatsappBody(e.target.value)}
                      required
                    />
                  </div>
                </>
              )}
            </div>
          )}

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            {currentBinding.configured && currentBinding.isActive && (
              <Button type="submit" disabled={sending || !canSubmit}>{sending ? t('common.action.sending') : 'Enviar'}</Button>
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

interface ChannelCardProps {
  icon: React.ReactNode
  label: string
  iconClass: string
  selectedRingClass: string
  binding: ChannelBindingState
  selected: boolean
  onSelect: () => void
}

function ChannelCard({ icon, label, iconClass, selectedRingClass, binding, selected, onSelect }: ChannelCardProps) {
  const statusLabel = binding.loading
    ? 'Verificando…'
    : !binding.configured
      ? 'Sem conta configurada'
      : !binding.isActive
        ? 'Ação pausada'
        : binding.connectorName ?? 'Conectado'

  const statusTone = binding.loading
    ? 'text-muted-foreground'
    : !binding.configured || !binding.isActive
      ? 'text-amber-700'
      : 'text-emerald-700'

  return (
    <button
      type="button"
      onClick={onSelect}
      className={[
        'flex items-center gap-3 rounded-lg border bg-card p-4 text-left transition-all focus:outline-none',
        selected ? `border-2 ring-1 ${selectedRingClass}` : 'border-border hover:border-primary/30 hover:bg-accent/30',
      ].join(' ')}
    >
      <div className={['flex h-11 w-11 shrink-0 items-center justify-center rounded-lg', iconClass].join(' ')}>
        {icon}
      </div>
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-1.5">
          <span className="text-sm font-semibold">{label}</span>
          {binding.configured && binding.isActive && (
            <CheckCircle2 size={13} className="text-emerald-500" />
          )}
        </div>
        <p className={['mt-0.5 truncate text-xs', statusTone].join(' ')}>
          {statusLabel}
        </p>
      </div>
    </button>
  )
}

function renderChannelHeader(binding: ChannelBindingState, goToIntegrations: () => void) {
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
              Você ainda não escolheu qual conta usar para esta ação. Configure em Configurações → Integrações → Ações.
            </p>
          </div>
        </div>
        <div className="flex flex-wrap gap-2 pl-8">
          <Button type="button" size="sm" variant="outline" onClick={goToIntegrations}>
            <ExternalLink className="mr-1 h-3.5 w-3.5" /> Configurar Integrações
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
