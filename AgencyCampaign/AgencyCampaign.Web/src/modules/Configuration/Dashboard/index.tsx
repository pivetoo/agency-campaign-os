import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, CardContent, Badge, Button, useApi, useI18n } from 'archon-ui'
import { ResponsivePie } from '@nivo/pie'
import {
  Building2,
  Plug,
  Wallet,
  Zap,
  CheckCircle2,
  AlertCircle,
  ArrowRight,
  Tag,
  Sparkles,
  RefreshCw,
} from 'lucide-react'
import { agencySettingsService } from '../../../services/agencySettingsService'
import { automationService } from '../../../services/automationService'
import { financialAccountService } from '../../../services/financialAccountService'
import { integrationPlatformService } from '../../../services/integrationPlatformService'
import { automationTriggerLabels } from '../../../types/automationTrigger'
import type { AgencySettings } from '../../../types/agencySettings'
import type { Automation } from '../../../types/automation'
import type { FinancialAccount } from '../../../types/financialAccount'
import type { IntegrationCategory } from '../../../types/integrationPlatform'

function classifyTrigger(trigger: string): 'Comercial' | 'Operação' | 'Financeiro' | 'Outros' {
  if (trigger.startsWith('proposal_') || trigger.startsWith('opportunity_') || trigger === 'follow_up_overdue') return 'Comercial'
  if (trigger.startsWith('campaign_') || trigger.startsWith('deliverable_')) return 'Operação'
  if (trigger.startsWith('financial_')) return 'Financeiro'
  return 'Outros'
}

const moduleColors: Record<string, string> = {
  Comercial: '#6366f1',
  Operação: '#0ea5e9',
  Financeiro: '#10b981',
  Outros: '#a1a1aa',
}

export default function ConfigurationDashboard() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [agency, setAgency] = useState<AgencySettings | null>(null)
  const [automations, setAutomations] = useState<Automation[]>([])
  const [accounts, setAccounts] = useState<FinancialAccount[]>([])
  const [categories, setCategories] = useState<IntegrationCategory[]>([])
  const { execute: runLoad, loading } = useApi<unknown>({ showErrorMessage: true })

  const load = async () => {
    await runLoad(async () => {
      const [agencyResult, automationsResult, accountsResult, categoriesResult] = await Promise.all([
        agencySettingsService.get(),
        automationService.getAutomations(1, 100),
        financialAccountService.getAll({ pageSize: 200, includeInactive: true }),
        integrationPlatformService.getActiveIntegrationCategories().catch(() => [] as IntegrationCategory[]),
      ])
      setAgency(agencyResult)
      setAutomations(automationsResult.items)
      setAccounts(accountsResult.data ?? [])
      setCategories(categoriesResult)
      return null
    })
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const activeAutomations = useMemo(() => automations.filter((a) => a.isActive), [automations])
  const activeAccounts = useMemo(() => accounts.filter((a) => a.isActive), [accounts])

  const automationByModule = useMemo(() => {
    const buckets: Record<string, number> = { Comercial: 0, Operação: 0, Financeiro: 0, Outros: 0 }
    for (const auto of activeAutomations) {
      const bucket = classifyTrigger(auto.trigger)
      buckets[bucket] = (buckets[bucket] ?? 0) + 1
    }
    return Object.entries(buckets)
      .filter(([, value]) => value > 0)
      .map(([id, value]) => ({ id, label: id, value, color: moduleColors[id] }))
  }, [activeAutomations])

  const recentAutomations = useMemo(() => {
    return [...automations]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5)
  }, [automations])

  const healthChecks = useMemo(() => {
    return [
      {
        label: 'Empresa identificada',
        ok: !!agency?.agencyName && agency.agencyName !== 'Minha agência',
        cta: () => navigate('/configuracao/empresa'),
      },
      {
        label: 'Logo da agência',
        ok: !!agency?.logoUrl,
        cta: () => navigate('/configuracao/empresa'),
      },
      {
        label: 'Conta financeira cadastrada',
        ok: activeAccounts.length > 0,
        cta: () => navigate('/configuracao/contas-financeiras'),
      },
      {
        label: 'Automação ativa',
        ok: activeAutomations.length > 0,
        cta: () => navigate('/configuracao/integracoes'),
      },
    ]
  }, [agency, activeAccounts, activeAutomations, navigate])

  const completedHealth = healthChecks.filter((c) => c.ok).length
  const healthPercent = Math.round((completedHealth / healthChecks.length) * 100)

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div className="border-l-4 border-primary pl-5">
          <h1 className="text-3xl font-bold text-foreground tracking-tight">
            <strong className="text-primary">{t('configuration.dashboard.title')}</strong>
          </h1>
          <p className="text-lg text-muted-foreground mt-3 leading-relaxed">
            Visão geral dos cadastros, automações e integrações
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={() => void load()}>
          <RefreshCw className="mr-1 h-3.5 w-3.5" /> {t('configuration.dashboard.refresh')}
        </Button>
      </div>

      <div className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4 mb-4">
        <Card>
          <CardContent className="pt-5 pb-5 flex items-start justify-between">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('configuration.dashboard.kpi.activeAutomations')}</p>
              <p className="text-2xl font-semibold mt-1">{activeAutomations.length}</p>
              <p className="text-[10px] text-muted-foreground">{automations.length - activeAutomations.length} inativas</p>
            </div>
            <span className="rounded-md bg-primary/15 p-2 text-primary"><Zap size={18} /></span>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-5 pb-5 flex items-start justify-between">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('configuration.dashboard.kpi.bankAccounts')}</p>
              <p className="text-2xl font-semibold mt-1">{activeAccounts.length}</p>
              <p className="text-[10px] text-muted-foreground">{accounts.length} no total</p>
            </div>
            <span className="rounded-md bg-primary/15 p-2 text-primary"><Wallet size={18} /></span>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-5 pb-5 flex items-start justify-between">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">{t('configuration.dashboard.kpi.integrationCategories')}</p>
              <p className="text-2xl font-semibold mt-1">{categories.length}</p>
              <p className="text-[10px] text-muted-foreground">disponíveis no IntegrationPlatform</p>
            </div>
            <span className="rounded-md bg-primary/15 p-2 text-primary"><Plug size={18} /></span>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 gap-3 lg:grid-cols-3 mb-4">
        <Card className="lg:col-span-2">
          <CardContent className="pt-5 pb-5">
            <div className="flex items-center justify-between mb-3">
              <div>
                <p className="text-sm font-semibold">{t('configuration.dashboard.health.title')}</p>
                <p className="text-xs text-muted-foreground">{completedHealth} de {healthChecks.length} itens · {healthPercent}%</p>
              </div>
              <span className="text-2xl font-semibold" style={{ color: healthPercent >= 80 ? '#10b981' : healthPercent >= 50 ? '#f59e0b' : '#ef4444' }}>
                {healthPercent}%
              </span>
            </div>
            <div className="h-2 w-full rounded-full bg-muted overflow-hidden mb-4">
              <div
                className="h-full transition-all"
                style={{
                  width: `${healthPercent}%`,
                  backgroundColor: healthPercent >= 80 ? '#10b981' : healthPercent >= 50 ? '#f59e0b' : '#ef4444',
                }}
              />
            </div>
            <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
              {healthChecks.map((check) => (
                <button
                  key={check.label}
                  type="button"
                  onClick={check.cta}
                  className="flex items-center justify-between rounded-md border bg-background px-3 py-2 text-left text-sm hover:border-primary/40"
                >
                  <span className="inline-flex items-center gap-2">
                    {check.ok ? (
                      <CheckCircle2 size={16} className="text-emerald-500" />
                    ) : (
                      <AlertCircle size={16} className="text-amber-500" />
                    )}
                    <span className={check.ok ? '' : 'font-medium'}>{check.label}</span>
                  </span>
                  <ArrowRight size={14} className="text-muted-foreground" />
                </button>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-5 pb-5">
            <p className="text-sm font-semibold mb-2">{t('configuration.dashboard.automationsByModule.title')}</p>
            <p className="text-xs text-muted-foreground mb-3">{t('configuration.dashboard.automationsByModule.subtitle')}</p>
            <div style={{ height: 220 }}>
              {automationByModule.length === 0 ? (
                <p className="flex h-full items-center justify-center text-xs text-muted-foreground text-center">
                  Cadastre uma automação para visualizar a distribuição.
                </p>
              ) : (
                <ResponsivePie
                  data={automationByModule}
                  margin={{ top: 10, right: 10, bottom: 30, left: 10 }}
                  innerRadius={0.55}
                  padAngle={1}
                  cornerRadius={4}
                  colors={({ data }) => data.color}
                  arcLabelsTextColor="#ffffff"
                  arcLinkLabelsSkipAngle={20}
                  arcLinkLabelsTextColor="#475569"
                  arcLinkLabelsThickness={1}
                  enableArcLinkLabels={false}
                  legends={[
                    {
                      anchor: 'bottom',
                      direction: 'row',
                      itemWidth: 80,
                      itemHeight: 16,
                      symbolSize: 10,
                      symbolShape: 'circle',
                    },
                  ]}
                />
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 gap-3 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardContent className="pt-5 pb-5">
            <div className="flex items-center justify-between mb-3">
              <p className="text-sm font-semibold">{t('configuration.dashboard.recentAutomations.title')}</p>
              <Button size="sm" variant="ghost" onClick={() => navigate('/configuracao/integracoes')}>
                Ver todas
              </Button>
            </div>
            {recentAutomations.length === 0 ? (
              <p className="text-xs text-muted-foreground">{t('configuration.dashboard.recentAutomations.empty')}</p>
            ) : (
              <div className="space-y-2">
                {recentAutomations.map((auto) => {
                  const moduleLabel = classifyTrigger(auto.trigger)
                  return (
                    <div
                      key={auto.id}
                      className="flex items-center justify-between rounded-md border bg-background px-3 py-2"
                    >
                      <div className="flex flex-col">
                        <span className="inline-flex items-center gap-2 text-sm font-medium">
                          <Zap size={14} className={auto.isActive ? 'text-emerald-500' : 'text-muted-foreground'} />
                          {auto.name}
                        </span>
                        <span className="text-[10px] text-muted-foreground">
                          {automationTriggerLabels[auto.trigger] ?? auto.trigger} · {moduleLabel}
                        </span>
                      </div>
                      <Badge variant={auto.isActive ? 'success' : 'outline'}>
                        {auto.isActive ? 'Ativa' : 'Inativa'}
                      </Badge>
                    </div>
                  )
                })}
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-5 pb-5">
            <p className="text-sm font-semibold mb-3">{t('configuration.dashboard.shortcuts.title')}</p>
            <div className="space-y-2">
              <button
                type="button"
                onClick={() => navigate('/configuracao/empresa')}
                className="flex w-full items-center gap-2 rounded-md border bg-background px-3 py-2 text-sm hover:border-primary/40"
              >
                <Building2 size={16} className="text-primary" />
                <span>{t('configuration.dashboard.shortcut.agencyData')}</span>
              </button>
              <button
                type="button"
                onClick={() => navigate('/configuracao/integracoes')}
                className="flex w-full items-center gap-2 rounded-md border bg-background px-3 py-2 text-sm hover:border-primary/40"
              >
                <Plug size={16} className="text-primary" />
                <span>{t('configuration.dashboard.shortcut.integrations')}</span>
              </button>
              <button
                type="button"
                onClick={() => navigate('/configuracao/origens-oportunidade')}
                className="flex w-full items-center gap-2 rounded-md border bg-background px-3 py-2 text-sm hover:border-primary/40"
              >
                <Sparkles size={16} className="text-primary" />
                <span>{t('configuration.dashboard.shortcut.opportunitySources')}</span>
              </button>
              <button
                type="button"
                onClick={() => navigate('/configuracao/tags-oportunidade')}
                className="flex w-full items-center gap-2 rounded-md border bg-background px-3 py-2 text-sm hover:border-primary/40"
              >
                <Tag size={16} className="text-primary" />
                <span>{t('configuration.dashboard.shortcut.opportunityTags')}</span>
              </button>
            </div>
          </CardContent>
        </Card>
      </div>

      {loading && (
        <p className="mt-4 text-center text-xs text-muted-foreground">{t('common.loading')}</p>
      )}
    </div>
  )
}
