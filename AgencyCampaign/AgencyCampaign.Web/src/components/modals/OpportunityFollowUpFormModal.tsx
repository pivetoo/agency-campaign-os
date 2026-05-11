import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, useApi, useI18n } from 'archon-ui'
import { opportunityService, type OpportunityFollowUp, type CreateOpportunityFollowUpRequest, type UpdateOpportunityFollowUpRequest } from '../../services/opportunityService'

interface OpportunityFollowUpFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  opportunityId: number
  followUp: OpportunityFollowUp | null
  onSuccess: () => void
}

const initialFormData: CreateOpportunityFollowUpRequest = {
  opportunityId: 0,
  subject: '',
  dueAt: new Date().toISOString(),
  notes: '',
}

export default function OpportunityFollowUpFormModal({ open, onOpenChange, opportunityId, followUp, onSuccess }: OpportunityFollowUpFormModalProps) {
  const { t } = useI18n()
  const isEditing = !!followUp
  const [formData, setFormData] = useState<CreateOpportunityFollowUpRequest>(initialFormData)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (followUp) {
      setFormData({
        opportunityId,
        subject: followUp.subject,
        dueAt: followUp.dueAt,
        notes: followUp.notes || '',
      })
      return
    }

    setFormData({ ...initialFormData, opportunityId })
  }, [followUp, opportunityId, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() => (
      isEditing
        ? opportunityService.updateFollowUp(followUp.id, {
            subject: formData.subject,
            dueAt: formData.dueAt,
            notes: formData.notes,
          } satisfies UpdateOpportunityFollowUpRequest)
        : opportunityService.createFollowUp(opportunityId, formData)
    ))

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '720px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.opportunityFollowUp.title.edit') : t('modal.opportunityFollowUp.title.new')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">{t('common.field.subject')}</label>
              <Input value={formData.subject} onChange={(e) => setFormData((prev) => ({ ...prev, subject: e.target.value }))} required />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.dueDate')}</label>
              <Input type="date" value={formData.dueAt.split('T')[0]} onChange={(e) => setFormData((prev) => ({ ...prev, dueAt: new Date(e.target.value).toISOString() }))} />
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
