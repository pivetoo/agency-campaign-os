import { PageLayout } from 'archon-ui'
import { ScrollText } from 'lucide-react'

export default function IntegrationLogs() {
  return (
    <PageLayout title="Logs de integração" subtitle="Histórico de execuções das integrações configuradas.">
      <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-24 text-muted-foreground">
        <ScrollText size={42} className="mb-3 opacity-40" />
        <p className="text-base font-medium">Em breve</p>
        <p className="mt-1 text-sm">O histórico detalhado de execuções estará disponível aqui.</p>
      </div>
    </PageLayout>
  )
}
