import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, SearchableSelect, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui'
import { campaignService, type CreateCampaignRequest, type UpdateCampaignRequest } from '../../services/campaignService'
import { brandService } from '../../services/brandService'
import { commercialResponsibleService } from '../../services/commercialResponsibleService'
import type { Campaign } from '../../types/campaign'
import type { Brand } from '../../types/brand'
import type { CommercialResponsible } from '../../types/commercialResponsible'

interface CampaignFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  campaign: Campaign | null
  onSuccess: () => void
}

const initialFormData: CreateCampaignRequest = {
  brandId: 0,
  name: '',
  description: '',
  objective: '',
  briefing: '',
  budget: 0,
  startsAt: '',
  endsAt: '',
  notes: '',
  status: 1,
}

export default function CampaignFormModal({ open, onOpenChange, campaign, onSuccess }: CampaignFormModalProps) {
  const { t } = useI18n()
  const isEditing = !!campaign

  const campaignStatusOptions = [
    { value: 1, label: t('modal.campaign.status.draft') },
    { value: 2, label: t('modal.campaign.status.planned') },
    { value: 3, label: t('modal.campaign.status.executing') },
    { value: 4, label: t('modal.campaign.status.inReview') },
    { value: 5, label: t('modal.campaign.status.completed') },
    { value: 6, label: t('modal.campaign.status.cancelled') },
  ]
  const [formData, setFormData] = useState<CreateCampaignRequest>(initialFormData)
  const [isActive, setIsActive] = useState(true)
  const [brands, setBrands] = useState<Brand[]>([])
  const [responsibles, setResponsibles] = useState<CommercialResponsible[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: fetchBrands } = useApi<Brand[]>({ showErrorMessage: true })

  useEffect(() => {
    if (open) {
      void fetchBrands(() => brandService.getAll()).then((result) => {
        if (result) {
          setBrands(result)
        }
      })
      void commercialResponsibleService.getAll().then(setResponsibles)
    }
  }, [open])

  useEffect(() => {
    if (campaign) {
      setFormData({
        brandId: campaign.brandId,
        name: campaign.name,
        description: campaign.description || '',
        objective: campaign.objective || '',
        briefing: campaign.briefing || '',
        budget: campaign.budget,
        startsAt: campaign.startsAt.slice(0, 10),
        endsAt: campaign.endsAt ? campaign.endsAt.slice(0, 10) : '',
        responsibleUserId: campaign.responsibleUserId,
        notes: campaign.notes || '',
        status: campaign.status,
      })
      setIsActive(campaign.isActive)
      return
    }

    setFormData(initialFormData)
    setIsActive(true)
  }, [campaign, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    if (!formData.brandId || formData.brandId <= 0) {
      return
    }

    const payload = {
      ...formData,
      endsAt: formData.endsAt || undefined,
      description: formData.description || undefined,
      objective: formData.objective || undefined,
      briefing: formData.briefing || undefined,
      responsibleUserId: formData.responsibleUserId,
      notes: formData.notes || undefined,
    }

    const result = await execute(() =>
      isEditing
        ? campaignService.update(campaign.id, {
            id: campaign.id,
            ...payload,
            isActive,
          } satisfies UpdateCampaignRequest)
        : campaignService.create(payload),
    )

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '1180px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.campaign.title.edit') : t('modal.campaign.title.new')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.brand')}</label>
              <SearchableSelect
                value={formData.brandId ? String(formData.brandId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, brandId: Number(value) }))}
                options={brands.map((brand) => ({ value: String(brand.id), label: brand.tradeName || brand.name }))}
                placeholder={t('common.placeholder.select')}
                searchPlaceholder={t('common.placeholder.search')}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.name')}</label>
              <Input value={formData.name} onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.status')}</label>
              <Select value={String(formData.status)} onValueChange={(value) => setFormData((prev) => ({ ...prev, status: Number(value) }))}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {campaignStatusOptions.map((option) => (
                    <SelectItem key={option.value} value={String(option.value)}>{option.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.campaign.field.responsible')}</label>
              <SearchableSelect
                value={formData.responsibleUserId ? String(formData.responsibleUserId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, responsibleUserId: value ? Number(value) : undefined }))}
                options={[
                  { value: '', label: 'Nenhum' },
                  ...responsibles.filter((r) => r.isActive).map((responsible) => ({ value: String(responsible.id), label: responsible.name })),
                ]}
                placeholder={t('common.placeholder.select')}
                searchPlaceholder={t('common.placeholder.search')}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.description')}</label>
              <Input value={formData.description || ''} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.objective')}</label>
              <Input value={formData.objective || ''} onChange={(e) => setFormData((prev) => ({ ...prev, objective: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.briefing')}</label>
              <Input value={formData.briefing || ''} onChange={(e) => setFormData((prev) => ({ ...prev, briefing: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.budget')}</label>
              <Input type="number" value={formData.budget} onChange={(e) => setFormData((prev) => ({ ...prev, budget: Number(e.target.value) }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.startDate')}</label>
              <Input type="date" value={formData.startsAt} onChange={(e) => setFormData((prev) => ({ ...prev, startsAt: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.endDate')}</label>
              <Input type="date" value={formData.endsAt || ''} onChange={(e) => setFormData((prev) => ({ ...prev, endsAt: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.notes')}</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>
          </div>

          <div className="flex flex-col-reverse gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              {isEditing && (
                <div className="flex items-center gap-2">
                  <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
                  <span className="text-sm">{t('common.status.activeFemale')}</span>
                </div>
              )}
            </div>

            <ModalFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
              <Button type="submit" disabled={loading || !formData.brandId}>{loading ? t('common.action.saving') : t('common.action.save')}</Button>
            </ModalFooter>
          </div>
        </form>
      </ModalContent>
    </Modal>
  )
}
