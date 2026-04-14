import { useMemo } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { AppLayout, useAuth, useAppNavigation, AuthService } from 'archon-ui'
import type { BreadcrumbItem } from 'archon-ui'
import { LayoutDashboard, Building2, Users, Megaphone, HandCoins, ReceiptText, Globe, Tags } from 'lucide-react'

export default function AgencyCampaignLayout() {
  const { user: authUser, contract, logout } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const { createMenuGroup } = useAppNavigation({})

  const handleLogout = async () => {
    await AuthService.logoutFromServer()
    logout()
  }

  const menuGroups = [
    createMenuGroup('Geral', [
      { key: 'dashboard', label: 'Dashboard', path: '/', icon: <LayoutDashboard size={20} /> },
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

    if (path.match(/^\/campanhas\/\d+$/)) {
      crumbs.push({ label: 'Campanhas', onClick: () => navigate('/campanhas') })
      crumbs.push({ label: 'Detalhes' })
    }

    return crumbs
  }, [location.pathname, navigate])

  return (
    <AppLayout
      title={contract?.systemApplicationName ?? 'Agency Campaign OS'}
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
