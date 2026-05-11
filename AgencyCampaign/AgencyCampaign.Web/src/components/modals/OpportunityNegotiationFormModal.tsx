import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, useApi, useI18n } from 'archon-ui'
import { opportunityService, type OpportunityNegotiation, type CreateOpportunityNegotiationRequest, type UpdateOpportunityNegotiationRequest } from '../../services/opportunityService'

interface OpportunityNegotiationFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  opportunityId: number
  negotiation: OpportunityNegotiation | null
  onSuccess: () => void
}

const initialFormData: CreateOpportunityNegotiationRequest = {
  opportunityId: 0,
  title: '',
  amount: 0,
  negotiatedAt: new Date().toISOString(),
  notes: '',
}

export default function OpportunityNegotiationFormModal({ open, onOpenChange, opportunityId, negotiation, onSuccess }: OpportunityNegotiationFormModalProps) {
  const { t } = useI18n()
  const isEditing = !!negotiation
  const [formData, setFormData] = useState<CreateOpportunityNegotiationRequest>(initialFormData)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (negotiation) {
      setFormData({
        opportunityId,
        title: negotiation.title,
        amount: negotiation.amount,
        negotiatedAt: negotiation.negotiatedAt,
        notes: negotiation.notes || '',
      })
      return
    }

    setFormData({ ...initialFormData, opportunityId })
  }, [negotiation, opportunityId, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() => (
      isEditing
        ? opportunityService.updateNegotiation(negotiation.id, {
            title: formData.title,
            amount: formData.amount,
            negotiatedAt: formData.negotiatedAt,
            notes: formData.notes,
          } satisfies UpdateOpportunityNegotiationRequest)
        : opportunityService.createNegotiation(opportunityId, formData)
    ))

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '720px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.opportunityNegotiation.title.edit') : t('modal.opportunityNegotiation.title.new')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">{t('common.field.title')}</label>
              <Input value={formData.title} onChange={(e) => setFormData((prev) => ({ ...prev, title: e.target.value }))} required />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.opportunityNegotiation.field.amount')}</label>
              <Input type="number" value={formData.amount === 0 ? '' : formData.amount} onChange={(e) => setFormData((prev) => ({ ...prev, amount: e.target.value === '' ? 0 : Number(e.target.value) }))} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.opportunityNegotiation.field.date')}</label>
              <Input type="date" value={formData.negotiatedAt.split('T')[0]} onChange={(e) => setFormData((prev) => ({ ...prev, negotiatedAt: new Date(e.target.value).toISOString() }))} />
            </div>
            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">{t('common.field.notes')}</label>
              <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading}>{loading ? t('common.action.saving') : t('common.action.save')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
