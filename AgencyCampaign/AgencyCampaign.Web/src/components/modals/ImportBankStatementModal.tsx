import { useState } from 'react'
import { Button, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, useApi, useI18n } from 'archon-ui'
import { bankTransactionService, type ImportBankTransactionItem } from '../../services/bankTransactionService'
import { BankTransactionDirection } from '../../types/bankTransaction'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  accountId: number
  onSuccess: () => void
}

export default function ImportBankStatementModal({ open, onOpenChange, accountId, onSuccess }: Props) {
  const { t } = useI18n()
  const [raw, setRaw] = useState('')
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const parse = (): ImportBankTransactionItem[] => {
    return raw
      .split('\n')
      .map((line) => line.trim())
      .filter(Boolean)
      .map((line) => {
        const [date, description, amount, dir] = line.split(';').map((part) => (part ?? '').trim())
        const direction = (dir || '').toUpperCase().startsWith('C') ? BankTransactionDirection.Credit : BankTransactionDirection.Debit
        const normalized = (amount || '').includes(',') ? (amount || '').replace(/\./g, '').replace(',', '.') : amount || ''
        const value = Number(normalized)
        return {
          externalId: `${date}|${value}|${description}`,
          occurredAt: new Date(date).toISOString(),
          amount: Math.abs(value),
          direction,
          description: description || '-',
        }
      })
      .filter((item) => !Number.isNaN(item.amount) && item.amount > 0)
  }

  const submit = async () => {
    const transactions = parse()
    if (transactions.length === 0) return
    const result = await execute(() => bankTransactionService.import({ accountId, transactions }))
    if (result !== null) {
      setRaw('')
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent>
        <ModalHeader><ModalTitle>{t('financial.reconciliation.import.title')}</ModalTitle></ModalHeader>
        <div className="space-y-2 py-2">
          <p className="text-xs text-muted-foreground">{t('financial.reconciliation.import.hint')}</p>
          <textarea
            className="w-full h-40 rounded-md border p-2 text-sm font-mono"
            value={raw}
            onChange={(event) => setRaw(event.target.value)}
            placeholder={'2026-05-31;Pagamento marca X;1500,00;C\n2026-05-30;Pix creator Y;800,00;D'}
          />
        </div>
        <ModalFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
          <Button type="button" disabled={loading || !raw.trim()} onClick={() => void submit()}>{loading ? t('common.action.saving') : t('financial.reconciliation.import.confirm')}</Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}
