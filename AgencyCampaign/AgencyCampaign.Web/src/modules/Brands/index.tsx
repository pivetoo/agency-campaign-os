import { useEffect, useState } from 'react'
import { PageLayout, DataTable, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { brandService } from '../../services/brandService'
import type { Brand } from '../../types/brand'
import BrandFormModal from '../../components/modals/BrandFormModal'

export default function Brands() {
  const [brands, setBrands] = useState<Brand[]>([])
  const [selectedBrand, setSelectedBrand] = useState<Brand | null>(null)
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

  const columns: DataTableColumn<Brand>[] = [
    { key: 'name', title: 'Nome', dataIndex: 'name' },
    { key: 'contactName', title: 'Contato', dataIndex: 'contactName' },
    { key: 'contactEmail', title: 'E-mail', dataIndex: 'contactEmail' },
  ]

  return (
    <>
      <PageLayout
        title="Marcas"
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
          emptyText="Nenhuma marca cadastrada"
          loading={loading}
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
    </>
  )
}
