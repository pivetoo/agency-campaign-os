import { useEffect, useMemo, useState } from 'react'
import { PageLayout, Card, CardContent, ConfirmModal, DataTable, useApi, Badge, Input, FilterPanel, TableToolbar, useI18n } from 'archon-ui'
import type { DataTableColumn, FilterSection } from 'archon-ui'
import { CheckCircle2, Pencil, Undo2 } from 'lucide-react'
import { financialEntryService, type FinancialEntryFilters } from '../../../services/financialEntryService'
import { financialAccountService } from '../../../services/financialAccountService'
import { FinancialEntryStatus, financialEntryCategoryLabels, financialEntryReceivableStatusLabels, financialEntryStatusLabels, type FinancialEntry, type FinancialSummary } from '../../../types/financialEntry'
import type { FinancialAccount } from '../../../types/financialAccount'
import FinancialEntryFormModal from '../../../components/modals/FinancialEntryFormModal'
import MarkAsPaidModal from '../../../components/modals/MarkAsPaidModal'
import AuditUtilityBar from '../../../components/buttons/AuditUtilityBar'
import { formatCurrency, dateInputToIso, isoToDateInput } from '../../../lib/format'

interface FinancialEntriesPageProps {
  type: 1 | 2
  title: string
  subtitle: string
}

export default function FinancialEntriesPage({ type, title, subtitle }: FinancialEntriesPageProps) {
  const { t } = useI18n()
  const isReceivable = type === 1
  const [entries, setEntries] = useState<FinancialEntry[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [summary, setSummary] = useState<FinancialSummary | null>(null)
  const [accounts, setAccounts] = useState<FinancialAccount[]>([])
  const [filters, setFilters] = useState<FinancialEntryFilters>({})
  const [selected, setSelected] = useState<FinancialEntry | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isMarkPaidOpen, setIsMarkPaidOpen] = useState(false)
  const [isReverseOpen, setIsReverseOpen] = useState(false)
  const { execute: fetchEntries, loading, pagination } = useApi<FinancialEntry[]>({ showErrorMessage: true })
  const { execute: fetchSummary } = useApi<FinancialSummary | null>({ showErrorMessage: true })
  const { execute: runReverse, loading: reversing } = useApi<FinancialEntry>({ showSuccessMessage: true, showErrorMessage: true })

  const handleReverse = async () => {
    if (!selected) return
    const result = await runReverse(() => financialEntryService.reverse(selected.id))
    if (result !== null) {
      setIsReverseOpen(false)
      setSelected(null)
      void loadEntries()
      void loadSummary()
    }
  }

  const loadEntries = async () => {
    const result = await fetchEntries(() => financialEntryService.getAll({ ...filters, type, page, pageSize }))
    if (result) setEntries(result)
  }

  const loadSummary = async () => {
    const result = await fetchSummary(() => financialEntryService.getSummary(type))
    setSummary(result)
  }

  useEffect(() => {
    void loadSummary()
    void financialAccountService.getAll({ pageSize: 200 }).then((r) => setAccounts(r.data ?? []))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [type])

  useEffect(() => {
    setPage(1)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filters, type])

  useEffect(() => {
    void loadEntries()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, filters, type])

  const statusLabels = isReceivable ? financialEntryReceivableStatusLabels : financialEntryStatusLabels
  const settledLabel = isReceivable ? t('financial.entries.kpi.settledReceivable') : t('financial.entries.kpi.settledPayable')
  const dueSoonLabel = isReceivable ? t('financial.entries.kpi.dueSoonReceivable') : t('financial.entries.kpi.dueSoonPayable')

  const filterSections: FilterSection[] = useMemo(() => [
    {
      key: 'status',
      label: t('common.field.status'),
      value: filters.status ? String(filters.status) : '',
      onChange: (value) => setFilters((prev) => ({ ...prev, status: value ? Number(value) : undefined })),
      options: Object.entries(statusLabels).map(([value, label]) => ({ value, label })),
    },
    {
      key: 'account',
      label: t('common.field.account'),
      value: filters.accountId ? String(filters.accountId) : '',
      onChange: (value) => setFilters((prev) => ({ ...prev, accountId: value ? Number(value) : undefined })),
      options: accounts.map((account) => ({ value: String(account.id), label: account.name })),
    },
  ], [filters.status, filters.accountId, accounts, statusLabels, t])

  const clearFilters = () => {
    setFilters((prev) => ({
      ...prev,
      status: undefined,
      accountId: undefined,
      search: prev.search,
      dueFrom: prev.dueFrom,
      dueTo: prev.dueTo,
    }))
  }

  const columns: DataTableColumn<FinancialEntry>[] = [
    {
      key: 'description',
      title: t('common.field.description'),
      dataIndex: 'description',
      render: (value: string, record: FinancialEntry) => (
        <span className="inline-flex flex-col">
          <span className="inline-flex items-center gap-1.5">
            <span>{value}</span>
            {record.installmentNumber && record.installmentTotal && (
              <Badge variant="outline">{record.installmentNumber}/{record.installmentTotal}</Badge>
            )}
            {record.invoiceNumber && (
              <Badge variant="outline" className="text-[10px]">NF {record.invoiceNumber}</Badge>
            )}
            {record.isAutoGenerated && (
              <Badge variant="outline" className="text-[10px]" title={t('financial.entries.badge.autoTooltip')}>{t('financial.entries.badge.auto')}</Badge>
            )}
          </span>
          {record.subcategoryName && (
            <span className="text-[10px]" style={{ color: record.subcategoryColor ?? undefined }}>
              {record.subcategoryName}
            </span>
          )}
        </span>
      ),
    },
    { key: 'counterpartyName', title: t('financial.entries.field.counterparty'), dataIndex: 'counterpartyName', hiddenBelow: 'md', render: (value?: string) => value || '-' },
    {
      key: 'category',
      title: t('common.field.category'),
      dataIndex: 'category',
      hiddenBelow: 'md',
      render: (value: number) => <Badge variant="outline">{financialEntryCategoryLabels[value] || '-'}</Badge>,
    },
    {
      key: 'campaignName',
      title: t('common.field.campaign'),
      dataIndex: 'campaignName',
      hiddenBelow: 'lg',
      render: (value?: string | null) => value || <span className="text-xs text-muted-foreground">—</span>,
    },
    {
      key: 'accountName',
      title: t('common.field.account'),
      dataIndex: 'accountName',
      hiddenBelow: 'lg',
      render: (value: string | null | undefined, record: FinancialEntry) => (
        <span className="inline-flex items-center gap-1.5">
          <span className="inline-block h-2 w-2 rounded-full" style={{ backgroundColor: record.accountColor ?? '#6b7280' }} />
          {value || '-'}
        </span>
      ),
    },
    { key: 'amount', title: t('common.field.value'), dataIndex: 'amount', render: (value: number) => formatCurrency(value) },
    { key: 'dueAt', title: t('financial.entries.field.dueDate'), dataIndex: 'dueAt', render: (value: string) => new Date(value).toLocaleDateString('pt-BR') },
    {
      key: 'status',
      title: t('common.field.status'),
      dataIndex: 'status',
      render: (value: number) => (
        <Badge variant={value === 2 ? 'success' : value === 3 ? 'destructive' : value === 4 ? 'outline' : 'warning'}>
          {statusLabels[value] || '-'}
        </Badge>
      ),
    },
    {
      key: 'actions',
      title: '',
      width: 130,
      render: (_value, record) => (
        <div className="flex gap-1">
          {record.status !== FinancialEntryStatus.Paid && record.status !== FinancialEntryStatus.Cancelled && (
            <button
              type="button"
              className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs text-emerald-700 hover:bg-emerald-50"
              onClick={(event) => { event.stopPropagation(); setSelected(record); setIsMarkPaidOpen(true) }}
            >
              <CheckCircle2 size={12} />
              {t('common.action.confirm')}
            </button>
          )}
          {record.isReversed && (
            <Badge variant="outline">{t('financial.entries.badge.reversed')}</Badge>
          )}
          {record.status === FinancialEntryStatus.Paid && !record.isReversed && !record.reversalOfEntryId && (
            <button
              type="button"
              className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs text-amber-700 hover:bg-amber-50"
              onClick={(event) => { event.stopPropagation(); setSelected(record); setIsReverseOpen(true) }}
            >
              <Undo2 size={12} />
              {t('financial.entries.action.reverse')}
            </button>
          )}
          <button
            type="button"
            className="p-1 text-muted-foreground hover:text-foreground"
            onClick={(event) => { event.stopPropagation(); setSelected(record); setIsFormOpen(true) }}
          >
            <Pencil size={14} />
          </button>
        </div>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title={title}
        subtitle={subtitle}
        actionsSlot={<AuditUtilityBar entityName="FinancialEntry" entityLabel="Lançamento" entityId={selected?.id ?? null} />}
        onAdd={() => { setSelected(null); setIsFormOpen(true) }}
        onRefresh={() => { void loadEntries(); void loadSummary() }}
        addLabel={t('financial.entries.action.new')}
      >
        <div className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4 mb-4" data-tour="financial-entries-kpis">
          <Card>
            <CardContent className="pt-5 pb-5">
              <p className="text-xs text-muted-foreground uppercase tracking-wide">{isReceivable ? t('financial.kpi.receivable') : t('financial.kpi.payable')}</p>
              <p className="text-2xl font-semibold mt-1">{formatCurrency(summary?.totalPending ?? 0)}</p>
              <p className="text-[10px] text-muted-foreground">{summary?.pendingCount ?? 0} {t('financial.entries.kpi.count')}</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-5 pb-5">
              <p className="text-xs text-muted-foreground uppercase tracking-wide">{settledLabel}</p>
              <p className="text-2xl font-semibold mt-1 text-emerald-600">{formatCurrency(summary?.totalSettledThisMonth ?? 0)}</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-5 pb-5">
              <p className="text-xs text-muted-foreground uppercase tracking-wide">{t('financial.entries.badge.overdue')}</p>
              <p className="text-2xl font-semibold mt-1 text-destructive">{formatCurrency(summary?.totalOverdue ?? 0)}</p>
              <p className="text-[10px] text-muted-foreground">{summary?.overdueCount ?? 0} {t('financial.entries.kpi.count')}</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-5 pb-5">
              <p className="text-xs text-muted-foreground uppercase tracking-wide">{dueSoonLabel}</p>
              <p className="text-2xl font-semibold mt-1 text-amber-600">{formatCurrency(summary?.totalDueNext7Days ?? 0)}</p>
            </CardContent>
          </Card>
        </div>

        <Card>
          <CardContent className="pt-4 space-y-3">
            <TableToolbar
              searchValue={filters.search ?? ''}
              onSearchChange={(value) => setFilters((prev) => ({ ...prev, search: value || undefined }))}
              searchPlaceholder={t('financial.entries.placeholder.search')}
              rightSlot={<FilterPanel sections={filterSections} onClearAll={clearFilters} />}
            />
            <div className="flex gap-2 md:max-w-md">
              <Input type="date" value={isoToDateInput(filters.dueFrom)} onChange={(e) => setFilters((prev) => ({ ...prev, dueFrom: e.target.value ? dateInputToIso(e.target.value) : undefined }))} />
              <Input type="date" value={isoToDateInput(filters.dueTo)} onChange={(e) => setFilters((prev) => ({ ...prev, dueTo: e.target.value ? dateInputToIso(e.target.value) : undefined }))} />
            </div>

            <DataTable
              columns={columns}
              data={entries}
              rowKey="id"
              emptyText={isReceivable ? t('financial.entries.emptyReceivable') : t('financial.entries.emptyPayable')}
              loading={loading}
              pageSize={pageSize}
              pageSizeOptions={[10, 20, 50]}
              totalCount={pagination?.totalCount}
              page={page}
              onPageChange={setPage}
              onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
            />
          </CardContent>
        </Card>
      </PageLayout>

      <FinancialEntryFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        entry={selected}
        defaultType={type}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelected(null)
          void loadEntries()
          void loadSummary()
        }}
      />

      <MarkAsPaidModal
        open={isMarkPaidOpen}
        onOpenChange={setIsMarkPaidOpen}
        entry={selected}
        onSuccess={() => {
          setIsMarkPaidOpen(false)
          setSelected(null)
          void loadEntries()
          void loadSummary()
        }}
      />

      <ConfirmModal
        open={isReverseOpen}
        onOpenChange={setIsReverseOpen}
        description={t('financial.entries.confirm.reverse')}
        variant="warning"
        onConfirm={() => void handleReverse()}
        loading={reversing}
      />
    </>
  )
}
