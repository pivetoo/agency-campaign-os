import { useEffect, useMemo, useState } from 'react'
import { PageLayout, DataTable, Badge, TableToolbar, FilterPanel, useApi, useI18n, usePermissions } from 'archon-ui'
import type { DataTableColumn, FilterSection } from 'archon-ui'
import { contentLicenseService } from '../../../../services/contentLicenseService'
import type { ContentLicense, ContentLicenseType } from '../../../../types/contentLicense'
import { formatCurrency, formatDate } from '../../../../lib/format'
import DeliverableLicensesSheet from '../../../../components/sheets/DeliverableLicensesSheet'

function typeLabel(type: ContentLicenseType, t: (k: string) => string): string {
  if (type === 1) return t('contentLicense.type.ugcReuse')
  if (type === 2) return t('contentLicense.type.paidWhitelisting')
  if (type === 3) return t('contentLicense.type.exclusivity')
  return t('contentLicense.type.other')
}

export default function OperationsLicenses() {
  const { t } = useI18n()
  const { hasPermission } = usePermissions()
  // Valor da licença só para quem tem acesso ao financeiro (mesma regra do M7).
  const canSeeFinancials = hasPermission('financialEntries.get.description')

  const [licenses, setLicenses] = useState<ContentLicense[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [editing, setEditing] = useState<{ deliverableId: number; campaignId: number } | null>(null)

  const { execute: fetchLicenses, loading, pagination } = useApi<ContentLicense[]>({ showErrorMessage: true })

  const load = async () => {
    const result = await fetchLicenses(() =>
      contentLicenseService.getLicenses({
        page,
        pageSize,
        status: statusFilter ? Number(statusFilter) : undefined,
        type: typeFilter ? Number(typeFilter) : undefined,
        search: debouncedSearch || undefined,
      }),
    )
    if (result) setLicenses(result)
  }

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(search), 300)
    return () => clearTimeout(timeout)
  }, [search])

  useEffect(() => {
    setPage(1)
  }, [debouncedSearch, statusFilter, typeFilter])

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch, statusFilter, typeFilter])

  const filterSections: FilterSection[] = useMemo(
    () => [
      {
        key: 'status',
        label: t('common.column.status'),
        value: statusFilter,
        onChange: setStatusFilter,
        options: [
          { value: '2', label: t('contentLicense.status.expiringSoon') },
          { value: '3', label: t('contentLicense.status.expired') },
          { value: '1', label: t('contentLicense.status.active') },
        ],
        allLabel: t('common.filter.allStatuses'),
      },
      {
        key: 'type',
        label: t('contentLicense.field.type'),
        value: typeFilter,
        onChange: setTypeFilter,
        options: [
          { value: '1', label: t('contentLicense.type.ugcReuse') },
          { value: '2', label: t('contentLicense.type.paidWhitelisting') },
          { value: '3', label: t('contentLicense.type.exclusivity') },
          { value: '4', label: t('contentLicense.type.other') },
        ],
        allLabel: t('common.filter.all'),
      },
    ],
    [statusFilter, typeFilter, t],
  )

  const statusBadge = (license: ContentLicense) => {
    if (license.status === 3 || license.daysUntilExpiry == null) {
      return <Badge variant="destructive">{t('contentLicense.status.expired')}</Badge>
    }
    if (license.status === 2) {
      return <Badge variant="warning">{t('contentLicense.daysLeft').replace('{0}', String(license.daysUntilExpiry))}</Badge>
    }
    return <Badge variant="success">{t('contentLicense.status.active')}</Badge>
  }

  const columns: DataTableColumn<ContentLicense>[] = [
    {
      key: 'deliverable',
      title: t('common.field.deliverable'),
      dataIndex: 'deliverableTitle',
      primary: true,
      render: (value: string, record) => value || `#${record.deliverableId}`,
    },
    {
      key: 'campaign',
      title: t('campaign.field.campaign'),
      dataIndex: 'campaignName',
      render: (value?: string) => value || '-',
    },
    {
      key: 'creator',
      title: t('creators.singular'),
      dataIndex: 'creatorName',
      render: (value?: string) => value || '-',
    },
    {
      key: 'type',
      title: t('contentLicense.field.type'),
      dataIndex: 'type',
      render: (value: ContentLicenseType) => typeLabel(value, t),
    },
    {
      key: 'channels',
      title: t('contentLicense.field.channels'),
      dataIndex: 'channels',
      hiddenBelow: 'lg',
      render: (value?: string) => value || '-',
    },
    {
      key: 'expiresAt',
      title: t('contentLicense.field.expiresAt'),
      dataIndex: 'expiresAt',
      render: (value?: string) => (value ? formatDate(value) : t('contentLicense.noExpiry')),
    },
    ...(canSeeFinancials
      ? [
          {
            key: 'value',
            title: t('contentLicense.field.value'),
            dataIndex: 'value',
            hiddenBelow: 'sm',
            render: (value?: number) => (value != null ? formatCurrency(value) : '-'),
          } as DataTableColumn<ContentLicense>,
        ]
      : []),
    {
      key: 'status',
      title: t('common.column.status'),
      dataIndex: 'status',
      cardTag: true,
      render: (_value, record) => statusBadge(record),
    },
  ]

  return (
    <>
      <PageLayout
        title={t('contentLicense.page.title')}
        subtitle={t('contentLicense.page.subtitle')}
        onRefresh={() => void load()}
        showDefaultActions={false}
      >
        <TableToolbar
          searchValue={search}
          onSearchChange={setSearch}
          searchPlaceholder={t('common.action.search')}
          rightSlot={<FilterPanel sections={filterSections} onClearAll={() => { setStatusFilter(''); setTypeFilter('') }} />}
          className="mb-3"
        />

        <DataTable
          columns={columns}
          data={licenses}
          rowKey="id"
          loading={loading}
          onRowDoubleClick={(record) => setEditing({ deliverableId: record.deliverableId, campaignId: record.campaignId })}
          emptyText={t('contentLicense.empty')}
          pageSize={pageSize}
          pageSizeOptions={[20, 50, 100]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(size) => {
            setPageSize(size)
            setPage(1)
          }}
        />
      </PageLayout>

      <DeliverableLicensesSheet
        open={editing !== null}
        onOpenChange={(open) => {
          if (!open) {
            setEditing(null)
            void load()
          }
        }}
        deliverableId={editing?.deliverableId ?? null}
        campaignId={editing?.campaignId ?? 0}
      />
    </>
  )
}
