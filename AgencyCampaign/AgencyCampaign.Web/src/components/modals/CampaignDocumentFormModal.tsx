import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, SearchableSelect, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui'
import { campaignDocumentService, type CreateCampaignDocumentRequest, type UpdateCampaignDocumentRequest } from '../../services/campaignDocumentService'
import { CampaignDocumentType, type CampaignDocument, type CampaignDocumentTypeValue } from '../../types/campaignDocument'
import type { CampaignCreator } from '../../types/campaignCreator'

interface CampaignDocumentFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  campaignId: number
  document: CampaignDocument | null
  campaignCreators: CampaignCreator[]
  onSuccess: () => void
}

const initialFormData: CreateCampaignDocumentRequest = {
  campaignId: 0,
  campaignCreatorId: undefined,
  documentType: CampaignDocumentType.CreatorAgreement,
  title: '',
  documentUrl: '',
  notes: '',
}

export default function CampaignDocumentFormModal({ open, onOpenChange, campaignId, document, campaignCreators, onSuccess }: CampaignDocumentFormModalProps) {
  const { t } = useI18n()
  const isEditing = !!document
  const [formData, setFormData] = useState<CreateCampaignDocumentRequest>(initialFormData)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (document) {
      setFormData({
        campaignId,
        campaignCreatorId: document.campaignCreatorId,
        documentType: document.documentType,
        title: document.title,
        documentUrl: document.documentUrl || '',
        notes: document.notes || '',
      })
      return
    }

    setFormData({ ...initialFormData, campaignId })
  }, [document, campaignId, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const payload = {
      ...formData,
      documentUrl: formData.documentUrl || undefined,
      notes: formData.notes || undefined,
      campaignCreatorId: formData.campaignCreatorId || undefined,
    }

    const result = await execute(() =>
      isEditing
        ? campaignDocumentService.update(document.id, {
            id: document.id,
            campaignCreatorId: payload.campaignCreatorId,
            documentType: payload.documentType,
            title: payload.title,
            documentUrl: payload.documentUrl,
            notes: payload.notes,
          } satisfies UpdateCampaignDocumentRequest)
        : campaignDocumentService.create(payload),
    )

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '920px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.document.title.edit') : t('modal.document.title.new')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.document.field.documentType')}</label>
              <Select value={String(formData.documentType)} onValueChange={(value) => setFormData((prev) => ({ ...prev, documentType: Number(value) as CampaignDocumentTypeValue }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">Aceite do creator</SelectItem>
                  <SelectItem value="2">Contrato da marca</SelectItem>
                  <SelectItem value="3">Termo de autorização</SelectItem>
                  <SelectItem value="4">Anexo de briefing</SelectItem>
                  <SelectItem value="5">Outro</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.document.field.campaignCreator')}</label>
              <SearchableSelect
                value={formData.campaignCreatorId ? String(formData.campaignCreatorId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, campaignCreatorId: value ? Number(value) : undefined }))}
                options={[
                  { value: '', label: 'Sem vínculo' },
                  ...campaignCreators.map((item) => ({ value: String(item.id), label: item.creator?.stageName || item.creator?.name || `Creator #${item.creatorId}` })),
                ]}
                placeholder="Opcional"
                searchPlaceholder={t('common.placeholder.search')}
              />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">{t('common.field.title')}</label>
              <Input value={formData.title} onChange={(e) => setFormData((prev) => ({ ...prev, title: e.target.value }))} required />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">{t('modal.document.field.documentUrl')}</label>
              <Input value={formData.documentUrl || ''} onChange={(e) => setFormData((prev) => ({ ...prev, documentUrl: e.target.value }))} />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">{t('common.field.notes')}</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading}>{loading ? t('common.action.saving') : t('common.action.save')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
