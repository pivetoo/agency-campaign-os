import type { ReactNode } from 'react'
import { TrendingUp, Hourglass, LineChart, Scale, PiggyBank, Receipt } from 'lucide-react'

export type ReportArea = 'comercial' | 'producao' | 'financeiro'

export interface ReportCatalogEntry {
  id: string
  area: ReportArea
  title: string
  description: string
  icon: ReactNode
  path: string
  requires?: string[]
}

export const reportCatalog: ReportCatalogEntry[] = [
  { id: 'financeiro-fluxo-caixa', area: 'financeiro', title: 'Fluxo de Caixa', description: 'Entradas e saídas previstas e realizadas por período.', icon: <TrendingUp size={20} />, path: '/relatorios/financeiro/fluxo-caixa', requires: ['financialReports.getCashFlow'] },
  { id: 'financeiro-aging', area: 'financeiro', title: 'Aging', description: 'Títulos a vencer e vencidos por faixa de atraso.', icon: <Hourglass size={20} />, path: '/relatorios/financeiro/aging', requires: ['financialReports.getAging'] },
  { id: 'financeiro-projecao', area: 'financeiro', title: 'Projeção de Fluxo', description: 'Saldo projetado semana a semana (horizonte de 12 semanas).', icon: <LineChart size={20} />, path: '/relatorios/financeiro/projecao', requires: ['financialReports.getCashFlowProjection'] },
  { id: 'financeiro-resultado', area: 'financeiro', title: 'Resultado (Competência)', description: 'Receita menos despesa no regime de competência (DRE).', icon: <Scale size={20} />, path: '/relatorios/financeiro/resultado', requires: ['financialReports.getAccrualResult'] },
  { id: 'financeiro-rentabilidade', area: 'financeiro', title: 'Rentabilidade por Campanha', description: 'Receita, custos e margem consolidados por campanha.', icon: <PiggyBank size={20} />, path: '/relatorios/financeiro/rentabilidade', requires: ['financialReports.getCampaignProfitability'] },
  { id: 'financeiro-retencoes', area: 'financeiro', title: 'Retenções Fiscais', description: 'Imposto retido na fonte por creator no período.', icon: <Receipt size={20} />, path: '/relatorios/financeiro/retencoes', requires: ['financialReports.getTaxWithholding'] },
]

export const reportAreaLabels: Record<ReportArea, string> = { comercial: 'Comercial', producao: 'Produção', financeiro: 'Financeiro' }

export const reportAreaOrder: ReportArea[] = ['comercial', 'producao', 'financeiro']
