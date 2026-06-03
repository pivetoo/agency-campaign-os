import { useEffect, useMemo, useState } from 'react'
import { Button, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, SearchableSelect, useApi, useI18n } from 'archon-ui'
import { financialEntryService } from '../../services/financialEntryService'
import { bankTransactionService } from '../../services/bankTransactionService'
import { FinancialEntryStatus, type FinancialEntry } from '../../types/financialEntry'
import { BankTransactionDirection, type BankTransaction } from '../../types/bankTransaction'
import { formatCurrency } from '../../lib/format'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  transaction: BankTransaction | null
  onSuccess: () => void
}

export default function MatchTransactionModal({ open, onOpenChange, transaction, onSuccess }: Props) {
  const { t } = useI18n()
  const [entries, setEntries] = useState<FinancialEntry[]>([])
  const [entryId, setEntryId] = useState<number>(0)
  const { execute: loadEntries } = useApi<FinancialEntry[]>({ showErrorMessage: true })
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open || !transaction) return
    setEntryId(0)
    const type = transaction.direction === BankTransactionDirection.Credit ? 1 : 2
    void loadEntries(() => financialEntryService.getAll({ accountId: transaction.accountId, type, pageSize: 200 }).then((r) => r.data ?? [])).then((result) => {
      const candidates = (result ?? []).filter((entry) => (entry.status === FinancialEntryStatus.Pending || entry.status === FinancialEntryStatus.Overdue) && entry.amount === transaction.amount)
      setEntries(candidates)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, transaction])

  const options = useMemo(() => entries.map((entry) => ({ value: String(entry.id), label: `${entry.description} · ${formatCurrency(entry.amount)} · ${new Date(entry.dueAt).toLocaleDateString('pt-BR')}` })), [entries])

  const submit = async () => {
    if (!transaction || !entryId) return
    const result = await execute(() => bankTransactionService.match(transaction.id, entryId))
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent>
        <ModalHeader><ModalTitle>{t('financial.reconciliation.match.title')}</ModalTitle></ModalHeader>
        <div className="space-y-3 py-2">
          {transaction && (
            <div className="rounded-lg border bg-muted/30 p-3 text-sm">
              <p className="font-medium">{transaction.description}</p>
              <p className="text-muted-foreground">{new Date(transaction.occurredAt).toLocaleDateString('pt-BR')} · {formatCurrency(transaction.amount)}</p>
            </div>
          )}
          <div className="space-y-1">
            <label className="text-sm font-medium">{t('financial.reconciliation.match.entry')}</label>
            <SearchableSelect value={entryId ? String(entryId) : ''} onValueChange={(value) => setEntryId(Number(value))} options={options} placeholder={t('financial.reconciliation.match.entryPlaceholder')} searchPlaceholder={t('common.placeholder.search')} />
            {entries.length === 0 && <p className="text-xs text-muted-foreground">{t('financial.reconciliation.match.noEntry')}</p>}
          </div>
        </div>
        <ModalFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
          <Button type="button" disabled={loading || !entryId} onClick={() => void submit()}>{loading ? t('common.action.saving') : t('financial.reconciliation.match.confirm')}</Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}
