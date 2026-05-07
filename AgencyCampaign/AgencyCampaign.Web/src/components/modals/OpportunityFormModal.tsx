import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, SearchableSelect, useApi } from 'archon-ui'
import { brandService } from '../../services/brandService'
import { commercialResponsibleService } from '../../services/commercialResponsibleService'
import { opportunitySourceService, opportunityTagService } from '../../services/opportunitySourceService'
import type { Brand } from '../../types/brand'
import type { CommercialResponsible } from '../../types/commercialResponsible'
import type { OpportunitySource, OpportunityTag } from '../../types/opportunitySource'
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
  opportunitySourceId: undefined,
  tagIds: [],
}

export default function OpportunityFormModal({ open, onOpenChange, opportunity, onSuccess }: OpportunityFormModalProps) {
  const isEditing = !!opportunity
  const [formData, setFormData] = useState<CreateOpportunityRequest>(initialFormData)
  const [brands, setBrands] = useState<Brand[]>([])
  const [responsibles, setResponsibles] = useState<CommercialResponsible[]>([])
  const [sources, setSources] = useState<OpportunitySource[]>([])
  const [tags, setTags] = useState<OpportunityTag[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    void brandService.getAll().then(setBrands)
    void commercialResponsibleService.getAll().then(setResponsibles)
    void opportunitySourceService.getAll(false).then(setSources)
    void opportunityTagService.getAll(false).then(setTags)
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
        opportunitySourceId: opportunity.opportunitySourceId,
        tagIds: opportunity.tags?.map((tag) => tag.id) ?? [],
      })
      return
    }

    setFormData(initialFormData)
  }, [opportunity, open])

  const toggleTag = (tagId: number) => {
    setFormData((prev) => {
      const current = prev.tagIds ?? []
      const next = current.includes(tagId)
        ? current.filter((id) => id !== tagId)
        : [...current, tagId]
      return { ...prev, tagIds: next }
    })
  }

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
              <SearchableSelect
                value={formData.brandId ? String(formData.brandId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, brandId: Number(value) }))}
                options={brands.map((brand) => ({ value: String(brand.id), label: brand.name }))}
                placeholder="Selecione uma marca"
                searchPlaceholder="Buscar marca"
              />
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
              <SearchableSelect
                value={formData.commercialResponsibleId ? String(formData.commercialResponsibleId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, commercialResponsibleId: value ? Number(value) : undefined }))}
                options={[
                  { value: '', label: 'Nenhum' },
                  ...responsibles.filter((r) => r.isActive).map((responsible) => ({ value: String(responsible.id), label: responsible.name })),
                ]}
                placeholder="Selecione um responsável"
                searchPlaceholder="Buscar responsável"
              />
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

            <div className="space-y-2">
              <label className="text-sm font-medium">Origem</label>
              <SearchableSelect
                value={formData.opportunitySourceId ? String(formData.opportunitySourceId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, opportunitySourceId: value ? Number(value) : undefined }))}
                options={sources.map((source) => ({ value: String(source.id), label: source.name }))}
                placeholder="Sem origem"
                searchPlaceholder="Buscar origem"
              />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">Tags</label>
              {tags.length === 0 ? (
                <p className="text-xs text-muted-foreground">Nenhuma tag cadastrada. Crie em Configuração → Tags de oportunidade.</p>
              ) : (
                <div className="flex flex-wrap gap-2">
                  {tags.map((tag) => {
                    const selected = formData.tagIds?.includes(tag.id) ?? false
                    return (
                      <button
                        key={tag.id}
                        type="button"
                        onClick={() => toggleTag(tag.id)}
                        className="rounded-full border px-2.5 py-1 text-xs font-medium transition"
                        style={{
                          backgroundColor: selected ? `${tag.color}25` : 'transparent',
                          borderColor: tag.color,
                          color: selected ? tag.color : 'var(--muted-foreground)',
                        }}
                      >
                        {tag.name}
                      </button>
                    )
                  })}
                </div>
              )}
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
