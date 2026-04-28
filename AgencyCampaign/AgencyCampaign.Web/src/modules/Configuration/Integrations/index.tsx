import { useEffect, useState } from 'react'
import {
  Badge,
  DataTable,
  PageLayout,
  Tabs,
  TabsList,
  TabsTrigger,
  TabsContent,
  useApi,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  SearchableSelect,
} from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Plug, GitBranch, Settings2, Pencil } from 'lucide-react'
import ConnectorConfigModal from '../../../components/modals/ConnectorConfigModal'
import { integrationPlataformService } from '../../../services/integrationPlataformService'
import type {
  IntegrationCategory,
  IntegrationPlataformIntegration,
  Connector,
} from '../../../types/integrationPlataform'

export default function Integrations() {
  const [activeTab, setActiveTab] = useState('connectors')

  // Conectores tab
  const [categories, setCategories] = useState<IntegrationCategory[]>([])
  const [selectedCategoryId, setSelectedCategoryId] = useState<number | null>(null)
  const [integrations, setIntegrations] = useState<IntegrationPlataformIntegration[]>([])
  const [connectors, setConnectors] = useState<Connector[]>([])
  const [selectedIntegration, setSelectedIntegration] = useState<IntegrationPlataformIntegration | null>(null)
  const [selectedConnector, setSelectedConnector] = useState<Connector | null>(null)
  const [isConnectorModalOpen, setIsConnectorModalOpen] = useState(false)
  const [connectorModalMode, setConnectorModalMode] = useState<'create' | 'edit'>('create')

  const { execute: fetchCategories, loading: loadingCategories } = useApi<IntegrationCategory[]>({ showErrorMessage: true })
  const { execute: fetchIntegrations, loading: loadingIntegrations } = useApi<IntegrationPlataformIntegration[]>({ showErrorMessage: true })
  const { execute: fetchConnectors, loading: loadingConnectors } = useApi<Connector[]>({ showErrorMessage: true })

  useEffect(() => {
    void loadCategories()
  }, [])

  useEffect(() => {
    if (selectedCategoryId) {
      void loadIntegrationsByCategory(selectedCategoryId)
    } else {
      setIntegrations([])
    }
  }, [selectedCategoryId])

  const loadCategories = async () => {
    const result = await fetchCategories(() => integrationPlataformService.getActiveIntegrationCategories())
    if (result) setCategories(result)
  }

  const loadIntegrationsByCategory = async (categoryId: number) => {
    const result = await fetchIntegrations(() =>
      integrationPlataformService.getIntegrationsByCategory(categoryId)
    )
    if (result) {
      setIntegrations(result)
      // Also load connectors for all integrations
      const allConnectors: Connector[] = []
      for (const integ of result) {
        const connResult = await integrationPlataformService.getConnectorsByIntegration(integ.id)
        allConnectors.push(...connResult)
      }
      setConnectors(allConnectors)
    }
  }

  const connectorColumns: DataTableColumn<Connector>[] = [
    { key: 'name', title: 'Nome', dataIndex: 'name' },
    {
      key: 'integrationId',
      title: 'Integração',
      dataIndex: 'integrationId',
      render: (value: number) => {
        const integ = integrations.find((i) => i.id === value)
        return <span>{integ?.name || `ID: ${value}`}</span>
      },
    },
    {
      key: 'isActive',
      title: 'Ativo',
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? 'Sim' : 'Não'}
        </Badge>
      ),
    },
    {
      key: 'actions',
      title: 'Ações',
      dataIndex: 'id',
      render: (_value: number, record: Connector) => (
        <Button
          size="sm"
          variant="secondary"
          onClick={() => {
            const integ = integrations.find((i) => i.id === record.integrationId)
            setSelectedIntegration(integ ?? null)
            setSelectedConnector(record)
            setConnectorModalMode('edit')
            setIsConnectorModalOpen(true)
          }}
        >
          <Pencil size={14} className="mr-1" />
          Editar
        </Button>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title="Integrações"
        subtitle="Configure conectores e execute pipelines do IntegrationPlataform"
      >
        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList>
            <TabsTrigger
              value="connectors"
              className="gap-2 data-[state=active]:bg-primary/15 data-[state=active]:text-primary data-[state=active]:shadow-sm"
            >
              <Plug size={16} />
              Conectores
            </TabsTrigger>
            <TabsTrigger
              value="pipelines"
              className="gap-2 data-[state=active]:bg-primary/15 data-[state=active]:text-primary data-[state=active]:shadow-sm"
            >
              <GitBranch size={16} />
              Pipelines
            </TabsTrigger>
          </TabsList>

          <TabsContent value="connectors" className="space-y-4">
            <div className="space-y-2 max-w-sm">
              <label className="text-sm font-medium">Categoria</label>
              <SearchableSelect
                options={categories.map((cat) => ({
                  value: String(cat.id),
                  label: cat.name,
                }))}
                value={selectedCategoryId ? String(selectedCategoryId) : undefined}
                onValueChange={(value) => setSelectedCategoryId(Number(value))}
                placeholder={loadingCategories ? 'Carregando...' : 'Selecione uma categoria'}
                emptyMessage="Nenhuma categoria encontrada"
              />
            </div>

            {selectedCategoryId && integrations.length > 0 && (
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {integrations.map((integ) => (
                  <Card key={integ.id} className="cursor-pointer hover:border-primary/50 transition-colors">
                    <CardHeader className="pb-2">
                      <CardTitle className="text-base">{integ.name}</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-2">
                      {integ.description && (
                        <p className="text-xs text-muted-foreground line-clamp-2">
                          {integ.description}
                        </p>
                      )}
                      <div className="flex items-center justify-between">
                        <Badge variant="outline">{integ.identifier}</Badge>
                        <Button
                          size="sm"
                          onClick={() => {
                            setSelectedIntegration(integ)
                            setSelectedConnector(null)
                            setConnectorModalMode('create')
                            setIsConnectorModalOpen(true)
                          }}
                        >
                          <Settings2 size={14} className="mr-1" />
                          Configurar
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}

            {selectedCategoryId && integrations.length === 0 && !loadingIntegrations && (
              <p className="text-sm text-muted-foreground">
                Nenhuma integração encontrada para esta categoria.
              </p>
            )}

            {connectors.length > 0 && (
              <div className="space-y-2">
                <h3 className="text-sm font-semibold">Conectores configurados</h3>
                <DataTable
                  columns={connectorColumns}
                  data={connectors}
                  rowKey="id"
                  emptyText="Nenhum conector configurado"
                  loading={loadingConnectors}
                  pageSize={10}
                />
              </div>
            )}
          </TabsContent>

          <TabsContent value="pipelines" className="space-y-4">
            <p className="text-sm text-muted-foreground">Pipelines em desenvolvimento.</p>
          </TabsContent>
        </Tabs>
      </PageLayout>

      <ConnectorConfigModal
        open={isConnectorModalOpen}
        onOpenChange={setIsConnectorModalOpen}
        integration={selectedIntegration}
        connector={selectedConnector}
        onSuccess={() => {
          if (selectedCategoryId) {
            void loadIntegrationsByCategory(selectedCategoryId)
          }
          setSelectedConnector(null)
        }}
      />
    </>
  )
}
