import { useCallback, useEffect, useMemo, useState } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { AppLayout, useAuth, useAppNavigation, AuthService } from 'archon-ui'
import type { BreadcrumbItem, NotificationItem } from 'archon-ui'
import { notificationService } from '../services/notificationService'
import type { Notification } from '../types/notification'
import { TourProvider, useTour } from '../components/tour/TourContext'
import ProductTour from '../components/tour/ProductTour'

function TourMount() {
  const { isOpen, closeTour } = useTour()
  return <ProductTour run={isOpen} onClose={closeTour} />
}
import { LayoutDashboard, Building2, Users, Megaphone, HandCoins, ReceiptText, Globe, Tags, Columns3, UserCheck, Plug, FileText, Blocks, List, Sparkles, Tag, Mail, ShieldCheck, Wallet, TrendingUp, Hourglass, Settings, ScrollText } from 'lucide-react'
import logoAgencyCampaign from '../assets/logo-agency-campaign.png'

export default function AgencyCampaignLayout() {
  const { user: authUser, contract, logout } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const { createMenuGroup } = useAppNavigation({})
  const sidebarLogo = <img src={logoAgencyCampaign} alt="Agency Campaign OS" style={{ width: 36, height: 36, objectFit: 'contain' }} />

  const handleLogout = async () => {
    await AuthService.logoutFromServer()
    logout()
  }

  const [notifications, setNotifications] = useState<Notification[]>([])

  const loadNotifications = useCallback(async () => {
    try {
      const items = await notificationService.getRecent(false, 20)
      setNotifications(items)
    } catch {
      // silent — notification fetch isn't critical
    }
  }, [])

  useEffect(() => {
    void loadNotifications()
    const interval = window.setInterval(() => {
      void loadNotifications()
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
    createMenuGroup('Geral', [
      { key: 'dashboard', label: 'Dashboard', path: '/', icon: <LayoutDashboard size={20} /> },
    ]),
    createMenuGroup('Comercial', [
      { key: 'comercial-pipeline', label: 'Pipeline', path: '/comercial/pipeline', icon: <Columns3 size={20} /> },
      { key: 'comercial-oportunidades', label: 'Oportunidades', path: '/comercial/oportunidades', icon: <List size={20} /> },
      { key: 'comercial-propostas', label: 'Propostas', path: '/comercial/propostas', icon: <Tags size={20} /> },
      { key: 'comercial-aprovacoes', label: 'Aprovações', path: '/comercial/aprovacoes', icon: <Globe size={20} /> },
      { key: 'comercial-followups', label: 'Atividades', path: '/comercial/followups', icon: <HandCoins size={20} /> },
      { key: 'comercial-responsaveis', label: 'Responsáveis', path: '/comercial/responsaveis', icon: <UserCheck size={20} /> },
    ]),
    createMenuGroup('Operação', [
      { key: 'marcas', label: 'Marcas', path: '/marcas', icon: <Building2 size={20} /> },
      { key: 'creators', label: 'Creators', path: '/creators', icon: <Users size={20} /> },
      { key: 'campanhas', label: 'Campanhas', path: '/campanhas', icon: <Megaphone size={20} /> },
      { key: 'operacao-aprovacoes', label: 'Aprovações', path: '/operacao/aprovacoes', icon: <ShieldCheck size={20} /> },
    ]),
    createMenuGroup('Finanças', [
      { key: 'financeiro-receber', label: 'Contas a receber', path: '/financeiro/receber', icon: <HandCoins size={20} /> },
      { key: 'financeiro-pagar', label: 'Contas a pagar', path: '/financeiro/pagar', icon: <ReceiptText size={20} /> },
      { key: 'financeiro-fluxo-caixa', label: 'Fluxo de caixa', path: '/financeiro/fluxo-caixa', icon: <TrendingUp size={20} /> },
      { key: 'financeiro-aging', label: 'Aging', path: '/financeiro/aging', icon: <Hourglass size={20} /> },
    ]),
  ]

  const configurationGroups = [
    createMenuGroup('Geral', [
      { key: 'configuracao-empresa', label: 'Empresa', path: '/configuracao/empresa', icon: <Settings size={20} /> },
      { key: 'configuracao-integracoes', label: 'Integrações', path: '/configuracao/integracoes', icon: <Plug size={20} /> },
      { key: 'configuracao-templates-email', label: 'Templates de e-mail', path: '/configuracao/templates-email', icon: <Mail size={20} /> },
    ]),
    createMenuGroup('Comercial', [
      { key: 'configuracao-pipeline-comercial', label: 'Estágios do funil', path: '/configuracao/pipeline-comercial', icon: <Columns3 size={20} /> },
      { key: 'configuracao-origens-oportunidade', label: 'Origens', path: '/configuracao/origens-oportunidade', icon: <Sparkles size={20} /> },
      { key: 'configuracao-tags-oportunidade', label: 'Tags', path: '/configuracao/tags-oportunidade', icon: <Tag size={20} /> },
      { key: 'configuracao-templates-proposta', label: 'Templates de proposta', path: '/configuracao/templates-proposta', icon: <FileText size={20} /> },
      { key: 'configuracao-blocos-proposta', label: 'Blocos de proposta', path: '/configuracao/blocos-proposta', icon: <Blocks size={20} /> },
    ]),
    createMenuGroup('Operação', [
      { key: 'configuracao-plataformas', label: 'Redes sociais', path: '/configuracao/plataformas', icon: <Globe size={20} /> },
      { key: 'configuracao-status-creators', label: 'Status dos creators', path: '/configuracao/status-creators', icon: <Users size={20} /> },
      { key: 'configuracao-tipos-entrega', label: 'Tipos de entrega', path: '/configuracao/tipos-entrega', icon: <Tags size={20} /> },
    ]),
    createMenuGroup('Finanças', [
      { key: 'configuracao-contas-financeiras', label: 'Contas bancárias', path: '/configuracao/contas-financeiras', icon: <Wallet size={20} /> },
      { key: 'configuracao-subcategorias-financeiras', label: 'Subcategorias', path: '/configuracao/subcategorias-financeiras', icon: <Tag size={20} /> },
    ]),
  ]

  const auditGroups: typeof systemGroups = []

  const isInConfiguration = location.pathname.startsWith('/configuracao')
  const isInAudit = location.pathname.startsWith('/auditoria')
  const currentModule = isInConfiguration ? 'configuracao' : isInAudit ? 'auditoria' : 'sistema'
  const menuGroups = isInConfiguration ? configurationGroups : isInAudit ? auditGroups : systemGroups

  const modules = [
    { id: 'sistema', name: 'Sistema', icon: <LayoutDashboard size={16} /> },
    { id: 'configuracao', name: 'Configuração', icon: <Settings size={16} /> },
    { id: 'auditoria', name: 'Auditoria', icon: <ScrollText size={16} /> },
  ]

  const handleModuleChange = (moduleId: string) => {
    if (moduleId === 'configuracao') {
      navigate('/configuracao')
    } else if (moduleId === 'auditoria') {
      navigate('/auditoria')
    } else {
      navigate('/')
    }
  }

  const homePathByModule = isInConfiguration ? '/configuracao' : isInAudit ? '/auditoria' : '/'

  const breadcrumbs = useMemo((): BreadcrumbItem[] => {
    const path = location.pathname
    const crumbs: BreadcrumbItem[] = [
      { label: 'Início', onClick: () => navigate(homePathByModule) },
    ]

    const routeMap: Record<string, string> = {
      '/': 'Dashboard',
      '/configuracao': 'Início da Configuração',
      '/auditoria': 'Auditoria',
      '/comercial': 'Pipeline',
      '/comercial/pipeline': 'Pipeline',
      '/comercial/oportunidades': 'Oportunidades',
      '/comercial/propostas': 'Propostas',
      '/comercial/aprovacoes': 'Aprovações',
      '/comercial/followups': 'Atividades',
      '/comercial/responsaveis': 'Responsáveis',
      '/marcas': 'Marcas',
      '/creators': 'Creators',
      '/campanhas': 'Campanhas',
      '/financeiro/receber': 'Contas a receber',
      '/financeiro/pagar': 'Contas a pagar',
      '/financeiro/fluxo-caixa': 'Fluxo de caixa',
      '/financeiro/aging': 'Aging',
      '/configuracao/plataformas': 'Redes Sociais',
      '/configuracao/pipeline-comercial': 'Estágios do Comercial',
      '/configuracao/status-creators': 'Status dos Creators',
      '/configuracao/tipos-entrega': 'Tipos de entrega',
      '/configuracao/integracoes': 'Integrações',
      '/configuracao/templates-proposta': 'Templates de proposta',
      '/configuracao/blocos-proposta': 'Blocos de proposta',
      '/configuracao/origens-oportunidade': 'Origens de oportunidade',
      '/configuracao/tags-oportunidade': 'Tags de oportunidade',
      '/configuracao/templates-email': 'Templates de e-mail',
      '/configuracao/contas-financeiras': 'Contas financeiras',
      '/configuracao/subcategorias-financeiras': 'Subcategorias financeiras',
      '/configuracao/empresa': 'Empresa',
      '/operacao/aprovacoes': 'Aprovações',
    }

    const currentLabel = routeMap[path]
    if (currentLabel && currentLabel !== 'Dashboard' && currentLabel !== 'Início da Configuração' && currentLabel !== 'Auditoria') {
      crumbs.push({ label: currentLabel })
    }

    if (path.match(/^\/comercial\/oportunidades\/\d+$/)) {
      crumbs.push({ label: 'Pipeline', onClick: () => navigate('/comercial/pipeline') })
      crumbs.push({ label: 'Detalhes' })
    }

    if (path.match(/^\/comercial\/propostas\/\d+$/)) {
      crumbs.push({ label: 'Propostas', onClick: () => navigate('/comercial/propostas') })
      crumbs.push({ label: 'Detalhes' })
    }

    if (path.match(/^\/campanhas\/\d+$/)) {
      crumbs.push({ label: 'Campanhas', onClick: () => navigate('/campanhas') })
      crumbs.push({ label: 'Detalhes' })
    }

    return crumbs
  }, [location.pathname, navigate, homePathByModule])

  return (
    <TourProvider>
      <AppLayout
        title={contract?.systemApplicationName ?? 'Kanvas for Agencies'}
        logo={sidebarLogo}
        subtitle={contract?.companyName ?? ''}
        user={{
          name: authUser?.name ?? '',
          email: authUser?.email ?? '',
          role: contract?.roleName,
        }}
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
