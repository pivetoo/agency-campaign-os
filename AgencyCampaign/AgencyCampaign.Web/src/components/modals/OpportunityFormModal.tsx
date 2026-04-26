import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui'
import { brandService } from '../../services/brandService'
import { commercialResponsibleService } from '../../services/commercialResponsibleService'
import type { Brand } from '../../types/brand'
import type { CommercialResponsible } from '../../types/commercialResponsible'
import { opportunityService, type Opportunity, type CreateOpportunityRequest, type UpdateOpportunityRequest } from '../../services/opportunityService'

interface OpportunityFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  opportunity: Opportunity | null
  onSuccess: () => void
}

const initialFormData: CreateOpportunityRequest = {
  brandId: 0,
  name: '',
  description: '',
  estimatedValue: 0,
  expectedCloseAt: undefined,
  contactName: '',
  contactEmail: '',
  notes: '',
}

export default function OpportunityFormModal({ open, onOpenChange, opportunity, onSuccess }: OpportunityFormModalProps) {
  const isEditing = !!opportunity
  const [formData, setFormData] = useState<CreateOpportunityRequest>(initialFormData)
  const [brands, setBrands] = useState<Brand[]>([])
  const [responsibles, setResponsibles] = useState<CommercialResponsible[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    void brandService.getAll().then(setBrands)
    void commercialResponsibleService.getAll().then(setResponsibles)
  }, [])

  useEffect(() => {
    if (opportunity) {
      setFormData({
        brandId: opportunity.brandId,
        name: opportunity.name,
        description: opportunity.description || '',
        estimatedValue: opportunity.estimatedValue,
        expectedCloseAt: opportunity.expectedCloseAt,
        commercialResponsibleId: opportunity.commercialResponsibleId,
        contactName: opportunity.contactName || '',
        contactEmail: opportunity.contactEmail || '',
        notes: opportunity.notes || '',
      })
      return
    }

    setFormData(initialFormData)
  }, [opportunity, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() => (
      isEditing
        ? opportunityService.update(opportunity.id, {
            id: opportunity.id,
            ...formData,
          } satisfies UpdateOpportunityRequest)
        : opportunityService.create(formData)
    ))

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '960px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar oportunidade' : 'Nova oportunidade'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div className="space-y-2">
              <label className="text-sm font-medium">Nome da oportunidade</label>
              <Input value={formData.name} onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Marca</label>
              <Select value={formData.brandId ? String(formData.brandId) : ''} onValueChange={(value) => setFormData((prev) => ({ ...prev, brandId: Number(value) }))}>
                <SelectTrigger><SelectValue placeholder="Selecione uma marca" /></SelectTrigger>
                <SelectContent>
                  {brands.map((brand) => (
                    <SelectItem key={brand.id} value={String(brand.id)}>{brand.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Valor estimado</label>
              <Input type="number" value={formData.estimatedValue === 0 ? '' : formData.estimatedValue} onChange={(e) => setFormData((prev) => ({ ...prev, estimatedValue: e.target.value === '' ? 0 : Number(e.target.value) }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Previsão de fechamento</label>
              <Input type="date" value={formData.expectedCloseAt?.split('T')[0] || ''} onChange={(e) => setFormData((prev) => ({ ...prev, expectedCloseAt: e.target.value ? new Date(e.target.value).toISOString() : undefined }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Responsável comercial</label>
              <Select value={formData.commercialResponsibleId ? String(formData.commercialResponsibleId) : '0'} onValueChange={(value) => setFormData((prev) => ({ ...prev, commercialResponsibleId: value === '0' ? undefined : Number(value) }))}>
                <SelectTrigger><SelectValue placeholder="Selecione um responsável" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="0">Nenhum</SelectItem>
                  {responsibles.filter((r) => r.isActive).map((responsible) => (
                    <SelectItem key={responsible.id} value={String(responsible.id)}>{responsible.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Contato</label>
              <Input value={formData.contactName || ''} onChange={(e) => setFormData((prev) => ({ ...prev, contactName: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">E-mail</label>
              <Input type="email" value={formData.contactEmail || ''} onChange={(e) => setFormData((prev) => ({ ...prev, contactEmail: e.target.value }))} />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">Descrição</label>
              <Input value={formData.description || ''} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">Observações</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={loading || !formData.brandId}>{loading ? 'Salvando...' : 'Salvar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
