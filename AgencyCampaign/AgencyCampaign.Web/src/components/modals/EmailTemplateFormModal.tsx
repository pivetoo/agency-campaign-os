import { useEffect, useMemo, useRef, useState } from 'react'
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
import { Braces } from 'lucide-react'
import { emailTemplateService } from '../../services/emailTemplateService'
import {
  EmailEventType,
  emailEventTypeLabels,
  type EmailEventTypeValue,
  type EmailTemplate,
  type EmailTemplateVariable,
  type EmailTemplateVariableMap,
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

type TargetField = 'subject' | 'body'

export default function EmailTemplateFormModal({ open, onOpenChange, template, onSuccess }: Props) {
  const isEditing = !!template
  const [name, setName] = useState('')
  const [eventType, setEventType] = useState<EmailEventTypeValue>(defaultEventType)
  const [subject, setSubject] = useState('')
  const [htmlBody, setHtmlBody] = useState('')
  const [isActive, setIsActive] = useState(true)
  const [variableMap, setVariableMap] = useState<EmailTemplateVariableMap>({})
  const [openPicker, setOpenPicker] = useState<TargetField | null>(null)
  const subjectRef = useRef<HTMLInputElement | null>(null)
  const bodyRef = useRef<HTMLTextAreaElement | null>(null)
  const subjectPickerRef = useRef<HTMLDivElement | null>(null)
  const bodyPickerRef = useRef<HTMLDivElement | null>(null)
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
    setOpenPicker(null)
  }, [open, template])

  useEffect(() => {
    if (!open || Object.keys(variableMap).length > 0) return
    let cancelled = false
    void emailTemplateService
      .getVariables()
      .then((map) => {
        if (!cancelled) setVariableMap(map)
      })
      .catch(() => {
        // silencioso — picker fica vazio se falhar
      })
    return () => {
      cancelled = true
    }
  }, [open, variableMap])

  useEffect(() => {
    if (!openPicker) return
    const handleClickOutside = (event: MouseEvent) => {
      const ref = openPicker === 'subject' ? subjectPickerRef : bodyPickerRef
      if (ref.current && !ref.current.contains(event.target as Node)) {
        setOpenPicker(null)
      }
    }
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpenPicker(null)
    }
    document.addEventListener('mousedown', handleClickOutside)
    document.addEventListener('keydown', handleEscape)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
      document.removeEventListener('keydown', handleEscape)
    }
  }, [openPicker])

  const variablesForEvent = useMemo<EmailTemplateVariable[]>(
    () => variableMap[String(eventType)] ?? [],
    [variableMap, eventType],
  )

  const isValid = useMemo(
    () => name.trim().length >= 2 && subject.trim().length >= 2 && htmlBody.trim().length >= 5,
    [name, subject, htmlBody],
  )

  const insertVariable = (field: TargetField, key: string) => {
    const token = `{{ ${key} }}`
    const el = field === 'subject' ? subjectRef.current : bodyRef.current
    if (!el) {
      if (field === 'subject') setSubject((prev) => prev + token)
      else setHtmlBody((prev) => prev + token)
      setOpenPicker(null)
      return
    }

    const current = field === 'subject' ? subject : htmlBody
    const start = el.selectionStart ?? current.length
    const end = el.selectionEnd ?? current.length
    const next = current.slice(0, start) + token + current.slice(end)

    if (field === 'subject') setSubject(next)
    else setHtmlBody(next)

    setOpenPicker(null)

    requestAnimationFrame(() => {
      el.focus()
      const caret = start + token.length
      el.setSelectionRange(caret, caret)
    })
  }

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

  const renderPicker = (field: TargetField) => {
    if (openPicker !== field) return null
    if (variablesForEvent.length === 0) {
      return (
        <div className="absolute right-0 top-full z-50 mt-1 w-72 rounded-md border bg-popover p-3 text-xs text-muted-foreground shadow-md">
          Nenhuma variável disponível para este evento.
        </div>
      )
    }
    return (
      <div className="absolute right-0 top-full z-50 mt-1 w-80 max-h-72 overflow-y-auto rounded-md border bg-popover shadow-md">
        <ul className="divide-y">
          {variablesForEvent.map((variable) => (
            <li key={variable.key}>
              <button
                type="button"
                onClick={() => insertVariable(field, variable.key)}
                className="flex w-full flex-col items-start gap-0.5 px-3 py-2 text-left hover:bg-muted/60"
              >
                <code className="text-xs font-medium text-primary">{`{{ ${variable.key} }}`}</code>
                <span className="text-xs font-medium">{variable.label}</span>
                <span className="text-[11px] text-muted-foreground">{variable.description}</span>
              </button>
            </li>
          ))}
        </ul>
      </div>
    )
  }

  const renderInsertButton = (field: TargetField, refContainer: React.RefObject<HTMLDivElement | null>) => (
    <div ref={refContainer} className="relative">
      <Button
        type="button"
        variant="outline"
        size="sm"
        onClick={() => setOpenPicker((prev) => (prev === field ? null : field))}
      >
        <Braces className="mr-1 h-3 w-3" /> Inserir variável
      </Button>
      {renderPicker(field)}
    </div>
  )

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
              <div className="flex items-center justify-between gap-2">
                <label className="text-sm font-medium">Assunto</label>
                {renderInsertButton('subject', subjectPickerRef)}
              </div>
              <Input
                ref={subjectRef}
                value={subject}
                onChange={(e) => setSubject(e.target.value)}
                placeholder="Ex.: Sua proposta {{ proposalName }} chegou"
                required
              />
            </div>
            <div className="space-y-2 md:col-span-2">
              <div className="flex items-center justify-between gap-2">
                <label className="text-sm font-medium">Corpo (HTML)</label>
                {renderInsertButton('body', bodyPickerRef)}
              </div>
              <textarea
                ref={bodyRef}
                className="min-h-[260px] w-full rounded-md border bg-background p-3 font-mono text-xs"
                value={htmlBody}
                onChange={(e) => setHtmlBody(e.target.value)}
                placeholder={'<p>Olá {{ contactName }},</p>\n<p>Segue a proposta {{ proposalName }} no valor de R$ {{ totalValue }}.</p>'}
              />
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
