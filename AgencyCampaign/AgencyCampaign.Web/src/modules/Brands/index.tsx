import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'

import { brandService, resolveBrandLogoUrl } from '../../services/brandService'
import type { Brand } from '../../types/brand'
import BrandFormModal from '../../components/modals/BrandFormModal'

export default function Brands() {
  const [brands, setBrands] = useState<Brand[]>([])
  const [selectedBrand, setSelectedBrand] = useState<Brand | null>(null)
  const [previewBrand, setPreviewBrand] = useState<Brand | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchBrands, loading } = useApi<Brand[]>({ showErrorMessage: true })

  const loadBrands = async () => {
    const result = await fetchBrands(() => brandService.getAll())
    if (result) {
      setBrands(result)
    }
  }

  useEffect(() => {
    void loadBrands()
  }, [])

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
    { key: 'name', title: 'Nome', dataIndex: 'name' },
    { key: 'tradeName', title: 'Nome fantasia', dataIndex: 'tradeName', render: (value?: string) => value || '-' },
    { key: 'document', title: 'Documento', dataIndex: 'document', render: (value?: string) => value || '-' },
    { key: 'contactName', title: 'Contato', dataIndex: 'contactName' },
    { key: 'contactEmail', title: 'E-mail', dataIndex: 'contactEmail' },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? 'Ativa' : 'Inativa'}
        </Badge>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title="Marcas"
        subtitle="Cadastre e gerencie as marcas atendidas pela agência"
        onAdd={() => { setSelectedBrand(null); setIsFormOpen(true) }}
        onEdit={() => selectedBrand && setIsFormOpen(true)}
        onRefresh={() => void loadBrands()}
        selectedRowsCount={selectedBrand ? 1 : 0}
      >
        <DataTable
          columns={columns}
          data={brands}
          rowKey="id"
          selectedRows={selectedBrand ? [selectedBrand] : []}
          onSelectionChange={(rows) => setSelectedBrand(rows[0] ?? null)}
          onRowDoubleClick={setPreviewBrand}
          emptyText="Nenhuma marca cadastrada"
          loading={loading}
          pageSize={5}
          pageSizeOptions={[5, 10, 20, 50]}
        />
      </PageLayout>

      <BrandFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        brand={selectedBrand}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedBrand(null)
          void loadBrands()
        }}
      />

      <Sheet open={!!previewBrand} onOpenChange={(open) => !open && setPreviewBrand(null)}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewBrand ? (
            <div className="flex h-full flex-col">
              <SheetPreviewHeader
                title={previewBrand.name}
                meta={
                  <Badge variant={previewBrand.isActive ? 'success' : 'destructive'}>
                    {previewBrand.isActive ? 'Ativa' : 'Inativa'}
                  </Badge>
                }
                description="Resumo rápido da marca selecionada"
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title="Dados principais" description="Informações cadastrais da marca">
                  <SheetPreviewGrid>
                    <SheetPreviewField label="Nome" value={previewBrand.name} />
                    <SheetPreviewField label="Nome fantasia" value={previewBrand.tradeName || '-'} />
                    <SheetPreviewField label="Documento" value={previewBrand.document || '-'} />
                    <SheetPreviewField label="Contato" value={previewBrand.contactName || '-'} />
                    <SheetPreviewField label="E-mail" value={previewBrand.contactEmail || '-'} />
                    <SheetPreviewField className="sm:col-span-2" label="Observações" value={previewBrand.notes || '-'} />
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
