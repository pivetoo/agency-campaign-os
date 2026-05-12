import { useCallback, useEffect, useMemo, useState } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { AppLayout, useAuth, useAppNavigation, AuthService, usePermissions, useI18n } from 'archon-ui'
import type { BreadcrumbItem, NotificationItem } from 'archon-ui'
import { notificationService } from '../services/notificationService'
import { profileApiService } from '../services/profileApiService'
import type { Notification } from '../types/notification'
import { TourProvider, useTour } from '../components/tour/TourContext'
import ProductTour from '../components/tour/ProductTour'

function TourMount() {
  const { isOpen, closeTour } = useTour()
  return <ProductTour run={isOpen} onClose={closeTour} />
}
import { LayoutDashboard, Building2, Briefcase, Users, UserCheck, Megaphone, HandCoins, ListChecks, ReceiptText, Globe, Share2, Package, Tags, Columns3, Plug, FileSignature, ScrollText, Blocks, Sparkles, Tag, Mail, ShieldCheck, Wallet, TrendingUp, Hourglass, Settings, UserCog, Paintbrush } from 'lucide-react'
import logoAgencyCampaign from '../assets/logo-empresa.png'

export default function AgencyCampaignLayout() {
  const { user: authUser, contract, logout } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const { createMenuGroup } = useAppNavigation({})
  const { isRoot } = usePermissions()
  const { t } = useI18n()
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

  const systemGroups = [
    createMenuGroup(t('nav.group.general'), [
      { key: 'dashboard', label: t('nav.item.dashboard'), path: '/', icon: <LayoutDashboard size={20} /> },
      ...(isRoot
        ? [{ key: 'usuarios', label: t('nav.item.users'), path: '/usuarios', icon: <UserCog size={20} /> }]
        : []),
    ]),
    createMenuGroup(t('nav.group.commercial'), [
      { key: 'comercial-pipeline', label: t('nav.item.pipeline'), path: '/comercial/pipeline', icon: <Columns3 size={20} /> },
      { key: 'comercial-propostas', label: t('nav.item.proposals'), path: '/comercial/propostas', icon: <Tags size={20} /> },
      { key: 'comercial-aprovacoes', label: t('nav.item.approvals'), path: '/comercial/aprovacoes', icon: <Globe size={20} /> },
      { key: 'comercial-followups', label: t('nav.item.activities'), path: '/comercial/followups', icon: <ListChecks size={20} /> },
    ]),
    createMenuGroup(t('nav.group.operations'), [
      { key: 'marcas', label: t('nav.item.brands'), path: '/marcas', icon: <Building2 size={20} /> },
      { key: 'creators', label: t('nav.item.creators'), path: '/creators', icon: <Users size={20} /> },
      { key: 'campanhas', label: t('nav.item.campaigns'), path: '/campanhas', icon: <Megaphone size={20} /> },
      { key: 'operacao-aprovacoes', label: t('nav.item.approvals'), path: '/operacao/aprovacoes', icon: <ShieldCheck size={20} /> },
    ]),
    createMenuGroup(t('nav.group.finance'), [
      { key: 'financeiro-receber', label: t('nav.item.accountsReceivable'), path: '/financeiro/receber', icon: <HandCoins size={20} /> },
      { key: 'financeiro-pagar', label: t('nav.item.accountsPayable'), path: '/financeiro/pagar', icon: <ReceiptText size={20} /> },
      { key: 'financeiro-repasses-creators', label: t('nav.item.creatorPayments'), path: '/financeiro/repasses-creators', icon: <HandCoins size={20} /> },
      { key: 'financeiro-fluxo-caixa', label: t('nav.item.cashFlow'), path: '/financeiro/fluxo-caixa', icon: <TrendingUp size={20} /> },
      { key: 'financeiro-aging', label: t('nav.item.aging'), path: '/financeiro/aging', icon: <Hourglass size={20} /> },
    ]),
  ]

  const configurationGroups = [
    createMenuGroup(t('nav.group.general'), [
      { key: 'configuracao-agencia', label: t('nav.item.agency'), path: '/configuracao', icon: <Briefcase size={20} /> },
      { key: 'configuracao-integracoes', label: t('nav.item.integrations'), path: '/configuracao/integracoes', icon: <Plug size={20} /> },
      { key: 'configuracao-templates-email', label: t('nav.item.emailTemplates'), path: '/configuracao/templates-email', icon: <Mail size={20} /> },
    ]),
    createMenuGroup(t('nav.group.commercial'), [
      { key: 'configuracao-pipeline-comercial', label: t('nav.item.commercialFunnel'), path: '/configuracao/pipeline-comercial', icon: <Columns3 size={20} /> },
      { key: 'configuracao-origens-oportunidade', label: t('nav.item.opportunitySources'), path: '/configuracao/origens-oportunidade', icon: <Sparkles size={20} /> },
      { key: 'configuracao-tags-oportunidade', label: t('nav.item.opportunityTags'), path: '/configuracao/tags-oportunidade', icon: <Tag size={20} /> },
      { key: 'configuracao-templates-proposta', label: t('nav.item.proposalTemplates'), path: '/configuracao/templates-proposta', icon: <FileSignature size={20} /> },
      { key: 'configuracao-template-proposta', label: t('nav.item.proposalTemplate'), path: '/configuracao/template-proposta', icon: <Paintbrush size={20} /> },
      { key: 'configuracao-blocos-proposta', label: t('nav.item.proposalBlocks'), path: '/configuracao/blocos-proposta', icon: <Blocks size={20} /> },
    ]),
    createMenuGroup(t('nav.group.operations'), [
      { key: 'configuracao-plataformas', label: t('nav.item.socialNetworks'), path: '/configuracao/plataformas', icon: <Share2 size={20} /> },
      { key: 'configuracao-status-creators', label: t('nav.item.creatorStatus'), path: '/configuracao/status-creators', icon: <UserCheck size={20} /> },
      { key: 'configuracao-tipos-entrega', label: t('nav.item.deliverableKinds'), path: '/configuracao/tipos-entrega', icon: <Package size={20} /> },
      { key: 'configuracao-templates-documento', label: t('nav.item.contractTemplates'), path: '/configuracao/templates-documento', icon: <ScrollText size={20} /> },
    ]),
    createMenuGroup(t('nav.group.finance'), [
      { key: 'configuracao-contas-financeiras', label: t('nav.item.bankAccounts'), path: '/configuracao/contas-financeiras', icon: <Wallet size={20} /> },
      { key: 'configuracao-subcategorias-financeiras', label: t('nav.item.financialSubcategories'), path: '/configuracao/subcategorias-financeiras', icon: <Tag size={20} /> },
    ]),
  ]

  const isInConfiguration = location.pathname.startsWith('/configuracao')
  const currentModule = isInConfiguration ? 'configuracao' : 'sistema'
  const menuGroups = isInConfiguration ? configurationGroups : systemGroups

  const modules = [
    { id: 'sistema', name: t('nav.module.panel'), icon: <LayoutDashboard size={16} /> },
    { id: 'configuracao', name: t('nav.module.configuration'), icon: <Settings size={16} /> },
  ]

  const handleModuleChange = (moduleId: string) => {
    if (moduleId === 'configuracao') {
      navigate('/configuracao')
    } else {
      navigate('/')
    }
  }

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
      '/comercial/followups': t('nav.item.activities'),
      '/marcas': t('nav.item.brands'),
      '/creators': t('nav.item.creators'),
      '/campanhas': t('nav.item.campaigns'),
      '/financeiro/receber': t('nav.item.accountsReceivable'),
      '/financeiro/pagar': t('nav.item.accountsPayable'),
      '/financeiro/repasses-creators': t('nav.item.creatorPayments'),
      '/financeiro/fluxo-caixa': t('nav.item.cashFlow'),
      '/financeiro/aging': t('nav.item.aging'),
      '/configuracao/plataformas': t('nav.item.socialNetworks'),
      '/configuracao/pipeline-comercial': t('nav.item.commercialFunnel'),
      '/configuracao/status-creators': t('nav.item.creatorStatus'),
      '/configuracao/tipos-entrega': t('nav.item.deliverableKinds'),
      '/configuracao/integracoes': t('nav.item.integrations'),
      '/configuracao/templates-proposta': t('nav.item.proposalTemplates'),
      '/configuracao/template-proposta': t('nav.item.proposalTemplate'),
      '/configuracao/blocos-proposta': t('nav.item.proposalBlocks'),
      '/configuracao/origens-oportunidade': t('nav.item.opportunitySources'),
      '/configuracao/tags-oportunidade': t('nav.item.opportunityTags'),
      '/configuracao/templates-email': t('nav.item.emailTemplates'),
      '/configuracao/templates-documento': t('nav.item.contractTemplates'),
      '/configuracao/contas-financeiras': t('nav.item.bankAccounts'),
      '/configuracao/subcategorias-financeiras': t('nav.item.financialSubcategories'),
      '/configuracao/empresa': t('nav.item.agency'),
      '/operacao/aprovacoes': t('nav.item.approvals'),
    }

    const currentLabel = routeMap[path]
    if (currentLabel && path !== '/configuracao' && path !== '/') {
      crumbs.push({ label: currentLabel })
    }

    if (path.match(/^\/comercial\/oportunidades\/\d+$/)) {
      crumbs.push({ label: t('nav.item.pipeline'), onClick: () => navigate('/comercial/pipeline') })
      crumbs.push({ label: t('breadcrumb.details') })
    }

    if (path.match(/^\/comercial\/propostas\/\d+$/)) {
      crumbs.push({ label: t('nav.item.proposals'), onClick: () => navigate('/comercial/propostas') })
      crumbs.push({ label: t('breadcrumb.details') })
    }

    if (path.match(/^\/campanhas\/\d+$/)) {
      crumbs.push({ label: t('nav.item.campaigns'), onClick: () => navigate('/campanhas') })
      crumbs.push({ label: t('breadcrumb.details') })
    }

    if (path.match(/^\/creators\/\d+$/)) {
      crumbs.push({ label: t('nav.item.creators'), onClick: () => navigate('/creators') })
      crumbs.push({ label: t('breadcrumb.details') })
    }

    return crumbs
  }, [location.pathname, navigate, homePathByModule, t])

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
        onLogout={handleLogout}
        menuGroups={menuGroups}
        modules={modules}
        currentModule={currentModule}
        onModuleChange={handleModuleChange}
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
