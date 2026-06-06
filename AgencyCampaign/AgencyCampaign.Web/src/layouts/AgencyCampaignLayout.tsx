import { useCallback, useEffect, useMemo, useState } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { AppLayout, useAuth, AuthService, usePermissions, useI18n } from 'archon-ui'
import type { BreadcrumbItem, NotificationItem, ModuleNavConfig } from 'archon-ui'
import { notificationService } from '../services/notificationService'
import { profileApiService } from '../services/profileApiService'
import type { Notification } from '../types/notification'
import { TourProvider, useTour } from '../components/tour/TourContext'
import ProductTour from '../components/tour/ProductTour'

function TourMount() {
  const { isOpen, closeTour } = useTour()
  return <ProductTour run={isOpen} onClose={closeTour} />
}
import { LayoutDashboard, Building2, Briefcase, Users, User, UserCheck, Megaphone, HandCoins, ReceiptText, Globe, Share2, Package, Tags, Columns3, Plug, FileSignature, ScrollText, Tag, ShieldCheck, Wallet, Settings, Paintbrush, Landmark, Compass, Trophy, ThumbsDown, Target, CalendarDays, Handshake, DollarSign, LifeBuoy, HelpCircle, FileBarChart2 } from 'lucide-react'
import logoAgencyCampaign from '../assets/logo-empresa.png'
import { reportCatalog, reportAreaOrder, reportAreaLabels, type ReportArea } from '../modules/Reports/catalog'

export default function AgencyCampaignLayout() {
  const { user: authUser, contract, logout } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const { isRoot, hasAnyPermission } = usePermissions()
  const { t } = useI18n()

  type NavItem = { key: string; label: string; path: string; icon: React.ReactNode; requires?: string[] }
  const canShow = (item: NavItem) => isRoot || !item.requires || hasAnyPermission(item.requires)
  const filterItems = (items: NavItem[]) => items.filter(canShow)
  const sidebarLogo = <img src={logoAgencyCampaign} alt="Mainstay" style={{ width: 28, height: 28, objectFit: 'contain' }} />

  const handleLogout = async () => {
    await AuthService.logoutFromServer()
    logout()
  }

  const [notifications, setNotifications] = useState<Notification[]>([])

  const loadNotifications = useCallback(async (silent = false) => {
    try {
      const items = await notificationService.getRecent(false, 20, { silent })
      setNotifications(items)
    } catch {
      // silent — notification fetch isn't critical
    }
  }, [])

  useEffect(() => {
    void loadNotifications()
    const interval = window.setInterval(() => {
      void loadNotifications(true)
    }, 60_000)
    return () => window.clearInterval(interval)
  }, [loadNotifications])

  const notificationItems = useMemo<NotificationItem[]>(() => {
    const typeMap: Record<number, NotificationItem['type']> = {
      0: 'info',
      1: 'success',
      2: 'warning',
      3: 'error',
    }
    return notifications.map((item) => ({
      id: String(item.id),
      title: item.title,
      message: item.message,
      timestamp: new Date(item.createdAt),
      read: item.isRead,
      type: typeMap[item.type] ?? 'info',
    }))
  }, [notifications])

  const handleNotificationRead = useCallback(async (id: string) => {
    const numericId = Number(id)
    if (!Number.isFinite(numericId)) return
    setNotifications((prev) => prev.map((item) => (item.id === numericId ? { ...item, isRead: true } : item)))
    const target = notifications.find((item) => item.id === numericId)
    try {
      await notificationService.markAsRead(numericId)
      if (target?.link) navigate(target.link)
    } catch {
      // ignore
    }
  }, [notifications, navigate])

  const handleMarkAllRead = useCallback(async () => {
    setNotifications((prev) => prev.map((item) => ({ ...item, isRead: true })))
    try {
      await notificationService.markAllAsRead()
    } catch {
      // ignore
    }
  }, [])

  const handleClearAll = useCallback(async () => {
    setNotifications([])
    try {
      await notificationService.clearAll()
    } catch {
      // ignore
    }
  }, [])

  const handleViewAllNotifications = useCallback(() => {
    navigate('/notificacoes')
  }, [navigate])

  const opModuleDefs: { key: string; label: string; icon: React.ReactNode; items: NavItem[] }[] = [
    { key: 'geral', label: t('nav.group.general'), icon: <LayoutDashboard size={20} />, items: [
      { key: 'dashboard', label: t('nav.item.dashboard'), path: '/', icon: <LayoutDashboard size={20} />, requires: ['dashboard.overview', 'dashboard.charts'] },
    ] },
    { key: 'comercial', label: t('nav.group.commercial'), icon: <Handshake size={20} />, items: [
      { key: 'marcas', label: t('nav.item.brands'), path: '/marcas', icon: <Building2 size={20} />, requires: ['brands.get'] },
      { key: 'comercial-pipeline', label: t('nav.item.pipeline'), path: '/comercial/pipeline', icon: <Columns3 size={20} />, requires: ['opportunities.board', 'opportunities.get'] },
      { key: 'comercial-propostas', label: t('nav.item.proposals'), path: '/comercial/propostas', icon: <Tags size={20} />, requires: ['proposals.get'] },
      { key: 'comercial-aprovacoes', label: t('nav.item.approvals'), path: '/comercial/aprovacoes', icon: <Globe size={20} />, requires: ['opportunityApprovals.get'] },
      { key: 'comercial-metas', label: 'Metas', path: '/comercial/metas', icon: <Target size={20} />, requires: ['commercialGoals.get'] },
    ] },
    { key: 'producao', label: t('nav.group.operations'), icon: <Megaphone size={20} />, items: [
      { key: 'creators', label: t('nav.item.creators'), path: '/creators', icon: <Users size={20} />, requires: ['creators.get'] },
      { key: 'campanhas', label: t('nav.item.campaigns'), path: '/campanhas', icon: <Megaphone size={20} />, requires: ['campaigns.get'] },
      { key: 'operacao-calendario', label: t('campaignCalendar.pageTitle'), path: '/operacao/calendario', icon: <CalendarDays size={20} />, requires: ['campaignDeliverables.get'] },
      { key: 'operacao-aprovacoes', label: t('nav.item.approvals'), path: '/operacao/aprovacoes', icon: <ShieldCheck size={20} />, requires: ['deliverableApprovals.get'] },
    ] },
    { key: 'financas', label: t('nav.group.finance'), icon: <DollarSign size={20} />, items: [
      { key: 'financeiro-contas', label: t('nav.item.bankAccounts'), path: '/financeiro/contas', icon: <Wallet size={20} />, requires: ['financialAccounts.get'] },
      { key: 'financeiro-receber', label: t('nav.item.accountsReceivable'), path: '/financeiro/receber', icon: <HandCoins size={20} />, requires: ['financialEntries.get'] },
      { key: 'financeiro-pagar', label: t('nav.item.accountsPayable'), path: '/financeiro/pagar', icon: <ReceiptText size={20} />, requires: ['financialEntries.get'] },
      { key: 'financeiro-repasses-creators', label: t('nav.item.creatorPayments'), path: '/financeiro/repasses-creators', icon: <HandCoins size={20} />, requires: ['creatorPayments.get'] },
      { key: 'financeiro-periodos', label: t('nav.item.financialPeriods'), path: '/financeiro/periodos', icon: <CalendarDays size={20} />, requires: ['financialPeriods.get'] },
      { key: 'financeiro-conciliacao', label: t('nav.item.reconciliation'), path: '/financeiro/conciliacao', icon: <Landmark size={20} />, requires: ['bankTransactions.getByAccount'] },
    ] },
  ]

  const configSubGroupDefs: { label: string; items: NavItem[] }[] = [
    { label: t('nav.group.general'), items: [
      { key: 'configuracao-agencia', label: t('nav.item.agency'), path: '/configuracao', icon: <Briefcase size={20} />, requires: ['agencySettings.get'] },
    ] },
    { label: t('nav.group.commercial'), items: [
      { key: 'configuracao-pipeline-comercial', label: t('nav.item.commercialFunnel'), path: '/configuracao/pipeline-comercial', icon: <Columns3 size={20} />, requires: ['commercialPipelineStages.get'] },
      { key: 'configuracao-origens-oportunidade', label: t('nav.item.opportunitySources'), path: '/configuracao/origens-oportunidade', icon: <Compass size={20} />, requires: ['opportunitySources.get'] },
      { key: 'configuracao-tags-oportunidade', label: t('nav.item.opportunityTags'), path: '/configuracao/tags-oportunidade', icon: <Tag size={20} />, requires: ['opportunityTags.get'] },
      { key: 'configuracao-motivos-ganho', label: 'Motivos de ganho', path: '/configuracao/motivos-ganho', icon: <Trophy size={20} />, requires: ['opportunityWinReasons.get'] },
      { key: 'configuracao-motivos-perda', label: 'Motivos de perda', path: '/configuracao/motivos-perda', icon: <ThumbsDown size={20} />, requires: ['opportunityLossReasons.get'] },
      { key: 'configuracao-politica-comercial', label: 'Política comercial', path: '/configuracao/politica-comercial', icon: <ShieldCheck size={20} />, requires: ['commercialPolicy.get'] },
      { key: 'configuracao-itens-proposta', label: t('nav.item.proposalTemplates'), path: '/configuracao/itens-proposta', icon: <FileSignature size={20} />, requires: ['proposalTemplates.get'] },
      { key: 'configuracao-layouts-proposta', label: 'Layouts da proposta', path: '/configuracao/layouts-proposta', icon: <Paintbrush size={20} />, requires: ['agencySettings.getProposalTemplateVersions'] },
    ] },
    { label: t('nav.group.operations'), items: [
      { key: 'configuracao-plataformas', label: t('nav.item.socialNetworks'), path: '/configuracao/plataformas', icon: <Share2 size={20} />, requires: ['platforms.get'] },
      { key: 'configuracao-status-creators', label: t('nav.item.creatorStatus'), path: '/configuracao/status-creators', icon: <UserCheck size={20} />, requires: ['campaignCreatorStatuses.get'] },
      { key: 'configuracao-tipos-entrega', label: t('nav.item.deliverableKinds'), path: '/configuracao/tipos-entrega', icon: <Package size={20} />, requires: ['deliverableKinds.get'] },
      { key: 'configuracao-modelos-contrato', label: t('nav.item.contractTemplates'), path: '/configuracao/modelos-contrato', icon: <ScrollText size={20} />, requires: ['campaignDocumentTemplates.get'] },
    ] },
    { label: t('nav.group.finance'), items: [
      { key: 'configuracao-bancos', label: t('nav.item.banks'), path: '/configuracao/bancos', icon: <Landmark size={20} />, requires: ['banks.get'] },
      { key: 'configuracao-subcategorias-financeiras', label: t('nav.item.financialSubcategories'), path: '/configuracao/subcategorias-financeiras', icon: <Tag size={20} />, requires: ['financialSubcategories.get'] },
    ] },
  ]

  const toNavRoutes = (items: NavItem[]) =>
    filterItems(items).map((i) => ({ key: i.key, label: i.label, path: i.path, icon: i.icon }))

  const opModules = opModuleDefs
    .map((def) => ({ key: def.key, label: def.label, icon: def.icon, group: 'op' as const, routes: toNavRoutes(def.items) }))
    .filter((m) => m.routes.length > 0)

  const configSubGroups = configSubGroupDefs
    .map((g) => ({ label: g.label, routes: toNavRoutes(g.items) }))
    .filter((g) => g.routes.length > 0)

  const integracoesRoutes = toNavRoutes([
    { key: 'configuracao-integracoes', label: t('nav.item.integrations'), path: '/configuracao/integracoes', icon: <Plug size={20} />, requires: ['integrations.get', 'integrations.getActive'] },
  ])

  const reportNavItems = (area: ReportArea): NavItem[] =>
    reportCatalog
      .filter((entry) => entry.area === area)
      .map((entry) => ({ key: entry.id, label: entry.title, path: entry.path, icon: entry.icon, requires: entry.requires }))

  const relatoriosSubGroups = reportAreaOrder
    .map((area) => ({ label: reportAreaLabels[area], routes: toNavRoutes(reportNavItems(area)) }))
    .filter((group) => group.routes.length > 0)

  const controleAcessoRoutes = isRoot
    ? toNavRoutes([{ key: 'usuarios', label: t('nav.item.users'), path: '/usuarios', icon: <User size={20} /> }])
    : []

  const relatoriosModule = relatoriosSubGroups.length > 0
    ? [{ key: 'relatorios', label: 'Relatórios', icon: <FileBarChart2 size={20} />, group: 'op' as const, subGroups: relatoriosSubGroups }]
    : []

  const sysModules = [
    ...(configSubGroups.length > 0 ? [{ key: 'configuracao', label: t('nav.module.configuration'), icon: <Settings size={20} />, group: 'sys' as const, subGroups: configSubGroups }] : []),
    ...(integracoesRoutes.length > 0 ? [{ key: 'integracoes', label: t('nav.item.integrations'), icon: <Plug size={20} />, group: 'sys' as const, routes: integracoesRoutes }] : []),
    ...(controleAcessoRoutes.length > 0 ? [{ key: 'controle-acesso', label: t('nav.item.users'), icon: <User size={20} />, group: 'sys' as const, routes: controleAcessoRoutes }] : []),
  ]

  const moduleNav: ModuleNavConfig = [...opModules, ...relatoriosModule, ...sysModules]

  const isInConfiguration = location.pathname.startsWith('/configuracao')
  const homePathByModule = isInConfiguration ? '/configuracao' : '/'

  const breadcrumbs = useMemo((): BreadcrumbItem[] => {
    const path = location.pathname
    const crumbs: BreadcrumbItem[] = [
      { label: t('breadcrumb.home'), onClick: () => navigate(homePathByModule) },
    ]

    const routeMap: Record<string, string> = {
      '/': t('nav.item.dashboard'),
      '/configuracao': t('nav.item.agency'),
      '/comercial': t('nav.item.pipeline'),
      '/comercial/pipeline': t('nav.item.pipeline'),
      '/comercial/oportunidades': t('nav.item.opportunities'),
      '/comercial/propostas': t('nav.item.proposals'),
      '/comercial/aprovacoes': t('nav.item.approvals'),
      '/comercial/metas': 'Metas',
      '/comercial/followups': t('nav.item.activities'),
      '/marcas': t('nav.item.brands'),
      '/creators': t('nav.item.creators'),
      '/campanhas': t('nav.item.campaigns'),
      '/financeiro/receber': t('nav.item.accountsReceivable'),
      '/financeiro/pagar': t('nav.item.accountsPayable'),
      '/financeiro/repasses-creators': t('nav.item.creatorPayments'),
      '/configuracao/plataformas': t('nav.item.socialNetworks'),
      '/configuracao/pipeline-comercial': t('nav.item.commercialFunnel'),
      '/configuracao/status-creators': t('nav.item.creatorStatus'),
      '/configuracao/tipos-entrega': t('nav.item.deliverableKinds'),
      '/configuracao/integracoes': t('nav.item.integrations'),
      '/relatorios': 'Relatórios',
      '/relatorios/comercial/funil': 'Funil de Conversão',
      '/relatorios/comercial/ganhos-perdas': 'Ganhos × Perdas',
      '/relatorios/comercial/forecast': 'Previsão (Forecast)',
      '/relatorios/comercial/metas': 'Metas × Realizado',
      '/relatorios/comercial/propostas': 'Propostas: Emitidas × Aceitas',
      '/relatorios/comercial/ranking': 'Ranking por Marca',
      '/relatorios/financeiro/fluxo-caixa': 'Fluxo de Caixa',
      '/relatorios/financeiro/aging': 'Aging',
      '/relatorios/financeiro/projecao': 'Projeção de Fluxo',
      '/relatorios/financeiro/resultado': 'Resultado (Competência)',
      '/relatorios/financeiro/rentabilidade': 'Rentabilidade por Campanha',
      '/relatorios/financeiro/retencoes': 'Retenções Fiscais',
      '/configuracao/itens-proposta': t('nav.item.proposalTemplates'),
      '/configuracao/layouts-proposta': 'Layouts da proposta',
      '/configuracao/origens-oportunidade': t('nav.item.opportunitySources'),
      '/configuracao/motivos-ganho': 'Motivos de ganho',
      '/configuracao/motivos-perda': 'Motivos de perda',
      '/configuracao/politica-comercial': 'Política comercial',
      '/configuracao/tags-oportunidade': t('nav.item.opportunityTags'),
      '/configuracao/modelos-contrato': t('nav.item.contractTemplates'),
      '/financeiro/contas': t('nav.item.bankAccounts'),
      '/configuracao/bancos': t('nav.item.banks'),
      '/configuracao/subcategorias-financeiras': t('nav.item.financialSubcategories'),
      '/configuracao/empresa': t('nav.item.agency'),
      '/operacao/aprovacoes': t('nav.item.approvals'),
    }

    const currentLabel = routeMap[path]
    if (currentLabel && path !== '/configuracao' && path !== '/' && path !== '/comercial/oportunidades') {
      crumbs.push({ label: currentLabel })
    }

    if (path === '/comercial/oportunidades') {
      crumbs.push({ label: t('nav.item.pipeline'), onClick: () => navigate('/comercial/pipeline') })
      crumbs.push({ label: t('nav.item.opportunities') })
    }

    if (path.match(/^\/comercial\/oportunidades\/\d+$/)) {
      crumbs.push({ label: t('nav.item.pipeline'), onClick: () => navigate('/comercial/pipeline') })
      crumbs.push({ label: t('breadcrumb.details') })
    }

    if (path.match(/^\/comercial\/propostas\/\d+$/)) {
      const state = location.state as { from?: string; opportunityId?: number; opportunityName?: string; tab?: string } | null
      if (state?.from === 'opportunity' && state.opportunityId) {
        crumbs.push({ label: t('nav.item.pipeline'), onClick: () => navigate('/comercial/pipeline') })
        crumbs.push({
          label: state.opportunityName ?? t('nav.item.opportunities'),
          onClick: () => navigate(`/comercial/oportunidades/${state.opportunityId}${state.tab ? `?tab=${state.tab}` : ''}`),
        })
        crumbs.push({ label: t('breadcrumb.details') })
      } else {
        crumbs.push({ label: t('nav.item.proposals'), onClick: () => navigate('/comercial/propostas') })
        crumbs.push({ label: t('breadcrumb.details') })
      }
    }

    if (path.match(/^\/campanhas\/\d+$/)) {
      crumbs.push({ label: t('nav.item.campaigns'), onClick: () => navigate('/campanhas') })
      crumbs.push({ label: t('breadcrumb.details') })
    }

    if (path.match(/^\/creators\/\d+$/)) {
      crumbs.push({ label: t('nav.item.creators'), onClick: () => navigate('/creators') })
      crumbs.push({ label: t('breadcrumb.details') })
    }

    const contractTemplateMatch = path.match(/^\/configuracao\/modelos-contrato\/(novo|\d+)$/)
    if (contractTemplateMatch) {
      crumbs.push({ label: t('nav.item.contractTemplates'), onClick: () => navigate('/configuracao/modelos-contrato') })
      crumbs.push({ label: contractTemplateMatch[1] === 'novo' ? 'Novo modelo' : 'Editar modelo' })
    }

    const proposalLayoutMatch = path.match(/^\/configuracao\/layouts-proposta\/(novo|\d+)$/)
    if (proposalLayoutMatch) {
      crumbs.push({ label: 'Layouts da proposta', onClick: () => navigate('/configuracao/layouts-proposta') })
      crumbs.push({ label: proposalLayoutMatch[1] === 'novo' ? 'Novo layout' : 'Editar layout' })
    }

    return crumbs
  }, [location.pathname, location.state, navigate, homePathByModule, t])

  return (
    <TourProvider>
      <AppLayout
        title={contract?.systemApplicationName ?? 'Kanvas'}
        logo={sidebarLogo}
        subtitle="by Mainstay"
        navbarCompanyName={contract?.companyName}
        user={{
          name: authUser?.name ?? '',
          email: authUser?.email ?? '',
          username: authUser?.username,
          role: contract?.roleName,
          avatarUrl: authUser?.avatarUrl,
        }}
        onAvatarUpload={profileApiService.uploadAvatar}
        onAvatarRemove={profileApiService.removeAvatar}
        onUpdateProfile={profileApiService.updateProfile}
        menuExtraItems={[
          { key: 'suporte', label: 'Suporte', icon: <LifeBuoy className="h-4 w-4" />, onClick: () => {} },
          { key: 'central-ajuda', label: 'Central de Ajuda', icon: <HelpCircle className="h-4 w-4" />, onClick: () => {} },
        ]}
        onLogout={handleLogout}
        navMode="module-rail"
        moduleNav={moduleNav}
        breadcrumbs={breadcrumbs}
        notifications={notificationItems}
        onNotificationRead={handleNotificationRead}
        onMarkAllAsRead={handleMarkAllRead}
        onClearAllNotifications={handleClearAll}
        onViewAllNotifications={handleViewAllNotifications}
      >
        <Outlet />
      </AppLayout>
      <TourMount />
    </TourProvider>
  )
}
