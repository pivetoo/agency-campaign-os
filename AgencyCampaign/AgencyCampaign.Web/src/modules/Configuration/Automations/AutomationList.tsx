import { useEffect, useState } from 'react'
import {
  Button,
  Card,
  CardContent,
  Badge,
  useApi,
  useI18n,
  DataTable,
} from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Plus, Zap, ClipboardList } from 'lucide-react'
import { automationService } from '../../../services/automationService'
import { integrationPlatformService } from '../../../services/integrationPlatformService'
import { automationTriggerLabels } from '../../../types/automationTrigger'
import type { Automation } from '../../../types/automation'
import AutomationExecutionLogsSheet from '../../../components/sheets/AutomationExecutionLogsSheet'

interface AutomationListProps {
  onCreate: () => void
  onEdit: (automation: Automation) => void
}

interface ResolvedNames {
  connectorName?: string
  pipelineName?: string
}

export default function AutomationList({ onCreate, onEdit }: AutomationListProps) {
  const { t } = useI18n()
  const [automations, setAutomations] = useState<Automation[]>([])
  const [resolved, setResolved] = useState<Record<number, ResolvedNames>>({})
  const [logsAutomation, setLogsAutomation] = useState<Automation | null>(null)

  const { execute: fetchAutomations, loading } = useApi<{
    items: Automation[]
    pagination: { totalItems: number; totalPages: number; currentPage: number; pageSize: number }
  }>({ showErrorMessage: true })

  useEffect(() => {
    void loadAutomations()
  }, [])

  async function loadAutomations() {
    const result = await fetchAutomations(() => automationService.getAutomations())
    if (result) {
      setAutomations(result.items)
      void resolveLookups(result.items)
    }
  }

  async function resolveLookups(items: Automation[]) {
    const lookups = await Promise.all(
      items.map(async (auto) => {
        try {
          const detail = await integrationPlatformService.getConnectorDetail(auto.connectorId)
          if (!detail?.connector) return [auto.id, {} as ResolvedNames] as const
          const integrationId = detail.connector.integrationId
          const pipelines = integrationId
            ? await integrationPlatformService.getPipelinesByIntegration(integrationId)
            : []
          const pipelineName = pipelines.find((p) => p.id === auto.pipelineId)?.name
          return [
            auto.id,
            {
              connectorName: detail.connector.name,
              pipelineName,
            } as ResolvedNames,
          ] as const
        } catch {
          return [auto.id, {} as ResolvedNames] as const
        }
      }),
    )
    setResolved(Object.fromEntries(lookups))
  }

  const columns: DataTableColumn<Automation>[] = [
    {
      key: 'name',
      title: t('common.field.name'),
      render: (value: string, row: Automation) => (
        <div className="flex items-center gap-2">
          <Zap size={16} className={row.isActive ? 'text-green-500' : 'text-muted-foreground'} />
          <span className="font-medium">{value}</span>
        </div>
      ),
    },
    {
      key: 'trigger',
      title: t('automations.column.when'),
      render: (value: string) => (
        <Badge variant="secondary">{automationTriggerLabels[value] ?? value}</Badge>
      ),
    },
    {
      key: 'connectorId',
      title: t('common.field.account'),
      render: (_value: number, row: Automation) => {
        const lookup = resolved[row.id]
        return lookup?.connectorName ? (
          <span className="text-sm">{lookup.connectorName}</span>
        ) : (
          <span className="text-xs text-muted-foreground">#{row.connectorId}</span>
        )
      },
    },
    {
      key: 'pipelineId',
      title: t('automations.column.action'),
      render: (_value: number, row: Automation) => {
        const lookup = resolved[row.id]
        return lookup?.pipelineName ? (
          <span className="text-sm">{lookup.pipelineName}</span>
        ) : (
          <span className="text-xs text-muted-foreground">#{row.pipelineId}</span>
        )
      },
    },
    {
      key: 'isActive',
      title: t('common.field.status'),
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'outline'}>
          {value ? t('common.status.activeFemale') : t('common.status.inactiveFemale')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      title: t('automations.column.actions'),
      render: (_: unknown, row: Automation) => (
        <div className="flex items-center gap-1">
          <Button size="sm" variant="ghost" onClick={() => onEdit(row)}>
            {t('common.action.edit')}
          </Button>
          <Button
            size="sm"
            variant="ghost"
            className="text-muted-foreground"
            onClick={() => setLogsAutomation(row)}
            title={t('automations.logs.title')}
          >
            <ClipboardList size={15} />
          </Button>
        </div>
      ),
    },
  ]

  const activeCount = automations.filter((a) => a.isActive).length
  const inactiveCount = automations.filter((a) => !a.isActive).length

  return (
    <>
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Badge variant="success">{t('automations.count.active').replace('{0}', String(activeCount))}</Badge>
          {inactiveCount > 0 && <Badge variant="outline">{t('automations.count.inactive').replace('{0}', String(inactiveCount))}</Badge>}
        </div>
        <Button onClick={onCreate}>
          <Plus size={16} className="mr-1" />
          {t('automations.action.new')}
        </Button>
      </div>

      {automations.length > 0 ? (
        <DataTable
          columns={columns}
          data={automations}
          rowKey="id"
          emptyText={t('automations.empty')}
          loading={loading}
          pageSize={10}
        />
      ) : (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Zap size={48} className="text-muted-foreground mb-4" />
            <p className="text-lg font-medium">{t('automations.empty.title')}</p>
            <p className="text-sm text-muted-foreground mt-1">
              {t('automations.empty.description')}
            </p>
            <Button className="mt-4" onClick={onCreate}>
              <Plus size={16} className="mr-1" />
              {t('automations.action.createFirst')}
            </Button>
          </CardContent>
        </Card>
      )}
    </div>

    <AutomationExecutionLogsSheet
      automation={logsAutomation}
      open={logsAutomation !== null}
      onClose={() => setLogsAutomation(null)}
    />
    </>
  )
}
