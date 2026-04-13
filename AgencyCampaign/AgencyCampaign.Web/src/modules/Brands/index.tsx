import { useEffect, useState } from 'react'
import { PageLayout, DataTable, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { brandService } from '../../services/brandService'
import type { Brand } from '../../types/brand'

export default function Brands() {
  const [brands, setBrands] = useState<Brand[]>([])
  const { execute: fetchBrands, loading } = useApi<Brand[]>({ showErrorMessage: true })

  useEffect(() => {
    void fetchBrands(() => brandService.getAll()).then((result) => {
      if (result) {
        setBrands(result)
      }
    })
  }, [])

  const columns: DataTableColumn<Brand>[] = [
    { key: 'name', title: 'Nome', dataIndex: 'name' },
    { key: 'contactName', title: 'Contato', dataIndex: 'contactName' },
    { key: 'contactEmail', title: 'E-mail', dataIndex: 'contactEmail' },
  ]

  return (
    <PageLayout title="Marcas" onRefresh={() => void fetchBrands(() => brandService.getAll()).then((result) => result && setBrands(result))}>
      <DataTable
        columns={columns}
        data={brands}
        rowKey="id"
        selectedRows={[]}
        onSelectionChange={() => {}}
        emptyText="Nenhuma marca cadastrada"
        loading={loading}
      />
    </PageLayout>
  )
}
