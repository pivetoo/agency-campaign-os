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
import AutomationFormModal from '../../../components/modals/AutomationFormModal'
import AutomationList from '../Automations/AutomationList'
import { integrationPlatformService } from '../../../services/integrationPlatformService'
import type {
  IntegrationCategory,
  IntegrationPlatformIntegration,
  Connector,
} from '../../../types/integrationPlatform'
import type { Automation } from '../../../types/automation'

export default function Integrations() {
  const [activeTab, setActiveTab] = useState('connectors')

  // Conectores tab
  const [categories, setCategories] = useState<IntegrationCategory[]>([])
  const [selectedCategoryId, setSelectedCategoryId] = useState<number | null>(null)
  const [integrations, setIntegrations] = useState<IntegrationPlatformIntegration[]>([])
  const [connectors, setConnectors] = useState<Connector[]>([])
  const [selectedIntegration, setSelectedIntegration] = useState<IntegrationPlatformIntegration | null>(null)
  const [selectedConnector, setSelectedConnector] = useState<Connector | null>(null)
  const [isConnectorModalOpen, setIsConnectorModalOpen] = useState(false)
  const [, setConnectorModalMode] = useState<'create' | 'edit'>('create')

  const [isAutomationModalOpen, setIsAutomationModalOpen] = useState(false)
  const [selectedAutomation, setSelectedAutomation] = useState<Automation | null>(null)
  const [automationsRefreshKey, setAutomationsRefreshKey] = useState(0)

  const { execute: fetchCategories, loading: loadingCategories } = useApi<IntegrationCategory[]>({ showErrorMessage: true })
  const { execute: fetchIntegrations, loading: loadingIntegrations } = useApi<IntegrationPlatformIntegration[]>({ showErrorMessage: true })
  const { loading: loadingConnectors } = useApi<Connector[]>({ showErrorMessage: true })

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
    const result = await fetchCategories(() => integrationPlatformService.getActiveIntegrationCategories())
    if (result) setCategories(result)
  }

  const loadIntegrationsByCategory = async (categoryId: number) => {
    const result = await fetchIntegrations(() =>
      integrationPlatformService.getIntegrationsByCategory(categoryId)
    )
    if (result) {
      setIntegrations(result)
      // Also load connectors for all integrations
      const allConnectors: Connector[] = []
      for (const integ of result) {
        const connResult = await integrationPlatformService.getConnectorsByIntegration(integ.id)
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
        subtitle="Configure conectores e automações do Kanvas"
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
              value="automations"
              className="gap-2 data-[state=active]:bg-primary/15 data-[state=active]:text-primary data-[state=active]:shadow-sm"
            >
              <GitBranch size={16} />
              Automações
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
              />
            </div>

            {selectedCategoryId && integrations.length > 0 && (
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {integrations.map((integ) => {
                  const integConnectors = connectors.filter((c) => c.integrationId === integ.id)
                  const activeCount = integConnectors.filter((c) => c.isActive).length
                  return (
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
                          <div className="flex flex-col gap-1">
                            <Badge variant="outline">{integ.identifier}</Badge>
                            <span className="text-[10px] text-muted-foreground">
                              {integConnectors.length === 0
                                ? 'Sem conectores'
                                : `${activeCount} ativo${activeCount === 1 ? '' : 's'} · ${integConnectors.length} total`}
                            </span>
                          </div>
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
                  )
                })}
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

          <TabsContent value="automations" className="space-y-4">
            <AutomationList
              key={automationsRefreshKey}
              onCreate={() => { setSelectedAutomation(null); setIsAutomationModalOpen(true) }}
              onEdit={(automation: Automation) => { setSelectedAutomation(automation); setIsAutomationModalOpen(true) }}
            />
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

      <AutomationFormModal
        open={isAutomationModalOpen}
        onOpenChange={setIsAutomationModalOpen}
        automation={selectedAutomation}
        onSuccess={() => {
          setIsAutomationModalOpen(false)
          setSelectedAutomation(null)
          setAutomationsRefreshKey((prev) => prev + 1)
        }}
      />
    </>
  )
}
