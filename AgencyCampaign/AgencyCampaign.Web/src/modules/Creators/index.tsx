import { useEffect, useState } from 'react'
import { PageLayout, DataTable, useApi, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, Badge } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { creatorService } from '../../services/creatorService'
import type { Creator } from '../../types/creator'
import CreatorFormModal from '../../components/modals/CreatorFormModal'

export default function Creators() {
  const [creators, setCreators] = useState<Creator[]>([])
  const [selectedCreator, setSelectedCreator] = useState<Creator | null>(null)
  const [previewCreator, setPreviewCreator] = useState<Creator | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchCreators, loading } = useApi<Creator[]>({ showErrorMessage: true })

  const loadCreators = async () => {
    const result = await fetchCreators(() => creatorService.getAll())
    if (result) {
      setCreators(result)
    }
  }

  useEffect(() => {
    void loadCreators()
  }, [])

  const columns: DataTableColumn<Creator>[] = [
    { key: 'name', title: 'Nome', dataIndex: 'name' },
    { key: 'email', title: 'E-mail', dataIndex: 'email' },
    { key: 'phone', title: 'Telefone', dataIndex: 'phone' },
    { key: 'document', title: 'Documento', dataIndex: 'document' },
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
        title="Influenciadores"
        subtitle="Cadastre e acompanhe a base de creators da agência"
        onAdd={() => { setSelectedCreator(null); setIsFormOpen(true) }}
        onEdit={() => selectedCreator && setIsFormOpen(true)}
        onRefresh={() => void loadCreators()}
        selectedRowsCount={selectedCreator ? 1 : 0}
      >
        <DataTable
          columns={columns}
          data={creators}
          rowKey="id"
          selectedRows={selectedCreator ? [selectedCreator] : []}
          onSelectionChange={(rows) => setSelectedCreator(rows[0] ?? null)}
          onRowDoubleClick={setPreviewCreator}
          emptyText="Nenhum influenciador cadastrado"
          loading={loading}
        />
      </PageLayout>

      <CreatorFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        creator={selectedCreator}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedCreator(null)
          void loadCreators()
        }}
      />

      <Sheet open={!!previewCreator} onOpenChange={(open) => !open && setPreviewCreator(null)}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewCreator ? (
            <div className="flex h-full flex-col">
              <SheetPreviewHeader
                title={previewCreator.name}
                meta={
                  <Badge variant={previewCreator.isActive ? 'success' : 'destructive'}>
                    {previewCreator.isActive ? 'Ativo' : 'Inativo'}
                  </Badge>
                }
                description="Resumo rápido do influenciador selecionado"
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title="Dados principais" description="Informações cadastrais do influenciador">
                  <SheetPreviewGrid>
                    <SheetPreviewField label="Nome" value={previewCreator.name} />
                    <SheetPreviewField label="Documento" value={previewCreator.document || '-'} />
                    <SheetPreviewField label="Telefone" value={previewCreator.phone || '-'} />
                    <SheetPreviewField label="Chave PIX" value={previewCreator.pixKey || '-'} />
                    <SheetPreviewField className="sm:col-span-2" label="E-mail" value={previewCreator.email || '-'} />
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
