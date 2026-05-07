import { useEffect, useState } from 'react'
import {
  Button,
  Card,
  CardContent,
  Badge,
  useApi,
  DataTable,
} from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Plus, Zap } from 'lucide-react'
import { automationService } from '../../../services/automationService'
import { integrationPlatformService } from '../../../services/integrationPlatformService'
import { automationTriggerLabels } from '../../../types/automationTrigger'
import type { Automation } from '../../../types/automation'

interface AutomationListProps {
  onCreate: () => void
  onEdit: (automation: Automation) => void
}

interface ResolvedNames {
  connectorName?: string
  pipelineName?: string
}

export default function AutomationList({ onCreate, onEdit }: AutomationListProps) {
  const [automations, setAutomations] = useState<Automation[]>([])
  const [resolved, setResolved] = useState<Record<number, ResolvedNames>>({})

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
      title: 'Nome',
      render: (value: string, row: Automation) => (
        <div className="flex items-center gap-2">
          <Zap size={16} className={row.isActive ? 'text-green-500' : 'text-muted-foreground'} />
          <span className="font-medium">{value}</span>
        </div>
      ),
    },
    {
      key: 'trigger',
      title: 'Gatilho',
      render: (value: string) => (
        <Badge variant="secondary">{automationTriggerLabels[value] ?? value}</Badge>
      ),
    },
    {
      key: 'connectorId',
      title: 'Conector',
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
      title: 'Pipeline',
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
      title: 'Status',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'outline'}>
          {value ? 'Ativa' : 'Inativa'}
        </Badge>
      ),
    },
    {
      key: 'actions',
      title: 'Ações',
      render: (_: unknown, row: Automation) => (
        <Button size="sm" variant="ghost" onClick={() => onEdit(row)}>
          Editar
        </Button>
      ),
    },
  ]

  const activeCount = automations.filter((a) => a.isActive).length
  const inactiveCount = automations.filter((a) => !a.isActive).length

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Badge variant="success">{activeCount} ativas</Badge>
          {inactiveCount > 0 && <Badge variant="outline">{inactiveCount} inativas</Badge>}
        </div>
        <Button onClick={onCreate}>
          <Plus size={16} className="mr-1" />
          Nova automação
        </Button>
      </div>

      {automations.length > 0 ? (
        <DataTable
          columns={columns}
          data={automations}
          rowKey="id"
          emptyText="Nenhuma automação cadastrada"
          loading={loading}
          pageSize={10}
        />
      ) : (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Zap size={48} className="text-muted-foreground mb-4" />
            <p className="text-lg font-medium">Nenhuma automação configurada</p>
            <p className="text-sm text-muted-foreground mt-1">
              Crie automações para executar pipelines quando eventos acontecerem no Kanvas.
            </p>
            <Button className="mt-4" onClick={onCreate}>
              <Plus size={16} className="mr-1" />
              Criar primeira automação
            </Button>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
