import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Ban, CheckCircle2 } from 'lucide-react'
import { brandService } from '../../services/brandService'
import type { Brand } from '../../types/brand'
import BrandFormModal from '../../components/modals/BrandFormModal'

const emptyBrand: Brand = {
  id: 0,
  name: '',
  isActive: false,
  createdAt: '',
}

function withBrandStatus(brand: Brand, isActive: boolean): Brand {
  return {
    ...emptyBrand,
    ...brand,
    isActive,
  }
}

function toUpdateRequest(brand: Brand) {
  return {
    id: brand.id,
    name: brand.name,
    tradeName: brand.tradeName,
    document: brand.document,
    contactName: brand.contactName,
    contactEmail: brand.contactEmail,
    notes: brand.notes,
    isActive: brand.isActive,
  }
}

export default function Brands() {
  const [brands, setBrands] = useState<Brand[]>([])
  const [selectedBrand, setSelectedBrand] = useState<Brand | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchBrands, loading } = useApi<Brand[]>({ showErrorMessage: true })
  const { execute: executeUpdate, loading: updating } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const loadBrands = async () => {
    const result = await fetchBrands(() => brandService.getAll())
    if (result) {
      setBrands(result)
    }
  }

  useEffect(() => {
    void loadBrands()
  }, [])

  const handleToggleActive = async () => {
    if (!selectedBrand) {
      return
    }

    const result = await executeUpdate(() => brandService.update(selectedBrand.id, toUpdateRequest(withBrandStatus(selectedBrand, !selectedBrand.isActive))))
    if (result !== null) {
      setSelectedBrand(null)
      void loadBrands()
    }
  }

  const columns: DataTableColumn<Brand>[] = [
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
        actions={[
          {
            key: 'toggle-active',
            label: selectedBrand?.isActive ? 'Inativar' : 'Ativar',
            icon: selectedBrand?.isActive ? <Ban className="h-4 w-4" /> : <CheckCircle2 className="h-4 w-4" />,
            variant: selectedBrand?.isActive ? 'outline-danger' : 'outline-success',
            onClick: () => { void handleToggleActive() },
            disabled: !selectedBrand || updating,
          },
        ]}
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
