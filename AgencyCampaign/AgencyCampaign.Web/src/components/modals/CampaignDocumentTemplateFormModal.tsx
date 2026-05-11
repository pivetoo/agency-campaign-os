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
  useI18n,
} from 'archon-ui'
import { Braces } from 'lucide-react'
import { campaignDocumentTemplateService } from '../../services/campaignDocumentTemplateService'
import {
  CampaignDocumentType,
  campaignDocumentTypeLabels,
  type CampaignDocumentTypeValue,
} from '../../types/campaignDocument'
import type {
  CampaignDocumentTemplate,
  CampaignDocumentTemplateVariable,
  CampaignDocumentTemplateVariableMap,
} from '../../types/campaignDocumentTemplate'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  template: CampaignDocumentTemplate | null
  onSuccess: () => void
}

const documentTypeOptions = Object.values(CampaignDocumentType).map((value) => ({
  value: String(value),
  label: campaignDocumentTypeLabels[value as CampaignDocumentTypeValue],
}))

export default function CampaignDocumentTemplateFormModal({ open, onOpenChange, template, onSuccess }: Props) {
  const { t } = useI18n()
  const isEditing = !!template
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [documentType, setDocumentType] = useState<CampaignDocumentTypeValue>(CampaignDocumentType.CreatorAgreement)
  const [body, setBody] = useState('')
  const [isActive, setIsActive] = useState(true)
  const [variableMap, setVariableMap] = useState<CampaignDocumentTemplateVariableMap>({})
  const [pickerOpen, setPickerOpen] = useState(false)
  const bodyRef = useRef<HTMLTextAreaElement | null>(null)
  const pickerContainerRef = useRef<HTMLDivElement | null>(null)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    if (template) {
      setName(template.name)
      setDescription(template.description ?? '')
      setDocumentType(template.documentType)
      setBody(template.body)
      setIsActive(template.isActive)
    } else {
      setName('')
      setDescription('')
      setDocumentType(CampaignDocumentType.CreatorAgreement)
      setBody('')
      setIsActive(true)
    }
    setPickerOpen(false)
  }, [open, template])

  useEffect(() => {
    if (!open || Object.keys(variableMap).length > 0) return
    let cancelled = false
    void campaignDocumentTemplateService
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
    if (!pickerOpen) return
    const handleClickOutside = (event: MouseEvent) => {
      if (pickerContainerRef.current && !pickerContainerRef.current.contains(event.target as Node)) {
        setPickerOpen(false)
      }
    }
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setPickerOpen(false)
    }
    document.addEventListener('mousedown', handleClickOutside)
    document.addEventListener('keydown', handleEscape)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
      document.removeEventListener('keydown', handleEscape)
    }
  }, [pickerOpen])

  const variablesForType = useMemo<CampaignDocumentTemplateVariable[]>(
    () => variableMap[String(documentType)] ?? [],
    [variableMap, documentType],
  )

  const groupedVariables = useMemo(() => {
    const groups = new Map<string, CampaignDocumentTemplateVariable[]>()
    variablesForType.forEach((variable) => {
      const list = groups.get(variable.group) ?? []
      list.push(variable)
      groups.set(variable.group, list)
    })
    return Array.from(groups.entries())
  }, [variablesForType])

  const isValid = useMemo(
    () => name.trim().length >= 2 && body.trim().length >= 10,
    [name, body],
  )

  const insertVariable = (key: string) => {
    const token = `{{ ${key} }}`
    const el = bodyRef.current
    if (!el) {
      setBody((prev) => prev + token)
      setPickerOpen(false)
      return
    }
    const start = el.selectionStart ?? body.length
    const end = el.selectionEnd ?? body.length
    const next = body.slice(0, start) + token + body.slice(end)
    setBody(next)
    setPickerOpen(false)
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
      description: description.trim() || undefined,
      documentType,
      body,
    }
    const result = await execute(() => {
      if (isEditing && template) {
        return campaignDocumentTemplateService.update(template.id, { id: template.id, ...payload, isActive })
      }
      return campaignDocumentTemplateService.create(payload)
    })
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '920px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.documentTemplate.title.edit') : t('modal.documentTemplate.title.new')}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.name')}</label>
              <Input
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Ex.: Contrato padrão de creator"
                required
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.type')}</label>
              <SearchableSelect
                value={String(documentType)}
                onValueChange={(value) => setDocumentType(Number(value) as CampaignDocumentTypeValue)}
                options={documentTypeOptions}
                placeholder="Selecione o tipo"
                searchPlaceholder="Buscar tipo"
              />
            </div>
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('common.field.description')}</label>
              <Input
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Texto curto explicando quando usar este template"
              />
            </div>
            <div className="space-y-2 md:col-span-2">
              <div className="flex items-center justify-between gap-2">
                <label className="text-sm font-medium">{t('modal.documentTemplate.field.body')}</label>
                <div ref={pickerContainerRef} className="relative">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setPickerOpen((prev) => !prev)}
                  >
                    <Braces className="mr-1 h-3 w-3" /> {t('common.action.insertVariable')}
                  </Button>
                  {pickerOpen && (
                    groupedVariables.length === 0 ? (
                      <div className="absolute right-0 top-full z-50 mt-1 w-72 rounded-md border bg-popover p-3 text-xs text-muted-foreground shadow-md">
                        Nenhuma variável disponível.
                      </div>
                    ) : (
                      <div className="absolute right-0 top-full z-50 mt-1 w-96 max-h-[420px] overflow-y-auto rounded-md border bg-popover shadow-md">
                        {groupedVariables.map(([group, items]) => (
                          <div key={group}>
                            <div className="sticky top-0 z-10 border-b bg-muted/40 px-3 py-1.5 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
                              {group}
                            </div>
                            <ul className="divide-y">
                              {items.map((variable) => (
                                <li key={variable.key}>
                                  <button
                                    type="button"
                                    onClick={() => insertVariable(variable.key)}
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
                        ))}
                      </div>
                    )
                  )}
                </div>
              </div>
              <textarea
                ref={bodyRef}
                className="min-h-[320px] w-full rounded-md border bg-background p-3 font-mono text-xs"
                value={body}
                onChange={(e) => setBody(e.target.value)}
                placeholder={'CONTRATO DE PARCERIA\n\nPelo presente instrumento, {{ brandName }}, CNPJ {{ brandDocument }}, e o creator {{ creatorName }}, CPF {{ creatorDocument }}, firmam o seguinte acordo no escopo da campanha {{ campaignName }}.\n\nValor combinado: {{ creatorAgreedAmount }}.\nVigência: {{ campaignStartDate }} a {{ campaignEndDate }}.\n\nAssinado em {{ today }}.'}
              />
            </div>
          </div>
          {isEditing && (
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
              <span>{t('common.status.active')}</span>
            </label>
          )}
          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              {t('common.action.cancel')}
            </Button>
            <Button type="submit" disabled={loading || !isValid}>
              {loading ? t('common.action.saving') : t('common.action.save')}
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
