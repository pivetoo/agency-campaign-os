import type { ReactNode } from 'react'
import { useState } from 'react'
import { PageLayout, Card, CardContent, Button } from 'archon-ui'
import { Download, FileText } from 'lucide-react'

interface ReportLayoutProps {
  title: string
  subtitle?: string
  filters?: ReactNode
  onRefresh?: () => void
  onExportCsv?: () => void | Promise<void>
  onExportPdf?: () => void | Promise<void>
  children: ReactNode
}

export default function ReportLayout({ title, subtitle, filters, onRefresh, onExportCsv, onExportPdf, children }: ReportLayoutProps) {
  const [exporting, setExporting] = useState<'csv' | 'pdf' | null>(null)

  const runExport = async (kind: 'csv' | 'pdf', fn?: () => void | Promise<void>) => {
    if (!fn) {
      return
    }
    setExporting(kind)
    try {
      await fn()
    } finally {
      setExporting(null)
    }
  }

  return (
    <PageLayout title={title} subtitle={subtitle} onRefresh={onRefresh} showDefaultActions={false}>
      <Card>
        <CardContent className="pt-4 space-y-4">
          <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
            <div className="flex flex-1 flex-wrap items-end gap-3">{filters}</div>
            <div className="flex shrink-0 items-center gap-2">
              {onExportCsv && (
                <Button variant="outline" size="sm" disabled={exporting !== null} onClick={() => void runExport('csv', onExportCsv)}>
                  <Download className="mr-1.5 h-4 w-4" />CSV
                </Button>
              )}
              {onExportPdf && (
                <Button variant="outline" size="sm" disabled={exporting !== null} onClick={() => void runExport('pdf', onExportPdf)}>
                  <FileText className="mr-1.5 h-4 w-4" />PDF
                </Button>
              )}
            </div>
          </div>
          {children}
        </CardContent>
      </Card>
    </PageLayout>
  )
}
