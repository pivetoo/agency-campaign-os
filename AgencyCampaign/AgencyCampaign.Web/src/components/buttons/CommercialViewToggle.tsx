import { useNavigate } from 'react-router-dom'
import { Columns3, List } from 'lucide-react'

const BASE = 'inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-[13px] font-semibold transition-colors'
const ACTIVE = 'bg-background text-foreground shadow-sm'
const IDLE = 'bg-transparent text-muted-foreground hover:text-foreground'

export default function CommercialViewToggle({ active }: { active: 'list' | 'kanban' }) {
  const navigate = useNavigate()
  return (
    <div className="inline-flex rounded-lg bg-muted p-[3px]">
      <button type="button" disabled={active === 'list'} onClick={() => navigate('/comercial/oportunidades')} className={`${BASE} ${active === 'list' ? ACTIVE : IDLE}`}>
        <List className="h-3.5 w-3.5" /> Lista
      </button>
      <button type="button" disabled={active === 'kanban'} onClick={() => navigate('/comercial/pipeline')} className={`${BASE} ${active === 'kanban' ? ACTIVE : IDLE}`}>
        <Columns3 className="h-3.5 w-3.5" /> Kanban
      </button>
    </div>
  )
}
