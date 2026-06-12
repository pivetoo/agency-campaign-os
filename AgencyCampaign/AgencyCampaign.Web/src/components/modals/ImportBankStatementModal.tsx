import { useState } from 'react'
import { Button, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, useApi, useI18n, useToast } from 'archon-ui'
import { bankTransactionService, type ImportBankTransactionItem, type ImportBankTransactionsResult } from '../../services/bankTransactionService'
import { BankTransactionDirection } from '../../types/bankTransaction'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  accountId: number
  onSuccess: () => void
}

export default function ImportBankStatementModal({ open, onOpenChange, accountId, onSuccess }: Props) {
  const { t } = useI18n()
  const { toast } = useToast()
  const [raw, setRaw] = useState('')
  const { execute, loading } = useApi<ImportBankTransactionsResult>({ showErrorMessage: true })

  const parse = (): ImportBankTransactionItem[] => {
    return raw
      .split('\n')
      .map((line) => line.trim())
      .filter(Boolean)
      .map((line) => {
        const [date, description, amount, dir, id] = line.split(';').map((part) => (part ?? '').trim())
        const direction = (dir || '').toUpperCase().startsWith('C') ? BankTransactionDirection.Credit : BankTransactionDirection.Debit
        const normalized = (amount || '').includes(',') ? (amount || '').replace(/\./g, '').replace(',', '.') : amount || ''
        const value = Number(normalized)
        return {
          // Usa o id real do banco (FITID/E2E) como ExternalId quando informado: dois lancamentos identicos no
          // mesmo dia deixam de ser deduplicados como um so. Sem id, cai no hash data|valor|descricao (legado).
          externalId: id ? id : `${date}|${value}|${description}`,
          occurredAt: new Date(date).toISOString(),
          amount: Math.abs(value),
          direction,
          description: description || '-',
        }
      })
      .filter((item) => !Number.isNaN(item.amount) && item.amount > 0)
  }

  const submit = async () => {
    const totalLines = raw.split('\n').map((line) => line.trim()).filter(Boolean).length
    const transactions = parse()
    if (transactions.length === 0) {
      toast({ title: t('financial.reconciliation.import.noneValid'), variant: 'warning' })
      return
    }
    const invalid = totalLines - transactions.length
    const result = await execute(() => bankTransactionService.import({ accountId, transactions }))
    if (result !== null) {
      let message = t('financial.reconciliation.import.result')
        .replace('{0}', String(result.imported))
        .replace('{1}', String(result.autoMatched))
        .replace('{2}', String(result.skipped))
      if (invalid > 0) {
        message += t('financial.reconciliation.import.resultInvalid').replace('{0}', String(invalid))
      }
      toast({ title: message, variant: 'success' })
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
            placeholder={'2026-05-31;Pagamento marca X;1500,00;C;E2E-12345\n2026-05-30;Pix creator Y;800,00;D'}
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
