import { useMemo } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { AppLayout, useAuth, useAppNavigation, AuthService } from 'archon-ui'
import type { BreadcrumbItem } from 'archon-ui'
import { LayoutDashboard, Building2, Users, Megaphone, HandCoins, ReceiptText, Globe, Tags, Briefcase } from 'lucide-react'

export default function AgencyCampaignLayout() {
  const { user: authUser, contract, logout } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const { createMenuGroup } = useAppNavigation({})
  const sidebarLogo = <img src="/logo-sistema.svg" alt="Agency Campaign OS" style={{ width: 28, height: 28 }} />

  const handleLogout = async () => {
    await AuthService.logoutFromServer()
    logout()
  }

  const menuGroups = [
    createMenuGroup('Geral', [
      { key: 'dashboard', label: 'Dashboard', path: '/', icon: <LayoutDashboard size={20} /> },
    ]),
    createMenuGroup('Comercial', [
      { key: 'comercial-oportunidades', label: 'Oportunidades', path: '/comercial/oportunidades', icon: <Briefcase size={20} /> },
      { key: 'comercial-propostas', label: 'Propostas', path: '/comercial/propostas', icon: <Tags size={20} /> },
      { key: 'comercial-negociacoes', label: 'Negociações', path: '/comercial/negociacoes', icon: <ReceiptText size={20} /> },
      { key: 'comercial-aprovacoes', label: 'Aprovações', path: '/comercial/aprovacoes', icon: <Globe size={20} /> },
      { key: 'comercial-followups', label: 'Follow-ups', path: '/comercial/followups', icon: <HandCoins size={20} /> },
    ]),
    createMenuGroup('Operação', [
      { key: 'marcas', label: 'Marcas', path: '/marcas', icon: <Building2 size={20} /> },
      { key: 'creators', label: 'Creators', path: '/creators', icon: <Users size={20} /> },
      { key: 'campanhas', label: 'Campanhas', path: '/campanhas', icon: <Megaphone size={20} /> },
    ]),
    createMenuGroup('Finanças', [
      { key: 'financeiro-receber', label: 'Contas a receber', path: '/financeiro/receber', icon: <HandCoins size={20} /> },
      { key: 'financeiro-pagar', label: 'Contas a pagar', path: '/financeiro/pagar', icon: <ReceiptText size={20} /> },
    ]),
    createMenuGroup('Configuração', [
      { key: 'configuracao-plataformas', label: 'Plataformas', path: '/configuracao/plataformas', icon: <Globe size={20} /> },
      { key: 'configuracao-tipos-entrega', label: 'Tipos de entrega', path: '/configuracao/tipos-entrega', icon: <Tags size={20} /> },
    ]),
  ]

  const breadcrumbs = useMemo((): BreadcrumbItem[] => {
    const path = location.pathname
    const crumbs: BreadcrumbItem[] = [
      { label: 'Início', onClick: () => navigate('/') },
    ]

    const routeMap: Record<string, string> = {
      '/': 'Dashboard',
      '/comercial': 'Oportunidades',
      '/comercial/oportunidades': 'Oportunidades',
      '/comercial/propostas': 'Propostas',
      '/comercial/negociacoes': 'Negociações',
      '/comercial/aprovacoes': 'Aprovações',
      '/comercial/followups': 'Follow-ups',
      '/marcas': 'Marcas',
      '/creators': 'Creators',
      '/campanhas': 'Campanhas',
      '/financeiro/receber': 'Contas a receber',
      '/financeiro/pagar': 'Contas a pagar',
      '/configuracao/plataformas': 'Plataformas',
      '/configuracao/tipos-entrega': 'Tipos de entrega',
    }

    const currentLabel = routeMap[path]
    if (currentLabel && currentLabel !== 'Dashboard') {
      crumbs.push({ label: currentLabel })
    }

    if (path.match(/^\/comercial\/oportunidades\/\d+$/)) {
      crumbs.push({ label: 'Oportunidades', onClick: () => navigate('/comercial/oportunidades') })
      crumbs.push({ label: 'Detalhes' })
    }

    if (path.match(/^\/campanhas\/\d+$/)) {
      crumbs.push({ label: 'Campanhas', onClick: () => navigate('/campanhas') })
      crumbs.push({ label: 'Detalhes' })
    }

    return crumbs
  }, [location.pathname, navigate])

  return (
    <AppLayout
      title={contract?.systemApplicationName ?? 'Agency Campaign OS'}
      logo={sidebarLogo}
      subtitle={contract?.companyName ?? ''}
      user={{
        name: authUser?.name ?? '',
        email: authUser?.email ?? '',
        role: contract?.roleName,
      }}
      onLogout={handleLogout}
      menuGroups={menuGroups}
      breadcrumbs={breadcrumbs}
      notifications={[]}
      onNotificationRead={() => {}}
      onMarkAllAsRead={() => {}}
      onClearAllNotifications={() => {}}
    >
      <Outlet />
    </AppLayout>
  )
}
