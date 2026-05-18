import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Button,
  Checkbox,
  ConfirmModal,
  Input,
  SearchableSelect,
  useApi,
  useI18n,
} from 'archon-ui'
import { RefreshCw, Trash2 } from 'lucide-react'
import HtmlTemplateEditor, { type TemplateVariableGroup } from '../../../components/editor/HtmlTemplateEditor'
import { campaignDocumentTemplateService } from '../../../services/campaignDocumentTemplateService'
import {
  CampaignDocumentType,
  campaignDocumentTypeLabels,
  type CampaignDocumentTypeValue,
} from '../../../types/campaignDocument'
import type { CampaignDocumentTemplate, CampaignDocumentTemplateVariableMap } from '../../../types/campaignDocumentTemplate'

const documentTypeOptions = Object.values(CampaignDocumentType).map((value) => ({
  value: String(value),
  label: campaignDocumentTypeLabels[value as CampaignDocumentTypeValue],
}))

const DEFAULT_BODY = `<h1>{{ campaignName }}</h1>
<p>Pelo presente instrumento, {{ brandName }} (CNPJ {{ brandDocument }}), doravante denominada CONTRATANTE,
e {{ creatorName }} (CPF {{ creatorDocument }}), doravante denominado CONTRATADO,
firmam o presente acordo para entrega do escopo da campanha {{ campaignName }}.</p>

<p><strong>Valor combinado:</strong> {{ creatorAgreedAmount }}<br/>
<strong>Vigência:</strong> {{ campaignStartDate }} a {{ campaignEndDate }}</p>

<p>Assinado em {{ today }}.</p>`

export default function DocumentTemplateEditor() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const { id } = useParams<{ id?: string }>()
  const isNew = !id || id === 'novo'
  const numericId = isNew ? null : Number(id)

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [documentType, setDocumentType] = useState<CampaignDocumentTypeValue>(CampaignDocumentType.CreatorAgreement)
  const [body, setBody] = useState(isNew ? DEFAULT_BODY : '')
  const [isActive, setIsActive] = useState(true)
  const [variableMap, setVariableMap] = useState<CampaignDocumentTemplateVariableMap>({})
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false)

  const { execute: fetchTemplate, loading: loadingTemplate } = useApi<CampaignDocumentTemplate | null>({ showErrorMessage: true })
  const { execute: runSave, loading: saving } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: runDelete, loading: deleting } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    let cancelled = false
    void campaignDocumentTemplateService.getVariables().then((map) => {
      if (!cancelled) setVariableMap(map)
    })
    return () => { cancelled = true }
  }, [])

  useEffect(() => {
    if (isNew || numericId === null) return
    void fetchTemplate(() => campaignDocumentTemplateService.getById(numericId)).then((result) => {
      if (!result) return
      setName(result.name)
      setDescription(result.description ?? '')
      setDocumentType(result.documentType)
      setBody(result.body)
      setIsActive(result.isActive)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isNew, numericId])

  const variableGroups = useMemo<TemplateVariableGroup[]>(() => {
    const list = variableMap[String(documentType)] ?? []
    const grouped = new Map<string, TemplateVariableGroup['items']>()
    list.forEach((variable) => {
      const arr = grouped.get(variable.group) ?? []
      arr.push({ key: variable.key, label: variable.label, description: variable.description })
      grouped.set(variable.group, arr)
    })
    return Array.from(grouped.entries()).map(([group, items]) => ({ group, items }))
  }, [variableMap, documentType])

  const fetchPreview = useCallback(
    (template: string) => campaignDocumentTemplateService.preview(template, documentType),
    [documentType],
  )

  const isValid = name.trim().length >= 2 && body.trim().length >= 10

  const handleSave = async () => {
    if (!isValid) return
    const payload = {
      name: name.trim(),
      description: description.trim() || undefined,
      documentType,
      body,
    }
    const result = await runSave(() => {
      if (isNew) return campaignDocumentTemplateService.create(payload)
      return campaignDocumentTemplateService.update(numericId!, { id: numericId!, ...payload, isActive })
    })
    if (result !== null) {
      navigate('/configuracao/modelos-contrato')
    }
  }

  const handleDelete = async () => {
    if (isNew || numericId === null) return
    const result = await runDelete(() => campaignDocumentTemplateService.delete(numericId))
    if (result !== null) {
      setConfirmDeleteOpen(false)
      navigate('/configuracao/modelos-contrato')
    }
  }

  return (
    <>
      <ConfirmModal
        open={confirmDeleteOpen}
        onOpenChange={setConfirmDeleteOpen}
        description={`Excluir o modelo "${name}"? Modelos em uso ficam apenas desativados.`}
        variant="danger"
        onConfirm={() => void handleDelete()}
        loading={deleting}
      />

      <div className="flex shrink-0 flex-wrap items-end gap-3 border-b bg-background px-4 py-3">
        <div className="min-w-[220px] flex-1">
          <label className="mb-1 block text-xs font-medium text-muted-foreground">Nome</label>
          <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: Contrato padrão de creator" />
        </div>

        <div className="w-56">
          <label className="mb-1 block text-xs font-medium text-muted-foreground">Tipo</label>
          <SearchableSelect
            value={String(documentType)}
            onValueChange={(value) => setDocumentType(Number(value) as CampaignDocumentTypeValue)}
            options={documentTypeOptions}
            placeholder="Selecione"
          />
        </div>

        <div className="min-w-[260px] flex-1">
          <label className="mb-1 block text-xs font-medium text-muted-foreground">Descrição</label>
          <Input value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Quando usar este modelo" />
        </div>

        {!isNew ? (
          <label className="flex items-center gap-2 pb-1 text-sm">
            <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
            <span>{t('common.status.active')}</span>
          </label>
        ) : null}

        {!isNew ? (
          <Button type="button" variant="ghost" size="sm" onClick={() => setConfirmDeleteOpen(true)}>
            <Trash2 className="mr-1.5 h-3.5 w-3.5" />
            Excluir
          </Button>
        ) : null}

        <Button type="button" size="sm" onClick={() => void handleSave()} disabled={saving || loadingTemplate || !isValid}>
          {saving ? (
            <RefreshCw className="mr-1.5 h-3.5 w-3.5 animate-spin" />
          ) : null}
          {saving ? 'Salvando...' : isNew ? 'Criar modelo' : 'Salvar'}
        </Button>
      </div>

      <HtmlTemplateEditor
        value={body}
        onChange={setBody}
        variables={variableGroups}
        fetchPreview={fetchPreview}
        height="calc(100vh - 170px)"
      />
    </>
  )
}
