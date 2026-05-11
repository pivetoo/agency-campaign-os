import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui'
import { opportunityService, type CreateOpportunityApprovalRequest, type OpportunityNegotiation } from '../../services/opportunityService'

interface OpportunityApprovalRequestFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  negotiation: OpportunityNegotiation | null
  onSuccess: () => void
}

const initialFormData: CreateOpportunityApprovalRequest = {
  opportunityNegotiationId: 0,
  approvalType: 4,
  reason: '',
  requestedByUserName: '',
}

export default function OpportunityApprovalRequestFormModal({ open, onOpenChange, negotiation, onSuccess }: OpportunityApprovalRequestFormModalProps) {
  const { t } = useI18n()
  const [formData, setFormData] = useState<CreateOpportunityApprovalRequest>(initialFormData)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (negotiation) {
      setFormData({
        opportunityNegotiationId: negotiation.id,
        approvalType: 4,
        reason: '',
        requestedByUserName: '',
      })
      return
    }

    setFormData(initialFormData)
  }, [negotiation, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() => opportunityService.createApprovalRequest(formData))
    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '720px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{t('modal.opportunityApproval.title')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.opportunityApproval.field.type')}</label>
              <Select value={String(formData.approvalType)} onValueChange={(value) => setFormData((prev) => ({ ...prev, approvalType: Number(value) }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">{t('modal.opportunityApproval.type.discount')}</SelectItem>
                  <SelectItem value="2">{t('modal.opportunityApproval.type.margin')}</SelectItem>
                  <SelectItem value="3">{t('modal.opportunityApproval.type.term')}</SelectItem>
                  <SelectItem value="4">{t('modal.opportunityApproval.type.exception')}</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.opportunityApproval.field.requestedBy')}</label>
              <Input value={formData.requestedByUserName} onChange={(e) => setFormData((prev) => ({ ...prev, requestedByUserName: e.target.value }))} required />
            </div>

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">{t('modal.opportunityApproval.field.reason')}</label>
              <Input value={formData.reason} onChange={(e) => setFormData((prev) => ({ ...prev, reason: e.target.value }))} required />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || !formData.opportunityNegotiationId}>{loading ? t('common.action.saving') : t('common.action.request')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
