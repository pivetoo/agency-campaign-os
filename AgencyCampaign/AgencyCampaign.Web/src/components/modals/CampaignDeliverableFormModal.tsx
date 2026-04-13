import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui'
import { campaignDeliverableService, type CreateCampaignDeliverableRequest, type UpdateCampaignDeliverableRequest } from '../../services/campaignDeliverableService'
import { creatorService } from '../../services/creatorService'
import type { CampaignDeliverable } from '../../types/campaignDeliverable'
import type { Creator } from '../../types/creator'

interface CampaignDeliverableFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  campaignId: number
  deliverable: CampaignDeliverable | null
  onSuccess: () => void
}

const initialFormData: CreateCampaignDeliverableRequest = {
  campaignId: 0,
  creatorId: 0,
  title: '',
  description: '',
  dueAt: '',
  publishedAt: '',
  status: 1,
  grossAmount: 0,
  creatorAmount: 0,
  agencyFeeAmount: 0,
}

export default function CampaignDeliverableFormModal({ open, onOpenChange, campaignId, deliverable, onSuccess }: CampaignDeliverableFormModalProps) {
  const isEditing = !!deliverable
  const [formData, setFormData] = useState<CreateCampaignDeliverableRequest>(initialFormData)
  const [creators, setCreators] = useState<Creator[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: fetchCreators } = useApi<Creator[]>({ showErrorMessage: true })

  useEffect(() => {
    if (open) {
      void fetchCreators(() => creatorService.getAll()).then((result) => {
        if (result) {
          setCreators(result)
        }
      })
    }
  }, [open])

  useEffect(() => {
    if (deliverable) {
      setFormData({
        campaignId,
        creatorId: deliverable.creatorId,
        title: deliverable.title,
        description: deliverable.description || '',
        dueAt: deliverable.dueAt.slice(0, 10),
        publishedAt: deliverable.publishedAt ? deliverable.publishedAt.slice(0, 10) : '',
        status: deliverable.status,
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

    if (!formData.creatorId || formData.creatorId <= 0) {
      return
    }

    const payload = {
      ...formData,
      publishedAt: formData.publishedAt || undefined,
    }

    const result = await execute(() =>
      isEditing
        ? campaignDeliverableService.update(deliverable.id, {
            id: deliverable.id,
            title: payload.title,
            description: payload.description,
            dueAt: payload.dueAt,
            publishedAt: payload.publishedAt,
            status: payload.status,
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
      <ModalContent size="lg">
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar entrega' : 'Nova entrega'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">Influenciador</label>
              <Select value={formData.creatorId ? String(formData.creatorId) : ''} onValueChange={(value) => setFormData((prev) => ({ ...prev, creatorId: Number(value) }))}>
                <SelectTrigger>
                  <SelectValue placeholder="Selecione um influenciador" />
                </SelectTrigger>
                <SelectContent>
                  {creators.map((creator) => (
                    <SelectItem key={creator.id} value={String(creator.id)}>{creator.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Título</label>
              <Input value={formData.title} onChange={(e) => setFormData((prev) => ({ ...prev, title: e.target.value }))} required />
            </div>
            <div className="space-y-2 col-span-2">
              <label className="text-sm font-medium">Descrição</label>
              <Input value={formData.description || ''} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Prazo</label>
              <Input type="date" value={formData.dueAt} onChange={(e) => setFormData((prev) => ({ ...prev, dueAt: e.target.value }))} required />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Publicado em</label>
              <Input type="date" value={formData.publishedAt || ''} onChange={(e) => setFormData((prev) => ({ ...prev, publishedAt: e.target.value }))} />
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
            <Button type="submit" disabled={loading || !formData.creatorId}>{loading ? 'Salvando...' : 'Salvar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
