import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, FilterPanel, useApi, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, Badge, TableToolbar, useI18n, Dropdown, DropdownTrigger, DropdownContent, DropdownItem, DropdownSeparator } from 'archon-ui'
import type { DataTableColumn, FilterSection } from 'archon-ui'
import { Link as LinkIcon, FileSpreadsheet, Download, Upload } from 'lucide-react'

import { creatorService, resolveCreatorPhotoUrl } from '../../services/creatorService'
import type { Creator } from '../../types/creator'
import CreatorFormModal from '../../components/modals/CreatorFormModal'
import CreatorAccessTokensModal from '../../components/modals/CreatorAccessTokensModal'
import CreatorImportModal from '../../components/modals/CreatorImportModal'

export default function Creators() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [creators, setCreators] = useState<Creator[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [includeInactiveFilter, setIncludeInactiveFilter] = useState('')
  const [selectedCreator, setSelectedCreator] = useState<Creator | null>(null)
  const [previewCreator, setPreviewCreator] = useState<Creator | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isTokensOpen, setIsTokensOpen] = useState(false)
  const [isImportOpen, setIsImportOpen] = useState(false)
  const { execute: fetchCreators, loading, pagination } = useApi<Creator[]>({ showErrorMessage: true })

  const loadCreators = async () => {
    const result = await fetchCreators(() => creatorService.getAll({ page, pageSize, search: debouncedSearch || undefined, includeInactive: includeInactiveFilter === 'all' }))
    if (result) {
      setCreators(result)
    }
  }

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(search), 300)
    return () => clearTimeout(timeout)
  }, [search])

  useEffect(() => {
    setPage(1)
  }, [debouncedSearch, includeInactiveFilter])

  useEffect(() => {
    void loadCreators()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch, includeInactiveFilter])

  const filterSections: FilterSection[] = useMemo(() => [
    {
      key: 'inactiveFilter',
      label: t('common.field.status'),
      value: includeInactiveFilter,
      onChange: setIncludeInactiveFilter,
      options: [
        { value: 'all', label: 'Incluir inativos' },
      ],
      allLabel: 'Somente ativos',
    },
  ], [includeInactiveFilter, t])

  const clearFilters = () => {
    setIncludeInactiveFilter('')
  }

  const renderPhotoCell = (_: unknown, record: Creator) => {
    const url = resolveCreatorPhotoUrl(record.photoUrl)
    const initial = (record.stageName?.trim() || record.name?.trim() || '?').charAt(0).toUpperCase()
    return (
      <div className="flex h-9 w-9 items-center justify-center overflow-hidden rounded-full border bg-muted/30">
        {url ? (
          <img src={url} alt={record.name} className="h-full w-full object-cover" />
        ) : (
          <span className="text-xs font-semibold text-muted-foreground">{initial}</span>
        )}
      </div>
    )
  }

  const columns: DataTableColumn<Creator>[] = [
    { key: 'photo', title: '', dataIndex: 'photoUrl', width: 56, render: renderPhotoCell },
    { key: 'name', title: t('common.field.name'), dataIndex: 'name' },
    { key: 'stageName', title: t('creators.field.stageName'), dataIndex: 'stageName', hiddenBelow: 'sm', render: (value?: string) => value || '-' },
    { key: 'primaryNiche', title: t('creators.field.niche'), dataIndex: 'primaryNiche', hiddenBelow: 'md', render: (value?: string) => value || '-' },
    { key: 'city', title: t('common.field.city'), dataIndex: 'city', hiddenBelow: 'md', render: (value?: string) => value || '-' },
    {
      key: 'isActive',
      title: t('common.field.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      title: '',
      width: 96,
      render: (_: unknown, record: Creator) => (
        <button
          type="button"
          className="text-xs text-primary hover:underline"
          onClick={(event) => { event.stopPropagation(); navigate(`/creators/${record.id}`) }}
        >
          {t('creators.action.open360')}
        </button>
      ),
    },
  ]

  const handleExport = async () => {
    const blob = await creatorService.exportCsv()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'influenciadores.csv'
    a.click()
    URL.revokeObjectURL(url)
  }

  const excelButton = (
    <Dropdown>
      <DropdownTrigger asChild>
        <button className="inline-flex items-center gap-1.5 rounded-lg border border-[#1d6f42]/50 px-3.5 py-1.5 text-sm font-medium text-[#1d6f42] transition-colors hover:bg-[#1d6f42]/8 hover:border-[#1d6f42]">
          <FileSpreadsheet size={15} className="text-[#1d6f42]" />
          Excel
        </button>
      </DropdownTrigger>
      <DropdownContent align="end" className="w-40">
        <DropdownItem className="gap-2 cursor-pointer" onSelect={() => setIsImportOpen(true)}>
          <Upload size={14} />
          Importar
        </DropdownItem>
        <DropdownSeparator />
        <DropdownItem className="gap-2 cursor-pointer" onSelect={() => void handleExport()}>
          <Download size={14} />
          Exportar
        </DropdownItem>
      </DropdownContent>
    </Dropdown>
  )

  return (
    <>
      <PageLayout
        title={t('creators.title')}
        subtitle={t('creators.subtitle')}
        actionsSlot={excelButton}
        onAdd={() => { setSelectedCreator(null); setIsFormOpen(true) }}
        onEdit={() => selectedCreator && setIsFormOpen(true)}
        onRefresh={() => void loadCreators()}
        selectedRowsCount={selectedCreator ? 1 : 0}
        actions={[
          {
            key: 'access-tokens',
            label: t('creators.action.portalLinks'),
            icon: <LinkIcon className="h-4 w-4" />,
            variant: 'outline',
            disabled: !selectedCreator,
            onClick: () => selectedCreator && setIsTokensOpen(true),
          },
        ]}
      >
        <div data-tour="creators-table">
        <TableToolbar
          searchValue={search}
          onSearchChange={setSearch}
          searchPlaceholder={t('common.action.search')}
          rightSlot={<FilterPanel sections={filterSections} onClearAll={clearFilters} />}
          className="mb-3"
        />
          <DataTable
            columns={columns}
            data={creators}
            rowKey="id"
            selectedRows={selectedCreator ? [selectedCreator] : []}
            onSelectionChange={(rows) => setSelectedCreator(rows[0] ?? null)}
            onRowDoubleClick={setPreviewCreator}
            emptyText={t('creators.empty')}
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

      <CreatorImportModal
        open={isImportOpen}
        onOpenChange={setIsImportOpen}
      />

      <CreatorFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        creator={selectedCreator}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedCreator(null)
          if (page === 1) {
            void loadCreators()
          } else {
            setPage(1)
          }
        }}
      />

      <CreatorAccessTokensModal
        open={isTokensOpen}
        onOpenChange={setIsTokensOpen}
        creator={selectedCreator}
      />

      <Sheet open={!!previewCreator} onOpenChange={(open) => !open && setPreviewCreator(null)}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewCreator ? (
            <div className="flex h-full flex-col">
              {(() => {
                const photoSrc = resolveCreatorPhotoUrl(previewCreator.photoUrl)
                const initial = (previewCreator.stageName?.trim() || previewCreator.name?.trim() || '?').charAt(0).toUpperCase()
                return (
                  <div className="mb-4 flex items-center justify-center">
                    <div className="flex items-center justify-center overflow-hidden rounded-full border bg-muted/20" style={{ width: 140, height: 140 }}>
                      {photoSrc ? (
                        <img src={photoSrc} alt={previewCreator.name} className="h-full w-full object-cover" />
                      ) : (
                        <span className="text-4xl font-semibold text-muted-foreground">{initial}</span>
                      )}
                    </div>
                  </div>
                )
              })()}
              <SheetPreviewHeader
                title={previewCreator.stageName || previewCreator.name}
                meta={
                  <Badge variant={previewCreator.isActive ? 'success' : 'destructive'}>
                    {previewCreator.isActive ? t('common.status.active') : t('common.status.inactive')}
                  </Badge>
                }
                description={t('creators.preview.description')}
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title={t('creators.preview.dataSection')} description={t('creators.preview.dataSectionDesc')}>
                  <SheetPreviewGrid>
                    <SheetPreviewField label={t('common.field.name')} value={previewCreator.name} />
                    <SheetPreviewField label={t('creators.field.stageName')} value={previewCreator.stageName || '-'} />
                    <SheetPreviewField label={t('common.field.document')} value={previewCreator.document || '-'} />
                    <SheetPreviewField label={t('common.field.phone')} value={previewCreator.phone || '-'} />
                    <SheetPreviewField label={t('creators.field.pixKey')} value={previewCreator.pixKey || '-'} />
                    <SheetPreviewField label={t('creators.field.niche')} value={previewCreator.primaryNiche || '-'} />
                    <SheetPreviewField label={t('common.field.city')} value={previewCreator.city || '-'} />
                    <SheetPreviewField label={t('common.field.state')} value={previewCreator.state || '-'} />
                    <SheetPreviewField label={t('creators.field.defaultFee')} value={previewCreator.defaultAgencyFeePercent.toFixed(2)} />
                    <SheetPreviewField className="sm:col-span-2" label={t('common.field.email')} value={previewCreator.email || '-'} />
                    <SheetPreviewField className="sm:col-span-2" label={t('common.field.notes')} value={previewCreator.notes || '-'} />
                  </SheetPreviewGrid>
                </SheetPreviewSection>
              </div>
            </div>
          ) : null}
        </SheetContent>
      </Sheet>
    </>
  )
}
