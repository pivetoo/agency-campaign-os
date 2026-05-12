import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, SearchableSelect, useApi, useI18n } from 'archon-ui'
import { proposalService, type Proposal, type CreateProposalRequest, type UpdateProposalRequest } from '../../services/proposalService'
import { opportunityService, type Opportunity } from '../../services/opportunityService'

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
}

export default function ProposalFormModal({ open, onOpenChange, proposal, presetOpportunityId, onSuccess }: ProposalFormModalProps) {
  const { t } = useI18n()
  const isEditing = !!proposal
  const [formData, setFormData] = useState<CreateProposalRequest>(initialFormData)
  const [opportunities, setOpportunities] = useState<Opportunity[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    void opportunityService.getAll({ pageSize: 30 }).then((r) => setOpportunities(r.data ?? []))
  }, [open])

  useEffect(() => {
    if (proposal) {
      setFormData({
        opportunityId: proposal.opportunityId,
        description: proposal.description || '',
        validityUntil: proposal.validityUntil,
        notes: proposal.notes || '',
      })
      return
    }

    setFormData({ ...initialFormData, opportunityId: presetOpportunityId ?? 0 })
  }, [proposal, open, presetOpportunityId])

  const opportunityOptions = opportunities.map((opportunity) => ({
    value: String(opportunity.id),
    label: `${opportunity.name} · ${opportunity.brand?.name || 'Marca'}`,
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
                    const r = await opportunityService.getAll({ search: term, pageSize: 20 })
                    return (r.data ?? []).map((opportunity) => ({
                      value: String(opportunity.id),
                      label: `${opportunity.name} · ${opportunity.brand?.name || 'Marca'}`,
                    }))
                  }}
                />
              </div>
            )}

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.validity')}</label>
              <Input
                type="date"
                value={formData.validityUntil?.split('T')[0] || ''}
                onChange={(e) => setFormData((prev) => ({ ...prev, validityUntil: e.target.value ? new Date(e.target.value).toISOString() : undefined }))}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.description')}</label>
              <Input value={formData.description || ''} onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))} />
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
