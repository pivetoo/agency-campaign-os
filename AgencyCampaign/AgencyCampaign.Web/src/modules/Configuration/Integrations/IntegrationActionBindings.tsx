import { useEffect, useMemo, useState } from 'react'
import { Badge, Button, Card, CardContent, SearchableSelect, useApi, useI18n } from 'archon-ui'
import { CheckCircle2, Save, Sparkles } from 'lucide-react'
import { agencyIntegrationBindingService } from '../../../services/agencyIntegrationBindingService'
import { integrationPlatformService } from '../../../services/integrationPlatformService'
import type { AgencyIntegrationBinding, IntegrationIntentDescriptor } from '../../../types/integrationBinding'
import type { Connector, Pipeline } from '../../../types/integrationPlatform'

interface IntentDraft {
  connectorId: number | null
  pipelineId: number | null
}

export default function IntegrationActionBindings() {
  const { t } = useI18n()
  const [intents, setIntents] = useState<IntegrationIntentDescriptor[]>([])
  const [bindings, setBindings] = useState<Record<string, AgencyIntegrationBinding>>({})
  const [drafts, setDrafts] = useState<Record<string, IntentDraft>>({})
  const [connectorsByCategory, setConnectorsByCategory] = useState<Record<string, Connector[]>>({})
  const [pipelinesByConnector, setPipelinesByConnector] = useState<Record<number, Pipeline[]>>({})

  const { execute: load, loading } = useApi<unknown>({ showErrorMessage: true })
  const { execute: saveOne, loading: saving } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    void load(async () => {
      const [catalog, current] = await Promise.all([
        agencyIntegrationBindingService.catalog(),
        agencyIntegrationBindingService.list(),
      ])
      setIntents(catalog)

      const byKey: Record<string, AgencyIntegrationBinding> = {}
      const draftsBuilt: Record<string, IntentDraft> = {}
      for (const intent of catalog) {
        const existing = current.find((b) => b.intentKey === intent.key)
        if (existing) {
          byKey[intent.key] = existing
          draftsBuilt[intent.key] = { connectorId: existing.connectorId, pipelineId: existing.pipelineId }
        } else {
          draftsBuilt[intent.key] = { connectorId: null, pipelineId: null }
        }
      }
      setBindings(byKey)
      setDrafts(draftsBuilt)

      const categoriesNeeded = Array.from(new Set(catalog.map((i) => i.categoryIdentifier)))
      const connectorsResults = await Promise.all(
        categoriesNeeded.map(async (identifier) => [identifier, await integrationPlatformService.getConnectorsByCategoryIdentifier(identifier)] as const),
      )
      const map: Record<string, Connector[]> = {}
      for (const [identifier, list] of connectorsResults) {
        map[identifier] = list.filter((c) => c.isActive)
      }
      setConnectorsByCategory(map)

      const connectorIds = Array.from(new Set(Object.values(map).flat().map((c) => c.id)))
      const pipelinesResults = await Promise.all(
        connectorIds.map(async (cid) => {
          const connector = Object.values(map).flat().find((c) => c.id === cid)
          if (!connector) return [cid, [] as Pipeline[]] as const
          const list = await integrationPlatformService.getPipelinesByIntegration(connector.integrationId)
          return [cid, list.filter((p) => p.isActive)] as const
        }),
      )
      const pipeMap: Record<number, Pipeline[]> = {}
      for (const [cid, list] of pipelinesResults) {
        pipeMap[cid] = list
      }
      setPipelinesByConnector(pipeMap)

      return null
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const setConnector = (intentKey: string, connectorId: number | null) => {
    setDrafts((prev) => ({
      ...prev,
      [intentKey]: { connectorId, pipelineId: null },
    }))
  }

  const setPipeline = (intentKey: string, pipelineId: number | null) => {
    setDrafts((prev) => ({
      ...prev,
      [intentKey]: { ...prev[intentKey], pipelineId },
    }))
  }

  const hasChanges = useMemo(() => {
    const result: Record<string, boolean> = {}
    for (const intent of intents) {
      const draft = drafts[intent.key]
      const current = bindings[intent.key]
      if (!draft) {
        result[intent.key] = false
        continue
      }
      if (!current) {
        result[intent.key] = !!(draft.connectorId && draft.pipelineId)
        continue
      }
      result[intent.key] = draft.connectorId !== current.connectorId || draft.pipelineId !== current.pipelineId
    }
    return result
  }, [intents, drafts, bindings])

  const handleSave = async (intentKey: string) => {
    const draft = drafts[intentKey]
    if (!draft?.connectorId || !draft?.pipelineId) return

    const result = await saveOne(() => agencyIntegrationBindingService.save({
      intentKey,
      connectorId: draft.connectorId!,
      pipelineId: draft.pipelineId!,
      isActive: true,
    }))

    if (result !== null) {
      const updated = await agencyIntegrationBindingService.getByIntentKey(intentKey)
      if (updated) {
        setBindings((prev) => ({ ...prev, [intentKey]: updated }))
      }
    }
  }

  if (loading && intents.length === 0) {
    return <div className="text-sm text-muted-foreground">{t('common.loading')}</div>
  }

  return (
    <div className="space-y-3">
      <div className="rounded-md border border-dashed border-border/70 bg-muted/20 p-4">
        <div className="flex items-start gap-3">
          <Sparkles className="mt-0.5 h-4 w-4 flex-shrink-0 text-primary" />
          <div className="space-y-1">
            <div className="text-sm font-medium text-foreground">{t('configuration.integrations.actions.title')}</div>
            <p className="text-xs text-muted-foreground">{t('configuration.integrations.actions.description')}</p>
          </div>
        </div>
      </div>

      {intents.map((intent) => {
        const connectors = connectorsByCategory[intent.categoryIdentifier] ?? []
        const draft = drafts[intent.key] ?? { connectorId: null, pipelineId: null }
        const pipelines = draft.connectorId ? (pipelinesByConnector[draft.connectorId] ?? []) : []
        const current = bindings[intent.key]
        const changed = hasChanges[intent.key]
        const noConnectors = connectors.length === 0

        return (
          <Card key={intent.key} data-testid={`intent-${intent.key}`}>
            <CardContent className="p-4">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-foreground">{intent.label}</span>
                    {current ? (
                      <Badge variant="success" className="text-[10px]"><CheckCircle2 className="mr-1 h-3 w-3" /> {t('configuration.integrations.actions.configured')}</Badge>
                    ) : (
                      <Badge variant="outline" className="text-[10px]">{t('configuration.integrations.actions.notConfigured')}</Badge>
                    )}
                  </div>
                  <div className="text-xs text-muted-foreground">
                    {t('configuration.integrations.actions.categoryLabel')}: <span className="font-mono">{intent.categoryIdentifier}</span> · {t('configuration.integrations.actions.intentLabel')}: <span className="font-mono">{intent.key}</span>
                  </div>
                </div>
              </div>

              <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-[1fr_1fr_auto]">
                <div className="space-y-1">
                  <label className="text-xs font-medium text-muted-foreground">{t('configuration.integrations.actions.connector')}</label>
                  <SearchableSelect
                    value={draft.connectorId ? String(draft.connectorId) : ''}
                    onValueChange={(value) => setConnector(intent.key, value ? Number(value) : null)}
                    options={connectors.map((c) => ({ value: String(c.id), label: c.name }))}
                    placeholder={noConnectors ? t('configuration.integrations.actions.noConnector') : t('common.placeholder.select')}
                    searchPlaceholder={t('common.placeholder.search')}
                    disabled={noConnectors}
                  />
                </div>
                <div className="space-y-1">
                  <label className="text-xs font-medium text-muted-foreground">{t('configuration.integrations.actions.pipeline')}</label>
                  <SearchableSelect
                    value={draft.pipelineId ? String(draft.pipelineId) : ''}
                    onValueChange={(value) => setPipeline(intent.key, value ? Number(value) : null)}
                    options={pipelines.map((p) => ({ value: String(p.id), label: p.name }))}
                    placeholder={!draft.connectorId ? t('configuration.integrations.actions.selectConnectorFirst') : pipelines.length === 0 ? t('configuration.integrations.actions.noPipeline') : t('common.placeholder.select')}
                    searchPlaceholder={t('common.placeholder.search')}
                    disabled={!draft.connectorId || pipelines.length === 0}
                  />
                </div>
                <div className="flex items-end">
                  <Button
                    size="sm"
                    icon={<Save className="h-4 w-4" />}
                    onClick={() => void handleSave(intent.key)}
                    disabled={!changed || saving || !draft.connectorId || !draft.pipelineId}
                  >
                    {saving ? t('common.action.saving') : t('common.action.save')}
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>
        )
      })}
    </div>
  )
}
