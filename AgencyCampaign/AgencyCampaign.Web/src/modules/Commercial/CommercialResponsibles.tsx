import { useEffect, useState } from 'react'
import { PageLayout, DataTable, Badge, useApi, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { commercialResponsibleService } from '../../services/commercialResponsibleService'
import type { CommercialResponsible } from '../../types/commercialResponsible'
import CommercialResponsibleFormModal from '../../components/modals/CommercialResponsibleFormModal'

export default function CommercialResponsibles() {
  const [responsibles, setResponsibles] = useState<CommercialResponsible[]>([])
  const [selectedResponsible, setSelectedResponsible] = useState<CommercialResponsible | null>(null)
  const [previewResponsible, setPreviewResponsible] = useState<CommercialResponsible | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchResponsibles, loading } = useApi<CommercialResponsible[]>({ showErrorMessage: true })

  const loadResponsibles = async () => {
    const result = await fetchResponsibles(() => commercialResponsibleService.getAll())
    if (result) {
      setResponsibles(result)
    }
  }

  useEffect(() => {
    void loadResponsibles()
  }, [])

  const columns: DataTableColumn<CommercialResponsible>[] = [
    { key: 'name', title: 'Nome', dataIndex: 'name' },
    { key: 'email', title: 'E-mail', dataIndex: 'email', render: (value?: string) => value || '-' },
    { key: 'phone', title: 'Telefone', dataIndex: 'phone', render: (value?: string) => value || '-' },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? 'Ativo' : 'Inativo'}
        </Badge>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title="Responsáveis Comerciais"
        subtitle="Cadastre e gerencie os responsáveis comerciais da agência"
        onAdd={() => { setSelectedResponsible(null); setIsFormOpen(true) }}
        onEdit={() => selectedResponsible && setIsFormOpen(true)}
        onRefresh={() => void loadResponsibles()}
        selectedRowsCount={selectedResponsible ? 1 : 0}
      >
        <DataTable
          columns={columns}
          data={responsibles}
          rowKey="id"
          selectedRows={selectedResponsible ? [selectedResponsible] : []}
          onSelectionChange={(rows) => setSelectedResponsible(rows[0] ?? null)}
          onRowDoubleClick={setPreviewResponsible}
          emptyText="Nenhum responsável comercial cadastrado"
          loading={loading}
          pageSize={5}
          pageSizeOptions={[5, 10, 20, 50]}
        />
      </PageLayout>

      <CommercialResponsibleFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        responsible={selectedResponsible}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedResponsible(null)
          void loadResponsibles()
        }}
      />

      <Sheet open={!!previewResponsible} onOpenChange={(open) => !open && setPreviewResponsible(null)}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewResponsible ? (
            <div className="flex h-full flex-col">
              <SheetPreviewHeader
                title={previewResponsible.name}
                meta={
                  <Badge variant={previewResponsible.isActive ? 'success' : 'destructive'}>
                    {previewResponsible.isActive ? 'Ativo' : 'Inativo'}
                  </Badge>
                }
                description="Resumo rápido do responsável comercial selecionado"
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title="Dados principais" description="Informações cadastrais do responsável">
                  <SheetPreviewGrid>
                    <SheetPreviewField label="Nome" value={previewResponsible.name} />
                    <SheetPreviewField label="E-mail" value={previewResponsible.email || '-'} />
                    <SheetPreviewField label="Telefone" value={previewResponsible.phone || '-'} />
                    <SheetPreviewField className="sm:col-span-2" label="Observações" value={previewResponsible.notes || '-'} />
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
