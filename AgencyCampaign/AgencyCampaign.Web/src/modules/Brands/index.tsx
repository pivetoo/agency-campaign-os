import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, TableToolbar, useI18n, Dropdown, DropdownTrigger, DropdownContent, DropdownItem, DropdownSeparator } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { FileSpreadsheet, Download, Upload } from 'lucide-react'

import { brandService, resolveBrandLogoUrl } from '../../services/brandService'
import type { Brand } from '../../types/brand'
import BrandFormModal from '../../components/modals/BrandFormModal'
import BrandImportModal from '../../components/modals/BrandImportModal'

export default function Brands() {
  const { t } = useI18n()
  const [brands, setBrands] = useState<Brand[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [selectedBrand, setSelectedBrand] = useState<Brand | null>(null)
  const [previewBrand, setPreviewBrand] = useState<Brand | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isImportOpen, setIsImportOpen] = useState(false)
  const { execute: fetchBrands, loading, pagination } = useApi<Brand[]>({ showErrorMessage: true })

  const loadBrands = async () => {
    const result = await fetchBrands(() => brandService.getAll({ page, pageSize, search: debouncedSearch || undefined }))
    if (result) {
      setBrands(result)
    }
  }

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(search), 300)
    return () => clearTimeout(timeout)
  }, [search])

  useEffect(() => {
    setPage(1)
  }, [debouncedSearch])

  useEffect(() => {
    void loadBrands()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch])

  const renderLogoCell = (_: unknown, record: Brand) => {
    const url = resolveBrandLogoUrl(record.logoUrl)
    return (
      <div className="flex h-9 w-9 items-center justify-center overflow-hidden rounded border bg-muted/30">
        {url ? (
          <img src={url} alt={record.name} className="h-full w-full object-contain" />
        ) : (
          <span className="text-xs font-semibold text-muted-foreground">
            {record.name?.charAt(0).toUpperCase() ?? '?'}
          </span>
        )}
      </div>
    )
  }

  const columns: DataTableColumn<Brand>[] = [
    { key: 'logo', title: '', dataIndex: 'logoUrl', width: 56, render: renderLogoCell },
    { key: 'name', title: t('common.field.name'), dataIndex: 'name' },
    { key: 'tradeName', title: t('common.field.tradeName'), dataIndex: 'tradeName', hiddenBelow: 'sm', render: (value?: string) => value || '-' },
    { key: 'document', title: t('common.field.document'), dataIndex: 'document', hiddenBelow: 'md', render: (value?: string) => value || '-' },
    { key: 'contactName', title: t('common.field.contact'), dataIndex: 'contactName', hiddenBelow: 'lg' },
    { key: 'contactEmail', title: t('common.field.email'), dataIndex: 'contactEmail', hiddenBelow: 'lg' },
    {
      key: 'isActive',
      title: t('common.field.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.status.activeFemale') : t('common.status.inactiveFemale')}
        </Badge>
      ),
    },
  ]

  const handleExport = async () => {
    const blob = await brandService.exportCsv()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'marcas.csv'
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
        title={t('brands.title')}
        subtitle={t('brands.subtitle')}
        actionsSlot={excelButton}
        onAdd={() => { setSelectedBrand(null); setIsFormOpen(true) }}
        onEdit={() => selectedBrand && setIsFormOpen(true)}
        onRefresh={() => void loadBrands()}
        selectedRowsCount={selectedBrand ? 1 : 0}
      >
        <TableToolbar
          searchValue={search}
          onSearchChange={setSearch}
          searchPlaceholder={t('common.action.search')}
          className="mb-3"
        />
        <DataTable
          columns={columns}
          data={brands}
          rowKey="id"
          selectedRows={selectedBrand ? [selectedBrand] : []}
          onSelectionChange={(rows) => setSelectedBrand(rows[0] ?? null)}
          onRowDoubleClick={setPreviewBrand}
          emptyText={t('brands.empty')}
          loading={loading}
          pageSize={pageSize}
          pageSizeOptions={[10, 20, 50]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
        />
      </PageLayout>

      <BrandImportModal
        open={isImportOpen}
        onOpenChange={setIsImportOpen}
      />

      <BrandFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        brand={selectedBrand}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedBrand(null)
          if (page === 1) {
            void loadBrands()
          } else {
            setPage(1)
          }
        }}
      />

      <Sheet open={!!previewBrand} onOpenChange={(open) => !open && setPreviewBrand(null)}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewBrand ? (
            <div className="flex h-full flex-col">
              {(() => {
                const logoSrc = resolveBrandLogoUrl(previewBrand.logoUrl)
                return (
                  <div className="mb-4 flex items-center justify-center overflow-hidden rounded-lg border bg-muted/20" style={{ height: 140 }}>
                    {logoSrc ? (
                      <img src={logoSrc} alt={previewBrand.name} className="h-full w-full object-contain p-3" />
                    ) : (
                      <span className="text-3xl font-semibold text-muted-foreground">
                        {previewBrand.name?.charAt(0).toUpperCase() ?? '?'}
                      </span>
                    )}
                  </div>
                )
              })()}
              <SheetPreviewHeader
                title={previewBrand.name}
                meta={
                  <Badge variant={previewBrand.isActive ? 'success' : 'destructive'}>
                    {previewBrand.isActive ? t('common.status.activeFemale') : t('common.status.inactiveFemale')}
                  </Badge>
                }
                description={t('brands.preview.description')}
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title={t('brands.preview.dataSection')} description={t('brands.preview.dataSectionDesc')}>
                  <SheetPreviewGrid>
                    <SheetPreviewField label={t('common.field.name')} value={previewBrand.name} />
                    <SheetPreviewField label={t('common.field.tradeName')} value={previewBrand.tradeName || '-'} />
                    <SheetPreviewField label={t('common.field.document')} value={previewBrand.document || '-'} />
                    <SheetPreviewField label={t('common.field.contact')} value={previewBrand.contactName || '-'} />
                    <SheetPreviewField label={t('common.field.email')} value={previewBrand.contactEmail || '-'} />
                    <SheetPreviewField className="sm:col-span-2" label={t('common.field.notes')} value={previewBrand.notes || '-'} />
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
