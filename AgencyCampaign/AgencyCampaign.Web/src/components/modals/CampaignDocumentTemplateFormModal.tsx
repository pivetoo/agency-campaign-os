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
import { campaignDocumentTemplateService } from '../../services/campaignDocumentTemplateService'
import {
  CampaignDocumentType,
  campaignDocumentTypeLabels,
  type CampaignDocumentTypeValue,
} from '../../types/campaignDocument'
import type { CampaignDocumentTemplate } from '../../types/campaignDocumentTemplate'

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

const placeholdersHint =
  'Variáveis disponíveis: {{ today }}, {{ campaignName }}, {{ campaignDescription }}, {{ campaignObjective }}, {{ campaignBriefing }}, {{ campaignStartDate }}, {{ campaignEndDate }}, {{ campaignBudget }}, {{ brandName }}, {{ brandTradeName }}, {{ brandDocument }}, {{ brandContactName }}, {{ brandContactEmail }}, {{ creatorName }}, {{ creatorStageName }}, {{ creatorEmail }}, {{ creatorDocument }}, {{ creatorAgreedAmount }}, {{ creatorAgencyFeePercent }}, {{ creatorAgencyFeeAmount }}, {{ scopeNotes }}.'

export default function CampaignDocumentTemplateFormModal({ open, onOpenChange, template, onSuccess }: Props) {
  const isEditing = !!template
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [documentType, setDocumentType] = useState<CampaignDocumentTypeValue>(CampaignDocumentType.CreatorAgreement)
  const [body, setBody] = useState('')
  const [isActive, setIsActive] = useState(true)
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
  }, [open, template])

  const isValid = useMemo(
    () => name.trim().length >= 2 && body.trim().length >= 10,
    [name, body],
  )

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
          <ModalTitle>{isEditing ? 'Editar template de documento' : 'Novo template de documento'}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">Nome</label>
              <Input
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Ex.: Contrato padrão de creator"
                required
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Tipo</label>
              <SearchableSelect
                value={String(documentType)}
                onValueChange={(value) => setDocumentType(Number(value) as CampaignDocumentTypeValue)}
                options={documentTypeOptions}
                placeholder="Selecione o tipo"
                searchPlaceholder="Buscar tipo"
              />
            </div>
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Descrição</label>
              <Input
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Texto curto explicando quando usar este template"
              />
            </div>
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Corpo do contrato</label>
              <textarea
                className="min-h-[320px] w-full rounded-md border bg-background p-3 font-mono text-xs"
                value={body}
                onChange={(e) => setBody(e.target.value)}
                placeholder={'CONTRATO DE PARCERIA\n\nPelo presente instrumento, {{ brandName }}, CNPJ {{ brandDocument }}, e o creator {{ creatorName }}, CPF {{ creatorDocument }}, firmam o seguinte acordo no escopo da campanha {{ campaignName }}.\n\nValor combinado: {{ creatorAgreedAmount }}.\nVigência: {{ campaignStartDate }} a {{ campaignEndDate }}.\n\nAssinado em {{ today }}.'}
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
