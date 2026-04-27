import { useEffect, useState } from 'react'
import { Badge, DataTable, PageLayout, Tabs, TabsList, TabsTrigger, TabsContent, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Plug, GitBranch, ScrollText } from 'lucide-react'
import IntegrationFormModal from '../../../components/modals/IntegrationFormModal'
import IntegrationPipelineFormModal from '../../../components/modals/IntegrationPipelineFormModal'
import { integrationService } from '../../../services/integrationService'
import { integrationPipelineService } from '../../../services/integrationPipelineService'
import { integrationLogService } from '../../../services/integrationLogService'
import { integrationCategoryService } from '../../../services/integrationCategoryService'
import type { Integration, IntegrationPipeline, IntegrationLog } from '../../../types/integration'

const statusLabels: Record<number, string> = {
  0: 'Pendente',
  1: 'Sucesso',
  2: 'Falha',
}

export default function Integrations() {
  const [activeTab, setActiveTab] = useState('integrations')
  const [integrations, setIntegrations] = useState<Integration[]>([])
  const [pipelines, setPipelines] = useState<IntegrationPipeline[]>([])
  const [logs, setLogs] = useState<IntegrationLog[]>([])
  const [selectedIntegration, setSelectedIntegration] = useState<Integration | null>(null)
  const [selectedPipeline, setSelectedPipeline] = useState<IntegrationPipeline | null>(null)
  const [isIntegrationFormOpen, setIsIntegrationFormOpen] = useState(false)
  const [isPipelineFormOpen, setIsPipelineFormOpen] = useState(false)
  const [categoryNames, setCategoryNames] = useState<Record<number, string>>({})

  const { execute: fetchIntegrations, loading: loadingIntegrations } = useApi<Integration[]>({ showErrorMessage: true })
  const { execute: fetchPipelines, loading: loadingPipelines } = useApi<IntegrationPipeline[]>({ showErrorMessage: true })
  const { execute: fetchLogs, loading: loadingLogs } = useApi<IntegrationLog[]>({ showErrorMessage: true })

  const loadIntegrations = async () => {
    const result = await fetchIntegrations(() => integrationService.getAll())
    if (result) setIntegrations(result)
  }

  const loadPipelines = async () => {
    const result = await fetchPipelines(() => integrationPipelineService.getAll())
    if (result) setPipelines(result)
  }

  const loadLogs = async () => {
    const result = await fetchLogs(() => integrationLogService.getAll())
    if (result) setLogs(result)
  }

  useEffect(() => {
    void loadIntegrations()
    void loadPipelines()
    void loadLogs()
  }, [])

  useEffect(() => {
    if (integrations.length === 0) return

    const uniqueCategoryIds = [...new Set(integrations.map((i) => i.categoryId).filter((id) => id > 0))]
    if (uniqueCategoryIds.length === 0) return

    integrationCategoryService.getActive().then((categories) => {
      const names: Record<number, string> = {}
      categories.forEach((cat) => {
        names[cat.id] = cat.name
      })
      setCategoryNames(names)
    }).catch(() => {
      // ignora erro silenciosamente - a tabela mostra o ID como fallback
    })
  }, [integrations])

  const integrationColumns: DataTableColumn<Integration>[] = [
    { key: 'identifier', title: 'Identificador', dataIndex: 'identifier' },
    { key: 'name', title: 'Nome', dataIndex: 'name' },
    {
      key: 'categoryId',
      title: 'Categoria',
      dataIndex: 'categoryId',
      render: (value: number) => <span>{categoryNames[value] || `ID: ${value}`}</span>,
    },
    { key: 'isActive', title: 'Ativo', dataIndex: 'isActive', render: (value: boolean) => <Badge variant={value ? 'success' : 'destructive'}>{value ? 'Sim' : 'Não'}</Badge> },
  ]

  const pipelineColumns: DataTableColumn<IntegrationPipeline>[] = [
    { key: 'integrationName', title: 'Integração', dataIndex: 'integrationName' },
    { key: 'identifier', title: 'Identificador', dataIndex: 'identifier' },
    { key: 'name', title: 'Nome', dataIndex: 'name' },
    { key: 'isActive', title: 'Ativo', dataIndex: 'isActive', render: (value: boolean) => <Badge variant={value ? 'success' : 'destructive'}>{value ? 'Sim' : 'Não'}</Badge> },
  ]

  const logColumns: DataTableColumn<IntegrationLog>[] = [
    { key: 'integrationName', title: 'Integracao', dataIndex: 'integrationName' },
    { key: 'integrationPipelineName', title: 'Pipeline', dataIndex: 'integrationPipelineName' },
    { key: 'status', title: 'Status', dataIndex: 'status', render: (value: number) => <Badge variant={value === 1 ? 'success' : value === 2 ? 'destructive' : 'outline'}>{statusLabels[value] || 'Pendente'}</Badge> },
    { key: 'durationMs', title: 'Duracao (ms)', dataIndex: 'durationMs', render: (value?: number) => value ? `${value}ms` : '-' },
    { key: 'errorMessage', title: 'Erro', dataIndex: 'errorMessage', render: (value?: string) => value ? <span className="text-destructive text-xs max-w-[200px] truncate inline-block" title={value}>{value}</span> : '-' },
    { key: 'createdAt', title: 'Data', dataIndex: 'createdAt', render: (value: string) => new Date(value).toLocaleString('pt-BR') },
  ]

  const handleTabChange = (value: string) => {
    setActiveTab(value)
    setSelectedIntegration(null)
    setSelectedPipeline(null)
  }

  return (
    <>
      <PageLayout
        title="Integrações"
        subtitle="Configure as integrações e pipelines do sistema"
        onAdd={() => {
          if (activeTab === 'integrations') { setSelectedIntegration(null); setIsIntegrationFormOpen(true) }
          else if (activeTab === 'pipelines') { setSelectedPipeline(null); setIsPipelineFormOpen(true) }
        }}
        onEdit={() => {
          if (activeTab === 'integrations' && selectedIntegration) setIsIntegrationFormOpen(true)
          else if (activeTab === 'pipelines' && selectedPipeline) setIsPipelineFormOpen(true)
        }}
        onRefresh={() => {
          if (activeTab === 'integrations') void loadIntegrations()
          else if (activeTab === 'pipelines') void loadPipelines()
          else void loadLogs()
        }}
        selectedRowsCount={activeTab === 'integrations' && selectedIntegration ? 1 : activeTab === 'pipelines' && selectedPipeline ? 1 : 0}
      >
        <Tabs value={activeTab} onValueChange={handleTabChange} className="mt-4">
          <TabsList>
            <TabsTrigger value="integrations"><Plug size={16} className="mr-2" />Integrações</TabsTrigger>
            <TabsTrigger value="pipelines"><GitBranch size={16} className="mr-2" />Pipelines</TabsTrigger>
            <TabsTrigger value="logs"><ScrollText size={16} className="mr-2" />Logs</TabsTrigger>
          </TabsList>

          <TabsContent value="integrations" className="mt-4">
            <DataTable
              columns={integrationColumns}
              data={integrations}
              rowKey="id"
              selectedRows={selectedIntegration ? [selectedIntegration] : []}
              onSelectionChange={(rows) => setSelectedIntegration(rows[0] ?? null)}
              emptyText="Nenhuma integração configurada"
              loading={loadingIntegrations}
              pageSize={10}
              pageSizeOptions={[5, 10, 20, 50]}
            />
          </TabsContent>

          <TabsContent value="pipelines" className="mt-4">
            <DataTable
              columns={pipelineColumns}
              data={pipelines}
              rowKey="id"
              selectedRows={selectedPipeline ? [selectedPipeline] : []}
              onSelectionChange={(rows) => setSelectedPipeline(rows[0] ?? null)}
              emptyText="Nenhum pipeline configurado"
              loading={loadingPipelines}
              pageSize={10}
              pageSizeOptions={[5, 10, 20, 50]}
            />
          </TabsContent>

          <TabsContent value="logs" className="mt-4">
            <DataTable
              columns={logColumns}
              data={logs}
              rowKey="id"
              emptyText="Nenhum log de execução"
              loading={loadingLogs}
              pageSize={10}
              pageSizeOptions={[5, 10, 20, 50]}
            />
          </TabsContent>
        </Tabs>
      </PageLayout>

      <IntegrationFormModal
        open={isIntegrationFormOpen}
        onOpenChange={setIsIntegrationFormOpen}
        integration={selectedIntegration}
        onSuccess={() => {
          setIsIntegrationFormOpen(false)
          setSelectedIntegration(null)
          void loadIntegrations()
        }}
      />

      <IntegrationPipelineFormModal
        open={isPipelineFormOpen}
        onOpenChange={setIsPipelineFormOpen}
        pipeline={selectedPipeline}
        integrations={integrations}
        onSuccess={() => {
          setIsPipelineFormOpen(false)
          setSelectedPipeline(null)
          void loadPipelines()
        }}
      />
    </>
  )
}
