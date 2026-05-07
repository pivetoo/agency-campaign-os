import { useEffect, useMemo, useState } from 'react'
import {
  Button,
  Checkbox,
  Input,
  Modal,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  SearchableSelect,
  useApi,
} from 'archon-ui'
import { emailTemplateService } from '../../services/emailTemplateService'
import {
  EmailEventType,
  emailEventTypeLabels,
  type EmailEventTypeValue,
  type EmailTemplate,
} from '../../types/emailTemplate'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  template: EmailTemplate | null
  onSuccess: () => void
}

const eventOptions = Object.values(EmailEventType).map((value) => ({
  value: String(value),
  label: emailEventTypeLabels[value as EmailEventTypeValue],
}))

const defaultEventType: EmailEventTypeValue = EmailEventType.ProposalSent

const placeholdersHint =
  'Variáveis disponíveis: {{ proposalName }}, {{ totalValue }}, {{ validityUntil }}, {{ opportunityName }}, {{ brandName }}, {{ contactName }}, {{ contactEmail }}, {{ responsibleName }}.'

export default function EmailTemplateFormModal({ open, onOpenChange, template, onSuccess }: Props) {
  const isEditing = !!template
  const [name, setName] = useState('')
  const [eventType, setEventType] = useState<EmailEventTypeValue>(defaultEventType)
  const [subject, setSubject] = useState('')
  const [htmlBody, setHtmlBody] = useState('')
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    if (template) {
      setName(template.name)
      setEventType(template.eventType)
      setSubject(template.subject)
      setHtmlBody(template.htmlBody)
      setIsActive(template.isActive)
    } else {
      setName('')
      setEventType(defaultEventType)
      setSubject('')
      setHtmlBody('')
      setIsActive(true)
    }
  }, [open, template])

  const isValid = useMemo(
    () => name.trim().length >= 2 && subject.trim().length >= 2 && htmlBody.trim().length >= 5,
    [name, subject, htmlBody],
  )

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    const payload = {
      name: name.trim(),
      eventType,
      subject: subject.trim(),
      htmlBody,
    }
    const result = await execute(() => {
      if (isEditing && template) {
        return emailTemplateService.update(template.id, { id: template.id, ...payload, isActive })
      }
      return emailTemplateService.create(payload)
    })
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '880px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar template de e-mail' : 'Novo template de e-mail'}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">Nome</label>
              <Input
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Ex.: Confirmação de envio de proposta"
                required
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Evento</label>
              <SearchableSelect
                value={String(eventType)}
                onValueChange={(value) => setEventType(Number(value) as EmailEventTypeValue)}
                options={eventOptions}
                placeholder="Selecione um evento"
                searchPlaceholder="Buscar evento"
              />
            </div>
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Assunto</label>
              <Input
                value={subject}
                onChange={(e) => setSubject(e.target.value)}
                placeholder="Ex.: Sua proposta {{ proposalName }} chegou"
                required
              />
            </div>
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Corpo (HTML)</label>
              <textarea
                className="min-h-[260px] w-full rounded-md border bg-background p-3 font-mono text-xs"
                value={htmlBody}
                onChange={(e) => setHtmlBody(e.target.value)}
                placeholder={'<p>Olá {{ contactName }},</p>\n<p>Segue a proposta {{ proposalName }} no valor de R$ {{ totalValue }}.</p>'}
              />
              <p className="text-xs text-muted-foreground">{placeholdersHint}</p>
            </div>
          </div>
          {isEditing && (
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
              <span>Ativo</span>
            </label>
          )}
          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancelar
            </Button>
            <Button type="submit" disabled={loading || !isValid}>
              {loading ? 'Salvando...' : 'Salvar'}
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
