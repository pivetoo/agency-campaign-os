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

export default function AutomationFormModal({ open, onOpenChange, automation, presetConnectorId, onSuccess }: Props) {
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
  const [pipelines, setPipelines] = useState<Pipeline[]>([])

  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    void integrationPlatformService.getActiveIntegrationCategories().then(setCategories)
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
      setCategoryId(null)
      setIntegrationId(null)
      setConnectorId(presetConnectorId ?? null)
      setPipelineId(null)
      setMappingRows([])
      setIsActive(true)

      if (presetConnectorId) {
        void resolveSelectionFromConnector(presetConnectorId)
      }
    }
  }, [open, automation, presetConnectorId])

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
    void integrationPlatformService.getIntegrationsByCategory(categoryId).then(setIntegrations)
  }, [categoryId])

  useEffect(() => {
    if (!integrationId) {
      setConnectors([])
      setPipelines([])
      return
    }
    void integrationPlatformService.getConnectorsByIntegration(integrationId).then(setConnectors)
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
          <ModalTitle>{isEditing ? 'Editar automação' : 'Nova automação'}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Nome</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: Enviar boleto quando criar conta a receber" required />
            </div>

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">Disparar quando</label>
              <SearchableSelect
                value={trigger}
                onValueChange={setTrigger}
                options={triggerOptions}
                placeholder="Selecione um evento"
                searchPlaceholder="Buscar evento"
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Categoria de integração</label>
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
              <label className="text-sm font-medium">Integração</label>
              <SearchableSelect
                value={integrationId ? String(integrationId) : ''}
                onValueChange={(value) => {
                  setIntegrationId(Number(value))
                  setConnectorId(null)
                  setPipelineId(null)
                }}
                options={integrations.map((integ) => ({ value: String(integ.id), label: integ.name }))}
                placeholder="Selecione"
                searchPlaceholder="Buscar integração"
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Conta conectada</label>
              <SearchableSelect
                value={connectorId ? String(connectorId) : ''}
                onValueChange={(value) => setConnectorId(Number(value))}
                options={connectors.filter((c) => c.isActive).map((c) => ({ value: String(c.id), label: c.name }))}
                placeholder="Selecione"
                searchPlaceholder="Buscar conta"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Ação a executar</label>
              <SearchableSelect
                value={pipelineId ? String(pipelineId) : ''}
                onValueChange={(value) => setPipelineId(Number(value))}
                options={pipelines.map((p) => ({ value: String(p.id), label: p.name }))}
                placeholder="Selecione"
                searchPlaceholder="Buscar ação"
              />
            </div>

            <label className="flex items-center gap-2 text-sm md:col-span-2">
              <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
              <span>Automação ativa</span>
            </label>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={loading || !isValid}>{loading ? 'Salvando...' : 'Salvar'}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
