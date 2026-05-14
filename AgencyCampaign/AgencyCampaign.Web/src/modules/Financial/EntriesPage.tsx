import { useEffect, useState } from 'react'
import { PageLayout, Card, CardContent, DataTable, useApi, Badge, Button, Input, SearchableSelect, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { CheckCircle2, Pencil, Trash2 } from 'lucide-react'
import { financialEntryService, type FinancialEntryFilters } from '../../services/financialEntryService'
import { financialAccountService } from '../../services/financialAccountService'
import {
  FinancialEntryStatus,
  financialEntryCategoryLabels,
  financialEntryReceivableStatusLabels,
  financialEntryStatusLabels,
  type FinancialEntry,
  type FinancialSummary,
} from '../../types/financialEntry'
import type { FinancialAccount } from '../../types/financialAccount'
import FinancialEntryFormModal from '../../components/modals/FinancialEntryFormModal'
import MarkAsPaidModal from '../../components/modals/MarkAsPaidModal'

interface FinancialEntriesPageProps {
  type: 1 | 2
  title: string
  subtitle: string
}

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function FinancialEntriesPage({ type, title, subtitle }: FinancialEntriesPageProps) {
  const { t } = useI18n()
  const isReceivable = type === 1
  const [entries, setEntries] = useState<FinancialEntry[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [summary, setSummary] = useState<FinancialSummary | null>(null)
  const [accounts, setAccounts] = useState<FinancialAccount[]>([])
  const [filters, setFilters] = useState<FinancialEntryFilters>({})
  const [selected, setSelected] = useState<FinancialEntry | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isMarkPaidOpen, setIsMarkPaidOpen] = useState(false)
  const { execute: fetchEntries, loading, pagination } = useApi<FinancialEntry[]>({ showErrorMessage: true })
  const { execute: fetchSummary } = useApi<FinancialSummary | null>({ showErrorMessage: true })

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
    void financialAccountService.getAll(false).then(setAccounts)
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
        onAdd={() => { setSelected(null); setIsFormOpen(true) }}
        onRefresh={() => { void loadEntries(); void loadSummary() }}
        showDefaultActions={false}
        actions={[
          {
            key: 'new',
            label: t('financial.entries.action.new'),
            testId: 'crud-add-button',
            icon: <Trash2 className="h-4 w-4 hidden" />,
            onClick: () => { setSelected(null); setIsFormOpen(true) },
          },
        ]}
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
            <div className="grid grid-cols-1 gap-3 md:grid-cols-4">
              <Input
                placeholder={t('financial.entries.placeholder.search')}
                value={filters.search ?? ''}
                onChange={(e) => setFilters((prev) => ({ ...prev, search: e.target.value || undefined }))}
              />
              <SearchableSelect
                value={filters.status ? String(filters.status) : ''}
                onValueChange={(value) => setFilters((prev) => ({ ...prev, status: value ? Number(value) : undefined }))}
                options={[
                  { value: '', label: t('common.filter.allStatuses') },
                  ...Object.entries(statusLabels).map(([value, label]) => ({ value, label })),
                ]}
                placeholder={t('common.field.status')}
              />
              <SearchableSelect
                value={filters.accountId ? String(filters.accountId) : ''}
                onValueChange={(value) => setFilters((prev) => ({ ...prev, accountId: value ? Number(value) : undefined }))}
                options={[
                  { value: '', label: t('financial.entries.filter.allAccounts') },
                  ...accounts.map((account) => ({ value: String(account.id), label: account.name })),
                ]}
                placeholder={t('financial.entries.placeholder.account')}
              />
              <div className="flex gap-2">
                <Input type="date" value={filters.dueFrom?.slice(0, 10) ?? ''} onChange={(e) => setFilters((prev) => ({ ...prev, dueFrom: e.target.value ? new Date(e.target.value).toISOString() : undefined }))} />
                <Input type="date" value={filters.dueTo?.slice(0, 10) ?? ''} onChange={(e) => setFilters((prev) => ({ ...prev, dueTo: e.target.value ? new Date(e.target.value).toISOString() : undefined }))} />
              </div>
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

            <div className="flex justify-end">
              <Button onClick={() => { setSelected(null); setIsFormOpen(true) }}>{t('financial.entries.action.new')}</Button>
            </div>
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
    </>
  )
}
