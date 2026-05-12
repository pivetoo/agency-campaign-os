import { useEffect, useMemo, useState } from 'react'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  ConfirmModal,
  PageLayout,
  SearchableSelect,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  useApi,
  useI18n,
} from 'archon-ui'
import { CheckCircle2, CircleDashed, ExternalLink, GitBranch, Megaphone, Pause, Pencil, Play, Plug, Plus, Settings2, Sparkles, Trash2, TriangleAlert, Workflow, Zap } from 'lucide-react'
import ConnectorConfigModal from '../../../components/modals/ConnectorConfigModal'
import ConnectorTestModal from '../../../components/modals/ConnectorTestModal'
import AutomationFormModal from '../../../components/modals/AutomationFormModal'
import AutomationList from '../Automations/AutomationList'
import { integrationPlatformService } from '../../../services/integrationPlatformService'
import { automationService } from '../../../services/automationService'
import type {
  Connector,
  IntegrationCategory,
  IntegrationPlatformIntegration,
} from '../../../types/integrationPlatform'
import type { Automation } from '../../../types/automation'
import { automationTriggerLabels } from '../../../types/automationTrigger'

type IntegrationStatus = 'in_use' | 'configured' | 'not_configured'

interface StatusConfig {
  label: string
  badgeVariant: 'success' | 'outline' | 'secondary'
  icon: typeof CheckCircle2
  iconClass: string
}

const STATUS: Record<IntegrationStatus, StatusConfig> = {
  in_use: {
    label: 'Em uso',
    badgeVariant: 'success',
    icon: CheckCircle2,
    iconClass: 'text-emerald-500',
  },
  configured: {
    label: 'Sem ações',
    badgeVariant: 'outline',
    icon: TriangleAlert,
    iconClass: 'text-amber-500',
  },
  not_configured: {
    label: 'Não conectada',
    badgeVariant: 'secondary',
    icon: CircleDashed,
    iconClass: 'text-muted-foreground',
  },
}

export default function Integrations() {
  const { t } = useI18n()
  const [activeTab, setActiveTab] = useState('connectors')

  const [categories, setCategories] = useState<IntegrationCategory[]>([])
  const [selectedCategoryId, setSelectedCategoryId] = useState<number | null>(null)
  const [integrations, setIntegrations] = useState<IntegrationPlatformIntegration[]>([])
  const [connectors, setConnectors] = useState<Connector[]>([])
  const [automations, setAutomations] = useState<Automation[]>([])
  const [selectedIntegrationId, setSelectedIntegrationId] = useState<number | null>(null)

  const [isConnectorModalOpen, setIsConnectorModalOpen] = useState(false)
  const [editingConnector, setEditingConnector] = useState<Connector | null>(null)

  const [isTestModalOpen, setIsTestModalOpen] = useState(false)
  const [testingConnector, setTestingConnector] = useState<Connector | null>(null)

  const [deletingConnector, setDeletingConnector] = useState<Connector | null>(null)
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false)

  const [isAutomationModalOpen, setIsAutomationModalOpen] = useState(false)
  const [selectedAutomation, setSelectedAutomation] = useState<Automation | null>(null)
  const [automationPresetConnectorId, setAutomationPresetConnectorId] = useState<number | null>(null)
  const [automationsRefreshKey, setAutomationsRefreshKey] = useState(0)

  const { execute: fetchCategories, loading: loadingCategories } = useApi<IntegrationCategory[]>({ showErrorMessage: true })
  const { execute: fetchIntegrations, loading: loadingIntegrations } = useApi<IntegrationPlatformIntegration[]>({ showErrorMessage: true })

  useEffect(() => {
    void loadCategories()
    void loadAutomations()
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

  const loadAutomations = async () => {
    try {
      const result = await automationService.getAutomations(1, 200)
      setAutomations(result.items)
    } catch {
      setAutomations([])
    }
  }

  const loadIntegrationsByCategory = async (categoryId: number) => {
    const result = await fetchIntegrations(() =>
      integrationPlatformService.getIntegrationsByCategory(categoryId),
    )
    if (!result) return

    setIntegrations(result)

    const allConnectors: Connector[] = []
    for (const integration of result) {
      const connectorsForIntegration = await integrationPlatformService.getConnectorsByIntegration(integration.id)
      allConnectors.push(...connectorsForIntegration)
    }
    setConnectors(allConnectors)

    const connectedIds = new Set(
      allConnectors.filter((c) => c.isActive).map((c) => c.integrationId),
    )
    const sorted = [...result].sort((a, b) => {
      const aConnected = connectedIds.has(a.id) ? 0 : 1
      const bConnected = connectedIds.has(b.id) ? 0 : 1
      return aConnected - bConnected
    })
    setSelectedIntegrationId((prev) => {
      if (prev && result.some((item) => item.id === prev)) return prev
      return sorted[0]?.id ?? null
    })
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

  const automationsForSelectedIntegration = useMemo(() => {
    const connectorIds = new Set(connectorsForSelectedIntegration.map((c) => c.id))
    return automations.filter((automation) => connectorIds.has(automation.connectorId))
  }, [automations, connectorsForSelectedIntegration])

  const totalConnectors = connectors.length
  const activeConnectors = connectors.filter((connector) => connector.isActive).length

  const sortedIntegrations = useMemo(() => {
    const connectedIds = new Set(
      connectors.filter((connector) => connector.isActive).map((connector) => connector.integrationId),
    )
    return [...integrations].sort((a, b) => {
      const aConnected = connectedIds.has(a.id) ? 0 : 1
      const bConnected = connectedIds.has(b.id) ? 0 : 1
      return aConnected - bConnected
    })
  }, [integrations, connectors])

  const computeStatus = (integrationId: number): IntegrationStatus => {
    const integrationConnectors = connectors.filter(
      (connector) => connector.integrationId === integrationId && connector.isActive,
    )
    if (integrationConnectors.length === 0) return 'not_configured'

    const connectorIds = new Set(integrationConnectors.map((connector) => connector.id))
    const hasActiveAutomation = automations.some(
      (automation) => automation.isActive && connectorIds.has(automation.connectorId),
    )

    return hasActiveAutomation ? 'in_use' : 'configured'
  }

  const openCreateConnector = () => {
    setEditingConnector(null)
    setIsConnectorModalOpen(true)
  }

  const openEditConnector = (connector: Connector) => {
    setEditingConnector(connector)
    setIsConnectorModalOpen(true)
  }

  const openTestConnector = (connector: Connector) => {
    setTestingConnector(connector)
    setIsTestModalOpen(true)
  }

  const askDeleteConnector = (connector: Connector) => {
    setDeletingConnector(connector)
    setIsDeleteModalOpen(true)
  }

  const confirmDeleteConnector = async () => {
    if (!deletingConnector) return
    try {
      await integrationPlatformService.deleteConnector(deletingConnector.id)
      if (selectedCategoryId) {
        await loadIntegrationsByCategory(selectedCategoryId)
      }
      void loadAutomations()
    } catch {
      // erro ja exibido pelo httpClient
    } finally {
      setIsDeleteModalOpen(false)
      setDeletingConnector(null)
    }
  }

  const toggleConnectorActive = async (connector: Connector) => {
    try {
      await integrationPlatformService.setConnectorActive(connector.id, !connector.isActive)
      if (selectedCategoryId) {
        await loadIntegrationsByCategory(selectedCategoryId)
      }
      void loadAutomations()
    } catch {
      // erro ja exibido pelo httpClient
    }
  }

  const openCreateAutomationForConnector = (connector: Connector) => {
    setSelectedAutomation(null)
    setAutomationPresetConnectorId(connector.id)
    setIsAutomationModalOpen(true)
  }

  const openCreateAutomationForIntegration = () => {
    setSelectedAutomation(null)
    const firstActive = connectorsForSelectedIntegration.find((c) => c.isActive)
    setAutomationPresetConnectorId(firstActive?.id ?? null)
    setIsAutomationModalOpen(true)
  }

  const editAutomation = (automation: Automation) => {
    setSelectedAutomation(automation)
    setAutomationPresetConnectorId(null)
    setIsAutomationModalOpen(true)
  }

  return (
    <>
      <PageLayout
        title={t('configuration.integrations.title')}
        subtitle={t('configuration.integrations.subtitle')}
      >
        <div className="mb-5 flex w-fit items-center gap-2.5 rounded-md border border-primary/30 px-3 py-2">
          <Megaphone className="h-4 w-4 shrink-0 text-amber-500" />
          <p className="text-xs text-primary/80">
            Não encontrou a integração que precisa?{' '}
            <a
              href="https://portal.mainstay.com.br"
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-0.5 font-semibold underline underline-offset-2 hover:text-primary"
            >
              Abra um ticket no nosso portal do cliente
              <ExternalLink className="h-3 w-3" />
            </a>
            {' '}— desenvolvemos a nova integração{' '}
            <span className="font-semibold">sem custo adicional</span> e com prazo de{' '}
            <span className="font-semibold">48 horas</span> para estar disponível.
          </p>
        </div>

        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList className="mb-6 h-auto w-full justify-start gap-6 rounded-none border-b border-border bg-transparent p-0">
            <TabsTrigger
              value="connectors"
              className="group gap-2 rounded-none border-b-2 border-transparent bg-transparent px-1 pb-3 pt-0 text-sm font-medium text-muted-foreground shadow-none hover:text-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:text-primary data-[state=active]:shadow-none"
            >
              <Plug className="h-4 w-4" /> {t('configuration.integrations.connectedAccounts')}
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
                <span className="text-xs text-muted-foreground">{t('configuration.integrations.field.category')}</span>
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
                <span className="text-xs text-muted-foreground">{t('configuration.integrations.field.connectedAccounts')}</span>
                <p className="font-medium">{selectedCategory ? totalConnectors : '—'}</p>
              </div>
              <div className="space-y-1">
                <span className="text-xs text-muted-foreground">{t('configuration.integrations.field.active')}</span>
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
                    Categorias agrupam integrações por finalidade (Email, WhatsApp, Cobrança, etc.).
                  </p>
                </CardContent>
              </Card>
            ) : integrations.length === 0 && !loadingIntegrations ? (
              <Card className="border-dashed">
                <CardContent className="flex flex-col items-center justify-center py-16 text-muted-foreground">
                  <Workflow size={42} className="mb-3 opacity-50" />
                  <p className="text-base font-medium">Nenhuma integração nesta categoria</p>
                  <p className="mt-1 text-sm">
                    Cadastre integrações no IntegrationPlatform para que apareçam aqui.
                  </p>
                </CardContent>
              </Card>
            ) : (
              <div className="grid gap-4 lg:grid-cols-12">
                <Card className="lg:col-span-5">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-base">Integrações</CardTitle>
                    <p className="text-sm text-muted-foreground">
                      Selecione uma integração para gerenciar as contas conectadas.
                    </p>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      {sortedIntegrations.map((integration) => {
                        const integrationConnectors = connectors.filter(
                          (connector) => connector.integrationId === integration.id,
                        )
                        const integrationActive = integrationConnectors.filter((connector) => connector.isActive).length
                        const isSelected = selectedIntegrationId === integration.id
                        const status = computeStatus(integration.id)
                        const statusConfig = STATUS[status]
                        const StatusIcon = statusConfig.icon

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
                              {integration.iconUrl ? (
                                <img
                                  src={integration.iconUrl}
                                  alt=""
                                  className="h-9 w-9 rounded-md border bg-card object-contain p-1"
                                  onError={(e) => {
                                    const img = e.currentTarget as HTMLImageElement
                                    img.style.display = 'none'
                                    const fallback = img.nextElementSibling as HTMLElement | null
                                    if (fallback) fallback.style.display = 'flex'
                                  }}
                                />
                              ) : null}
                              <div
                                className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-primary"
                                style={{ display: integration.iconUrl ? 'none' : 'flex' }}
                              >
                                <Plug size={16} />
                              </div>

                              <div className="min-w-0 flex-1">
                                <div className="flex items-center gap-2">
                                  <span className="truncate text-sm font-semibold text-foreground">
                                    {integration.name}
                                  </span>
                                  <Badge variant={statusConfig.badgeVariant} className="gap-1">
                                    <StatusIcon size={11} className={statusConfig.iconClass} />
                                    {statusConfig.label}
                                  </Badge>
                                </div>
                                <div className="mt-1.5 flex flex-wrap gap-1.5 text-xs">
                                  <Badge variant="outline">{integration.identifier}</Badge>
                                  <Badge variant="outline">
                                    {integrationConnectors.length === 0
                                      ? 'Sem contas'
                                      : `${integrationActive} ativa${integrationActive === 1 ? '' : 's'} · ${integrationConnectors.length} total`}
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
                    <CardTitle className="text-base">{t('configuration.integrations.connectedAccounts')}</CardTitle>
                    <p className="text-sm text-muted-foreground">
                      Cada conta é uma instância configurada da integração com credenciais próprias.
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
                          <div className="flex items-start gap-3">
                            {selectedIntegration.iconUrl && (
                              <img
                                src={selectedIntegration.iconUrl}
                                alt=""
                                className="h-12 w-12 rounded-md border bg-card object-contain p-1.5"
                                onError={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
                              />
                            )}
                            <div>
                              <p className="text-lg font-semibold">{selectedIntegration.name}</p>
                              {selectedIntegration.description && (
                                <p className="mt-1 text-sm text-muted-foreground">
                                  {selectedIntegration.description}
                                </p>
                              )}
                            </div>
                          </div>
                          <Button size="sm" onClick={openCreateConnector}>
                            <Plus size={14} className="mr-1.5" />
                            Conectar conta
                          </Button>
                        </div>

                        <div className="flex flex-wrap gap-2">
                          {(() => {
                            const status = computeStatus(selectedIntegration.id)
                            const cfg = STATUS[status]
                            const Icon = cfg.icon
                            return (
                              <Badge variant={cfg.badgeVariant} className="gap-1">
                                <Icon size={12} className={cfg.iconClass} />
                                {cfg.label}
                              </Badge>
                            )
                          })()}
                          <Badge variant="outline">{selectedIntegration.identifier}</Badge>
                          <Badge variant="outline">
                            {connectorsForSelectedIntegration.length} {connectorsForSelectedIntegration.length === 1 ? 'conta' : 'contas'}
                          </Badge>
                        </div>

                        {connectorsForSelectedIntegration.length === 0 ? (
                          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-10 text-muted-foreground">
                            <Settings2 size={36} className="mb-3 opacity-50" />
                            <p className="text-sm font-medium">{t('configuration.integrations.noAccounts')}</p>
                            <p className="mt-1 text-xs">
                              Use o botão "Conectar conta" acima para configurar a primeira.
                            </p>
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
                                      {connector.isActive ? 'Ativa' : 'Inativa'}
                                    </Badge>
                                  </div>
                                  {connector.systemApplicationId && (
                                    <p className="mt-1 text-xs text-muted-foreground">
                                      App: {connector.systemApplicationId}
                                    </p>
                                  )}
                                </div>
                                <div className="flex items-center gap-1">
                                  {connector.isActive && (
                                    <Button
                                      size="sm"
                                      variant="ghost"
                                      title={t('configuration.integrations.action.test')}
                                      onClick={() => openTestConnector(connector)}
                                    >
                                      <Sparkles size={14} className="mr-1" />
                                      Testar
                                    </Button>
                                  )}
                                  {connector.isActive && (
                                    <Button
                                      size="sm"
                                      variant="ghost"
                                      title={t('configuration.integrations.action.linkEvent')}
                                      onClick={() => openCreateAutomationForConnector(connector)}
                                    >
                                      <Zap size={14} className="mr-1" />
                                      Automação
                                    </Button>
                                  )}
                                  <Button
                                    size="sm"
                                    variant="ghost"
                                    title={connector.isActive ? 'Pausar conta' : 'Reativar conta'}
                                    onClick={() => toggleConnectorActive(connector)}
                                  >
                                    {connector.isActive ? (
                                      <>
                                        <Pause size={14} className="mr-1" />
                                        Pausar
                                      </>
                                    ) : (
                                      <>
                                        <Play size={14} className="mr-1" />
                                        Reativar
                                      </>
                                    )}
                                  </Button>
                                  <Button
                                    size="sm"
                                    variant="ghost"
                                    title={t('configuration.integrations.action.editSettings')}
                                    onClick={() => openEditConnector(connector)}
                                  >
                                    <Pencil size={14} className="mr-1" />
                                    Editar
                                  </Button>
                                  <Button
                                    size="sm"
                                    variant="ghost"
                                    title={t('configuration.integrations.action.delete')}
                                    onClick={() => askDeleteConnector(connector)}
                                  >
                                    <Trash2 size={14} className="text-destructive" />
                                  </Button>
                                </div>
                              </div>
                            ))}
                          </div>
                        )}

                        <div className="border-t pt-4 space-y-3">
                          <div className="flex items-center justify-between gap-3">
                            <div className="flex items-center gap-2">
                              <Zap size={16} className="text-primary" />
                              <p className="text-sm font-semibold">Automações usando esta integração</p>
                              <Badge variant="outline" className="ml-1">
                                {automationsForSelectedIntegration.length}
                              </Badge>
                            </div>
                            {connectorsForSelectedIntegration.some((c) => c.isActive) && (
                              <Button size="sm" variant="outline" onClick={openCreateAutomationForIntegration}>
                                <Plus size={14} className="mr-1.5" />
                                Vincular evento
                              </Button>
                            )}
                          </div>

                          {automationsForSelectedIntegration.length === 0 ? (
                            <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-6 text-muted-foreground">
                              <p className="text-xs">
                                {connectorsForSelectedIntegration.some((c) => c.isActive)
                                  ? 'Esta integração ainda não dispara nenhuma ação automática.'
                                  : 'Conecte e ative uma conta para começar a vincular eventos.'}
                              </p>
                            </div>
                          ) : (
                            <div className="space-y-1.5">
                              {automationsForSelectedIntegration.map((automation) => {
                                const connector = connectorsForSelectedIntegration.find((c) => c.id === automation.connectorId)
                                return (
                                  <div
                                    key={automation.id}
                                    className="flex items-center justify-between gap-3 rounded-lg border bg-card p-2.5"
                                  >
                                    <div className="min-w-0 flex-1">
                                      <div className="flex flex-wrap items-center gap-2">
                                        <span className="truncate text-sm font-medium">{automation.name}</span>
                                        <Badge variant="secondary" className="text-[10px]">
                                          {automationTriggerLabels[automation.trigger] ?? automation.trigger}
                                        </Badge>
                                        <Badge variant={automation.isActive ? 'success' : 'outline'} className="text-[10px]">
                                          {automation.isActive ? 'Ativa' : 'Pausada'}
                                        </Badge>
                                      </div>
                                      {connector && (
                                        <p className="mt-0.5 text-[10px] text-muted-foreground">
                                          via {connector.name}
                                        </p>
                                      )}
                                    </div>
                                    <Button size="sm" variant="ghost" onClick={() => editAutomation(automation)}>
                                      <Pencil size={12} />
                                    </Button>
                                  </div>
                                )
                              })}
                            </div>
                          )}
                        </div>
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
                setAutomationPresetConnectorId(null)
                setIsAutomationModalOpen(true)
              }}
              onEdit={(automation: Automation) => {
                setSelectedAutomation(automation)
                setAutomationPresetConnectorId(null)
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
          void loadAutomations()
          setEditingConnector(null)
        }}
      />

      <AutomationFormModal
        open={isAutomationModalOpen}
        onOpenChange={setIsAutomationModalOpen}
        automation={selectedAutomation}
        presetConnectorId={automationPresetConnectorId}
        onSuccess={() => {
          setIsAutomationModalOpen(false)
          setSelectedAutomation(null)
          setAutomationPresetConnectorId(null)
          setAutomationsRefreshKey((prev) => prev + 1)
          void loadAutomations()
        }}
      />

      <ConnectorTestModal
        open={isTestModalOpen}
        onOpenChange={setIsTestModalOpen}
        connector={testingConnector}
        integration={selectedIntegration}
        category={selectedCategory}
      />

      <ConfirmModal
        open={isDeleteModalOpen}
        onOpenChange={(open) => {
          setIsDeleteModalOpen(open)
          if (!open) setDeletingConnector(null)
        }}
        onConfirm={confirmDeleteConnector}
        title="Excluir conta"
        description={
          deletingConnector
            ? `Tem certeza que deseja excluir a conta "${deletingConnector.name}"? Esta ação não pode ser desfeita e as automações que dependem dela vão parar de funcionar.`
            : ''
        }
        confirmText="Excluir"
        cancelText="Cancelar"
        variant="danger"
      />
    </>
  )
}
