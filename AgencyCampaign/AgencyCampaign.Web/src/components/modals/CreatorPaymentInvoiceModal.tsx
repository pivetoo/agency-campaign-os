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

export default function CreatorPaymentInvoiceModal({ open, onOpenChange, payment, onSuccess }: Props) {
  const { t } = useI18n()
  const [invoiceNumber, setInvoiceNumber] = useState('')
  const [invoiceUrl, setInvoiceUrl] = useState('')
  const [issuedAt, setIssuedAt] = useState('')
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open || !payment) return
    setInvoiceNumber(payment.invoiceNumber ?? '')
    setInvoiceUrl(payment.invoiceUrl ?? '')
    setIssuedAt(payment.invoiceIssuedAt ? payment.invoiceIssuedAt.slice(0, 10) : '')
  }, [open, payment?.id])

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!payment) return
    const result = await execute(() =>
      creatorPaymentService.attachInvoice(payment.id, {
        invoiceNumber: invoiceNumber.trim() || undefined,
        invoiceUrl: invoiceUrl.trim() || undefined,
        issuedAt: issuedAt ? new Date(issuedAt).toISOString() : undefined,
      }),
    )
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="lg">
        <ModalHeader>
          <ModalTitle>{t('modal.creatorPayment.title.attachInvoice')}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.creatorPayment.field.invoiceNumber')}</label>
              <Input value={invoiceNumber} onChange={(e) => setInvoiceNumber(e.target.value)} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.creatorPayment.field.issuedAt')}</label>
              <Input type="date" value={issuedAt} onChange={(e) => setIssuedAt(e.target.value)} />
            </div>
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('modal.creatorPayment.field.invoiceUrl')}</label>
              <Input
                value={invoiceUrl}
                onChange={(e) => setInvoiceUrl(e.target.value)}
                placeholder="https://..."
              />
            </div>
          </div>
          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading}>{loading ? t('common.action.saving') : t('modal.creatorPayment.action.attachInvoice')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
