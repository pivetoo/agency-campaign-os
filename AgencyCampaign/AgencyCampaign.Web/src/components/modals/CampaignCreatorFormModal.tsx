import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui'
import { campaignCreatorService, type CreateCampaignCreatorRequest, type UpdateCampaignCreatorRequest } from '../../services/campaignCreatorService'
import { creatorService } from '../../services/creatorService'
import type { CampaignCreator } from '../../types/campaignCreator'
import type { Creator } from '../../types/creator'

interface CampaignCreatorFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  campaignId: number
  campaignCreator: CampaignCreator | null
  onSuccess: () => void
}

const initialFormData: CreateCampaignCreatorRequest = {
  campaignId: 0,
  creatorId: 0,
  agreedAmount: 0,
  agencyFeeAmount: 0,
  notes: '',
  status: 1,
}

export default function CampaignCreatorFormModal({ open, onOpenChange, campaignId, campaignCreator, onSuccess }: CampaignCreatorFormModalProps) {
  const isEditing = !!campaignCreator
  const [formData, setFormData] = useState<CreateCampaignCreatorRequest>(initialFormData)
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
    if (campaignCreator) {
      setFormData({
        campaignId,
        creatorId: campaignCreator.creatorId,
        agreedAmount: campaignCreator.agreedAmount,
        agencyFeeAmount: campaignCreator.agencyFeeAmount,
        notes: campaignCreator.notes || '',
        status: campaignCreator.status,
      })
      return
    }

    setFormData({ ...initialFormData, campaignId })
  }, [campaignCreator, campaignId, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    if (!formData.creatorId || formData.creatorId <= 0) {
      return
    }

    const payload = {
      ...formData,
      notes: formData.notes || undefined,
    }

    const result = await execute(() =>
      isEditing
        ? campaignCreatorService.update(campaignCreator.id, {
            id: campaignCreator.id,
            agreedAmount: payload.agreedAmount,
            agencyFeeAmount: payload.agencyFeeAmount,
            notes: payload.notes,
            status: payload.status,
          } satisfies UpdateCampaignCreatorRequest)
        : campaignCreatorService.create(payload),
    )

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar creator da campanha' : 'Adicionar creator à campanha'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">Creator</label>
            <Select value={formData.creatorId ? String(formData.creatorId) : ''} onValueChange={(value) => setFormData((prev) => ({ ...prev, creatorId: Number(value) }))} disabled={isEditing}>
              <SelectTrigger><SelectValue placeholder="Selecione um creator" /></SelectTrigger>
              <SelectContent>
                {creators.map((creator) => (
                  <SelectItem key={creator.id} value={String(creator.id)}>{creator.stageName || creator.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">Status</label>
              <Select value={String(formData.status)} onValueChange={(value) => setFormData((prev) => ({ ...prev, status: Number(value) }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">Convidado</SelectItem>
                  <SelectItem value="2">Pendente aprovação</SelectItem>
                  <SelectItem value="3">Confirmado</SelectItem>
                  <SelectItem value="4">Em execução</SelectItem>
                  <SelectItem value="5">Entregue</SelectItem>
                  <SelectItem value="6">Cancelado</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Valor combinado</label>
              <Input type="number" value={formData.agreedAmount} onChange={(e) => setFormData((prev) => ({ ...prev, agreedAmount: Number(e.target.value) }))} />
            </div>
            <div className="space-y-2 col-span-2">
              <label className="text-sm font-medium">Fee da agência</label>
              <Input type="number" value={formData.agencyFeeAmount} onChange={(e) => setFormData((prev) => ({ ...prev, agencyFeeAmount: Number(e.target.value) }))} />
            </div>
            <div className="space-y-2 col-span-2">
              <label className="text-sm font-medium">Observações</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
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
