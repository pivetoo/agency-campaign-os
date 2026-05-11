import { useEffect, useState } from 'react'
import {
  Button,
  Input,
  Modal,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  useApi,
  useI18n,
} from 'archon-ui'
import { creatorPaymentService } from '../../services/creatorPaymentService'
import type { CreatorPayment } from '../../types/creatorPayment'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  payment: CreatorPayment | null
  onSuccess: () => void
}

export default function CreatorPaymentMarkPaidModal({ open, onOpenChange, payment, onSuccess }: Props) {
  const { t } = useI18n()
  const [paidAt, setPaidAt] = useState('')
  const [provider, setProvider] = useState('')
  const [providerTransactionId, setProviderTransactionId] = useState('')
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    const now = new Date()
    const local = new Date(now.getTime() - now.getTimezoneOffset() * 60000)
    setPaidAt(local.toISOString().slice(0, 16))
    setProvider(payment?.provider ?? '')
    setProviderTransactionId(payment?.providerTransactionId ?? '')
  }, [open, payment?.id])

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!payment) return
    const result = await execute(() =>
      creatorPaymentService.markPaid(payment.id, {
        paidAt: new Date(paidAt).toISOString(),
        provider: provider.trim() || undefined,
        providerTransactionId: providerTransactionId.trim() || undefined,
      }),
    )
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="lg">
        <ModalHeader>
          <ModalTitle>{t('modal.creatorPayment.title.markPaid')}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('modal.creatorPayment.field.paidAt')}</label>
              <Input type="datetime-local" value={paidAt} onChange={(e) => setPaidAt(e.target.value)} required />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.creatorPayment.field.provider')}</label>
              <Input value={provider} onChange={(e) => setProvider(e.target.value)} placeholder="Asaas, manual, etc." />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.creatorPayment.field.transactionId')}</label>
              <Input value={providerTransactionId} onChange={(e) => setProviderTransactionId(e.target.value)} placeholder="Ex: pay_..." />
            </div>
          </div>
          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || !paidAt}>{loading ? t('common.action.saving') : t('modal.creatorPayment.action.confirmPayment')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
