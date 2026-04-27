import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, SearchableSelect, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui'
import { campaignDocumentService, type CreateCampaignDocumentRequest, type UpdateCampaignDocumentRequest } from '../../services/campaignDocumentService'
import type { CampaignDocument } from '../../types/campaignDocument'
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
  documentType: 1,
  title: '',
  documentUrl: '',
  notes: '',
}

export default function CampaignDocumentFormModal({ open, onOpenChange, campaignId, document, campaignCreators, onSuccess }: CampaignDocumentFormModalProps) {
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
          <ModalTitle>{isEditing ? 'Editar documento' : 'Novo documento'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div className="space-y-2">
              <label className="text-sm font-medium">Tipo</label>
              <Select value={String(formData.documentType)} onValueChange={(value) => setFormData((prev) => ({ ...prev, documentType: Number(value) }))}>
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
              <label className="text-sm font-medium">Creator da campanha</label>
              <SearchableSelect
                value={formData.campaignCreatorId ? String(formData.campaignCreatorId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, campaignCreatorId: value ? Number(value) : undefined }))}
                options={[
                  { value: '', label: 'Sem vínculo' },
                  ...campaignCreators.map((item) => ({ value: String(item.id), label: item.creator?.stageName || item.creator?.name || `Creator #${item.creatorId}` })),
                ]}
                placeholder="Opcional"
                searchPlaceholder="Buscar creator"
              />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">Título</label>
              <Input value={formData.title} onChange={(e) => setFormData((prev) => ({ ...prev, title: e.target.value }))} required />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">URL do documento</label>
              <Input value={formData.documentUrl || ''} onChange={(e) => setFormData((prev) => ({ ...prev, documentUrl: e.target.value }))} />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">Observações</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={loading}>{loading ? 'Salvando...' : 'Salvar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
