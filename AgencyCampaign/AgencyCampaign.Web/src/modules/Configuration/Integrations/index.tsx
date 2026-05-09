import { useEffect, useMemo, useState } from 'react'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  PageLayout,
  SearchableSelect,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  useApi,
} from 'archon-ui'
import { GitBranch, Pencil, Plug, Plus, Settings2, Workflow } from 'lucide-react'
import ConnectorConfigModal from '../../../components/modals/ConnectorConfigModal'
import AutomationFormModal from '../../../components/modals/AutomationFormModal'
import AutomationList from '../Automations/AutomationList'
import { integrationPlatformService } from '../../../services/integrationPlatformService'
import type {
  Connector,
  IntegrationCategory,
  IntegrationPlatformIntegration,
} from '../../../types/integrationPlatform'
import type { Automation } from '../../../types/automation'

export default function Integrations() {
  const [activeTab, setActiveTab] = useState('connectors')

  const [categories, setCategories] = useState<IntegrationCategory[]>([])
  const [selectedCategoryId, setSelectedCategoryId] = useState<number | null>(null)
  const [integrations, setIntegrations] = useState<IntegrationPlatformIntegration[]>([])
  const [connectors, setConnectors] = useState<Connector[]>([])
  const [selectedIntegrationId, setSelectedIntegrationId] = useState<number | null>(null)

  const [isConnectorModalOpen, setIsConnectorModalOpen] = useState(false)
  const [editingConnector, setEditingConnector] = useState<Connector | null>(null)

  const [isAutomationModalOpen, setIsAutomationModalOpen] = useState(false)
  const [selectedAutomation, setSelectedAutomation] = useState<Automation | null>(null)
  const [automationsRefreshKey, setAutomationsRefreshKey] = useState(0)

  const { execute: fetchCategories, loading: loadingCategories } = useApi<IntegrationCategory[]>({ showErrorMessage: true })
  const { execute: fetchIntegrations, loading: loadingIntegrations } = useApi<IntegrationPlatformIntegration[]>({ showErrorMessage: true })

  useEffect(() => {
    void loadCategories()
  }, [])

  useEffect(() => {
    if (selectedCategoryId) {
      void loadIntegrationsByCategory(selectedCategoryId)
    } else {
      setIntegrations([])
      setConnectors([])
      setSelectedIntegrationId(null)
    }
  }, [selectedCategoryId])

  const loadCategories = async () => {
    const result = await fetchCategories(() => integrationPlatformService.getActiveIntegrationCategories())
    if (result) setCategories(result)
  }

  const loadIntegrationsByCategory = async (categoryId: number) => {
    const result = await fetchIntegrations(() =>
      integrationPlatformService.getIntegrationsByCategory(categoryId),
    )
    if (!result) return

    setIntegrations(result)
    setSelectedIntegrationId((prev) => {
      if (prev && result.some((item) => item.id === prev)) return prev
      return result[0]?.id ?? null
    })

    const allConnectors: Connector[] = []
    for (const integration of result) {
      const connectorsForIntegration = await integrationPlatformService.getConnectorsByIntegration(integration.id)
      allConnectors.push(...connectorsForIntegration)
    }
    setConnectors(allConnectors)
  }

  const selectedCategory = useMemo(
    () => categories.find((category) => category.id === selectedCategoryId) ?? null,
    [categories, selectedCategoryId],
  )

  const selectedIntegration = useMemo(
    () => integrations.find((integration) => integration.id === selectedIntegrationId) ?? null,
    [integrations, selectedIntegrationId],
  )

  const connectorsForSelectedIntegration = useMemo(
    () => connectors.filter((connector) => connector.integrationId === selectedIntegrationId),
    [connectors, selectedIntegrationId],
  )

  const totalConnectors = connectors.length
  const activeConnectors = connectors.filter((connector) => connector.isActive).length

  const openCreateConnector = () => {
    setEditingConnector(null)
    setIsConnectorModalOpen(true)
  }

  const openEditConnector = (connector: Connector) => {
    setEditingConnector(connector)
    setIsConnectorModalOpen(true)
  }

  return (
    <>
      <PageLayout
        title="Integrações"
        subtitle="Configure conectores externos e automações disparadas pelos eventos do Kanvas"
      >
        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList className="mb-6 h-auto w-full justify-start gap-6 rounded-none border-b border-border bg-transparent p-0">
            <TabsTrigger
              value="connectors"
              className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none"
            >
              <Plug className="h-4 w-4" /> Conectores
            </TabsTrigger>
            <TabsTrigger
              value="automations"
              className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none"
            >
              <GitBranch className="h-4 w-4" /> Automações
            </TabsTrigger>
          </TabsList>

          <TabsContent value="connectors" className="space-y-4">
            <div className="grid grid-cols-2 gap-4 rounded-lg border bg-card p-4 md:grid-cols-4">
              <div className="space-y-1">
                <span className="text-xs text-muted-foreground">Categoria</span>
                <SearchableSelect
                  options={categories.map((category) => ({
                    value: String(category.id),
                    label: category.name,
                  }))}
                  value={selectedCategoryId ? String(selectedCategoryId) : undefined}
                  onValueChange={(value) => setSelectedCategoryId(value ? Number(value) : null)}
                  placeholder={loadingCategories ? 'Carregando...' : 'Selecione uma categoria'}
                />
              </div>
              <div className="space-y-1">
                <span className="text-xs text-muted-foreground">Integrações</span>
                <p className="font-medium">
                  {selectedCategory ? `${integrations.length} disponíveis` : '—'}
                </p>
              </div>
              <div className="space-y-1">
                <span className="text-xs text-muted-foreground">Conectores configurados</span>
                <p className="font-medium">{selectedCategory ? totalConnectors : '—'}</p>
              </div>
              <div className="space-y-1">
                <span className="text-xs text-muted-foreground">Ativos</span>
                <div className="mt-1">
                  {selectedCategory ? (
                    <Badge variant={activeConnectors > 0 ? 'success' : 'outline'}>
                      {activeConnectors} de {totalConnectors}
                    </Badge>
                  ) : (
                    <span className="text-sm text-muted-foreground">—</span>
                  )}
                </div>
              </div>
            </div>

            {!selectedCategory ? (
              <Card className="border-dashed">
                <CardContent className="flex flex-col items-center justify-center py-16 text-muted-foreground">
                  <Plug size={42} className="mb-3 opacity-50" />
                  <p className="text-base font-medium">Selecione uma categoria para começar</p>
                  <p className="mt-1 text-sm">
                    Categorias agrupam integrações por domínio (Email, Contratos, Pagamentos, etc.).
                  </p>
                </CardContent>
              </Card>
            ) : integrations.length === 0 && !loadingIntegrations ? (
              <Card className="border-dashed">
                <CardContent className="flex flex-col items-center justify-center py-16 text-muted-foreground">
                  <Workflow size={42} className="mb-3 opacity-50" />
                  <p className="text-base font-medium">Nenhuma integração nesta categoria</p>
                  <p className="mt-1 text-sm">
                    Cadastre integrações no IntegrationPlatform para que elas apareçam aqui.
                  </p>
                </CardContent>
              </Card>
            ) : (
              <div className="grid gap-4 lg:grid-cols-12">
                <Card className="lg:col-span-5">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-base">Integrações</CardTitle>
                    <p className="text-sm text-muted-foreground">
                      Selecione uma integração para ver e configurar seus conectores.
                    </p>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      {integrations.map((integration) => {
                        const integrationConnectors = connectors.filter(
                          (connector) => connector.integrationId === integration.id,
                        )
                        const integrationActive = integrationConnectors.filter((connector) => connector.isActive).length
                        const isSelected = selectedIntegrationId === integration.id

                        return (
                          <button
                            key={integration.id}
                            type="button"
                            onClick={() => setSelectedIntegrationId(integration.id)}
                            className={[
                              'w-full rounded-lg border bg-card p-3 text-left transition-all focus:outline-none focus:ring-2 focus:ring-primary/30',
                              isSelected
                                ? 'border-primary ring-1 ring-primary/20'
                                : 'border-border hover:border-primary/40 hover:bg-accent/30',
                            ].join(' ')}
                          >
                            <div className="flex items-start gap-3">
                              <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-primary">
                                <Plug size={16} />
                              </div>

                              <div className="min-w-0 flex-1">
                                <div className="flex items-center gap-2">
                                  <span className="truncate text-sm font-semibold text-foreground">
                                    {integration.name}
                                  </span>
                                  <Badge variant={integration.isActive ? 'success' : 'outline'}>
                                    {integration.isActive ? 'Ativa' : 'Inativa'}
                                  </Badge>
                                </div>
                                <div className="mt-1.5 flex flex-wrap gap-1.5 text-xs">
                                  <Badge variant="outline">{integration.identifier}</Badge>
                                  <Badge variant="outline">
                                    {integrationConnectors.length === 0
                                      ? 'Sem conectores'
                                      : `${integrationActive} ativo${integrationActive === 1 ? '' : 's'} · ${integrationConnectors.length} total`}
                                  </Badge>
                                </div>
                              </div>
                            </div>
                          </button>
                        )
                      })}
                    </div>
                  </CardContent>
                </Card>

                <Card className="lg:col-span-7">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-base">Conectores</CardTitle>
                    <p className="text-sm text-muted-foreground">
                      Cada conector é uma instância configurada da integração com credenciais próprias.
                    </p>
                  </CardHeader>
                  <CardContent>
                    {!selectedIntegration ? (
                      <div className="flex min-h-[240px] items-center justify-center rounded-lg border border-dashed text-sm text-muted-foreground">
                        Selecione uma integração à esquerda.
                      </div>
                    ) : (
                      <div className="space-y-4">
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <p className="text-lg font-semibold">{selectedIntegration.name}</p>
                            {selectedIntegration.description && (
                              <p className="mt-1 text-sm text-muted-foreground">
                                {selectedIntegration.description}
                              </p>
                            )}
                          </div>
                          <Button size="sm" onClick={openCreateConnector}>
                            <Plus size={14} className="mr-1.5" />
                            Novo conector
                          </Button>
                        </div>

                        <div className="flex flex-wrap gap-2">
                          <Badge variant={selectedIntegration.isActive ? 'success' : 'destructive'}>
                            {selectedIntegration.isActive ? 'Integração ativa' : 'Integração inativa'}
                          </Badge>
                          <Badge variant="outline">{selectedIntegration.identifier}</Badge>
                          <Badge variant="outline">
                            {connectorsForSelectedIntegration.length} conector{connectorsForSelectedIntegration.length === 1 ? '' : 'es'}
                          </Badge>
                        </div>

                        {connectorsForSelectedIntegration.length === 0 ? (
                          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-10 text-muted-foreground">
                            <Settings2 size={36} className="mb-3 opacity-50" />
                            <p className="text-sm font-medium">Nenhum conector configurado</p>
                            <p className="mt-1 text-xs">
                              Crie um conector para usar essa integração nas automações.
                            </p>
                            <Button size="sm" className="mt-3" onClick={openCreateConnector}>
                              <Plus size={14} className="mr-1.5" />
                              Criar primeiro conector
                            </Button>
                          </div>
                        ) : (
                          <div className="space-y-2">
                            {connectorsForSelectedIntegration.map((connector) => (
                              <div
                                key={connector.id}
                                className="flex items-center justify-between gap-3 rounded-lg border bg-card p-3"
                              >
                                <div className="min-w-0 flex-1">
                                  <div className="flex items-center gap-2">
                                    <span className="truncate text-sm font-semibold text-foreground">
                                      {connector.name}
                                    </span>
                                    <Badge variant={connector.isActive ? 'success' : 'outline'}>
                                      {connector.isActive ? 'Ativo' : 'Inativo'}
                                    </Badge>
                                  </div>
                                  {connector.systemApplicationId && (
                                    <p className="mt-1 text-xs text-muted-foreground">
                                      App: {connector.systemApplicationId}
                                    </p>
                                  )}
                                </div>
                                <div className="flex items-center gap-1">
                                  <Button
                                    size="sm"
                                    variant="ghost"
                                    onClick={() => openEditConnector(connector)}
                                  >
                                    <Pencil size={14} className="mr-1" />
                                    Editar
                                  </Button>
                                </div>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    )}
                  </CardContent>
                </Card>
              </div>
            )}
          </TabsContent>

          <TabsContent value="automations" className="space-y-4">
            <AutomationList
              key={automationsRefreshKey}
              onCreate={() => {
                setSelectedAutomation(null)
                setIsAutomationModalOpen(true)
              }}
              onEdit={(automation: Automation) => {
                setSelectedAutomation(automation)
                setIsAutomationModalOpen(true)
              }}
            />
          </TabsContent>
        </Tabs>
      </PageLayout>

      <ConnectorConfigModal
        open={isConnectorModalOpen}
        onOpenChange={setIsConnectorModalOpen}
        integration={selectedIntegration}
        connector={editingConnector}
        onSuccess={() => {
          if (selectedCategoryId) {
            void loadIntegrationsByCategory(selectedCategoryId)
          }
          setEditingConnector(null)
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
