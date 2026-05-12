import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, SearchableSelect, useApi, useI18n } from 'archon-ui'
import { brandService } from '../../services/brandService'
import { commercialResponsibleService } from '../../services/commercialResponsibleService'
import { opportunitySourceService, opportunityTagService } from '../../services/opportunitySourceService'
import type { Brand } from '../../types/brand'
import type { CommercialResponsible } from '../../types/commercialResponsible'
import type { OpportunitySource, OpportunityTag } from '../../types/opportunitySource'
import { opportunityService, type Opportunity, type CreateOpportunityRequest, type UpdateOpportunityRequest } from '../../services/opportunityService'
import { cleanFormPayload } from '../../lib/cleanFormPayload'

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
  const { t } = useI18n()
  const isEditing = !!opportunity
  const [formData, setFormData] = useState<CreateOpportunityRequest>(initialFormData)
  const [brands, setBrands] = useState<Brand[]>([])
  const [responsibles, setResponsibles] = useState<CommercialResponsible[]>([])
  const [sources, setSources] = useState<OpportunitySource[]>([])
  const [tags, setTags] = useState<OpportunityTag[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    void brandService.getAll({ pageSize: 30 }).then((r) => setBrands(r.data ?? []))
    void commercialResponsibleService.getAll().then(setResponsibles)
    void opportunitySourceService.getAll(false).then(setSources)
    void opportunityTagService.getAll(false).then(setTags)
  }, [open])

  useEffect(() => {
    if (opportunity) {
      setFormData({
        brandId: opportunity.brandId,
        name: opportunity.name,
        description: opportunity.description || '',
        estimatedValue: opportunity.estimatedValue,
        expectedCloseAt: opportunity.expectedCloseAt,
        responsibleUserId: opportunity.responsibleUserId,
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

    const cleaned = cleanFormPayload(formData)

    const result = await execute(() => (
      isEditing
        ? opportunityService.update(opportunity.id, {
            id: opportunity.id,
            ...cleaned,
          } satisfies UpdateOpportunityRequest)
        : opportunityService.create(cleaned)
    ))

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '960px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.opportunity.title.edit') : t('modal.opportunity.title.new')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <label htmlFor="opportunity-name" className="text-sm font-medium">{t('modal.opportunity.field.name')}</label>
              <Input id="opportunity-name" value={formData.name} onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))} required />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.brand')}</label>
              <SearchableSelect
                value={formData.brandId ? String(formData.brandId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, brandId: Number(value) }))}
                options={brands.map((brand) => ({ value: String(brand.id), label: brand.name }))}
                placeholder={t('common.placeholder.select')}
                searchPlaceholder={t('common.placeholder.search')}
                onSearch={async (term) => {
                  const r = await brandService.getAll({ search: term, pageSize: 20 })
                  return (r.data ?? []).map((brand) => ({ value: String(brand.id), label: brand.name }))
                }}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="opportunity-estimated-value" className="text-sm font-medium">{t('modal.opportunity.field.estimatedValue')}</label>
              <Input id="opportunity-estimated-value" type="number" value={formData.estimatedValue === 0 ? '' : formData.estimatedValue} onChange={(e) => setFormData((prev) => ({ ...prev, estimatedValue: e.target.value === '' ? 0 : Number(e.target.value) }))} />
            </div>

            <div className="space-y-2">
              <label htmlFor="opportunity-expected-close-at" className="text-sm font-medium">{t('modal.opportunity.field.expectedClose')}</label>
              <Input id="opportunity-expected-close-at" type="date" value={formData.expectedCloseAt?.split('T')[0] || ''} onChange={(e) => setFormData((prev) => ({ ...prev, expectedCloseAt: e.target.value ? new Date(e.target.value).toISOString() : undefined }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.opportunity.field.responsible')}</label>
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
              <label htmlFor="opportunity-contact-name" className="text-sm font-medium">{t('common.field.contact')}</label>
              <Input id="opportunity-contact-name" value={formData.contactName || ''} onChange={(e) => setFormData((prev) => ({ ...prev, contactName: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label htmlFor="opportunity-contact-email" className="text-sm font-medium">{t('common.field.email')}</label>
              <Input id="opportunity-contact-email" type="email" value={formData.contactEmail || ''} onChange={(e) => setFormData((prev) => ({ ...prev, contactEmail: e.target.value }))} />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label htmlFor="opportunity-description" className="text-sm font-medium">{t('common.field.description')}</label>
              <Input id="opportunity-description" value={formData.description || ''} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label htmlFor="opportunity-notes" className="text-sm font-medium">{t('common.field.notes')}</label>
              <Input id="opportunity-notes" value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.origin')}</label>
              <SearchableSelect
                value={formData.opportunitySourceId ? String(formData.opportunitySourceId) : ''}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, opportunitySourceId: value ? Number(value) : undefined }))}
                options={sources.map((source) => ({ value: String(source.id), label: source.name }))}
                placeholder="Sem origem"
                searchPlaceholder={t('common.placeholder.search')}
              />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">{t('common.field.tags')}</label>
              {tags.length === 0 ? (
                <p className="text-xs text-muted-foreground">{t('modal.opportunity.placeholder.noTags')}</p>
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
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || !formData.brandId}>{loading ? t('common.action.saving') : t('common.action.save')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
