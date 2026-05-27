import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, SearchableSelect, useApi, useI18n } from 'archon-ui'
import { proposalService, type Proposal, type CreateProposalRequest, type UpdateProposalRequest } from '../../services/proposalService'
import { opportunityService, type Opportunity } from '../../services/opportunityService'
import { agencySettingsService, type ProposalTemplateVersion } from '../../services/agencySettingsService'
import { dateInputToIso, isoToDateInput } from '../../lib/format'

interface ProposalFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  proposal: Proposal | null
  presetOpportunityId?: number | null
  onSuccess: (proposal?: Proposal) => void
}

const initialFormData: CreateProposalRequest = {
  opportunityId: 0,
  description: '',
  validityUntil: undefined,
  notes: '',
  proposalLayoutId: null,
  discountPercent: null,
  paymentTermDays: null,
}

const DEFAULT_LAYOUT_VALUE = '__default__'

export default function ProposalFormModal({ open, onOpenChange, proposal, presetOpportunityId, onSuccess }: ProposalFormModalProps) {
  const { t } = useI18n()
  const isEditing = !!proposal
  const [formData, setFormData] = useState<CreateProposalRequest>(initialFormData)
  const [opportunities, setOpportunities] = useState<Opportunity[]>([])
  const [layouts, setLayouts] = useState<ProposalTemplateVersion[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    void opportunityService.getAll({ pageSize: 10 }).then((r) => setOpportunities(r.data ?? []))
    void agencySettingsService.getProposalTemplateVersions().then((r) => setLayouts(r.data ?? []))
  }, [open])

  useEffect(() => {
    if (proposal) {
      setFormData({
        opportunityId: proposal.opportunityId,
        description: proposal.description || '',
        validityUntil: proposal.validityUntil,
        notes: proposal.notes || '',
        proposalLayoutId: proposal.proposalLayoutId ?? null,
        discountPercent: proposal.discountPercent ?? null,
        paymentTermDays: proposal.paymentTermDays ?? null,
      })
      return
    }

    setFormData({ ...initialFormData, opportunityId: presetOpportunityId ?? 0 })
  }, [proposal, open, presetOpportunityId])

  const layoutOptions = [
    { value: DEFAULT_LAYOUT_VALUE, label: t('modal.proposal.option.defaultLayout') },
    ...layouts.map((layout) => ({
      value: String(layout.id),
      label: layout.isActive ? t('modal.proposal.option.layoutDefaultSuffix').replace('{0}', layout.name) : layout.name,
    })),
  ]

  const opportunityOptions = opportunities.map((opportunity) => ({
    value: String(opportunity.id),
    label: `${opportunity.name} · ${opportunity.brand?.name || t('modal.proposal.fallback.brand')}`,
  }))

  const handleOpportunityChange = (value: string) => {
    setFormData((prev) => ({
      ...prev,
      opportunityId: Number(value),
    }))
  }

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() =>
      isEditing
        ? proposalService.update(proposal.id, {
            id: proposal.id,
            ...formData,
          } satisfies UpdateProposalRequest)
        : proposalService.create(formData),
    )

    if (result !== null) {
      onSuccess(result as Proposal)
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '960px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.proposal.title.edit') : t('modal.proposal.title.new')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            {!presetOpportunityId && (
              <div className="space-y-2">
                <label className="text-sm font-medium">{t('modal.proposal.field.opportunity')}</label>
                <SearchableSelect
                  value={formData.opportunityId ? String(formData.opportunityId) : ''}
                  onValueChange={handleOpportunityChange}
                  options={opportunityOptions}
                  placeholder={t('common.placeholder.select')}
                  searchPlaceholder={t('common.placeholder.search')}
                  onSearch={async (term) => {
                    const r = await opportunityService.getAll({ search: term, pageSize: 10 })
                    return (r.data ?? []).map((opportunity) => ({
                      value: String(opportunity.id),
                      label: `${opportunity.name} · ${opportunity.brand?.name || t('modal.proposal.fallback.brand')}`,
                    }))
                  }}
                />
              </div>
            )}

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.validity')}</label>
              <Input
                type="date"
                value={isoToDateInput(formData.validityUntil)}
                onChange={(e) => setFormData((prev) => ({ ...prev, validityUntil: e.target.value ? dateInputToIso(e.target.value) : undefined }))}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.description')}</label>
              <Input value={formData.description || ''} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.proposal.field.layout')}</label>
              <SearchableSelect
                value={formData.proposalLayoutId ? String(formData.proposalLayoutId) : DEFAULT_LAYOUT_VALUE}
                onValueChange={(value) => setFormData((prev) => ({ ...prev, proposalLayoutId: value === DEFAULT_LAYOUT_VALUE ? null : Number(value) }))}
                options={layoutOptions}
                placeholder={t('modal.proposal.option.defaultLayout')}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.proposal.field.discount')}</label>
              <Input
                type="number"
                min={0}
                max={100}
                step="0.01"
                value={formData.discountPercent ?? ''}
                onChange={(e) => setFormData((prev) => ({ ...prev, discountPercent: e.target.value === '' ? null : Number(e.target.value) }))}
                placeholder={t('modal.proposal.placeholder.discount')}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.proposal.field.paymentTerm')}</label>
              <Input
                type="number"
                min={0}
                max={3650}
                step="1"
                value={formData.paymentTermDays ?? ''}
                onChange={(e) => setFormData((prev) => ({ ...prev, paymentTermDays: e.target.value === '' ? null : Number(e.target.value) }))}
                placeholder={t('modal.proposal.placeholder.paymentTerm')}
              />
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr', gap: '1rem' }}>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.notes')}</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || !formData.opportunityId}>{loading ? t('common.action.saving') : isEditing ? t('common.action.save') : t('modal.proposal.action.createContinue')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
