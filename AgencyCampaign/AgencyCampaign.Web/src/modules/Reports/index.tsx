import { useNavigate } from 'react-router-dom'
import { PageLayout, Card, CardContent, usePermissions } from 'archon-ui'
import { reportCatalog, reportAreaOrder, reportAreaLabels, type ReportCatalogEntry } from './catalog'

export default function Reports() {
  const navigate = useNavigate()
  const { isRoot, hasAnyPermission } = usePermissions()

  const canSee = (entry: ReportCatalogEntry) => isRoot || !entry.requires || hasAnyPermission(entry.requires)

  return (
    <PageLayout title="Relatórios" subtitle="Central de relatórios do Mainstay" showDefaultActions={false}>
      <div className="space-y-8">
        {reportAreaOrder.map((area) => {
          const entries = reportCatalog.filter((entry) => entry.area === area && canSee(entry))
          if (entries.length === 0) {
            return null
          }
          return (
            <section key={area} className="space-y-3">
              <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">{reportAreaLabels[area]}</h2>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {entries.map((entry) => (
                  <Card key={entry.id} className="cursor-pointer transition-colors hover:border-primary/40" onClick={() => navigate(entry.path)}>
                    <CardContent className="flex items-start gap-3 pt-5 pb-5">
                      <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-primary/15 text-primary">{entry.icon}</span>
                      <div className="space-y-1">
                        <p className="text-sm font-semibold">{entry.title}</p>
                        <p className="text-xs text-muted-foreground">{entry.description}</p>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </section>
          )
        })}
      </div>
    </PageLayout>
  )
}
