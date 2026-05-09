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
import { campaignDocumentService } from '../../services/campaignDocumentService'
import { campaignDocumentTemplateService } from '../../services/campaignDocumentTemplateService'
import {
  CampaignDocumentType,
  campaignDocumentTypeLabels,
  type CampaignDocumentTypeValue,
} from '../../types/campaignDocument'
import type { CampaignDocumentTemplate } from '../../types/campaignDocumentTemplate'
import type { CampaignCreator } from '../../types/campaignCreator'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  campaignId: number
  campaignCreators: CampaignCreator[]
  onSuccess: () => void
}

export default function CampaignDocumentGenerateFromTemplateModal({
  open,
  onOpenChange,
  campaignId,
  campaignCreators,
  onSuccess,
}: Props) {
  const [documentType, setDocumentType] = useState<CampaignDocumentTypeValue>(CampaignDocumentType.CreatorAgreement)
  const [templates, setTemplates] = useState<CampaignDocumentTemplate[]>([])
  const [templateId, setTemplateId] = useState<number | undefined>()
  const [campaignCreatorId, setCampaignCreatorId] = useState<number | undefined>()
  const [title, setTitle] = useState('')

  const { execute: loadTemplates, loading: templatesLoading } = useApi<CampaignDocumentTemplate[]>({
    showErrorMessage: true,
  })
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    setTitle('')
    setTemplateId(undefined)
    setCampaignCreatorId(undefined)
    setDocumentType(CampaignDocumentType.CreatorAgreement)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  useEffect(() => {
    if (!open) return
    void loadTemplates(() => campaignDocumentTemplateService.getActiveByDocumentType(documentType)).then((result) => {
      if (result) {
        setTemplates(result)
        setTemplateId(result[0]?.id)
      } else {
        setTemplates([])
        setTemplateId(undefined)
      }
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, documentType])

  const documentTypeOptions = useMemo(
    () =>
      Object.values(CampaignDocumentType).map((value) => ({
        value: String(value),
        label: campaignDocumentTypeLabels[value as CampaignDocumentTypeValue],
      })),
    [],
  )

  const templateOptions = useMemo(
    () => templates.map((template) => ({ value: String(template.id), label: template.name })),
    [templates],
  )

  const creatorOptions = useMemo(
    () => [
      { value: '', label: 'Sem vínculo (campanha completa)' },
      ...campaignCreators.map((item) => ({
        value: String(item.id),
        label: item.creator?.stageName || item.creator?.name || `Creator #${item.creatorId}`,
      })),
    ],
    [campaignCreators],
  )

  const isValid = !!templateId && title.trim().length >= 2

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!templateId) return

    const result = await execute(() =>
      campaignDocumentService.generateFromTemplate({
        campaignId,
        campaignCreatorId,
        templateId,
        title: title.trim(),
      }),
    )
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '720px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>Gerar documento a partir de template</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">Tipo de documento</label>
              <SearchableSelect
                value={String(documentType)}
                onValueChange={(value) => setDocumentType(Number(value) as CampaignDocumentTypeValue)}
                options={documentTypeOptions}
                placeholder="Selecione"
                searchPlaceholder="Buscar tipo"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Template</label>
              <SearchableSelect
                value={templateId ? String(templateId) : ''}
                onValueChange={(value) => setTemplateId(value ? Number(value) : undefined)}
                options={templateOptions}
                placeholder={templatesLoading ? 'Carregando...' : templateOptions.length === 0 ? 'Sem templates ativos para este tipo' : 'Selecione um template'}
                searchPlaceholder="Buscar template"
                disabled={templatesLoading || templateOptions.length === 0}
              />
            </div>
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Creator vinculado</label>
              <SearchableSelect
                value={campaignCreatorId ? String(campaignCreatorId) : ''}
                onValueChange={(value) => setCampaignCreatorId(value ? Number(value) : undefined)}
                options={creatorOptions}
                placeholder="Opcional"
                searchPlaceholder="Buscar creator"
              />
            </div>
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Título do documento</label>
              <Input
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="Ex.: Contrato — João Silva — Campanha Verão 2026"
                required
              />
            </div>
          </div>
          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancelar
            </Button>
            <Button type="submit" disabled={loading || !isValid}>
              {loading ? 'Gerando...' : 'Gerar documento'}
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
