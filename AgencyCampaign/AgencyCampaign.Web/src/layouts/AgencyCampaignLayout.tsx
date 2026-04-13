import { Outlet, useNavigate } from 'react-router-dom'
import { AppLayout, useAuth, useAppNavigation, AuthService } from 'archon-ui'
import { LayoutDashboard, Building2, Users, Megaphone } from 'lucide-react'

export default function AgencyCampaignLayout() {
  const { user: authUser, contract, logout } = useAuth()
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
  ]

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
      breadcrumbs={[{ label: 'Início', onClick: () => navigate('/') }]}
      notifications={[]}
      onNotificationRead={() => {}}
      onMarkAllAsRead={() => {}}
      onClearAllNotifications={() => {}}
    >
      <Outlet />
    </AppLayout>
  )
}
