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
  budget: 0,
  startsAt: '',
  endsAt: '',
}

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
        budget: campaign.budget,
        startsAt: campaign.startsAt.slice(0, 10),
        endsAt: campaign.endsAt ? campaign.endsAt.slice(0, 10) : '',
      })
      setIsActive(campaign.isActive)
      return
    }

    setFormData(initialFormData)
    setIsActive(true)
  }, [campaign, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const payload = {
      ...formData,
      endsAt: formData.endsAt || undefined,
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
      <ModalContent size="lg">
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar campanha' : 'Nova campanha'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">Marca</label>
              <Select value={formData.brandId ? String(formData.brandId) : ''} onValueChange={(value) => setFormData((prev) => ({ ...prev, brandId: Number(value) }))}>
                <SelectTrigger>
                  <SelectValue placeholder="Selecione uma marca" />
                </SelectTrigger>
                <SelectContent>
                  {brands.map((brand) => (
                    <SelectItem key={brand.id} value={String(brand.id)}>{brand.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Nome</label>
              <Input value={formData.name} onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))} required />
            </div>
            <div className="space-y-2 col-span-2">
              <label className="text-sm font-medium">Descrição</label>
              <Input value={formData.description || ''} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} />
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
          </div>

          {isEditing && (
            <div className="flex items-center gap-2">
              <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
              <span className="text-sm">Ativa</span>
            </div>
          )}

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={loading}>{loading ? 'Salvando...' : 'Salvar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
