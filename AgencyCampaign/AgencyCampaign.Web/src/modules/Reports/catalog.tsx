import type { ReactNode } from 'react'
import { TrendingUp, Hourglass, LineChart, Scale, PiggyBank, Receipt, Filter, Trophy, Target, FileText, Award, Megaphone, Users, Share2, Clock, CheckCircle2, ScrollText } from 'lucide-react'

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
  { id: 'comercial-funil', area: 'comercial', title: 'Funil de Conversão', description: 'Conversão por estágio do pipeline comercial.', icon: <Filter size={20} />, path: '/relatorios/comercial/funil', requires: ['opportunities.analytics'] },
  { id: 'comercial-ganhos-perdas', area: 'comercial', title: 'Ganhos × Perdas', description: 'Motivos de ganho e de perda das oportunidades.', icon: <Trophy size={20} />, path: '/relatorios/comercial/ganhos-perdas', requires: ['opportunities.analytics'] },
  { id: 'comercial-forecast', area: 'comercial', title: 'Previsão (Forecast)', description: 'Pipeline ponderado por probabilidade.', icon: <TrendingUp size={20} />, path: '/relatorios/comercial/forecast', requires: ['opportunities.forecast'] },
  { id: 'comercial-metas', area: 'comercial', title: 'Metas × Realizado', description: 'Meta versus realizado por período e responsável.', icon: <Target size={20} />, path: '/relatorios/comercial/metas', requires: ['commercialGoals.progress'] },
  { id: 'comercial-propostas', area: 'comercial', title: 'Propostas: Emitidas × Aceitas', description: 'Volume, valor e taxa de aceite de propostas.', icon: <FileText size={20} />, path: '/relatorios/comercial/propostas', requires: ['commercialReports.getProposalsFunnel'] },
  { id: 'comercial-ranking', area: 'comercial', title: 'Ranking por Marca', description: 'Marcas por valor ganho no período.', icon: <Award size={20} />, path: '/relatorios/comercial/ranking', requires: ['commercialReports.getBrandRanking'] },
  { id: 'comercial-receita-creator', area: 'comercial', title: 'Receita por Creator', description: 'Quanto cada creator gerou em negócios fechados.', icon: <Users size={20} />, path: '/relatorios/comercial/receita-creator', requires: ['commercialReports.getBrandRanking'] },
  { id: 'producao-campanhas', area: 'producao', title: 'Performance de Campanhas', description: 'Alcance, engajamento e EMV por campanha.', icon: <Megaphone size={20} />, path: '/relatorios/producao/campanhas', requires: ['productionReports.getCampaignPerformance'] },
  { id: 'producao-creators', area: 'producao', title: 'Desempenho por Creator', description: 'Alcance e engajamento por creator.', icon: <Users size={20} />, path: '/relatorios/producao/creators', requires: ['productionReports.getCreatorPerformance'] },
  { id: 'producao-plataforma', area: 'producao', title: 'Produção por Plataforma', description: 'Entregas e métricas por plataforma.', icon: <Share2 size={20} />, path: '/relatorios/producao/plataforma', requires: ['productionReports.getPlatformProduction'] },
  { id: 'producao-sla', area: 'producao', title: 'Entregáveis: Prazo × Atraso', description: 'SLA dos entregáveis por vencimento.', icon: <Clock size={20} />, path: '/relatorios/producao/sla', requires: ['productionReports.getDeliverableSla'] },
  { id: 'producao-aprovacoes', area: 'producao', title: 'Aprovação e Rodadas', description: 'Tempo de aprovação e rodadas de revisão.', icon: <CheckCircle2 size={20} />, path: '/relatorios/producao/aprovacoes', requires: ['productionReports.getApprovalCycle'] },
  { id: 'producao-licencas', area: 'producao', title: 'Licenças de Conteúdo', description: 'Status de expiração das licenças.', icon: <ScrollText size={20} />, path: '/relatorios/producao/licencas', requires: ['productionReports.getContentLicenses'] },
  { id: 'financeiro-fluxo-caixa', area: 'financeiro', title: 'Fluxo de Caixa', description: 'Entradas e saídas previstas e realizadas por período.', icon: <TrendingUp size={20} />, path: '/relatorios/financeiro/fluxo-caixa', requires: ['financialReports.getCashFlow'] },
  { id: 'financeiro-aging', area: 'financeiro', title: 'Aging', description: 'Títulos a vencer e vencidos por faixa de atraso.', icon: <Hourglass size={20} />, path: '/relatorios/financeiro/aging', requires: ['financialReports.getAging'] },
  { id: 'financeiro-projecao', area: 'financeiro', title: 'Projeção de Fluxo', description: 'Saldo projetado semana a semana (horizonte de 12 semanas).', icon: <LineChart size={20} />, path: '/relatorios/financeiro/projecao', requires: ['financialReports.getCashFlowProjection'] },
  { id: 'financeiro-resultado', area: 'financeiro', title: 'Resultado (Competência)', description: 'Receita menos despesa no regime de competência (DRE).', icon: <Scale size={20} />, path: '/relatorios/financeiro/resultado', requires: ['financialReports.getAccrualResult'] },
  { id: 'financeiro-rentabilidade', area: 'financeiro', title: 'Rentabilidade por Campanha', description: 'Receita, custos e margem consolidados por campanha.', icon: <PiggyBank size={20} />, path: '/relatorios/financeiro/rentabilidade', requires: ['financialReports.getCampaignProfitability'] },
  { id: 'financeiro-retencoes', area: 'financeiro', title: 'Retenções Fiscais', description: 'Imposto retido na fonte por creator no período.', icon: <Receipt size={20} />, path: '/relatorios/financeiro/retencoes', requires: ['financialReports.getTaxWithholding'] },
]

export const reportAreaLabels: Record<ReportArea, string> = { comercial: 'Comercial', producao: 'Produção', financeiro: 'Financeiro' }

export const reportAreaOrder: ReportArea[] = ['comercial', 'producao', 'financeiro']
