import { useEffect, useMemo, useState } from 'react'
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, SearchableSelect, useApi } from 'archon-ui'
import { CheckCircle2, CircleDashed, Sparkles, Zap } from 'lucide-react'
import { integrationCapabilityService } from '../../../services/integrationCapabilityService'
import { integrationPlatformService } from '../../../services/integrationPlatformService'
import type { IntegrationCapability, IntegrationIntentDescriptor } from '../../../types/integrationCapability'
import type { Connector } from '../../../types/integrationPlatform'

interface CapabilityRowState {
  selectedConnectorId: number | null
  isActive: boolean
  saving: boolean
}

const CATEGORY_LABELS: Record<string, string> = {
  email: 'Email',
  messaging: 'Mensagens (WhatsApp / SMS)',
  'digital-signature': 'Assinatura digital',
  banking: 'Conta bancária',
  payment: 'Pagamentos',
  invoice: 'Notas fiscais',
}

const labelForCategory = (identifier: string): string => CATEGORY_LABELS[identifier] ?? identifier.replace(/-/g, ' ')

export default function CapabilityList() {
  const [catalog, setCatalog] = useState<IntegrationIntentDescriptor[]>([])
  const [, setCapabilities] = useState<IntegrationCapability[]>([])
  const [connectorsByCategory, setConnectorsByCategory] = useState<Record<string, Connector[]>>({})
  const [rowState, setRowState] = useState<Record<string, CapabilityRowState>>({})

  const { execute: fetchCatalog, loading: loadingCatalog } = useApi<IntegrationIntentDescriptor[]>({ showErrorMessage: true })
  const { execute: fetchCapabilities, loading: loadingCapabilities } = useApi<IntegrationCapability[]>({ showErrorMessage: true })

  useEffect(() => {
    void loadAll()
  }, [])

  const loadAll = async () => {
    const [catalogResult, capabilitiesResult] = await Promise.all([
      fetchCatalog(() => integrationCapabilityService.getCatalog()),
      fetchCapabilities(() => integrationCapabilityService.getAll()),
    ])

    const safeCatalog = catalogResult ?? []
    const safeCapabilities = capabilitiesResult ?? []
    setCatalog(safeCatalog)
    setCapabilities(safeCapabilities)

    const uniqueCategories: string[] = Array.from(new Set(safeCatalog.map((item) => item.categoryIdentifier)))
    const connectorsMap: Record<string, Connector[]> = {}
    await Promise.all(uniqueCategories.map(async (identifier: string) => {
      try {
        connectorsMap[identifier] = await integrationPlatformService.getConnectorsByCategoryIdentifier(identifier)
      } catch {
        connectorsMap[identifier] = []
      }
    }))
    setConnectorsByCategory(connectorsMap)

    const nextState: Record<string, CapabilityRowState> = {}
    safeCatalog.forEach((descriptor) => {
      const existing = safeCapabilities.find((capability) => capability.intentKey === descriptor.key)
      nextState[descriptor.key] = {
        selectedConnectorId: existing?.connectorId ?? null,
        isActive: existing?.isActive ?? true,
        saving: false,
      }
    })
    setRowState(nextState)
  }

  const grouped = useMemo(() => {
    const map = new Map<string, IntegrationIntentDescriptor[]>()
    for (const descriptor of catalog) {
      const list = map.get(descriptor.categoryIdentifier) ?? []
      list.push(descriptor)
      map.set(descriptor.categoryIdentifier, list)
    }
    return Array.from(map.entries()).sort(([aIdentifier], [bIdentifier]) => {
      const aHasAccount = (connectorsByCategory[aIdentifier]?.length ?? 0) > 0
      const bHasAccount = (connectorsByCategory[bIdentifier]?.length ?? 0) > 0
      if (aHasAccount !== bHasAccount) {
        return aHasAccount ? -1 : 1
      }
      return labelForCategory(aIdentifier).localeCompare(labelForCategory(bIdentifier))
    })
  }, [catalog, connectorsByCategory])

  const configuredCount = useMemo(
    () => catalog.filter((descriptor) => {
      const state = rowState[descriptor.key]
      return state?.selectedConnectorId && state.isActive
    }).length,
    [catalog, rowState],
  )

  const updateRow = (intentKey: string, patch: Partial<CapabilityRowState>) => {
    setRowState((prev) => ({
      ...prev,
      [intentKey]: { ...prev[intentKey], ...patch },
    }))
  }

  const handleConnectorChange = async (descriptor: IntegrationIntentDescriptor, connectorId: number | null) => {
    if (!connectorId) {
      updateRow(descriptor.key, { selectedConnectorId: null })
      try {
        await integrationCapabilityService.remove(descriptor.key)
        setCapabilities((prev) => prev.filter((item) => item.intentKey !== descriptor.key))
      } catch {
        // erro ja exibido pelo httpClient
      }
      return
    }

    updateRow(descriptor.key, { selectedConnectorId: connectorId, saving: true })
    try {
      const saved = await integrationCapabilityService.setCapability({
        intentKey: descriptor.key,
        connectorId,
        isActive: rowState[descriptor.key]?.isActive ?? true,
      })
      setCapabilities((prev) => {
        const filtered = prev.filter((item) => item.intentKey !== descriptor.key)
        return [...filtered, saved]
      })
    } catch {
      // erro ja exibido pelo httpClient
    } finally {
      updateRow(descriptor.key, { saving: false })
    }
  }

  const handleToggleActive = async (descriptor: IntegrationIntentDescriptor) => {
    const state = rowState[descriptor.key]
    if (!state?.selectedConnectorId) return

    const newActive = !state.isActive
    updateRow(descriptor.key, { isActive: newActive, saving: true })
    try {
      const saved = await integrationCapabilityService.setCapability({
        intentKey: descriptor.key,
        connectorId: state.selectedConnectorId,
        isActive: newActive,
      })
      setCapabilities((prev) => {
        const filtered = prev.filter((item) => item.intentKey !== descriptor.key)
        return [...filtered, saved]
      })
    } catch {
      updateRow(descriptor.key, { isActive: !newActive })
    } finally {
      updateRow(descriptor.key, { saving: false })
    }
  }

  if (loadingCatalog || loadingCapabilities) {
    return (
      <Card className="border-dashed">
        <CardContent className="flex items-center justify-center py-12 text-sm text-muted-foreground">
          Carregando ações de negócio…
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-4 rounded-lg border bg-card p-4 md:grid-cols-3">
        <div className="space-y-1">
          <span className="text-xs text-muted-foreground">Ações disponíveis</span>
          <p className="font-medium">{catalog.length}</p>
        </div>
        <div className="space-y-1">
          <span className="text-xs text-muted-foreground">Configuradas e ativas</span>
          <div className="mt-0.5">
            <Badge variant={configuredCount > 0 ? 'success' : 'outline'}>
              {configuredCount} de {catalog.length}
            </Badge>
          </div>
        </div>
        <div className="space-y-1 hidden md:block">
          <span className="text-xs text-muted-foreground">Como funciona</span>
          <p className="text-xs text-muted-foreground">
            Para cada ação do Kanvas, escolha qual conta deve ser usada. Você só precisa cadastrar a conta uma vez em "Contas conectadas".
          </p>
        </div>
      </div>

      {grouped.length === 0 ? (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center py-12 text-muted-foreground">
            <Sparkles size={36} className="mb-3 opacity-50" />
            <p className="text-sm font-medium">Catálogo de ações vazio</p>
            <p className="mt-1 text-xs">Nenhuma ação de negócio está disponível para configuração.</p>
          </CardContent>
        </Card>
      ) : (
        grouped.map(([categoryIdentifier, descriptors]) => {
          const connectors = connectorsByCategory[categoryIdentifier] ?? []
          const categoryLabel = labelForCategory(categoryIdentifier)
          return (
            <Card key={categoryIdentifier}>
              <CardHeader className="pb-2">
                <CardTitle className="text-base">{categoryLabel}</CardTitle>
                <p className="text-sm text-muted-foreground">
                  Ações do Kanvas que precisam de uma conta de <span className="font-medium">{categoryLabel.toLowerCase()}</span>.
                </p>
              </CardHeader>
              <CardContent>
                {connectors.length === 0 ? (
                  <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-6 text-muted-foreground">
                    <CircleDashed size={28} className="mb-2 opacity-50" />
                    <p className="text-xs">Você ainda não tem nenhuma conta de {categoryLabel.toLowerCase()}.</p>
                    <p className="text-xs">Cadastre uma na aba "Contas conectadas" para liberar estas ações.</p>
                  </div>
                ) : (
                  <div className="space-y-2">
                    {descriptors.map((descriptor) => {
                      const state = rowState[descriptor.key] ?? { selectedConnectorId: null, isActive: true, saving: false }
                      const configured = !!state.selectedConnectorId
                      return (
                        <div key={descriptor.key} className="flex flex-col gap-3 rounded-lg border bg-card p-3 md:flex-row md:items-center">
                          <div className="min-w-0 flex-1">
                            <div className="flex flex-wrap items-center gap-2">
                              <Zap size={14} className="text-primary" />
                              <span className="text-sm font-semibold">{descriptor.label}</span>
                              {configured && state.isActive ? (
                                <Badge variant="success" className="gap-1">
                                  <CheckCircle2 size={11} className="text-white" />
                                  Ativa
                                </Badge>
                              ) : configured ? (
                                <Badge variant="outline">Pausada</Badge>
                              ) : (
                                <Badge variant="secondary">Não configurada</Badge>
                              )}
                            </div>
                          </div>
                          <div className="w-full md:w-72">
                            <SearchableSelect
                              options={connectors.map((connector) => ({
                                value: String(connector.id),
                                label: `${connector.name}${connector.isActive ? '' : ' (inativa)'}`,
                              }))}
                              value={state.selectedConnectorId ? String(state.selectedConnectorId) : undefined}
                              onValueChange={(value) => handleConnectorChange(descriptor, value ? Number(value) : null)}
                              placeholder="Escolha uma conta"
                              disabled={state.saving}
                            />
                          </div>
                          {configured && (
                            <Button
                              size="sm"
                              variant={state.isActive ? 'outline' : 'secondary'}
                              onClick={() => handleToggleActive(descriptor)}
                              disabled={state.saving}
                            >
                              {state.isActive ? 'Pausar' : 'Reativar'}
                            </Button>
                          )}
                        </div>
                      )
                    })}
                  </div>
                )}
              </CardContent>
            </Card>
          )
        })
      )}
    </div>
  )
}
