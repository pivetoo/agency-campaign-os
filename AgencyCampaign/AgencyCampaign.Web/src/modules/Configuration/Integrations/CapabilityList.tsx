import { useEffect, useMemo, useState } from 'react'
import type { LucideIcon } from 'lucide-react'
import { Badge, Button, Card, CardContent, SearchableSelect, useApi } from 'archon-ui'
import { ArrowLeftRight, CheckCircle2, ChevronRight, CircleDashed, FileSignature, HandCoins, Mail, MessageCircle, Sparkles, Wallet } from 'lucide-react'
import { integrationCapabilityService } from '../../../services/integrationCapabilityService'
import type { IntegrationCapabilitySummary } from '../../../types/integrationCapability'

interface RowState {
  selectedConnectorId: number | null
  isActive: boolean
  saving: boolean
}

interface ActionModule {
  id: string
  label: string
  description: string
  icon: LucideIcon
  intentKeys: string[]
}

const ACTION_MODULES: ActionModule[] = [
  {
    id: 'email',
    label: 'Email',
    description: 'Envio de propostas, documentos e notificações por email.',
    icon: Mail,
    intentKeys: ['proposal.send-email', 'campaign-document.send-email', 'notification.send-transactional'],
  },
  {
    id: 'whatsapp',
    label: 'Disparo de mensagens por WhatsApp',
    description: 'Envio de mensagens para clientes e creators via WhatsApp.',
    icon: MessageCircle,
    intentKeys: ['proposal.send-whatsapp', 'creator-portal.notify-whatsapp'],
  },
  {
    id: 'signature',
    label: 'Assinatura digital',
    description: 'Coleta de assinaturas em contratos e documentos da campanha.',
    icon: FileSignature,
    intentKeys: ['campaign-document.send-signature'],
  },
  {
    id: 'payments',
    label: 'Pagamentos',
    description: 'Pagamentos a creators e fornecedores via PIX, TED ou outros meios.',
    icon: HandCoins,
    intentKeys: ['creator-payment.schedule-pix', 'payable.transfer'],
  },
  {
    id: 'receivables',
    label: 'Recebimentos',
    description: 'Cobranças emitidas para clientes (boleto, PIX, cartão).',
    icon: Wallet,
    intentKeys: ['receivable.issue-invoice'],
  },
]

export default function CapabilityList() {
  const [summary, setSummary] = useState<IntegrationCapabilitySummary[]>([])
  const [rowState, setRowState] = useState<Record<string, RowState>>({})
  const [selectedModuleId, setSelectedModuleId] = useState<string | null>(null)

  const { execute: fetchSummary, loading } = useApi<IntegrationCapabilitySummary[]>({ showErrorMessage: true })

  useEffect(() => {
    void loadSummary()
  }, [])

  const loadSummary = async () => {
    const result = await fetchSummary(() => integrationCapabilityService.getSummary())
    const safe = result ?? []
    setSummary(safe)

    const nextState: Record<string, RowState> = {}
    safe.forEach((item) => {
      nextState[item.intentKey] = {
        selectedConnectorId: item.configuredConnectorId ?? null,
        isActive: item.isActive,
        saving: false,
      }
    })
    setRowState(nextState)
  }

  const summaryByIntent = useMemo(() => {
    const map = new Map<string, IntegrationCapabilitySummary>()
    summary.forEach((item) => map.set(item.intentKey, item))
    return map
  }, [summary])

  const moduleStats = useMemo(() => {
    return ACTION_MODULES.map((module) => {
      let configured = 0
      let active = 0
      let hasAccount = false
      module.intentKeys.forEach((intentKey) => {
        const item = summaryByIntent.get(intentKey)
        if (item && item.availableConnectors.length > 0) hasAccount = true
        const state = rowState[intentKey]
        if (state?.selectedConnectorId) {
          configured += 1
          if (state.isActive) active += 1
        }
      })
      return { moduleId: module.id, total: module.intentKeys.length, configured, active, hasAccount }
    })
  }, [summaryByIntent, rowState])

  const overallStats = useMemo(() => {
    const total = moduleStats.reduce((acc, stats) => acc + stats.total, 0)
    const active = moduleStats.reduce((acc, stats) => acc + stats.active, 0)
    return { total, active }
  }, [moduleStats])

  const orderedModules = useMemo(() => {
    const tierOf = (moduleId: string): number => {
      const stats = moduleStats.find((item) => item.moduleId === moduleId)
      if (!stats) return 3
      if (stats.configured > 0) return 0
      if (stats.hasAccount) return 1
      return 2
    }
    return [...ACTION_MODULES].sort((a, b) => {
      const tierA = tierOf(a.id)
      const tierB = tierOf(b.id)
      if (tierA !== tierB) return tierA - tierB
      return a.label.localeCompare(b.label)
    })
  }, [moduleStats])

  useEffect(() => {
    if (selectedModuleId) return
    if (orderedModules.length > 0) {
      setSelectedModuleId(orderedModules[0].id)
    }
  }, [orderedModules, selectedModuleId])

  const updateRow = (intentKey: string, patch: Partial<RowState>) => {
    setRowState((prev) => ({ ...prev, [intentKey]: { ...prev[intentKey], ...patch } }))
  }

  const handleConnectorChange = async (intentKey: string, connectorId: number | null) => {
    if (!connectorId) {
      updateRow(intentKey, { selectedConnectorId: null })
      try {
        await integrationCapabilityService.remove(intentKey)
        await loadSummary()
      } catch {
        // erro ja exibido pelo httpClient
      }
      return
    }

    const wasConfigured = !!rowState[intentKey]?.selectedConnectorId
    const isActive = wasConfigured ? (rowState[intentKey]?.isActive ?? true) : true
    updateRow(intentKey, { selectedConnectorId: connectorId, isActive, saving: true })
    try {
      await integrationCapabilityService.setCapability({
        intentKey,
        connectorId,
        isActive,
      })
    } catch {
      // erro ja exibido pelo httpClient
    } finally {
      updateRow(intentKey, { saving: false })
    }
  }

  const handleToggleActive = async (intentKey: string) => {
    const state = rowState[intentKey]
    if (!state?.selectedConnectorId) return

    const newActive = !state.isActive
    updateRow(intentKey, { isActive: newActive, saving: true })
    try {
      await integrationCapabilityService.setCapability({
        intentKey,
        connectorId: state.selectedConnectorId,
        isActive: newActive,
      })
    } catch {
      updateRow(intentKey, { isActive: !newActive })
    } finally {
      updateRow(intentKey, { saving: false })
    }
  }

  if (loading && summary.length === 0) {
    return (
      <Card className="border-dashed">
        <CardContent className="flex items-center justify-center py-12 text-sm text-muted-foreground">
          Carregando ações de negócio…
        </CardContent>
      </Card>
    )
  }

  const selectedModule = ACTION_MODULES.find((module) => module.id === selectedModuleId) ?? orderedModules[0]
  const selectedModuleStats = moduleStats.find((stats) => stats.moduleId === selectedModule?.id)

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-4 rounded-lg border bg-card p-4 md:grid-cols-3">
        <div className="space-y-1">
          <span className="text-xs text-muted-foreground">Ações disponíveis</span>
          <p className="font-medium">{overallStats.total}</p>
        </div>
        <div className="space-y-1">
          <span className="text-xs text-muted-foreground">Configuradas e ativas</span>
          <div className="mt-0.5">
            <Badge variant={overallStats.active > 0 ? 'success' : 'outline'}>
              {overallStats.active} de {overallStats.total}
            </Badge>
          </div>
        </div>
        <div className="space-y-1 hidden md:block">
          <span className="text-xs text-muted-foreground">Como funciona</span>
          <p className="text-xs text-muted-foreground">
            Escolha uma área à esquerda e defina qual conta o Kanvas deve usar para cada ação.
          </p>
        </div>
      </div>

      <div className="grid gap-4 lg:grid-cols-12">
        <Card className="lg:col-span-4">
          <CardContent className="p-2">
            <div className="space-y-1">
              {orderedModules.map((module) => {
                const Icon = module.icon
                const stats = moduleStats.find((item) => item.moduleId === module.id)
                const isSelected = module.id === selectedModuleId
                const configuredRatio = stats ? `${stats.active}/${stats.total}` : `0/${module.intentKeys.length}`
                const allConfigured = stats ? stats.active === stats.total && stats.total > 0 : false
                const noAccount = stats ? !stats.hasAccount : true
                return (
                  <button
                    key={module.id}
                    type="button"
                    onClick={() => setSelectedModuleId(module.id)}
                    className={[
                      'group flex w-full items-center gap-3 rounded-lg border p-3 text-left transition-all focus:outline-none focus:ring-2 focus:ring-primary/30',
                      isSelected
                        ? 'border-primary bg-primary/5 ring-1 ring-primary/20'
                        : 'border-transparent hover:border-primary/30 hover:bg-accent/30',
                    ].join(' ')}
                  >
                    <div className={[
                      'flex h-10 w-10 shrink-0 items-center justify-center rounded-lg',
                      isSelected ? 'bg-primary/15 text-primary' : 'bg-muted text-muted-foreground group-hover:bg-primary/10 group-hover:text-primary',
                    ].join(' ')}>
                      <Icon size={18} />
                    </div>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-semibold text-foreground">{module.label}</p>
                      <div className="mt-1 flex items-center gap-1.5">
                        {noAccount ? (
                          <span className="text-[11px] text-amber-600">Sem conta cadastrada</span>
                        ) : allConfigured ? (
                          <span className="text-[11px] text-emerald-600">Tudo configurado</span>
                        ) : (
                          <span className="text-[11px] text-muted-foreground">{configuredRatio} ativas</span>
                        )}
                      </div>
                    </div>
                    <ChevronRight size={16} className={isSelected ? 'text-primary' : 'text-muted-foreground/40'} />
                  </button>
                )
              })}
            </div>
          </CardContent>
        </Card>

        <Card className="lg:col-span-8">
          <CardContent className="space-y-4 p-5">
            {!selectedModule ? (
              <div className="flex flex-col items-center justify-center py-12 text-muted-foreground">
                <Sparkles size={36} className="mb-3 opacity-50" />
                <p className="text-sm font-medium">Selecione uma área à esquerda.</p>
              </div>
            ) : (
              <>
                <div className="flex items-start gap-3 border-b border-border pb-4">
                  <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-primary/15 text-primary">
                    <selectedModule.icon size={22} />
                  </div>
                  <div className="min-w-0 flex-1">
                    <h3 className="text-lg font-semibold">{selectedModule.label}</h3>
                    <p className="mt-0.5 text-sm text-muted-foreground">{selectedModule.description}</p>
                  </div>
                  {selectedModuleStats && (
                    <Badge variant={selectedModuleStats.active === selectedModuleStats.total && selectedModuleStats.total > 0 ? 'success' : 'outline'}>
                      {selectedModuleStats.active} de {selectedModuleStats.total} ativas
                    </Badge>
                  )}
                </div>

                {!selectedModuleStats?.hasAccount ? (
                  <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-amber-200 bg-amber-50/40 py-10 text-center">
                    <CircleDashed size={36} className="mb-3 text-amber-500 opacity-70" />
                    <p className="text-sm font-medium text-foreground">Você ainda não tem uma conta nesta área</p>
                    <p className="mt-1 max-w-md text-xs text-muted-foreground">
                      Cadastre uma conta na aba <span className="font-medium">"Contas conectadas"</span> para liberar as ações de {selectedModule.label.toLowerCase()}.
                    </p>
                  </div>
                ) : (
                  <div className="space-y-2.5">
                    {selectedModule.intentKeys.map((intentKey) => {
                      const item = summaryByIntent.get(intentKey)
                      const connectors = item?.availableConnectors ?? []
                      const label = item?.label ?? intentKey
                      const state = rowState[intentKey] ?? { selectedConnectorId: null, isActive: true, saving: false }
                      const configured = !!state.selectedConnectorId

                      return (
                        <div key={intentKey} className="flex flex-col gap-3 rounded-lg border bg-card p-4 transition-colors hover:border-primary/30 md:flex-row md:items-center">
                          <div className="min-w-0 flex-1">
                            <div className="flex flex-wrap items-center gap-2">
                              <span className="text-sm font-semibold">{label}</span>
                              {configured && state.isActive ? (
                                <Badge variant="success" className="gap-1">
                                  <CheckCircle2 size={11} className="text-white" />
                                  Ativa
                                </Badge>
                              ) : configured ? (
                                <Badge variant="destructive">Pausada</Badge>
                              ) : (
                                <Badge variant="secondary">Não configurada</Badge>
                              )}
                            </div>
                            {!configured && (
                              <p className="mt-1 text-xs text-muted-foreground">Selecione a conta que o Kanvas deve usar para esta ação.</p>
                            )}
                          </div>
                          <div className="w-full md:w-72">
                            <SearchableSelect
                              options={connectors.map((connector) => ({
                                value: String(connector.id),
                                label: connector.integrationName
                                  ? `${connector.integrationName} — ${connector.name}${connector.isActive ? '' : ' (inativa)'}`
                                  : `${connector.name}${connector.isActive ? '' : ' (inativa)'}`,
                                iconUrl: connector.integrationIconUrl,
                              }))}
                              value={state.selectedConnectorId ? String(state.selectedConnectorId) : undefined}
                              onValueChange={(value) => handleConnectorChange(intentKey, value ? Number(value) : null)}
                              placeholder="Escolha uma conta"
                              disabled={state.saving}
                            />
                          </div>
                          {configured && (
                            <Button
                              size="sm"
                              variant={state.isActive ? 'outline' : 'secondary'}
                              onClick={() => handleToggleActive(intentKey)}
                              disabled={state.saving}
                              className="md:min-w-[88px]"
                            >
                              {state.isActive ? (
                                <>
                                  <ArrowLeftRight size={12} className="mr-1.5" />
                                  Pausar
                                </>
                              ) : (
                                'Reativar'
                              )}
                            </Button>
                          )}
                        </div>
                      )
                    })}
                  </div>
                )}
              </>
            )}
          </CardContent>
        </Card>
      </div>

      {summary.length === 0 && !loading && (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center py-12 text-muted-foreground">
            <Sparkles size={36} className="mb-3 opacity-50" />
            <p className="text-sm font-medium">Catálogo de ações vazio</p>
            <p className="mt-1 text-xs">Nenhuma ação de negócio está disponível para configuração.</p>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
