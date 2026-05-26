import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, SearchableSelect, useApi, useAuth, useI18n, UsersManagementService } from 'archon-ui'
import { X } from 'lucide-react'
import { opportunityService, type CreateOpportunityApprovalRequest, type OpportunityNegotiation } from '../../services/opportunityService'

interface OpportunityApprovalRequestFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  negotiation: OpportunityNegotiation | null
  onSuccess: () => void
}

interface ApproverOption {
  userId: number
  name: string
  email: string
}

const initialFormData: CreateOpportunityApprovalRequest = {
  opportunityNegotiationId: 0,
  approvalType: 4,
  reason: '',
  requestedByUserName: '',
}

const APPROVER_SENTINEL = '__none__'

export default function OpportunityApprovalRequestFormModal({ open, onOpenChange, negotiation, onSuccess }: OpportunityApprovalRequestFormModalProps) {
  const { t } = useI18n()
  const { user: authUser } = useAuth()
  const [formData, setFormData] = useState<CreateOpportunityApprovalRequest>(initialFormData)
  const [users, setUsers] = useState<ApproverOption[]>([])
  const [selectedApprovers, setSelectedApprovers] = useState<ApproverOption[]>([])
  const [pendingApprover, setPendingApprover] = useState<string>(APPROVER_SENTINEL)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    void UsersManagementService.listInCurrentContract().then((list) => {
      setUsers(list.filter((u) => u.isActive).map((u) => ({ userId: u.userId, name: u.name, email: u.email })))
    })
  }, [open])

  useEffect(() => {
    if (negotiation) {
      setFormData({
        opportunityNegotiationId: negotiation.id,
        approvalType: 4,
        reason: '',
        requestedByUserName: authUser?.name ?? '',
        requestedByUserId: authUser?.id,
      })
      setSelectedApprovers([])
      setPendingApprover(APPROVER_SENTINEL)
      return
    }

    setFormData(initialFormData)
    setSelectedApprovers([])
  }, [negotiation, open, authUser])

  const handleApproverChange = (value: string) => {
    if (value === APPROVER_SENTINEL) {
      setPendingApprover(APPROVER_SENTINEL)
      return
    }

    const userId = Number(value)
    const option = users.find((u) => u.userId === userId)
    if (option && !selectedApprovers.some((item) => item.userId === userId)) {
      setSelectedApprovers((prev) => [...prev, option])
    }
    setPendingApprover(APPROVER_SENTINEL)
  }

  const removeApprover = (userId: number) => {
    setSelectedApprovers((prev) => prev.filter((item) => item.userId !== userId))
  }

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const payload: CreateOpportunityApprovalRequest = {
      ...formData,
      approvers: selectedApprovers.map((item) => ({ userId: item.userId, userName: item.name })),
    }

    const result = await execute(() => opportunityService.createApprovalRequest(payload))
    if (result !== null) {
      onSuccess()
    }
  }

  const approverOptions = users
    .filter((u) => !selectedApprovers.some((item) => item.userId === u.userId) && u.userId !== authUser?.id)
    .map((u) => ({ value: String(u.userId), label: u.name, description: u.email }))

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '720px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{t('modal.opportunityApproval.title')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
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

            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">{t('modal.opportunityApproval.field.approvers')}</label>
              <SearchableSelect
                value={pendingApprover}
                onValueChange={handleApproverChange}
                placeholder={t('modal.opportunityApproval.placeholder.approvers')}
                options={[{ value: APPROVER_SENTINEL, label: t('modal.opportunityApproval.placeholder.approvers') }, ...approverOptions]}
              />
              {selectedApprovers.length > 0 && (
                <div className="flex flex-wrap gap-1.5 pt-1">
                  {selectedApprovers.map((approver) => (
                    <span key={approver.userId} className="inline-flex items-center gap-1.5 rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
                      {approver.name}
                      <button type="button" onClick={() => removeApprover(approver.userId)} className="rounded-full hover:bg-primary/20" aria-label={t('common.action.remove')}>
                        <X className="h-3 w-3" />
                      </button>
                    </span>
                  ))}
                </div>
              )}
              <p className="text-xs text-muted-foreground">{t('modal.opportunityApproval.help.approvers')}</p>
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || !formData.opportunityNegotiationId || selectedApprovers.length === 0}>{loading ? t('common.action.saving') : t('common.action.request')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
