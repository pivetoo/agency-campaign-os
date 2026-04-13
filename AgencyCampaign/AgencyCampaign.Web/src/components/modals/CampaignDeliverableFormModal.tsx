import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui'
import { campaignDeliverableService, type CreateCampaignDeliverableRequest, type UpdateCampaignDeliverableRequest } from '../../services/campaignDeliverableService'
import { campaignCreatorService } from '../../services/campaignCreatorService'
import type { CampaignDeliverable } from '../../types/campaignDeliverable'
import type { CampaignCreator } from '../../types/campaignCreator'

interface CampaignDeliverableFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  campaignId: number
  deliverable: CampaignDeliverable | null
  onSuccess: () => void
}

const initialFormData: CreateCampaignDeliverableRequest = {
  campaignId: 0,
  campaignCreatorId: 0,
  title: '',
  description: '',
  type: 1,
  platform: 1,
  dueAt: '',
  status: 1,
  publishedUrl: '',
  evidenceUrl: '',
  notes: '',
  grossAmount: 0,
  creatorAmount: 0,
  agencyFeeAmount: 0,
}

export default function CampaignDeliverableFormModal({ open, onOpenChange, campaignId, deliverable, onSuccess }: CampaignDeliverableFormModalProps) {
  const isEditing = !!deliverable
  const [formData, setFormData] = useState<CreateCampaignDeliverableRequest>(initialFormData)
  const [campaignCreators, setCampaignCreators] = useState<CampaignCreator[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: fetchCampaignCreators } = useApi<CampaignCreator[]>({ showErrorMessage: true })

  useEffect(() => {
    if (open && campaignId > 0) {
      void fetchCampaignCreators(() => campaignCreatorService.getByCampaign(campaignId)).then((result) => {
        if (result) {
          setCampaignCreators(result)
        }
      })
    }
  }, [open, campaignId])

  useEffect(() => {
    if (deliverable) {
      setFormData({
        campaignId,
        campaignCreatorId: deliverable.campaignCreatorId,
        title: deliverable.title,
        description: deliverable.description || '',
        type: deliverable.type,
        platform: deliverable.platform,
        dueAt: deliverable.dueAt.slice(0, 10),
        status: deliverable.status,
        publishedUrl: deliverable.publishedUrl || '',
        evidenceUrl: deliverable.evidenceUrl || '',
        notes: deliverable.notes || '',
        grossAmount: deliverable.grossAmount,
        creatorAmount: deliverable.creatorAmount,
        agencyFeeAmount: deliverable.agencyFeeAmount,
      })
      return
    }

    setFormData({ ...initialFormData, campaignId })
  }, [deliverable, campaignId, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    if (!formData.campaignCreatorId || formData.campaignCreatorId <= 0) {
      return
    }

    const payload = {
      ...formData,
      description: formData.description || undefined,
      publishedUrl: formData.publishedUrl || undefined,
      evidenceUrl: formData.evidenceUrl || undefined,
      notes: formData.notes || undefined,
    }

    const result = await execute(() =>
      isEditing
        ? campaignDeliverableService.update(deliverable.id, {
            id: deliverable.id,
            title: payload.title,
            description: payload.description,
            type: payload.type,
            platform: payload.platform,
            dueAt: payload.dueAt,
            status: payload.status,
            publishedUrl: payload.publishedUrl,
            evidenceUrl: payload.evidenceUrl,
            notes: payload.notes,
            grossAmount: payload.grossAmount,
            creatorAmount: payload.creatorAmount,
            agencyFeeAmount: payload.agencyFeeAmount,
          } satisfies UpdateCampaignDeliverableRequest)
        : campaignDeliverableService.create(payload),
    )

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="xl">
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar entrega' : 'Nova entrega'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">Creator da campanha</label>
              <Select value={formData.campaignCreatorId ? String(formData.campaignCreatorId) : ''} onValueChange={(value) => setFormData((prev) => ({ ...prev, campaignCreatorId: Number(value) }))}>
                <SelectTrigger>
                  <SelectValue placeholder="Selecione um creator" />
                </SelectTrigger>
                <SelectContent>
                  {campaignCreators.map((item) => (
                    <SelectItem key={item.id} value={String(item.id)}>{item.creator?.stageName || item.creator?.name || `Creator #${item.creatorId}`}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Status</label>
              <Select value={String(formData.status)} onValueChange={(value) => setFormData((prev) => ({ ...prev, status: Number(value) }))}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">Pendente</SelectItem>
                  <SelectItem value="2">Em revisão</SelectItem>
                  <SelectItem value="3">Aprovada</SelectItem>
                  <SelectItem value="4">Publicada</SelectItem>
                  <SelectItem value="5">Cancelada</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Título</label>
              <Input value={formData.title} onChange={(e) => setFormData((prev) => ({ ...prev, title: e.target.value }))} required />
            </div>

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Descrição</label>
              <Input value={formData.description || ''} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Tipo</label>
              <Select value={String(formData.type)} onValueChange={(value) => setFormData((prev) => ({ ...prev, type: Number(value) }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">Reel</SelectItem>
                  <SelectItem value="2">Story</SelectItem>
                  <SelectItem value="3">Post feed</SelectItem>
                  <SelectItem value="4">Vídeo</SelectItem>
                  <SelectItem value="5">Live</SelectItem>
                  <SelectItem value="6">Combo</SelectItem>
                  <SelectItem value="7">Outro</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Plataforma</label>
              <Select value={String(formData.platform)} onValueChange={(value) => setFormData((prev) => ({ ...prev, platform: Number(value) }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">Instagram</SelectItem>
                  <SelectItem value="2">TikTok</SelectItem>
                  <SelectItem value="3">YouTube</SelectItem>
                  <SelectItem value="4">Kwai</SelectItem>
                  <SelectItem value="5">X</SelectItem>
                  <SelectItem value="6">Outro</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Prazo</label>
              <Input type="date" value={formData.dueAt} onChange={(e) => setFormData((prev) => ({ ...prev, dueAt: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">URL publicada</label>
              <Input value={formData.publishedUrl || ''} onChange={(e) => setFormData((prev) => ({ ...prev, publishedUrl: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Evidência</label>
              <Input value={formData.evidenceUrl || ''} onChange={(e) => setFormData((prev) => ({ ...prev, evidenceUrl: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Observações</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium">Valor bruto</label>
              <Input type="number" value={formData.grossAmount} onChange={(e) => setFormData((prev) => ({ ...prev, grossAmount: Number(e.target.value) }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Repasse creator</label>
              <Input type="number" value={formData.creatorAmount} onChange={(e) => setFormData((prev) => ({ ...prev, creatorAmount: Number(e.target.value) }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Fee agência</label>
              <Input type="number" value={formData.agencyFeeAmount} onChange={(e) => setFormData((prev) => ({ ...prev, agencyFeeAmount: Number(e.target.value) }))} required />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={loading || !formData.campaignCreatorId}>{loading ? 'Salvando...' : 'Salvar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
