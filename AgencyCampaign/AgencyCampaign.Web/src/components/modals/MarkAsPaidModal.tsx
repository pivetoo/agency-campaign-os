import { useEffect, useState } from 'react'
import {
  Button,
  Input,
  Modal,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  SearchableSelect,
  useApi,
  useI18n,
} from 'archon-ui'
import { financialEntryService } from '../../services/financialEntryService'
import { financialAccountService } from '../../services/financialAccountService'
import type { FinancialEntry } from '../../types/financialEntry'
import type { FinancialAccount } from '../../types/financialAccount'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  entry: FinancialEntry | null
  onSuccess: () => void
}

export default function MarkAsPaidModal({ open, onOpenChange, entry, onSuccess }: Props) {
  const { t } = useI18n()
  const [accounts, setAccounts] = useState<FinancialAccount[]>([])
  const [accountId, setAccountId] = useState<number>(0)
  const [paymentMethod, setPaymentMethod] = useState('')
  const [paidAt, setPaidAt] = useState('')
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open || !entry) return
    void financialAccountService.getAll({ pageSize: 200 }).then((r) => setAccounts(r.data ?? []))
    setAccountId(entry.accountId)
    setPaymentMethod(entry.paymentMethod || '')
    setPaidAt(new Date().toISOString().slice(0, 10))
  }, [open, entry])

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!entry) return
    const result = await execute(() =>
      financialEntryService.markAsPaid(entry.id, {
        accountId,
        paymentMethod: paymentMethod.trim() || undefined,
        paidAt: paidAt ? new Date(paidAt).toISOString() : undefined,
      }),
    )
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="form">
        <ModalHeader>
          <ModalTitle>{entry?.type === 1 ? t('modal.markAsPaid.title.receive') : t('modal.markAsPaid.title.pay')}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="rounded-md border bg-muted/40 p-3 text-sm">
            <p className="font-medium">{entry?.description}</p>
            <p className="text-xs text-muted-foreground">
              {entry?.counterpartyName ? `${entry.counterpartyName} · ` : ''}
              R$ {entry?.amount.toFixed(2) ?? '0,00'} · venc. {entry ? new Date(entry.dueAt).toLocaleDateString('pt-BR') : ''}
            </p>
          </div>
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('modal.markAsPaid.field.destinationAccount')}</label>
              <SearchableSelect
                value={accountId ? String(accountId) : ''}
                onValueChange={(value) => setAccountId(Number(value))}
                options={accounts.map((account) => ({ value: String(account.id), label: account.name }))}
                placeholder="Selecione a conta"
                searchPlaceholder="Buscar conta"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.date')}</label>
              <Input type="date" value={paidAt} onChange={(e) => setPaidAt(e.target.value)} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.markAsPaid.field.method')}</label>
              <Input value={paymentMethod} onChange={(e) => setPaymentMethod(e.target.value)} placeholder="PIX, boleto, transferência..." />
            </div>
          </div>
          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || accountId === 0}>{loading ? t('common.action.saving') : t('common.action.confirm')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
