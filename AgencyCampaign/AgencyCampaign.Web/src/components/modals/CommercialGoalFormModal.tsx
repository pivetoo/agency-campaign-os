import { useEffect, useState } from 'react'
import { Button, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, SearchableSelect, useApi, useI18n } from 'archon-ui'
import { commercialGoalService, type CreateCommercialGoalRequest } from '../../services/commercialGoalService'
import { commercialResponsibleService } from '../../services/commercialResponsibleService'
import type { CommercialResponsible } from '../../types/commercialResponsible'
import { CommercialGoalPeriodType, commercialGoalPeriodTypeLabels, type CommercialGoal } from '../../types/commercialGoal'
import { todayDateInput } from '../../lib/format'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  goal: CommercialGoal | null
  onSuccess: () => void
}

function firstDayOfMonth(): string {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-01`
}

const initialFormData: CreateCommercialGoalRequest = {
  userId: null,
  periodType: CommercialGoalPeriodType.Month,
  periodStart: '',
  targetAmount: 0,
  notes: '',
}

export default function CommercialGoalFormModal({ open, onOpenChange, goal, onSuccess }: Props) {
  const { t } = useI18n()
  const isEditing = !!goal
  const [formData, setFormData] = useState<CreateCommercialGoalRequest>(initialFormData)
  const [responsibles, setResponsibles] = useState<CommercialResponsible[]>([])
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    void commercialResponsibleService.getAll().then(setResponsibles)
  }, [open])

  useEffect(() => {
    if (goal) {
      setFormData({
        userId: goal.userId ?? null,
        periodType: goal.periodType,
        periodStart: goal.periodStart,
        targetAmount: goal.targetAmount,
        notes: goal.notes ?? '',
      })
      return
    }
    setFormData({ ...initialFormData, periodStart: `${firstDayOfMonth()}T00:00:00.000Z` })
  }, [goal, open])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    const payload: CreateCommercialGoalRequest = {
      ...formData,
      userId: formData.userId ?? null,
      notes: formData.notes?.trim() || undefined,
    }
    const result = await execute(() => isEditing && goal
      ? commercialGoalService.update(goal.id, { ...payload, id: goal.id, isActive: goal.isActive })
      : commercialGoalService.create(payload))
    if (result !== null) onSuccess()
  }

  const periodStartDate = formData.periodStart ? formData.periodStart.split('T')[0] ?? '' : ''

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="form">
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.commercialGoal.title.edit') : t('modal.commercialGoal.title.new')}</ModalTitle>
        </ModalHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t('modal.commercialGoal.field.scope')}</label>
            <SearchableSelect
              value={formData.userId ? String(formData.userId) : ''}
              onValueChange={(value) => setFormData((prev) => ({ ...prev, userId: value ? Number(value) : null }))}
              options={[
                { value: '', label: t('modal.commercialGoal.option.allSellers') },
                ...responsibles.filter((r) => r.isActive).map((r) => ({ value: String(r.id), label: r.name })),
              ]}
              placeholder={t('common.placeholder.select')}
              searchPlaceholder={t('common.placeholder.search')}
            />
            <p className="text-[11px] text-muted-foreground">{t('modal.commercialGoal.help.scope')}</p>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.commercialGoal.field.period')}</label>
              <select
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                value={formData.periodType}
                onChange={(e) => setFormData((prev) => ({ ...prev, periodType: Number(e.target.value) as typeof CommercialGoalPeriodType[keyof typeof CommercialGoalPeriodType] }))}
              >
                <option value={1}>{commercialGoalPeriodTypeLabels[1]}</option>
                <option value={2}>{commercialGoalPeriodTypeLabels[2]}</option>
                <option value={3}>{commercialGoalPeriodTypeLabels[3]}</option>
              </select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.commercialGoal.field.start')}</label>
              <Input
                type="date"
                value={periodStartDate}
                onChange={(e) => setFormData((prev) => ({ ...prev, periodStart: e.target.value ? `${e.target.value}T00:00:00.000Z` : `${todayDateInput()}T00:00:00.000Z` }))}
              />
              <p className="text-[11px] text-muted-foreground">{t('modal.commercialGoal.help.start')}</p>
            </div>
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">{t('modal.commercialGoal.field.target')}</label>
            <Input type="number" min={0} step="0.01" value={formData.targetAmount || ''} onChange={(e) => setFormData((prev) => ({ ...prev, targetAmount: e.target.value === '' ? 0 : Number(e.target.value) }))} required />
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">{t('modal.commercialGoal.field.notes')}</label>
            <textarea
              className="min-h-[60px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              value={formData.notes ?? ''}
              onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))}
              placeholder={t('modal.commercialGoal.placeholder.notes')}
            />
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading}>{loading ? t('common.action.saving') : t('common.action.save')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
