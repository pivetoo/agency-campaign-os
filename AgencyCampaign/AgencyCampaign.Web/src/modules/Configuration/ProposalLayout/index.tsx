import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button, Checkbox, ConfirmModal, Input, useApi } from 'archon-ui'
import { RefreshCw, Trash2 } from 'lucide-react'
import HtmlTemplateEditor, { type TemplateVariableGroup } from '../../../components/editor/HtmlTemplateEditor'
import { agencySettingsService, type ProposalLayout, type ProposalTemplateVersion } from '../../../services/agencySettingsService'

const VARIABLES: TemplateVariableGroup[] = [
  {
    group: 'Agência',
    items: [
      { key: 'agency.primaryColor', label: 'Cor primária (hex)' },
      { key: 'agency.logo', label: 'Logo (img tag ou texto)' },
      { key: 'agency.name', label: 'Nome da agência' },
      { key: 'agency.displayName', label: 'Nome fantasia ou nome' },
      { key: 'agency.emailHtml', label: 'E-mail (div ou vazio)' },
      { key: 'agency.email', label: 'E-mail (texto simples)' },
      { key: 'agency.phone', label: 'Telefone' },
      { key: 'agency.address', label: 'Endereço' },
      { key: 'agency.document', label: 'CNPJ / documento' },
      { key: 'agency.nameWithDocument', label: 'Nome + documento' },
      { key: 'agency.contactLine', label: 'Email · Fone · Endereço' },
    ],
  },
  {
    group: 'Proposta',
    items: [
      { key: 'proposal.name', label: 'Título da proposta' },
      { key: 'proposal.descriptionHtml', label: 'Descrição em <p> ou vazio' },
      { key: 'proposal.description', label: 'Descrição (texto simples)' },
      { key: 'proposal.date', label: 'Data de emissão' },
      { key: 'proposal.client', label: 'Nome do cliente' },
      { key: 'proposal.clientRow', label: '<tr> do cliente ou vazio' },
      { key: 'proposal.owner', label: 'Responsável interno' },
      { key: 'proposal.ownerRow', label: '<tr> do responsável ou vazio' },
    ],
  },
  {
    group: 'Itens e totais',
    items: [
      { key: 'proposal.items', label: 'Tabela completa de itens' },
      { key: 'proposal.totals', label: 'Bloco de total' },
      { key: 'proposal.totalFormatted', label: 'Valor total formatado (R$)' },
      { key: 'proposal.validityHtml', label: 'Validade em <p> ou vazio' },
      { key: 'proposal.notesHtml', label: 'Observações ou vazio' },
    ],
  },
]

export default function ProposalLayoutEditor() {
  const navigate = useNavigate()
  const { id } = useParams<{ id?: string }>()
  const isNew = !id || id === 'novo'
  const numericId = isNew ? null : Number(id)

  const [name, setName] = useState('')
  const [template, setTemplate] = useState('')
  const [isDefault, setIsDefault] = useState(false)
  const [layoutPresets, setLayoutPresets] = useState<ProposalLayout[]>([])
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false)

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
        description={`Excluir o layout "${name}"? Propostas que usavam este layout voltam ao padrão da agência.`}
        variant="danger"
        onConfirm={() => void handleDelete()}
        loading={deleting}
      />

      <div className="flex shrink-0 flex-wrap items-end gap-3 border-b bg-background px-4 py-3">
        <div className="min-w-[260px] flex-1">
          <label className="mb-1 block text-xs font-medium text-muted-foreground">Nome</label>
          <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: Layout Premium B2B" />
        </div>

        <label className="flex items-center gap-2 pb-1 text-sm">
          <Checkbox checked={isDefault} onCheckedChange={(checked) => setIsDefault(!!checked)} />
          <span>Definir como padrão</span>
        </label>

        {!isNew ? (
          <Button type="button" variant="ghost" size="sm" onClick={() => setConfirmDeleteOpen(true)}>
            <Trash2 className="mr-1.5 h-3.5 w-3.5" />
            Excluir
          </Button>
        ) : null}

        <Button type="button" size="sm" onClick={() => void handleSave()} disabled={saving || loadingVersion || !isValid}>
          {saving ? <RefreshCw className="mr-1.5 h-3.5 w-3.5 animate-spin" /> : null}
          {saving ? 'Salvando...' : isNew ? 'Criar layout' : 'Salvar'}
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
              <span className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Carregar:</span>
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
