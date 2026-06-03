import { PageLayout, Card, CardContent, EmptyState } from 'archon-ui'
import { FileBarChart2 } from 'lucide-react'

export default function Reports() {
  return (
    <PageLayout title="Relatórios" subtitle="Central de relatórios do Kanvas" showDefaultActions={false}>
      <Card>
        <CardContent className="py-16">
          <EmptyState
            icon={<FileBarChart2 className="h-10 w-10" />}
            title="Relatórios em breve"
            description="Este módulo ainda está em construção. Em breve você encontrará aqui os relatórios consolidados das suas campanhas."
          />
        </CardContent>
      </Card>
    </PageLayout>
  )
}
