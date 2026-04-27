import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, SearchableSelect, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui'
import { campaignDeliverableService, type CreateCampaignDeliverableRequest, type UpdateCampaignDeliverableRequest } from '../../services/campaignDeliverableService'
import { campaignCreatorService } from '../../services/campaignCreatorService'
import { platformService } from '../../services/platformService'
import { deliverableKindService } from '../../services/deliverableKindService'
import type { CampaignDeliverable } from '../../types/campaignDeliverable'
import type { CampaignCreator } from '../../types/campaignCreator'
import type { Platform } from '../../types/platform'
import type { DeliverableKind } from '../../types/deliverableKind'

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
  deliverableKindId: 0,
  platformId: 0,
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
  const [platforms, setPlatforms] = useState<Platform[]>([])
  const [deliverableKinds, setDeliverableKinds] = useState<DeliverableKind[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: fetchCampaignCreators } = useApi<CampaignCreator[]>({ showErrorMessage: true })
  const { execute: fetchPlatforms } = useApi<Platform[]>({ showErrorMessage: true })
  const { execute: fetchDeliverableKinds } = useApi<DeliverableKind[]>({ showErrorMessage: true })

  useEffect(() => {
    if (open && campaignId > 0) {
      void fetchCampaignCreators(() => campaignCreatorService.getByCampaign(campaignId)).then((result) => {
        if (result) {
          setCampaignCreators(result)
        }
      })

      void fetchPlatforms(() => platformService.getActive()).then((result) => {
        if (result) {
          setPlatforms(result)
        }
      })

      void fetchDeliverableKinds(() => deliverableKindService.getActive()).then((result) => {
        if (result) {
          setDeliverableKinds(result)
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
        deliverableKindId: deliverable.deliverableKindId,
        platformId: deliverable.platformId,
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

    if (!formData.campaignCreatorId || !formData.deliverableKindId || !formData.platformId) {
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
            deliverableKindId: payload.deliverableKindId,
            platformId: payload.platformId,
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
      <ModalContent size="full" style={{ maxWidth: '980px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar entrega' : 'Nova entrega'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-5">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div className="space-y-2">
              <label className="text-sm font-medium">Creator da campanha</label>
              <SearchableSelect
                value={formData.campaignCreatorId ? String(formData.campaignCreatorId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, campaignCreatorId: Number(value) }))}
                options={campaignCreators.map((item) => ({ value: String(item.id), label: item.creator?.stageName || item.creator?.name || `Creator #${item.creatorId}` }))}
                placeholder="Selecione um creator"
                searchPlaceholder="Buscar creator"
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Status</label>
              <Select value={String(formData.status)} onValueChange={(value) => setFormData((prev) => ({ ...prev, status: Number(value) }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">Pendente</SelectItem>
                  <SelectItem value="2">Em revisão</SelectItem>
                  <SelectItem value="3">Aprovada</SelectItem>
                  <SelectItem value="4">Publicada</SelectItem>
                  <SelectItem value="5">Cancelada</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">Título</label>
              <Input value={formData.title} onChange={(e) => setFormData((prev) => ({ ...prev, title: e.target.value }))} required />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">Descrição</label>
              <Input value={formData.description || ''} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Tipo</label>
              <SearchableSelect
                value={formData.deliverableKindId ? String(formData.deliverableKindId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, deliverableKindId: Number(value) }))}
                options={deliverableKinds.map((item) => ({ value: String(item.id), label: item.name }))}
                placeholder="Selecione um tipo"
                searchPlaceholder="Buscar tipo"
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Plataforma</label>
              <SearchableSelect
                value={formData.platformId ? String(formData.platformId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, platformId: Number(value) }))}
                options={platforms.map((item) => ({ value: String(item.id), label: item.name }))}
                placeholder="Selecione uma plataforma"
                searchPlaceholder="Buscar plataforma"
              />
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

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '1rem' }}>
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
            <Button type="submit" disabled={loading || !formData.campaignCreatorId || !formData.deliverableKindId || !formData.platformId}>{loading ? 'Salvando...' : 'Salvar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
