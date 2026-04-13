import { useEffect, useState } from 'react'
import { PageLayout, DataTable, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { creatorService } from '../../services/creatorService'
import type { Creator } from '../../types/creator'
import CreatorFormModal from '../../components/modals/CreatorFormModal'

export default function Creators() {
  const [creators, setCreators] = useState<Creator[]>([])
  const [selectedCreator, setSelectedCreator] = useState<Creator | null>(null)
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
  ]

  return (
    <>
      <PageLayout
        title="Influenciadores"
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
    </>
  )
}
