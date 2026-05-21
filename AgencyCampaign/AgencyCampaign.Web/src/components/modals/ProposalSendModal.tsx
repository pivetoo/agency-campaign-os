import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, useApi, useI18n } from 'archon-ui'
import { AlertTriangle, CheckCircle2, ExternalLink, FileText, Mail, MessageCircle, Send } from 'lucide-react'
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
  const recipientPreview = channel === 'email' ? recipientEmail : recipientPhone

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
      <ModalContent size="full" style={{ maxWidth: '580px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>Enviar proposta</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-5">
          <ProposalChip name={proposalName} agencyName={agencyName} />

          <div>
            <SegmentedControl
              channel={channel}
              emailBinding={emailBinding}
              whatsappBinding={whatsappBinding}
              onSelect={setChannel}
              onSelectDisabled={goToIntegrations}
            />
            <CaptionStatus binding={currentBinding} channel={channel} />
          </div>

          {currentBinding.configured && currentBinding.isActive ? (
            channel === 'email' ? (
              <div className="space-y-4">
                <FieldRow label="Destinatário">
                  <Input type="email" value={recipientEmail} onChange={(e) => setRecipientEmail(e.target.value)} required />
                </FieldRow>
                <FieldRow label="Assunto">
                  <Input value={subject} onChange={(e) => setSubject(e.target.value)} required />
                </FieldRow>
                <FieldRow label="Mensagem" hint="Pode incluir o link público da proposta">
                  <textarea
                    className="min-h-[160px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                    value={emailBody}
                    onChange={(e) => setEmailBody(e.target.value)}
                    required
                  />
                </FieldRow>
              </div>
            ) : (
              <div className="space-y-4">
                <FieldRow label="Telefone do destinatário">
                  <Input type="tel" placeholder="+55 11 99999-9999" value={recipientPhone} onChange={(e) => setRecipientPhone(e.target.value)} required />
                </FieldRow>
                <FieldRow label="Mensagem">
                  <textarea
                    className="min-h-[140px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                    value={whatsappBody}
                    onChange={(e) => setWhatsappBody(e.target.value)}
                    required
                  />
                </FieldRow>
              </div>
            )
          ) : (
            <ChannelMissingNotice binding={currentBinding} goToIntegrations={goToIntegrations} onMarkAsSent={handleMarkAsSent} markingSent={markingSent} />
          )}

          <ModalFooter className="flex w-full items-center justify-between gap-3 sm:flex-row">
            <p className="hidden truncate text-xs text-muted-foreground sm:block">
              {currentBinding.configured && currentBinding.isActive && recipientPreview ? (
                <>Será enviado para <span className="font-semibold text-foreground">{recipientPreview}</span></>
              ) : null}
            </p>
            <div className="flex flex-shrink-0 items-center gap-2">
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
              {currentBinding.configured && currentBinding.isActive && (
                <Button type="submit" disabled={sending || !canSubmit}>
                  <Send size={14} className="mr-1.5" />
                  {sending ? t('common.action.sending') : channel === 'email' ? 'Enviar email' : 'Enviar WhatsApp'}
                </Button>
              )}
            </div>
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

function ProposalChip({ name, agencyName }: { name: string; agencyName?: string }) {
  return (
    <div className="flex items-center gap-3 rounded-[10px] border border-border bg-muted/30 px-3.5 py-2.5">
      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-rose-100 text-rose-700">
        <FileText size={18} />
      </div>
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-semibold text-foreground">{name}</p>
        <p className="truncate text-[11.5px] text-muted-foreground">Proposta comercial{agencyName ? ` · ${agencyName}` : ''}</p>
      </div>
    </div>
  )
}

interface SegmentedControlProps {
  channel: Channel
  emailBinding: ChannelBindingState
  whatsappBinding: ChannelBindingState
  onSelect: (channel: Channel) => void
  onSelectDisabled: () => void
}

function SegmentedControl({ channel, emailBinding, whatsappBinding, onSelect, onSelectDisabled }: SegmentedControlProps) {
  return (
    <div className="inline-flex w-full gap-1 rounded-[10px] bg-muted p-1" role="tablist">
      <SegmentedButton
        active={channel === 'email'}
        onClick={() => onSelect('email')}
        icon={<Mail size={16} />}
        label="Email"
        binding={emailBinding}
        onClickDisabled={onSelectDisabled}
      />
      <SegmentedButton
        active={channel === 'whatsapp'}
        onClick={() => onSelect('whatsapp')}
        icon={<MessageCircle size={16} />}
        label="WhatsApp"
        binding={whatsappBinding}
        onClickDisabled={onSelectDisabled}
      />
    </div>
  )
}

interface SegmentedButtonProps {
  active: boolean
  onClick: () => void
  icon: React.ReactNode
  label: string
  binding: ChannelBindingState
  onClickDisabled: () => void
}

function SegmentedButton({ active, onClick, icon, label, binding, onClickDisabled }: SegmentedButtonProps) {
  const disabled = !binding.configured || !binding.isActive
  const badge = binding.loading
    ? null
    : !binding.configured
      ? { label: 'Não configurado', tone: 'amber' as const }
      : !binding.isActive
        ? { label: 'Pausado', tone: 'amber' as const }
        : { label: 'Pronto', tone: 'green' as const }

  const handleClick = () => {
    if (disabled) {
      onClickDisabled()
      return
    }
    onClick()
  }

  return (
    <button
      type="button"
      onClick={handleClick}
      role="tab"
      aria-selected={active}
      aria-disabled={disabled}
      title={disabled ? 'Configure a integração antes de usar este canal' : undefined}
      className={[
        'group relative flex flex-1 items-center justify-center gap-2 rounded-[7px] px-3 py-2.5 text-sm font-semibold transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30',
        active
          ? 'bg-card text-foreground shadow-[0_1px_2px_rgba(15,27,45,0.08),0_0_0_1px_rgba(15,27,45,0.04)]'
          : disabled
            ? 'cursor-not-allowed text-muted-foreground/70 opacity-75'
            : 'text-muted-foreground hover:text-foreground',
      ].join(' ')}
    >
      {icon}
      <span>{label}</span>
      {badge && (
        <span
          className={[
            'ml-0.5 rounded px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wider',
            badge.tone === 'green'
              ? active
                ? 'bg-emerald-100 text-emerald-700'
                : 'bg-muted-foreground/10 text-muted-foreground'
              : active
                ? 'bg-amber-100 text-amber-700'
                : 'bg-muted-foreground/10 text-muted-foreground',
          ].join(' ')}
        >
          {badge.label}
        </span>
      )}
    </button>
  )
}

function CaptionStatus({ binding, channel }: { binding: ChannelBindingState; channel: Channel }) {
  if (binding.loading) {
    return <p className="mt-2 text-xs text-muted-foreground">Verificando configuração…</p>
  }

  if (!binding.configured) {
    return (
      <p className="mt-2 flex items-center gap-1.5 text-xs text-amber-700">
        <AlertTriangle size={13} />
        {channel === 'email' ? 'Email' : 'WhatsApp'} sem conta configurada
      </p>
    )
  }

  if (!binding.isActive) {
    return (
      <p className="mt-2 flex items-center gap-1.5 text-xs text-amber-700">
        <AlertTriangle size={13} />
        Conta <span className="font-semibold">{binding.connectorName}</span> está pausada
      </p>
    )
  }

  return (
    <p className="mt-2 flex items-center gap-1.5 text-xs text-muted-foreground">
      <CheckCircle2 size={13} className="text-emerald-600" />
      Enviando via <span className="font-semibold text-foreground">{binding.connectorName}</span>
    </p>
  )
}

function FieldRow({ label, hint, children }: { label: string; hint?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <div className="flex items-baseline justify-between">
        <label className="text-[12px] font-semibold uppercase tracking-wide text-muted-foreground">{label}</label>
        {hint && <span className="text-[11px] text-muted-foreground/70">{hint}</span>}
      </div>
      {children}
    </div>
  )
}

interface ChannelMissingNoticeProps {
  binding: ChannelBindingState
  goToIntegrations: () => void
  onMarkAsSent: () => void
  markingSent: boolean
}

function ChannelMissingNotice({ binding, goToIntegrations, onMarkAsSent, markingSent }: ChannelMissingNoticeProps) {
  const title = !binding.configured ? 'Canal não configurado' : 'Canal pausado'
  const description = !binding.configured
    ? 'Configure uma conta em Configurações → Integrações → Ações para liberar este canal, ou marque a proposta como enviada manualmente.'
    : `A conta ${binding.connectorName} está vinculada mas pausada. Reative em Configurações → Integrações → Ações.`

  return (
    <div className="space-y-3 rounded-md border border-amber-200 bg-amber-50 p-4">
      <div className="flex items-start gap-3">
        <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-amber-600" />
        <div className="space-y-1">
          <p className="text-sm font-semibold text-amber-900">{title}</p>
          <p className="text-xs text-amber-800">{description}</p>
        </div>
      </div>
      <div className="flex flex-wrap gap-2 pl-8">
        <Button type="button" size="sm" variant="outline" onClick={goToIntegrations}>
          <ExternalLink className="mr-1 h-3.5 w-3.5" /> Configurar Integrações
        </Button>
        <Button type="button" size="sm" variant="outline-primary" onClick={onMarkAsSent} disabled={markingSent}>
          {markingSent ? 'Salvando…' : 'Marcar como enviada mesmo assim'}
        </Button>
      </div>
    </div>
  )
}
