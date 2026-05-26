import { useEffect, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, useApi, useI18n } from 'archon-ui'
import { opportunityService, type OpportunityNegotiation, type CreateOpportunityNegotiationRequest, type UpdateOpportunityNegotiationRequest } from '../../services/opportunityService'
import { commercialPolicyService } from '../../services/commercialPolicyService'
import CurrencyInput from '../inputs/CurrencyInput'
import { dateInputToIso, isoToDateInput, todayDateInput } from '../../lib/format'

interface OpportunityNegotiationFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  opportunityId: number
  estimatedValue?: number
  negotiation: OpportunityNegotiation | null
  onSuccess: () => void
}

const initialFormData: CreateOpportunityNegotiationRequest = {
  opportunityId: 0,
  title: '',
  amount: 0,
  negotiatedAt: dateInputToIso(todayDateInput()),
  notes: '',
  discountPercent: null,
  marginPercent: null,
  paymentTermDays: null,
}

export default function OpportunityNegotiationFormModal({ open, onOpenChange, opportunityId, estimatedValue = 0, negotiation, onSuccess }: OpportunityNegotiationFormModalProps) {
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
        discountPercent: negotiation.discountPercent ?? null,
        marginPercent: negotiation.marginPercent ?? null,
        paymentTermDays: negotiation.paymentTermDays ?? null,
      })
      return
    }

    setFormData({ ...initialFormData, opportunityId, negotiatedAt: dateInputToIso(todayDateInput()) })
  }, [negotiation, opportunityId, open])

  // Desconto calculado automaticamente: (valor estimado - valor negociado) / valor estimado
  useEffect(() => {
    const computed = estimatedValue > 0 && formData.amount > 0
      ? Math.max(0, Math.round(((estimatedValue - formData.amount) / estimatedValue) * 1000) / 10)
      : null
    setFormData((prev) => (prev.discountPercent === computed ? prev : { ...prev, discountPercent: computed }))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [formData.amount, estimatedValue])

  // Prazo de pagamento ja vem do padrao da politica em novas negociacoes
  useEffect(() => {
    if (!open || isEditing) {
      return
    }
    let cancelled = false
    void commercialPolicyService.get().then((policy) => {
      if (!cancelled && policy?.defaultPaymentTermDays != null) {
        setFormData((prev) => (prev.paymentTermDays == null ? { ...prev, paymentTermDays: policy.defaultPaymentTermDays! } : prev))
      }
    }).catch(() => {})
    return () => { cancelled = true }
  }, [open, isEditing])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() => (
      isEditing
        ? opportunityService.updateNegotiation(negotiation.id, {
            title: formData.title,
            amount: formData.amount,
            negotiatedAt: formData.negotiatedAt,
            notes: formData.notes,
            discountPercent: formData.discountPercent,
            marginPercent: formData.marginPercent,
            paymentTermDays: formData.paymentTermDays,
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
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">{t('common.field.title')}</label>
              <Input value={formData.title} onChange={(e) => setFormData((prev) => ({ ...prev, title: e.target.value }))} required />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.opportunityNegotiation.field.amount')}</label>
              <CurrencyInput value={formData.amount} onChange={(amount) => setFormData((prev) => ({ ...prev, amount }))} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.opportunityNegotiation.field.date')}</label>
              <Input type="date" value={isoToDateInput(formData.negotiatedAt)} onChange={(e) => setFormData((prev) => ({ ...prev, negotiatedAt: dateInputToIso(e.target.value) }))} />
            </div>
            <div style={{ gridColumn: '1 / -1' }} className="mt-1 border-t border-border pt-3">
              <div className="text-sm font-semibold text-foreground">{t('modal.opportunityNegotiation.section.policyCheck')}</div>
              <p className="mt-0.5 text-xs text-muted-foreground">{t('modal.opportunityNegotiation.section.policyCheckHelp')}</p>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.opportunityNegotiation.field.discount')}</label>
              <div className="flex h-9 items-center rounded-md border border-input bg-muted/40 px-3 text-sm text-foreground">
                {formData.discountPercent != null ? `${formData.discountPercent}%` : '—'}
              </div>
              <p className="text-[11px] text-muted-foreground">{estimatedValue > 0 ? t('modal.opportunityNegotiation.help.discountCalc') : t('modal.opportunityNegotiation.help.discountSetValue')}</p>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.opportunityNegotiation.field.paymentTerm')}</label>
              <Input type="number" min={0} max={3650} step="1" placeholder={t('modal.opportunityNegotiation.placeholder.paymentTerm')} value={formData.paymentTermDays ?? ''} onChange={(e) => setFormData((prev) => ({ ...prev, paymentTermDays: e.target.value === '' ? null : Number(e.target.value) }))} />
            </div>
            <div className="space-y-2" style={{ gridColumn: '1 / -1' }}>
              <label className="text-sm font-medium">{t('modal.opportunityNegotiation.field.margin')} <span className="font-normal text-muted-foreground">{t('modal.opportunityNegotiation.field.marginOptionalSuffix')}</span></label>
              <Input type="number" min={0} max={100} step="0.1" placeholder={t('modal.opportunityNegotiation.placeholder.margin')} value={formData.marginPercent ?? ''} onChange={(e) => setFormData((prev) => ({ ...prev, marginPercent: e.target.value === '' ? null : Number(e.target.value) }))} />
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
