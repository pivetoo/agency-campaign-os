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
  useI18n,
} from 'archon-ui'
import { creatorSocialHandleService } from '../../services/creatorSocialHandleService'
import { platformService } from '../../services/platformService'
import type { CreatorSocialHandle } from '../../types/creatorSocialHandle'
import type { Platform } from '../../types/platform'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  creatorId: number
  handle: CreatorSocialHandle | null
  onSuccess: () => void
}

export default function CreatorSocialHandleFormModal({ open, onOpenChange, creatorId, handle, onSuccess }: Props) {
  const { t } = useI18n()
  const isEditing = !!handle
  const [platforms, setPlatforms] = useState<Platform[]>([])
  const [platformId, setPlatformId] = useState<number>(0)
  const [handleText, setHandleText] = useState('')
  const [profileUrl, setProfileUrl] = useState('')
  const [followers, setFollowers] = useState<string>('')
  const [engagementRate, setEngagementRate] = useState<string>('')
  const [isPrimary, setIsPrimary] = useState(false)
  const [isActive, setIsActive] = useState(true)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (!open) return
    void platformService.getAll({ pageSize: 200 }).then((r) => setPlatforms(r.data ?? []))
    if (handle) {
      setPlatformId(handle.platformId)
      setHandleText(handle.handle)
      setProfileUrl(handle.profileUrl ?? '')
      setFollowers(handle.followers != null ? String(handle.followers) : '')
      setEngagementRate(handle.engagementRate != null ? String(handle.engagementRate) : '')
      setIsPrimary(handle.isPrimary)
      setIsActive(handle.isActive)
    } else {
      setPlatformId(0)
      setHandleText('')
      setProfileUrl('')
      setFollowers('')
      setEngagementRate('')
      setIsPrimary(false)
      setIsActive(true)
    }
  }, [open, handle])

  const isValid = useMemo(() => platformId > 0 && handleText.trim().length > 0, [platformId, handleText])

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    const followersValue = followers.trim() === '' ? null : Number(followers)
    const engagementValue = engagementRate.trim() === '' ? null : Number(engagementRate)
    const payload = {
      creatorId,
      platformId,
      handle: handleText.trim(),
      profileUrl: profileUrl.trim() || undefined,
      followers: followersValue,
      engagementRate: engagementValue,
      isPrimary,
    }
    const result = await execute(() => {
      if (isEditing && handle) {
        return creatorSocialHandleService.update(handle.id, { id: handle.id, ...payload, isActive })
      }
      return creatorSocialHandleService.create(payload)
    })
    if (result !== null) onSuccess()
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="form">
        <ModalHeader>
          <ModalTitle>{isEditing ? t('modal.socialHandle.title.edit') : t('modal.socialHandle.title.new')}</ModalTitle>
        </ModalHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.field.platform')}</label>
              <SearchableSelect
                value={platformId ? String(platformId) : ''}
                onValueChange={(value) => setPlatformId(Number(value))}
                options={platforms.filter((p) => p.isActive).map((p) => ({ value: String(p.id), label: p.name }))}
                placeholder={t('common.placeholder.select')}
                searchPlaceholder={t('common.placeholder.search')}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.socialHandle.field.handle')}</label>
              <Input value={handleText} onChange={(e) => setHandleText(e.target.value)} placeholder="@usuario" required />
            </div>
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('modal.socialHandle.field.profileUrl')}</label>
              <Input value={profileUrl} onChange={(e) => setProfileUrl(e.target.value)} placeholder="https://..." />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.socialHandle.field.followers')}</label>
              <Input type="number" value={followers} onChange={(e) => setFollowers(e.target.value)} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('modal.socialHandle.field.engagement')}</label>
              <Input type="number" step="0.01" min="0" max="100" value={engagementRate} onChange={(e) => setEngagementRate(e.target.value)} />
            </div>
          </div>
          <div className="flex flex-wrap gap-6">
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={isPrimary} onCheckedChange={(checked) => setIsPrimary(!!checked)} />
              <span>{t('modal.socialHandle.field.isPrimary')}</span>
            </label>
            {isEditing && (
              <label className="flex items-center gap-2 text-sm">
                <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
                <span>{t('common.status.active')}</span>
              </label>
            )}
          </div>
          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || !isValid}>{loading ? t('common.action.saving') : t('common.action.save')}</Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
