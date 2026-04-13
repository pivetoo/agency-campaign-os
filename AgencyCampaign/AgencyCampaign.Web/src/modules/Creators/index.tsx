import { useEffect, useState } from 'react'
import { PageLayout, DataTable, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { creatorService } from '../../services/creatorService'
import type { Creator } from '../../types/creator'

export default function Creators() {
  const [creators, setCreators] = useState<Creator[]>([])
  const { execute: fetchCreators, loading } = useApi<Creator[]>({ showErrorMessage: true })

  useEffect(() => {
    void fetchCreators(() => creatorService.getAll()).then((result) => {
      if (result) {
        setCreators(result)
      }
    })
  }, [])

  const columns: DataTableColumn<Creator>[] = [
    { key: 'name', title: 'Nome', dataIndex: 'name' },
    { key: 'email', title: 'E-mail', dataIndex: 'email' },
    { key: 'phone', title: 'Telefone', dataIndex: 'phone' },
    { key: 'document', title: 'Documento', dataIndex: 'document' },
  ]

  return (
    <PageLayout title="Influenciadores" onRefresh={() => void fetchCreators(() => creatorService.getAll()).then((result) => result && setCreators(result))}>
      <DataTable
        columns={columns}
        data={creators}
        rowKey="id"
        selectedRows={[]}
        onSelectionChange={() => {}}
        emptyText="Nenhum influenciador cadastrado"
        loading={loading}
      />
    </PageLayout>
  )
}
