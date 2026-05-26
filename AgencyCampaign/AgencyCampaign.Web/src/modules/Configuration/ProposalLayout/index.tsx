import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button, Checkbox, ConfirmModal, Input, useApi, useI18n } from 'archon-ui'
import { RefreshCw, Trash2 } from 'lucide-react'
import HtmlTemplateEditor, { type TemplateVariableGroup } from '../../../components/editor/HtmlTemplateEditor'
import { agencySettingsService, type ProposalLayout, type ProposalTemplateVersion } from '../../../services/agencySettingsService'

export default function ProposalLayoutEditor() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const { id } = useParams<{ id?: string }>()
  const isNew = !id || id === 'novo'
  const numericId = isNew ? null : Number(id)

  const [name, setName] = useState('')
  const [template, setTemplate] = useState('')
  const [isDefault, setIsDefault] = useState(false)
  const [layoutPresets, setLayoutPresets] = useState<ProposalLayout[]>([])
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false)

  const VARIABLES: TemplateVariableGroup[] = useMemo(() => [
    {
      group: t('configuration.proposalLayout.variables.agency.group'),
      items: [
        { key: 'agency.primaryColor', label: t('configuration.proposalLayout.variables.agency.primaryColor') },
        { key: 'agency.logo', label: t('configuration.proposalLayout.variables.agency.logo') },
        { key: 'agency.name', label: t('configuration.proposalLayout.variables.agency.name') },
        { key: 'agency.displayName', label: t('configuration.proposalLayout.variables.agency.displayName') },
        { key: 'agency.emailHtml', label: t('configuration.proposalLayout.variables.agency.emailHtml') },
        { key: 'agency.email', label: t('configuration.proposalLayout.variables.agency.email') },
        { key: 'agency.phone', label: t('configuration.proposalLayout.variables.agency.phone') },
        { key: 'agency.address', label: t('configuration.proposalLayout.variables.agency.address') },
        { key: 'agency.document', label: t('configuration.proposalLayout.variables.agency.document') },
        { key: 'agency.nameWithDocument', label: t('configuration.proposalLayout.variables.agency.nameWithDocument') },
        { key: 'agency.contactLine', label: t('configuration.proposalLayout.variables.agency.contactLine') },
      ],
    },
    {
      group: t('configuration.proposalLayout.variables.proposal.group'),
      items: [
        { key: 'proposal.name', label: t('configuration.proposalLayout.variables.proposal.name') },
        { key: 'proposal.descriptionHtml', label: t('configuration.proposalLayout.variables.proposal.descriptionHtml') },
        { key: 'proposal.description', label: t('configuration.proposalLayout.variables.proposal.description') },
        { key: 'proposal.date', label: t('configuration.proposalLayout.variables.proposal.date') },
        { key: 'proposal.client', label: t('configuration.proposalLayout.variables.proposal.client') },
        { key: 'proposal.clientRow', label: t('configuration.proposalLayout.variables.proposal.clientRow') },
        { key: 'proposal.owner', label: t('configuration.proposalLayout.variables.proposal.owner') },
        { key: 'proposal.ownerRow', label: t('configuration.proposalLayout.variables.proposal.ownerRow') },
      ],
    },
    {
      group: t('configuration.proposalLayout.variables.itemsTotals.group'),
      items: [
        { key: 'proposal.items', label: t('configuration.proposalLayout.variables.itemsTotals.items') },
        { key: 'proposal.totals', label: t('configuration.proposalLayout.variables.itemsTotals.totals') },
        { key: 'proposal.totalFormatted', label: t('configuration.proposalLayout.variables.itemsTotals.totalFormatted') },
        { key: 'proposal.validityHtml', label: t('configuration.proposalLayout.variables.itemsTotals.validityHtml') },
        { key: 'proposal.notesHtml', label: t('configuration.proposalLayout.variables.itemsTotals.notesHtml') },
      ],
    },
  ], [t])

  const { execute: fetchVersion, loading: loadingVersion } = useApi<ProposalTemplateVersion | null>({ showErrorMessage: true })
  const { execute: runSave, loading: saving } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    let cancelled = false
    void agencySettingsService.getProposalLayouts().then((response) => {
      if (!cancelled && response.data) {
        setLayoutPresets(response.data)
        if (isNew && !template && response.data.length > 0) {
          setTemplate(response.data[0].template)
        }
      }
    })
    return () => { cancelled = true }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    if (isNew || numericId === null) return
    void fetchVersion(() => agencySettingsService.getProposalTemplateVersionById(numericId)).then((result) => {
      if (!result) return
      setName(result.name)
      setTemplate(result.template)
      setIsDefault(result.isActive)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isNew, numericId])

  const fetchPreview = useCallback(
    (htmlTemplate: string) =>
      agencySettingsService.previewProposalTemplate(htmlTemplate).then((res) => res.data?.html ?? ''),
    [],
  )

  const isValid = name.trim().length >= 2 && template.trim().length >= 10

  const handleSave = async () => {
    if (!isValid) return
    const result = await runSave(() => {
      if (isNew) {
        return agencySettingsService.saveProposalTemplateVersion(name.trim(), template, isDefault)
      }
      return agencySettingsService.updateProposalTemplateVersion(numericId!, name.trim(), template, isDefault)
    })
    if (result !== null) {
      navigate('/configuracao/layouts-proposta')
    }
  }

  const handleDelete = async () => {
    if (isNew || numericId === null) return
    const result = await runDelete(() => agencySettingsService.deleteProposalTemplateVersion(numericId))
    if (result !== null) {
      setConfirmDeleteOpen(false)
      navigate('/configuracao/layouts-proposta')
    }
  }

  const handleLoadPreset = (key: string) => {
    const preset = layoutPresets.find((layout) => layout.key === key)
    if (preset) setTemplate(preset.template)
  }

  return (
    <>
      <ConfirmModal
        open={confirmDeleteOpen}
        onOpenChange={setConfirmDeleteOpen}
        description={t('configuration.proposalLayouts.confirm.delete').replace('{0}', name)}
        variant="danger"
        onConfirm={() => void handleDelete()}
        loading={deleting}
      />

      <div className="flex shrink-0 flex-wrap items-end gap-3 border-b bg-background px-4 py-3">
        <div className="min-w-[260px] flex-1">
          <label className="mb-1 block text-xs font-medium text-muted-foreground">{t('common.field.name')}</label>
          <Input value={name} onChange={(e) => setName(e.target.value)} placeholder={t('configuration.proposalLayout.name.placeholder')} />
        </div>

        <label className="flex items-center gap-2 pb-1 text-sm">
          <Checkbox checked={isDefault} onCheckedChange={(checked) => setIsDefault(!!checked)} />
          <span>{t('configuration.proposalLayout.setAsDefault')}</span>
        </label>

        {!isNew ? (
          <Button type="button" variant="ghost" size="sm" onClick={() => setConfirmDeleteOpen(true)}>
            <Trash2 className="mr-1.5 h-3.5 w-3.5" />
            {t('common.action.delete')}
          </Button>
        ) : null}

        <Button type="button" size="sm" onClick={() => void handleSave()} disabled={saving || loadingVersion || !isValid}>
          {saving ? <RefreshCw className="mr-1.5 h-3.5 w-3.5 animate-spin" /> : null}
          {saving ? t('common.action.saving') : isNew ? t('configuration.proposalLayout.action.create') : t('common.action.save')}
        </Button>
      </div>

      <HtmlTemplateEditor
        value={template}
        onChange={setTemplate}
        variables={VARIABLES}
        fetchPreview={fetchPreview}
        tokenFormat={(key) => `{{${key}}}`}
        height="calc(100vh - 170px)"
        toolbarLeftSlot={
          layoutPresets.length > 0 ? (
            <>
              <span className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">{t('configuration.proposalLayout.loadPreset')}</span>
              {layoutPresets.map((preset) => (
                <button
                  key={preset.key}
                  type="button"
                  onClick={() => handleLoadPreset(preset.key)}
                  className="rounded-md border border-border px-3 py-1 text-xs font-medium text-foreground hover:bg-muted"
                >
                  {preset.name}
                </button>
              ))}
            </>
          ) : null
        }
      />
    </>
  )
}
