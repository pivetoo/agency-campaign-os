import { useEffect, useMemo, useState } from 'react'
import { Badge, Button, ConfirmModal, DataTable, PageLayout, SearchableSelect, useApi, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Link2, Unlink, Upload } from 'lucide-react'
import { financialAccountService } from '../../../services/financialAccountService'
import { bankTransactionService, type ReconciliationSummary } from '../../../services/bankTransactionService'
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
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [summary, setSummary] = useState<ReconciliationSummary | null>(null)

  const { execute: fetchAccounts } = useApi<FinancialAccount[]>({ showErrorMessage: true })
  const { execute: fetchTransactions, loading, pagination } = useApi<BankTransaction[]>({ showErrorMessage: true })
  const { execute: fetchSummary } = useApi<ReconciliationSummary>({ showErrorMessage: true })
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
    const result = await fetchTransactions(() => bankTransactionService.getByAccount(accountId, page, pageSize))
    if (result) {
      setTransactions(result)
      setSelected(null)
    }
  }

  const loadSummary = async () => {
    if (!accountId) {
      setSummary(null)
      return
    }
    const result = await fetchSummary(() => bankTransactionService.getSummary(accountId))
    setSummary(result)
  }

  const refresh = () => {
    void loadTransactions()
    void loadSummary()
  }

  useEffect(() => {
    setPage(1)
    void loadSummary()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accountId])

  useEffect(() => {
    void loadTransactions()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accountId, page, pageSize])

  const handleUnmatch = async () => {
    if (!selected) return
    const result = await runUnmatch(() => bankTransactionService.unmatch(selected.id))
    if (result !== null) {
      setIsConfirmUnmatchOpen(false)
      setSelected(null)
      refresh()
    }
  }

  const selectedAccount = useMemo(() => accounts.find((item) => item.id === accountId), [accounts, accountId])
  const bankBalance = selectedAccount?.lastSyncedBalance ?? null
  const balanceDiff = selectedAccount && bankBalance != null ? selectedAccount.currentBalance - bankBalance : null
  const reconciled = balanceDiff != null && Math.abs(balanceDiff) < 0.005

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
        onRefresh={() => refresh()}
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

        {selectedAccount && (
          <div className="mt-3 grid grid-cols-1 gap-2 text-sm sm:grid-cols-3">
            <Stat label={t('financial.reconciliation.balance.mainstay')} value={formatCurrency(selectedAccount.currentBalance)} />
            <Stat
              label={t('financial.reconciliation.balance.bank')}
              value={bankBalance != null ? formatCurrency(bankBalance) : t('financial.reconciliation.balance.notSynced')}
              tone={bankBalance == null ? 'muted' : undefined}
            />
            {balanceDiff != null && (
              <Stat
                label={t('financial.reconciliation.balance.difference')}
                value={formatCurrency(balanceDiff)}
                tone={reconciled ? 'ok' : 'warn'}
              />
            )}
          </div>
        )}

        <div className="mt-3 grid grid-cols-3 gap-2 text-sm">
          <Stat label={t('financial.reconciliation.stat.total')} value={String(summary?.total ?? 0)} />
          <Stat label={t('financial.reconciliation.stat.matched')} value={String(summary?.matched ?? 0)} />
          <Stat label={t('financial.reconciliation.stat.pending')} value={String(summary?.pending ?? 0)} />
        </div>

        <div className="mt-4">
          <DataTable
            columns={columns}
            data={transactions}
            rowKey="id"
            emptyText={loading ? t('common.loading') : t('financial.reconciliation.empty')}
            loading={loading}
            pageSize={pageSize}
            pageSizeOptions={[10, 20, 50]}
            totalCount={pagination?.totalCount}
            page={page}
            onPageChange={setPage}
            onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
          />
        </div>
      </PageLayout>

      <MatchTransactionModal
        open={isMatchOpen}
        onOpenChange={setIsMatchOpen}
        transaction={selected}
        onSuccess={() => { setIsMatchOpen(false); setSelected(null); refresh() }}
      />

      <ImportBankStatementModal
        open={isImportOpen}
        onOpenChange={setIsImportOpen}
        accountId={accountId ?? 0}
        onSuccess={() => { setIsImportOpen(false); refresh() }}
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

function Stat({ label, value, tone }: { label: string; value: string; tone?: 'ok' | 'warn' | 'muted' }) {
  const valueClass = tone === 'warn'
    ? 'text-amber-600'
    : tone === 'ok'
      ? 'text-emerald-600'
      : tone === 'muted'
        ? 'text-muted-foreground'
        : ''
  return (
    <div className="rounded-lg border bg-primary/5 p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className={`text-sm font-semibold ${valueClass}`}>{value}</p>
    </div>
  )
}
