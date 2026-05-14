import { useEffect, useState } from 'react'
import { NavLink, Outlet, useNavigate, useParams } from 'react-router-dom'
import { Calendar, FileText, Home, Receipt, User } from 'lucide-react'
import { useI18n } from 'archon-ui'
import { creatorPortalService, type PortalSession } from '../../services/creatorPortalService'

export default function CreatorPortalLayout() {
  const { t } = useI18n()
  const { token } = useParams<{ token: string }>()
  const navigate = useNavigate()
  const [session, setSession] = useState<PortalSession | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!token) {
      navigate('/portal/invalid')
      return
    }

    let cancelled = false
    setLoading(true)

    void creatorPortalService
      .me(token)
      .then((result) => {
        if (cancelled) return
        if (!result) {
          setError('Link inválido, expirado ou revogado.')
        } else {
          setSession(result)
        }
      })
      .catch(() => {
        if (!cancelled) setError('Link inválido, expirado ou revogado.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [token, navigate])

  if (loading) {
    return <div className="flex min-h-screen items-center justify-center text-sm text-muted-foreground">Carregando...</div>
  }

  if (error || !session) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-3 p-6 text-center">
        <h2 className="text-lg font-semibold">Acesso não autorizado</h2>
        <p className="text-sm text-muted-foreground">{error ?? 'Link de acesso não encontrado.'}</p>
        <p className="text-xs text-muted-foreground">Solicite um novo link à agência.</p>
      </div>
    )
  }

  return (
    <div data-testid="public-creator-portal-page" className="flex min-h-screen flex-col bg-background">
      <header className="border-b bg-primary/5 px-4 py-3">
        <div className="mx-auto flex max-w-3xl items-center justify-between">
          <div>
            <h1 className="text-base font-semibold leading-tight">Olá, {session.creator.stageName ?? session.creator.name}</h1>
            <p className="text-xs text-muted-foreground">{t('creatorPortal.layout.title')}</p>
          </div>
          {session.creator.primaryNiche && (
            <span className="rounded-full bg-primary/15 px-2 py-0.5 text-xs text-primary">{session.creator.primaryNiche}</span>
          )}
        </div>
      </header>

      <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-4 pb-24">
        <Outlet context={{ token, session, refresh: () => navigate(0) }} />
      </main>

      <nav className="fixed bottom-0 left-0 right-0 border-t bg-background">
        <div className="mx-auto flex max-w-3xl items-stretch justify-around">
          <PortalNavItem to={`/portal/${token}`} end icon={<Home size={18} />} label={t('creatorPortal.layout.nav.home')} />
          <PortalNavItem to={`/portal/${token}/campanhas`} testId="portal-tab-campanhas" icon={<Calendar size={18} />} label={t('creatorPortal.layout.nav.campaigns')} />
          <PortalNavItem to={`/portal/${token}/contratos`} testId="portal-tab-contratos" icon={<FileText size={18} />} label={t('creatorPortal.layout.nav.contracts')} />
          <PortalNavItem to={`/portal/${token}/pagamentos`} testId="portal-tab-pagamentos" icon={<Receipt size={18} />} label={t('creatorPortal.layout.nav.payments')} />
          <PortalNavItem to={`/portal/${token}/perfil`} testId="portal-tab-perfil" icon={<User size={18} />} label={t('creatorPortal.layout.nav.profile')} />
        </div>
      </nav>
    </div>
  )
}

function PortalNavItem({ to, icon, label, end, testId }: { to: string; icon: React.ReactNode; label: string; end?: boolean; testId?: string }) {
  return (
    <NavLink
      to={to}
      end={end}
      data-testid={testId}
      className={({ isActive }) =>
        `flex flex-1 flex-col items-center gap-0.5 px-1 py-2 text-[10px] transition-colors ${
          isActive ? 'text-primary' : 'text-muted-foreground hover:text-foreground'
        }`
      }
    >
      {icon}
      <span>{label}</span>
    </NavLink>
  )
}
