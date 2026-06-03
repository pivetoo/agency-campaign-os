import { useEffect, useMemo, useState } from 'react'
import { Badge, Button, ConfirmModal, DataTable, PageLayout, SearchableSelect, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Link2, Unlink, Upload } from 'lucide-react'
import { financialAccountService } from '../../../services/financialAccountService'
import { bankTransactionService } from '../../../services/bankTransactionService'
import type { FinancialAccount } from '../../../types/financialAccount'
import { BankTransactionDirection, bankTransactionDirectionLabels, bankTransactionMatchKindLabels, type BankTransaction } from '../../../types/bankTransaction'
import { formatCurrency } from '../../../lib/format'
import MatchTransactionModal from '../../../components/modals/MatchTransactionModal'
import ImportBankStatementModal from '../../../components/modals/ImportBankStatementModal'

export default function Reconciliation() {
  const { t } = useI18n()
  const [accounts, setAccounts] = useState<FinancialAccount[]>([])
  const [accountId, setAccountId] = useState<number | undefined>()
  const [transactions, setTransactions] = useState<BankTransaction[]>([])
  const [selected, setSelected] = useState<BankTransaction | null>(null)
  const [isMatchOpen, setIsMatchOpen] = useState(false)
  const [isImportOpen, setIsImportOpen] = useState(false)
  const [isConfirmUnmatchOpen, setIsConfirmUnmatchOpen] = useState(false)

  const { execute: fetchAccounts } = useApi<FinancialAccount[]>({ showErrorMessage: true })
  const { execute: fetchTransactions, loading } = useApi<BankTransaction[]>({ showErrorMessage: true })
  const { execute: runUnmatch, loading: unmatching } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    void fetchAccounts(() => financialAccountService.getAll({ pageSize: 200 }).then((r) => r.data ?? [])).then((result) => {
      const active = (result ?? []).filter((account) => account.isActive)
      setAccounts(active)
      if (active.length > 0 && accountId === undefined) {
        setAccountId(active[0].id)
      }
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const loadTransactions = async () => {
    if (!accountId) return
    const result = await fetchTransactions(() => bankTransactionService.getByAccount(accountId, 1, 100).then((r) => r.data ?? []))
    if (result) {
      setTransactions(result)
      setSelected(null)
    }
  }

  useEffect(() => {
    void loadTransactions()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accountId])

  const handleUnmatch = async () => {
    if (!selected) return
    const result = await runUnmatch(() => bankTransactionService.unmatch(selected.id))
    if (result !== null) {
      setIsConfirmUnmatchOpen(false)
      setSelected(null)
      void loadTransactions()
    }
  }

  const totals = useMemo(() => {
    const matched = transactions.filter((item) => item.financialEntryId).length
    return { total: transactions.length, matched, pending: transactions.length - matched }
  }, [transactions])

  const columns: DataTableColumn<BankTransaction>[] = [
    {
      key: 'occurredAt',
      title: t('financial.reconciliation.field.date'),
      dataIndex: 'occurredAt',
      width: 110,
      render: (value: string) => new Date(value).toLocaleDateString('pt-BR'),
    },
    {
      key: 'description',
      title: t('financial.reconciliation.field.description'),
      dataIndex: 'description',
      primary: true,
      render: (value: string) => value || '—',
    },
    {
      key: 'direction',
      title: t('financial.reconciliation.field.direction'),
      dataIndex: 'direction',
      width: 100,
      hiddenBelow: 'md',
      render: (value: number) => (
        <Badge variant={value === BankTransactionDirection.Credit ? 'success' : 'outline'}>{bankTransactionDirectionLabels[value]}</Badge>
      ),
    },
    {
      key: 'amount',
      title: t('common.field.value'),
      dataIndex: 'amount',
      width: 130,
      render: (value: number, record: BankTransaction) => (
        <span className={record.direction === BankTransactionDirection.Credit ? 'text-emerald-600' : 'text-red-600'}>
          {record.direction === BankTransactionDirection.Credit ? '+' : '-'}{formatCurrency(value)}
        </span>
      ),
    },
    {
      key: 'status',
      title: t('common.field.status'),
      dataIndex: 'financialEntryId',
      width: 150,
      cardTag: true,
      render: (value: number | null | undefined, record: BankTransaction) =>
        value ? (
          <Badge variant="success">{t('financial.reconciliation.badge.matched')} · {record.matchKind ? bankTransactionMatchKindLabels[record.matchKind] : ''}</Badge>
        ) : (
          <Badge variant="outline">{t('financial.reconciliation.badge.pending')}</Badge>
        ),
    },
    {
      key: 'actions',
      title: '',
      width: 120,
      render: (_value: unknown, record: BankTransaction) =>
        record.financialEntryId ? (
          <button
            type="button"
            className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs text-amber-700 hover:bg-amber-50"
            onClick={() => { setSelected(record); setIsConfirmUnmatchOpen(true) }}
          >
            <Unlink size={12} /> {t('financial.reconciliation.action.unmatch')}
          </button>
        ) : (
          <button
            type="button"
            className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs text-primary hover:bg-primary/10"
            onClick={() => { setSelected(record); setIsMatchOpen(true) }}
          >
            <Link2 size={12} /> {t('financial.reconciliation.action.match')}
          </button>
        ),
    },
  ]

  return (
    <>
      <PageLayout
        title={t('financial.reconciliation.title')}
        subtitle={t('financial.reconciliation.subtitle')}
        onRefresh={() => void loadTransactions()}
        showDefaultActions={false}
      >
        <div className="flex flex-wrap items-end gap-3">
          <div className="space-y-1 min-w-[260px]">
            <label className="text-xs text-muted-foreground">{t('financial.reconciliation.field.account')}</label>
            <SearchableSelect
              value={accountId ? String(accountId) : ''}
              onValueChange={(value) => setAccountId(value ? Number(value) : undefined)}
              options={accounts.map((account) => ({ value: String(account.id), label: account.name }))}
              placeholder={t('financial.reconciliation.field.account')}
              searchPlaceholder={t('common.placeholder.search')}
            />
          </div>
          <div className="ml-auto">
            <Button size="sm" variant="outline" onClick={() => setIsImportOpen(true)} disabled={!accountId}>
              <Upload size={14} className="mr-1" /> {t('financial.reconciliation.action.import')}
            </Button>
          </div>
        </div>

        <div className="mt-3 grid grid-cols-3 gap-2 text-sm">
          <Stat label={t('financial.reconciliation.stat.total')} value={String(totals.total)} />
          <Stat label={t('financial.reconciliation.stat.matched')} value={String(totals.matched)} />
          <Stat label={t('financial.reconciliation.stat.pending')} value={String(totals.pending)} />
        </div>

        <div className="mt-4">
          <DataTable
            columns={columns}
            data={transactions}
            rowKey="id"
            emptyText={loading ? t('common.loading') : t('financial.reconciliation.empty')}
            loading={loading}
            pageSize={10}
            pageSizeOptions={[10, 20, 50]}
          />
        </div>
      </PageLayout>

      <MatchTransactionModal
        open={isMatchOpen}
        onOpenChange={setIsMatchOpen}
        transaction={selected}
        onSuccess={() => { setIsMatchOpen(false); setSelected(null); void loadTransactions() }}
      />

      <ImportBankStatementModal
        open={isImportOpen}
        onOpenChange={setIsImportOpen}
        accountId={accountId ?? 0}
        onSuccess={() => { setIsImportOpen(false); void loadTransactions() }}
      />

      <ConfirmModal
        open={isConfirmUnmatchOpen}
        onOpenChange={setIsConfirmUnmatchOpen}
        description={t('financial.reconciliation.confirm.unmatch')}
        variant="warning"
        onConfirm={() => void handleUnmatch()}
        loading={unmatching}
      />
    </>
  )
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border bg-primary/5 p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="text-sm font-semibold">{value}</p>
    </div>
  )
}
