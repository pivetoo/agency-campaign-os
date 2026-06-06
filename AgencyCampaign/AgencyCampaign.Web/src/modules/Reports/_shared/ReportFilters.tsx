import { Input } from 'archon-ui'

interface PeriodFilterProps {
  from: string
  to: string
  onChange: (range: { from: string; to: string }) => void
}

export function ReportPeriodFilter({ from, to, onChange }: PeriodFilterProps) {
  return (
    <>
      <div className="space-y-1">
        <label className="text-xs text-muted-foreground">De</label>
        <Input type="date" value={from} onChange={(e) => onChange({ from: e.target.value, to })} />
      </div>
      <div className="space-y-1">
        <label className="text-xs text-muted-foreground">Até</label>
        <Input type="date" value={to} onChange={(e) => onChange({ from, to: e.target.value })} />
      </div>
    </>
  )
}
