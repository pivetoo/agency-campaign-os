import { useEffect, useMemo, useState } from 'react'
import { Card, CardContent, Button, Input, useApi } from 'archon-ui'
import { ResponsiveLine } from '@nivo/line'
import { ResponsivePie } from '@nivo/pie'
import { Activity, RefreshCw, TrendingUp, Users, FileEdit } from 'lucide-react'
import { auditService } from '../../services/auditService'
import { AuditAction, auditActionLabels, type AuditActionValue, type AuditStats } from '../../types/audit'

const moduleColors: Record<AuditActionValue, string> = {
  1: '#22c55e', // Insert
  2: '#6366f1', // Update
  3: '#ef4444', // Delete
}

const formatDate = (date: string) => {
  const [, month, day] = date.split('-')
  return `${day}/${month}`
}

const todayIso = () => new Date().toISOString().slice(0, 10)
const daysAgoIso = (days: number) => {
  const target = new Date()
  target.setDate(target.getDate() - days)
  return target.toISOString().slice(0, 10)
}

export default function AuditDashboard() {
  const [stats, setStats] = useState<AuditStats | null>(null)
  const [from, setFrom] = useState(daysAgoIso(30))
  const [to, setTo] = useState(todayIso())
  const { execute, loading } = useApi<AuditStats>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => auditService.getStats(from, to))
    if (result) setStats(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const lineData = useMemo(() => {
    if (!stats) return []
    return [
      {
        id: 'eventos',
        data: stats.volumeByDay.map((point) => ({ x: formatDate(point.date), y: point.count })),
      },
    ]
  }, [stats])

  const actionPieData = useMemo(() => {
    if (!stats) return []
    return stats.actionDistribution.map((item) => ({
      id: auditActionLabels[item.action],
      label: auditActionLabels[item.action],
      value: item.count,
      color: moduleColors[item.action],
    }))
  }, [stats])

  const maxUserCount = stats?.topUsers.reduce((max, item) => Math.max(max, item.count), 0) ?? 0
  const maxEntityCount = stats?.topEntities.reduce((max, item) => Math.max(max, item.count), 0) ?? 0

  const actionTotals = useMemo(() => {
    const map: Record<AuditActionValue, number> = { 1: 0, 2: 0, 3: 0 }
    stats?.actionDistribution.forEach((item) => {
      map[item.action] = item.count
    })
    return map
  }, [stats])

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div className="border-l-4 border-primary pl-5">
          <h1 className="text-3xl font-bold text-foreground tracking-tight">
            <strong className="text-primary">Dashboard</strong>
          </h1>
          <p className="text-lg text-muted-foreground mt-3 leading-relaxed">
            Visão consolidada das alterações registradas no sistema
          </p>
        </div>
        <div className="flex flex-wrap items-end gap-2">
          <div>
            <label className="block text-xs text-muted-foreground mb-1">De</label>
            <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="h-9 w-40" />
          </div>
          <div>
            <label className="block text-xs text-muted-foreground mb-1">Até</label>
            <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="h-9 w-40" />
          </div>
          <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
            <RefreshCw className="mr-1 h-3.5 w-3.5" /> Aplicar
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4">
        <Card>
          <CardContent className="flex items-center justify-between pt-5 pb-5">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">Total de eventos</p>
              <p className="text-2xl font-semibold mt-1">{stats?.totalEntries ?? 0}</p>
            </div>
            <div className="rounded-lg bg-primary/15 p-2 text-primary">
              <Activity className="h-5 w-5" />
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center justify-between pt-5 pb-5">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">Criações</p>
              <p className="text-2xl font-semibold mt-1 text-green-600">{actionTotals[AuditAction.Insert]}</p>
            </div>
            <div className="rounded-lg bg-green-100 p-2 text-green-600">
              <TrendingUp className="h-5 w-5" />
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center justify-between pt-5 pb-5">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">Edições</p>
              <p className="text-2xl font-semibold mt-1 text-indigo-600">{actionTotals[AuditAction.Update]}</p>
            </div>
            <div className="rounded-lg bg-indigo-100 p-2 text-indigo-600">
              <FileEdit className="h-5 w-5" />
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center justify-between pt-5 pb-5">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">Usuários ativos</p>
              <p className="text-2xl font-semibold mt-1">{stats?.topUsers.length ?? 0}</p>
            </div>
            <div className="rounded-lg bg-amber-100 p-2 text-amber-600">
              <Users className="h-5 w-5" />
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-3 xl:grid-cols-[1.4fr_0.9fr]">
        <Card>
          <CardContent className="pt-5">
            <p className="text-sm font-medium mb-3">Eventos por dia</p>
            <div style={{ height: 260 }}>
              {stats && stats.volumeByDay.length > 0 ? (
                <ResponsiveLine
                  data={lineData}
                  margin={{ top: 16, right: 24, bottom: 36, left: 40 }}
                  xScale={{ type: 'point' }}
                  yScale={{ type: 'linear', min: 0, max: 'auto' }}
                  axisLeft={{ tickValues: 4 }}
                  axisBottom={{ tickRotation: -25 }}
                  enableArea
                  areaOpacity={0.18}
                  colors={['#6366f1']}
                  pointSize={4}
                  useMesh
                  curve="monotoneX"
                />
              ) : (
                <p className="flex h-full items-center justify-center text-sm text-muted-foreground">
                  Nenhum evento no período.
                </p>
              )}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-5">
            <p className="text-sm font-medium mb-3">Distribuição por ação</p>
            <div style={{ height: 260 }}>
              {actionPieData.length > 0 ? (
                <ResponsivePie
                  data={actionPieData}
                  margin={{ top: 8, right: 80, bottom: 8, left: 8 }}
                  innerRadius={0.55}
                  padAngle={1}
                  cornerRadius={3}
                  colors={{ datum: 'data.color' }}
                  arcLabelsSkipAngle={12}
                  arcLinkLabelsSkipAngle={10}
                  arcLinkLabelsThickness={1}
                />
              ) : (
                <p className="flex h-full items-center justify-center text-sm text-muted-foreground">
                  Sem dados.
                </p>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-3 xl:grid-cols-2">
        <Card>
          <CardContent className="pt-5">
            <p className="text-sm font-medium mb-3">Top usuários</p>
            {stats && stats.topUsers.length > 0 ? (
              <ul className="space-y-2">
                {stats.topUsers.map((item) => (
                  <li key={item.name} className="flex items-center gap-3 text-sm">
                    <span className="w-48 truncate" title={item.name}>{item.name}</span>
                    <div className="flex-1 h-2 bg-muted rounded">
                      <div
                        className="h-full bg-primary rounded"
                        style={{ width: maxUserCount > 0 ? `${(item.count / maxUserCount) * 100}%` : 0 }}
                      />
                    </div>
                    <span className="w-12 text-right font-medium">{item.count}</span>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-sm text-muted-foreground">Nenhum usuário com eventos.</p>
            )}
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-5">
            <p className="text-sm font-medium mb-3">Top entidades alteradas</p>
            {stats && stats.topEntities.length > 0 ? (
              <ul className="space-y-2">
                {stats.topEntities.map((item) => (
                  <li key={item.name} className="flex items-center gap-3 text-sm">
                    <span className="w-48 truncate" title={item.name}>{item.name}</span>
                    <div className="flex-1 h-2 bg-muted rounded">
                      <div
                        className="h-full bg-indigo-500 rounded"
                        style={{ width: maxEntityCount > 0 ? `${(item.count / maxEntityCount) * 100}%` : 0 }}
                      />
                    </div>
                    <span className="w-12 text-right font-medium">{item.count}</span>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-sm text-muted-foreground">Nenhuma entidade alterada no período.</p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
