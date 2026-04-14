import { useEffect, useState } from 'react'
import { PageLayout, DataTable, useApi, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, Badge } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Ban, CheckCircle2 } from 'lucide-react'
import { creatorService } from '../../services/creatorService'
import type { Creator } from '../../types/creator'
import CreatorFormModal from '../../components/modals/CreatorFormModal'

const emptyCreator: Creator = {
  id: 0,
  name: '',
  defaultAgencyFeePercent: 0,
  isActive: false,
  createdAt: '',
}

function withCreatorStatus(creator: Creator, isActive: boolean): Creator {
  return {
    ...emptyCreator,
    ...creator,
    isActive,
  }
}

function toUpdateRequest(creator: Creator) {
  return {
    id: creator.id,
    name: creator.name,
    stageName: creator.stageName,
    email: creator.email,
    phone: creator.phone,
    document: creator.document,
    pixKey: creator.pixKey,
    primaryNiche: creator.primaryNiche,
    city: creator.city,
    state: creator.state,
    notes: creator.notes,
    defaultAgencyFeePercent: creator.defaultAgencyFeePercent,
    isActive: creator.isActive,
  }
}

export default function Creators() {
  const [creators, setCreators] = useState<Creator[]>([])
  const [selectedCreator, setSelectedCreator] = useState<Creator | null>(null)
  const [previewCreator, setPreviewCreator] = useState<Creator | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { execute: fetchCreators, loading } = useApi<Creator[]>({ showErrorMessage: true })
  const { execute: executeUpdate, loading: updating } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const loadCreators = async () => {
    const result = await fetchCreators(() => creatorService.getAll())
    if (result) {
      setCreators(result)
    }
  }

  useEffect(() => {
    void loadCreators()
  }, [])

  const handleToggleActive = async () => {
    if (!selectedCreator) {
      return
    }

    const result = await executeUpdate(() => creatorService.update(selectedCreator.id, toUpdateRequest(withCreatorStatus(selectedCreator, !selectedCreator.isActive))))
    if (result !== null) {
      setSelectedCreator(null)
      void loadCreators()
    }
  }

  const columns: DataTableColumn<Creator>[] = [
    { key: 'name', title: 'Nome', dataIndex: 'name' },
    { key: 'stageName', title: 'Nome artístico', dataIndex: 'stageName', render: (value?: string) => value || '-' },
    { key: 'primaryNiche', title: 'Nicho', dataIndex: 'primaryNiche', render: (value?: string) => value || '-' },
    { key: 'city', title: 'Cidade', dataIndex: 'city', render: (value?: string) => value || '-' },
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
        title="Creators"
        subtitle="Cadastre e acompanhe a base de creators da agência"
        onAdd={() => { setSelectedCreator(null); setIsFormOpen(true) }}
        onEdit={() => selectedCreator && setIsFormOpen(true)}
        onRefresh={() => void loadCreators()}
        selectedRowsCount={selectedCreator ? 1 : 0}
        actions={[
          {
            key: 'toggle-active',
            label: selectedCreator?.isActive ? 'Inativar' : 'Ativar',
            icon: selectedCreator?.isActive ? <Ban className="h-4 w-4" /> : <CheckCircle2 className="h-4 w-4" />,
            variant: selectedCreator?.isActive ? 'outline-danger' : 'outline-success',
            onClick: () => { void handleToggleActive() },
            disabled: !selectedCreator || updating,
          },
        ]}
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
                title={previewCreator.stageName || previewCreator.name}
                meta={
                  <Badge variant={previewCreator.isActive ? 'success' : 'destructive'}>
                    {previewCreator.isActive ? 'Ativo' : 'Inativo'}
                  </Badge>
                }
                description="Resumo rápido do creator selecionado"
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title="Dados principais" description="Informações cadastrais e operacionais do creator">
                  <SheetPreviewGrid>
                    <SheetPreviewField label="Nome" value={previewCreator.name} />
                    <SheetPreviewField label="Nome artístico" value={previewCreator.stageName || '-'} />
                    <SheetPreviewField label="Documento" value={previewCreator.document || '-'} />
                    <SheetPreviewField label="Telefone" value={previewCreator.phone || '-'} />
                    <SheetPreviewField label="Chave PIX" value={previewCreator.pixKey || '-'} />
                    <SheetPreviewField label="Nicho" value={previewCreator.primaryNiche || '-'} />
                    <SheetPreviewField label="Cidade" value={previewCreator.city || '-'} />
                    <SheetPreviewField label="Estado" value={previewCreator.state || '-'} />
                    <SheetPreviewField label="Fee padrão (%)" value={previewCreator.defaultAgencyFeePercent.toFixed(2)} />
                    <SheetPreviewField className="sm:col-span-2" label="E-mail" value={previewCreator.email || '-'} />
                    <SheetPreviewField className="sm:col-span-2" label="Observações" value={previewCreator.notes || '-'} />
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
