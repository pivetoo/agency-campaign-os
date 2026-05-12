import { useEffect, useMemo, useState } from 'react'
import {
  Button,
  Checkbox,
  Input,
  Modal,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  SearchableSelect,
  useApi,
  useI18n,
} from 'archon-ui'
import { automationService } from '../../services/automationService'
import { integrationPlatformService } from '../../services/integrationPlatformService'
import {
  automationTriggerGroups,
  automationTriggerLabels,
} from '../../types/automationTrigger'
import type { Automation, CreateAutomationPayload } from '../../types/automation'
import type {
  Connector,
  IntegrationCategory,
  IntegrationPlatformIntegration,
  Pipeline,
} from '../../types/integrationPlatform'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  automation: Automation | null
  presetConnectorId?: number | null
  presetCategoryId?: number | null
  presetIntegrationId?: number | null
  onSuccess: () => void
}

interface MappingRow {
  id: string
  key: string
  value: string
}

const triggerOptions = automationTriggerGroups.flatMap((group) =>
  group.triggers.map((trigger) => ({
    value: trigger,
    label: `${group.label} · ${automationTriggerLabels[trigger] ?? trigger}`,
  })),
)

function parseMappingJson(json: string | undefined): MappingRow[] {
  if (!json) return []
  try {
    const parsed = JSON.parse(json) as Record<string, string>
    return Object.entries(parsed).map(([key, value], index) => ({
      id: `${index}-${key}`,
      key,
      value: value ?? '',
    }))
  } catch {
    return []
  }
}

function mappingToObject(rows: MappingRow[]): Record<string, string> {
  return rows.reduce<Record<string, string>>((acc, row) => {
    if (row.key.trim()) {
      acc[row.key.trim()] = row.value
    }
    return acc
  }, {})
}

export default function AutomationFormModal({ open, onOpenChange, automation, presetConnectorId, presetCategoryId, presetIntegrationId, onSuccess }: Props) {
  const { t } = useI18n()
  const isEditing = !!automation
  const [name, setName] = useState('')
  const [trigger, setTrigger] = useState<string>('proposal_sent')
  const [triggerCondition, setTriggerCondition] = useState('')
  const [categoryId, setCategoryId] = useState<number | null>(null)
  const [integrationId, setIntegrationId] = useState<number | null>(null)
  const [connectorId, setConnectorId] = useState<number | null>(null)
  const [pipelineId, setPipelineId] = useState<number | null>(null)
  const [mappingRows, setMappingRows] = useState<MappingRow[]>([])
  const [isActive, setIsActive] = useState(true)

  const [categories, setCategories] = useState<IntegrationCategory[]>([])
  const [integrations, setIntegrations] = useState<IntegrationPlatformIntegration[]>([])
  const [connectors, setConnectors] = useState<Connector[]>([])
  const [activeConnectors, setActiveConnectors] = useState<Connector[]>([])
  const [pipelines, setPipelines] = useState<Pipeline[]>([])

  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    void integrationPlatformService.getActiveIntegrationCategories().then(setCategories)
    void integrationPlatformService.getActiveConnectors().then(setActiveConnectors)
  }, [open])

  useEffect(() => {
    if (!open) return
    if (automation) {
      setName(automation.name)
      setTrigger(automation.trigger)
      setTriggerCondition(automation.triggerCondition ?? '')
      setConnectorId(automation.connectorId)
      setPipelineId(automation.pipelineId)
      setMappingRows(parseMappingJson(automation.variableMappingJson))
      setIsActive(automation.isActive)

      void resolveSelectionFromConnector(automation.connectorId)
    } else {
      setName('')
      setTrigger('proposal_sent')
      setTriggerCondition('')
      setCategoryId(presetCategoryId ?? null)
      setIntegrationId(presetIntegrationId ?? null)
      setConnectorId(presetConnectorId ?? null)
      setPipelineId(null)
      setMappingRows([])
      setIsActive(true)

      if (presetConnectorId && !presetIntegrationId) {
        void resolveSelectionFromConnector(presetConnectorId)
      }
    }
  }, [open, automation, presetConnectorId, presetCategoryId, presetIntegrationId])

  const resolveSelectionFromConnector = async (connectorIdValue: number) => {
    try {
      const detail = await integrationPlatformService.getConnectorDetail(connectorIdValue)
      if (detail?.connector?.integrationId) {
        setIntegrationId(detail.connector.integrationId)
      }
    } catch {
      // ignore
    }
  }

  useEffect(() => {
    if (!categoryId) {
      setIntegrations([])
      return
    }
    void integrationPlatformService.getIntegrationsByCategory(categoryId).then((all) => {
      const connectedIds = new Set(activeConnectors.map((connector) => connector.integrationId))
      setIntegrations(all.filter((integration) => connectedIds.has(integration.id)))
    })
  }, [categoryId, activeConnectors])

  useEffect(() => {
    if (!integrationId) {
      setConnectors([])
      setPipelines([])
      return
    }
    void integrationPlatformService.getConnectorsByIntegration(integrationId).then((result) => {
      setConnectors(result)
      if (!presetConnectorId) {
        const active = result.filter((c) => c.isActive)
        if (active.length === 1) {
          setConnectorId(active[0].id)
        }
      }
    })
    void integrationPlatformService.getPipelinesByIntegration(integrationId).then(setPipelines)
  }, [integrationId])

  const isValid = useMemo(
    () => name.trim().length >= 2 && connectorId !== null && pipelineId !== null,
    [name, connectorId, pipelineId],
  )

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!connectorId || !pipelineId) return
    const payload: CreateAutomationPayload = {
      name: name.trim(),
      trigger,
      triggerCondition: triggerCondition.trim() || undefined,
      connectorId,
      pipelineId,
      variableMapping: mappingToObject(mappingRows),
      isActive,
    }
    const result = await execute(() => {
      if (isEditing && automation) {
        return automationService.updateAutomation(automation.id, { id: automation.id, ...payload })
      }
      return automationService.createAutomation(payload)
    })
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '880px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.automation.title.edit') : t('modal.automation.title.new')}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('common.field.name')}</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: Enviar boleto quando criar conta a receber" required />
            </div>

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('modal.automation.field.triggerOn')}</label>
              <SearchableSelect
                value={trigger}
                onValueChange={setTrigger}
                options={triggerOptions}
                placeholder={t('modal.automation.placeholder.event')}
                searchPlaceholder={t('modal.automation.placeholder.searchEvent')}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.automation.field.integrationCategory')}</label>
              <SearchableSelect
                value={categoryId ? String(categoryId) : ''}
                onValueChange={(value) => {
                  setCategoryId(Number(value))
                  setIntegrationId(null)
                  setConnectorId(null)
                  setPipelineId(null)
                }}
                options={categories.map((cat) => ({ value: String(cat.id), label: cat.name }))}
                placeholder="Selecione"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.automation.field.integration')}</label>
              <SearchableSelect
                value={integrationId ? String(integrationId) : ''}
                onValueChange={(value) => {
                  setIntegrationId(Number(value))
                  setConnectorId(null)
                  setPipelineId(null)
                }}
                options={integrations.map((integ) => ({ value: String(integ.id), label: integ.name }))}
                placeholder={categoryId && integrations.length === 0 ? t('modal.automation.placeholder.noAccounts') : 'Selecione'}
                searchPlaceholder={t('modal.automation.placeholder.searchIntegration')}
                disabled={!categoryId || integrations.length === 0}
              />
              {categoryId && integrations.length === 0 && (
                <p className="text-[10px] text-muted-foreground">
                  {t('modal.automation.hint.connectFirst')}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.automation.field.connectedAccount')}</label>
              <SearchableSelect
                value={connectorId ? String(connectorId) : ''}
                onValueChange={(value) => setConnectorId(Number(value))}
                options={connectors.filter((c) => c.isActive).map((c) => ({ value: String(c.id), label: c.name }))}
                placeholder="Selecione"
                searchPlaceholder={t('modal.automation.placeholder.searchAccount')}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.automation.field.action')}</label>
              <SearchableSelect
                value={pipelineId ? String(pipelineId) : ''}
                onValueChange={(value) => setPipelineId(Number(value))}
                options={pipelines.filter((p) => !p.isTestPipeline).map((p) => ({ value: String(p.id), label: p.name }))}
                placeholder="Selecione"
                searchPlaceholder={t('modal.automation.placeholder.searchAction')}
              />
            </div>

            <label className="flex items-center gap-2 text-sm md:col-span-2">
              <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
              <span>{t('modal.automation.field.isActive')}</span>
            </label>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || !isValid}>{loading ? t('common.action.saving') : t('common.action.save')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
