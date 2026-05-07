import { PageLayout, Card, CardContent } from 'archon-ui'
import { ScrollText } from 'lucide-react'

export default function AuditDashboard() {
  return (
    <PageLayout
      title="Auditoria"
      subtitle="Em desenvolvimento"
      showDefaultActions={false}
    >
      <Card className="border-dashed">
        <CardContent className="flex flex-col items-center justify-center py-16 text-center">
          <ScrollText size={48} className="text-muted-foreground mb-4" />
          <p className="text-lg font-medium">Em breve</p>
          <p className="text-sm text-muted-foreground mt-1 max-w-md">
            Histórico unificado de eventos de propostas, oportunidades, campanhas e lançamentos financeiros.
            Ainda estamos desenhando esta área.
          </p>
        </CardContent>
      </Card>
    </PageLayout>
  )
}
