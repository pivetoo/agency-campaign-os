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
import type { Automation } from '../../../types/automation'

interface AutomationListProps {
  onCreate: () => void
  onEdit: (automation: Automation) => void
}

export default function AutomationList({ onCreate, onEdit }: AutomationListProps) {
  const [automations, setAutomations] = useState<Automation[]>([])

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
    }
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
        <Badge variant="secondary">{translateTrigger(value)}</Badge>
      ),
    },
    {
      key: 'connectorId',
      title: 'Conector',
      render: (value: number) => <span className="text-sm text-muted-foreground">ID: {value}</span>,
    },
    {
      key: 'pipelineId',
      title: 'Pipeline',
      render: (value: number) => <span className="text-sm text-muted-foreground">ID: {value}</span>,
    },
    {
      key: 'isActive',
      title: 'Status',
      render: (value: boolean) => (
        <Badge variant={value ? 'default' : 'outline'}>
          {value ? 'Ativo' : 'Inativo'}
        </Badge>
      ),
    },
    {
      key: 'actions',
      title: 'Acoes',
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
          <Badge variant="default">{activeCount} ativas</Badge>
          {inactiveCount > 0 && <Badge variant="outline">{inactiveCount} inativas</Badge>}
        </div>
        <Button onClick={onCreate}>
          <Plus size={16} className="mr-1" />
          Nova automacao
        </Button>
      </div>

      {automations.length > 0 ? (
        <DataTable
          columns={columns}
          data={automations}
          rowKey="id"
          emptyText="Nenhuma automacao cadastrada"
          loading={loading}
          pageSize={10}
        />
      ) : (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Zap size={48} className="text-muted-foreground mb-4" />
            <p className="text-lg font-medium">Nenhuma automacao configurada</p>
            <p className="text-sm text-muted-foreground mt-1">
              Crie automacoes para executar pipelines quando eventos acontecerem no Kanvas.
            </p>
            <Button className="mt-4" onClick={onCreate}>
              <Plus size={16} className="mr-1" />
              Criar primeira automacao
            </Button>
          </CardContent>
        </Card>
      )}
    </div>
  )
}

function translateTrigger(trigger: string): string {
  const map: Record<string, string> = {
    campaign_approved: 'Campanha aprovada',
    proposal_approved: 'Proposta aprovada',
    follow_up_overdue: 'Follow-up atrasado',
    new_campaign: 'Nova campanha criada',
    payment_due: 'Conta a receber vencida',
  }
  return map[trigger] ?? trigger
}
