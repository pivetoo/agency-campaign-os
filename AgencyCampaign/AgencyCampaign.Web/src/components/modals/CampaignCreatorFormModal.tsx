import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, SearchableSelect, useApi } from 'archon-ui'
import { campaignCreatorService, type CreateCampaignCreatorRequest, type UpdateCampaignCreatorRequest } from '../../services/campaignCreatorService'
import { campaignCreatorStatusService } from '../../services/campaignCreatorStatusService'
import { creatorService } from '../../services/creatorService'
import type { CampaignCreator } from '../../types/campaignCreator'
import type { CampaignCreatorStatus } from '../../types/campaignCreatorStatus'
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
  campaignCreatorStatusId: 0,
  agreedAmount: 0,
  agencyFeePercent: 0,
  notes: '',
}

export default function CampaignCreatorFormModal({ open, onOpenChange, campaignId, campaignCreator, onSuccess }: CampaignCreatorFormModalProps) {
  const isEditing = !!campaignCreator
  const [formData, setFormData] = useState<CreateCampaignCreatorRequest>(initialFormData)
  const [creators, setCreators] = useState<Creator[]>([])
  const [statuses, setStatuses] = useState<CampaignCreatorStatus[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: fetchCreators } = useApi<Creator[]>({ showErrorMessage: true })
  const { execute: fetchStatuses } = useApi<CampaignCreatorStatus[]>({ showErrorMessage: true })

  useEffect(() => {
    if (open) {
      void fetchCreators(() => creatorService.getAll()).then((result) => {
        if (result) {
          setCreators(result.filter((creator) => creator.isActive))
        }
      })
      void fetchStatuses(() => campaignCreatorStatusService.getActive()).then((result) => {
        if (result) {
          setStatuses(result)
          setFormData((prev) => ({
            ...prev,
            campaignCreatorStatusId: prev.campaignCreatorStatusId || result.find((s) => s.isInitial)?.id || result[0]?.id || 0,
          }))
        }
      })
    }
  }, [open])

  useEffect(() => {
    if (isEditing) {
      return
    }

    const selectedCreator = creators.find((item) => item.id === formData.creatorId)
    if (!selectedCreator) {
      return
    }

    setFormData((prev) => ({
      ...prev,
      agencyFeePercent: selectedCreator.defaultAgencyFeePercent ?? 0,
    }))
  }, [creators, formData.creatorId, isEditing])

  useEffect(() => {
    if (campaignCreator) {
      setFormData({
        campaignId,
        creatorId: campaignCreator.creatorId,
        campaignCreatorStatusId: campaignCreator.campaignCreatorStatusId || statuses.find((s) => s.isInitial)?.id || 0,
        agreedAmount: campaignCreator.agreedAmount,
        agencyFeePercent: campaignCreator.agencyFeePercent,
        notes: campaignCreator.notes || '',
      })
      return
    }

    setFormData({ ...initialFormData, campaignId, campaignCreatorStatusId: statuses.find((s) => s.isInitial)?.id || 0 })
  }, [campaignCreator, campaignId, open, statuses])

  const calculatedAgencyFeeAmount = Number(((formData.agreedAmount * (formData.agencyFeePercent || 0)) / 100).toFixed(2))

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
            notes: payload.notes,
            campaignCreatorStatusId: payload.campaignCreatorStatusId,
          } satisfies UpdateCampaignCreatorRequest)
        : campaignCreatorService.create(payload),
    )

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '860px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar creator da campanha' : 'Adicionar creator à campanha'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div className="space-y-2">
              <label className="text-sm font-medium">Creator</label>
              <SearchableSelect
                value={formData.creatorId ? String(formData.creatorId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, creatorId: Number(value) }))}
                options={creators.map((creator) => ({ value: String(creator.id), label: `${creator.stageName || creator.name} · fee ${creator.defaultAgencyFeePercent ?? 0}%` }))}
                placeholder="Selecione um creator"
                searchPlaceholder="Buscar creator"
                disabled={isEditing}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Status</label>
              <SearchableSelect
                value={String(formData.campaignCreatorStatusId)}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, campaignCreatorStatusId: Number(value) }))}
                options={statuses.map((status) => ({ value: String(status.id), label: status.name }))}
                placeholder="Selecione um status"
                searchPlaceholder="Buscar status"
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Valor combinado</label>
              <Input
                type="number"
                value={formData.agreedAmount === 0 ? '' : formData.agreedAmount}
                onChange={(e) => setFormData((prev) => ({
                  ...prev,
                  agreedAmount: e.target.value === '' ? 0 : Number(e.target.value),
                }))}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Fee da agência (%)</label>
              <Input type="number" value={formData.agencyFeePercent} disabled />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Fee calculado</label>
              <Input type="number" value={calculatedAgencyFeeAmount} disabled />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
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
