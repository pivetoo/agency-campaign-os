import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, SearchableSelect, useApi, useI18n } from 'archon-ui'
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
  const { t } = useI18n()
  const isEditing = !!campaignCreator
  const [formData, setFormData] = useState<CreateCampaignCreatorRequest>(initialFormData)
  const [creators, setCreators] = useState<Creator[]>([])
  const [statuses, setStatuses] = useState<CampaignCreatorStatus[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: fetchStatuses } = useApi<CampaignCreatorStatus[]>({ showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    void creatorService.getAll({ pageSize: 10 }).then((r) => {
      setCreators((r.data ?? []).filter((c) => c.isActive))
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
          <ModalTitle>{isEditing ? t('modal.campaignCreator.title.edit') : t('modal.campaignCreator.title.new')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="space-y-2" data-testid="form-field-creator">
              <label className="text-sm font-medium">{t('creators.singular')}</label>
              <SearchableSelect
                value={formData.creatorId ? String(formData.creatorId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, creatorId: Number(value) }))}
                options={creators.map((creator) => ({ value: String(creator.id), label: `${creator.stageName || creator.name} · fee ${creator.defaultAgencyFeePercent ?? 0}%` }))}
                placeholder={t('common.placeholder.select')}
                searchPlaceholder={t('common.placeholder.search')}
                disabled={isEditing}
                onSearch={async (term) => {
                  const r = await creatorService.getAll({ search: term, pageSize: 10 })
                  const found = (r.data ?? []).filter((c) => c.isActive)
                  setCreators((prev) => {
                    const map = new Map(prev.map((c) => [c.id, c]))
                    found.forEach((c) => map.set(c.id, c))
                    return Array.from(map.values())
                  })
                  return found.map((c) => ({ value: String(c.id), label: `${c.stageName || c.name} · fee ${c.defaultAgencyFeePercent ?? 0}%` }))
                }}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.status')}</label>
              <SearchableSelect
                value={String(formData.campaignCreatorStatusId)}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, campaignCreatorStatusId: Number(value) }))}
                options={statuses.map((status) => ({ value: String(status.id), label: status.name }))}
                placeholder={t('common.placeholder.select')}
                searchPlaceholder={t('common.placeholder.search')}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.campaignCreator.field.agreedAmount')}</label>
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
              <label className="text-sm font-medium">{t('modal.campaignCreator.field.agencyFeePercent')}</label>
              <Input type="number" value={formData.agencyFeePercent} disabled />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.campaignCreator.field.agencyFeeCalc')}</label>
              <Input type="number" value={calculatedAgencyFeeAmount} disabled />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">{t('common.field.notes')}</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || !formData.creatorId}>{loading ? t('common.action.saving') : t('common.action.save')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
