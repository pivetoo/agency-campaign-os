import { useEffect, useMemo, useState } from 'react'
import {
  Button,
  Checkbox,
  Input,
  Modal,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  SearchableSelect,
  useApi,
} from 'archon-ui'
import { commercialResponsibleService } from '../../services/commercialResponsibleService'
import type { CommercialResponsible, CommercialUser } from '../../types/commercialResponsible'

interface CommercialResponsibleFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  responsible: CommercialResponsible | null
  onSuccess: () => void
}

export default function CommercialResponsibleFormModal({
  open,
  onOpenChange,
  responsible,
  onSuccess,
}: CommercialResponsibleFormModalProps) {
  const isEditing = !!responsible
  const [users, setUsers] = useState<CommercialUser[]>([])
  const [selectedUserId, setSelectedUserId] = useState<string>('')
  const [notes, setNotes] = useState('')
  const [isActive, setIsActive] = useState(true)

  const { execute: loadUsers, loading: loadingUsers } = useApi<CommercialUser[]>({ showErrorMessage: true })
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return

    if (responsible) {
      setSelectedUserId(String(responsible.userId))
      setNotes(responsible.notes ?? '')
      setIsActive(responsible.isActive)
      return
    }

    setSelectedUserId('')
    setNotes('')
    setIsActive(true)
    void loadUsers(() => commercialResponsibleService.getAvailableUsers()).then((result) => {
      if (result) setUsers(result)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, responsible])

  const userOptions = useMemo(
    () => users.map((user) => ({
      value: String(user.id),
      label: `${user.name} · ${user.email}`,
    })),
    [users]
  )

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const result = await execute(() => {
      if (isEditing && responsible) {
        return commercialResponsibleService.update(responsible.id, {
          id: responsible.id,
          notes: notes || undefined,
          isActive,
        })
      }
      return commercialResponsibleService.create({
        userId: Number(selectedUserId),
        notes: notes || undefined,
      })
    })

    if (result !== null) {
      onSuccess()
    }
  }

  const isValid = isEditing ? true : selectedUserId !== ''

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="form">
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar responsável comercial' : 'Novo responsável comercial'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          {isEditing && responsible ? (
            <div className="rounded-md border border-border/70 bg-muted/30 p-3">
              <div className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                Usuário vinculado (IdentityManagement)
              </div>
              <div className="mt-1 text-sm font-semibold text-foreground">{responsible.name}</div>
              {responsible.email ? (
                <div className="text-xs text-muted-foreground">{responsible.email}</div>
              ) : null}
              <div className="mt-2 text-[11px] text-muted-foreground">
                Para alterar o usuário vinculado, exclua e recadastre.
              </div>
            </div>
          ) : (
            <div className="space-y-2">
              <label className="text-sm font-medium">Usuário do IdentityManagement</label>
              <SearchableSelect
                value={selectedUserId}
                onValueChange={setSelectedUserId}
                options={userOptions}
                placeholder={loadingUsers ? 'Carregando usuários...' : 'Selecione um usuário'}
                searchPlaceholder="Buscar por nome ou e-mail..."
                disabled={loadingUsers || users.length === 0}
              />
              {!loadingUsers && users.length === 0 ? (
                <p className="text-xs text-muted-foreground">
                  Nenhum usuário disponível. Todos os usuários ativos do IdentityManagement já estão vinculados como responsáveis.
                </p>
              ) : null}
            </div>
          )}

          <div className="space-y-2">
            <label className="text-sm font-medium">Observações</label>
            <Input
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Anotações internas sobre o responsável"
            />
          </div>

          <div className="flex items-center justify-between gap-4">
            <div>
              {isEditing && (
                <div className="flex items-center gap-2">
                  <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
                  <span className="text-sm">Ativo</span>
                </div>
              )}
            </div>

            <ModalFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
              <Button type="submit" disabled={loading || !isValid}>
                {loading ? 'Salvando...' : 'Salvar'}
              </Button>
            </ModalFooter>
          </div>
        </form>
      </ModalContent>
    </Modal>
  )
}
