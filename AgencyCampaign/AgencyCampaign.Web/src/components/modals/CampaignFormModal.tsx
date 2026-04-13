import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui'
import { campaignService, type CreateCampaignRequest, type UpdateCampaignRequest } from '../../services/campaignService'
import { brandService } from '../../services/brandService'
import type { Campaign } from '../../types/campaign'
import type { Brand } from '../../types/brand'

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
  internalOwnerName: '',
  notes: '',
  status: 1,
}

const campaignStatusOptions = [
  { value: 1, label: 'Rascunho' },
  { value: 2, label: 'Planejada' },
  { value: 3, label: 'Em execução' },
  { value: 4, label: 'Em revisão' },
  { value: 5, label: 'Concluída' },
  { value: 6, label: 'Cancelada' },
]

export default function CampaignFormModal({ open, onOpenChange, campaign, onSuccess }: CampaignFormModalProps) {
  const isEditing = !!campaign
  const [formData, setFormData] = useState<CreateCampaignRequest>(initialFormData)
  const [isActive, setIsActive] = useState(true)
  const [brands, setBrands] = useState<Brand[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: fetchBrands } = useApi<Brand[]>({ showErrorMessage: true })

  useEffect(() => {
    if (open) {
      void fetchBrands(() => brandService.getAll()).then((result) => {
        if (result) {
          setBrands(result)
        }
      })
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
        internalOwnerName: campaign.internalOwnerName || '',
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
      internalOwnerName: formData.internalOwnerName || undefined,
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
          <ModalTitle>{isEditing ? 'Editar campanha' : 'Nova campanha'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div className="space-y-2">
              <label className="text-sm font-medium">Marca</label>
              <Select value={formData.brandId ? String(formData.brandId) : ''} onValueChange={(value) => setFormData((prev) => ({ ...prev, brandId: Number(value) }))}>
                <SelectTrigger>
                  <SelectValue placeholder="Selecione uma marca" />
                </SelectTrigger>
                <SelectContent>
                  {brands.map((brand) => (
                    <SelectItem key={brand.id} value={String(brand.id)}>{brand.tradeName || brand.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Nome</label>
              <Input value={formData.name} onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Status</label>
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
              <label className="text-sm font-medium">Responsável interno</label>
              <Input value={formData.internalOwnerName || ''} onChange={(e) => setFormData((prev) => ({ ...prev, internalOwnerName: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Descrição</label>
              <Input value={formData.description || ''} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Objetivo</label>
              <Input value={formData.objective || ''} onChange={(e) => setFormData((prev) => ({ ...prev, objective: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Briefing</label>
              <Input value={formData.briefing || ''} onChange={(e) => setFormData((prev) => ({ ...prev, briefing: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Budget</label>
              <Input type="number" value={formData.budget} onChange={(e) => setFormData((prev) => ({ ...prev, budget: Number(e.target.value) }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Início</label>
              <Input type="date" value={formData.startsAt} onChange={(e) => setFormData((prev) => ({ ...prev, startsAt: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Fim</label>
              <Input type="date" value={formData.endsAt || ''} onChange={(e) => setFormData((prev) => ({ ...prev, endsAt: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Observações</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '1rem' }}>
            <div>
              {isEditing && (
                <div className="flex items-center gap-2">
                  <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
                  <span className="text-sm">Ativa</span>
                </div>
              )}
            </div>

            <ModalFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
              <Button type="submit" disabled={loading || !formData.brandId}>{loading ? 'Salvando...' : 'Salvar'}</Button>
            </ModalFooter>
          </div>
        </form>
      </ModalContent>
    </Modal>
  )
}
