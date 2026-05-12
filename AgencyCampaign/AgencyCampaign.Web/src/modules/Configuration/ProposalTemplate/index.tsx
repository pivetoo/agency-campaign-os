import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import Editor, { type OnMount } from '@monaco-editor/react'
import { Button, useApi, useI18n } from 'archon-ui'
import { Braces, ChevronDown, List, RefreshCw, Trash2 } from 'lucide-react'
import { agencySettingsService } from '../../../services/agencySettingsService'
import type { ProposalLayout, ProposalTemplateVersion } from '../../../services/agencySettingsService'
import type { AgencySettings } from '../../../types/agencySettings'

const DEBOUNCE_MS = 900

const VARIABLES = [
  {
    group: 'Agência',
    items: [
      { variable: '{{agency.primaryColor}}', description: 'Cor primária (hex)' },
      { variable: '{{agency.logo}}', description: 'Logo (img tag ou texto)' },
      { variable: '{{agency.name}}', description: 'Nome da agência' },
      { variable: '{{agency.displayName}}', description: 'Nome fantasia ou nome' },
      { variable: '{{agency.emailHtml}}', description: 'E-mail (div ou vazio)' },
      { variable: '{{agency.email}}', description: 'E-mail (texto simples)' },
      { variable: '{{agency.phone}}', description: 'Telefone' },
      { variable: '{{agency.address}}', description: 'Endereço' },
      { variable: '{{agency.document}}', description: 'CNPJ / documento' },
      { variable: '{{agency.documentSuffix}}', description: '" (CNPJ)" ou string vazia' },
      { variable: '{{agency.nameWithDocument}}', description: 'Nome + documento' },
      { variable: '{{agency.contactLine}}', description: 'Email · Fone · Endereço' },
    ],
  },
  {
    group: 'Proposta',
    items: [
      { variable: '{{proposal.name}}', description: 'Título da proposta' },
      { variable: '{{proposal.descriptionHtml}}', description: 'Descrição em <p> ou vazio' },
      { variable: '{{proposal.description}}', description: 'Descrição (texto simples)' },
      { variable: '{{proposal.date}}', description: 'Data de emissão' },
      { variable: '{{proposal.client}}', description: 'Nome do cliente' },
      { variable: '{{proposal.clientRow}}', description: '<tr> do cliente ou vazio' },
      { variable: '{{proposal.owner}}', description: 'Responsável interno' },
      { variable: '{{proposal.ownerRow}}', description: '<tr> do responsável ou vazio' },
    ],
  },
  {
    group: 'Itens e totais',
    items: [
      { variable: '{{proposal.items}}', description: 'Tabela completa de itens' },
      { variable: '{{proposal.totals}}', description: 'Bloco de total' },
      { variable: '{{proposal.totalFormatted}}', description: 'Valor total formatado (R$)' },
      { variable: '{{proposal.validityHtml}}', description: 'Validade em <p> ou vazio' },
      { variable: '{{proposal.notesHtml}}', description: 'Observações ou vazio' },
    ],
  },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

export default function ProposalTemplate() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [template, setTemplate] = useState<string>('')
  const [previewHtml, setPreviewHtml] = useState<string>('')
  const [layouts, setLayouts] = useState<ProposalLayout[]>([])
  const [versions, setVersions] = useState<ProposalTemplateVersion[]>([])
  const [varPickerOpen, setVarPickerOpen] = useState(false)
  const [savePickerOpen, setSavePickerOpen] = useState(false)
  const [newVersionName, setNewVersionName] = useState('')
  const [newVersionActivate, setNewVersionActivate] = useState(true)

  const editorRef = useRef<Parameters<OnMount>[0] | null>(null)
  const varPickerRef = useRef<HTMLDivElement | null>(null)
  const savePickerRef = useRef<HTMLDivElement | null>(null)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const { execute: fetchLayouts } = useApi<ProposalLayout[]>({ showErrorMessage: true })
  const { execute: fetchSettings } = useApi<AgencySettings | null>({ showErrorMessage: true })
  const { execute: fetchVersions } = useApi<ProposalTemplateVersion[]>({ showErrorMessage: true })
  const { execute: runSaveVersion, loading: savingVersion } = useApi<ProposalTemplateVersion>({
    showSuccessMessage: true,
    showErrorMessage: true,
  })
  const { execute: runActivate } = useApi<ProposalTemplateVersion>({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: runDelete } = useApi<unknown>({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: runPreview } = useApi<{ html: string }>({ showErrorMessage: false })

  const loadVersions = useCallback(async () => {
    const result = await fetchVersions(() => agencySettingsService.getProposalTemplateVersions())
    if (result) {
      setVersions(result)
    }
    return result
  }, [fetchVersions])

  const load = useCallback(async () => {
    const [layoutsResult, settingsResult, versionsResult] = await Promise.all([
      fetchLayouts(() => agencySettingsService.getProposalLayouts()),
      fetchSettings(() => agencySettingsService.get()),
      fetchVersions(() => agencySettingsService.getProposalTemplateVersions()),
    ])
    if (layoutsResult) {
      setLayouts(layoutsResult)
    }
    if (versionsResult) {
      setVersions(versionsResult)
    }
    if (settingsResult) {
      const saved = settingsResult.proposalHtmlTemplate
      if (saved) {
        setTemplate(saved)
      } else if (layoutsResult && layoutsResult.length > 0) {
        setTemplate(layoutsResult[0].template)
      }
    }
  }, [fetchLayouts, fetchSettings, fetchVersions])

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    if (!template) {
      return
    }
    if (debounceRef.current) {
      clearTimeout(debounceRef.current)
    }
    debounceRef.current = setTimeout(async () => {
      const result = await runPreview(() => agencySettingsService.previewProposalTemplate(template))
      if (result) {
        setPreviewHtml(result.html)
      }
    }, DEBOUNCE_MS)
    return () => {
      if (debounceRef.current) {
        clearTimeout(debounceRef.current)
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [template])

  useEffect(() => {
    const pickers = [
      { open: varPickerOpen, ref: varPickerRef, close: () => setVarPickerOpen(false) },
      { open: savePickerOpen, ref: savePickerRef, close: () => setSavePickerOpen(false) },
    ]
    const active = pickers.filter((p) => p.open)
    if (active.length === 0) {
      return
    }
    const handleClickOutside = (event: MouseEvent) => {
      active.forEach(({ ref, close }) => {
        if (ref.current && !ref.current.contains(event.target as Node)) {
          close()
        }
      })
    }
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        active.forEach(({ close }) => close())
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    document.addEventListener('keydown', handleEscape)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
      document.removeEventListener('keydown', handleEscape)
    }
  }, [varPickerOpen, savePickerOpen])

  const handleEditorMount: OnMount = (editorInstance) => {
    editorRef.current = editorInstance
  }

  const handleLayoutLoad = (key: string) => {
    const layout = layouts.find((l) => l.key === key)
    if (layout) {
      setTemplate(layout.template)
    }
  }

  const insertVariable = (variable: string) => {
    const ed = editorRef.current
    setVarPickerOpen(false)
    if (!ed) {
      setTemplate((prev) => prev + variable)
      return
    }
    const selection = ed.getSelection()
    if (!selection) {
      setTemplate((prev) => prev + variable)
      return
    }
    ed.executeEdits('', [{ range: selection, text: variable, forceMoveMarkers: true }])
    ed.focus()
  }

  const handleOpenSavePicker = () => {
    const nextName = `Versão ${versions.length + 1}`
    setNewVersionName(nextName)
    setNewVersionActivate(true)
    setSavePickerOpen(true)
  }

  const handleSaveVersion = async () => {
    if (!newVersionName.trim()) {
      return
    }
    const result = await runSaveVersion(() =>
      agencySettingsService.saveProposalTemplateVersion(newVersionName.trim(), template, newVersionActivate),
    )
    if (result) {
      setSavePickerOpen(false)
      const updated = await loadVersions()
      if (updated) {
        setVersions(updated)
      }
    }
  }

  const handleActivate = async (version: ProposalTemplateVersion) => {
    const result = await runActivate(() => agencySettingsService.activateProposalTemplateVersion(version.id))
    if (result) {
      setTemplate(version.template)
      const updated = await loadVersions()
      if (updated) {
        setVersions(updated)
      }
    }
  }

  const handleDelete = async (version: ProposalTemplateVersion) => {
    if (!window.confirm(t('configuration.proposalTemplate.confirm.deleteVersion').replace('{0}', version.name))) {
      return
    }
    const result = await runDelete(() => agencySettingsService.deleteProposalTemplateVersion(version.id))
    if (result !== null) {
      const updated = await loadVersions()
      if (updated) {
        setVersions(updated)
      }
    }
  }

  return (
    <div className="flex flex-col overflow-hidden" style={{ height: 'calc(100vh - 100px)' }}>
      <div className="flex shrink-0 items-center justify-between gap-3 border-b bg-background px-4 py-2.5">
        <div className="flex items-center gap-1.5">
          {layouts.map((layout) => (
            <button
              key={layout.key}
              type="button"
              onClick={() => handleLayoutLoad(layout.key)}
              className="rounded-md border border-border px-3 py-1.5 text-xs font-medium text-foreground hover:bg-muted"
            >
              {layout.name}
            </button>
          ))}
        </div>

        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => navigate('/configuracao/templates-proposta')}
          >
            <List className="mr-1.5 h-3.5 w-3.5" />
            Lista
          </Button>

          <div ref={varPickerRef} className="relative">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setVarPickerOpen((v) => !v)}
            >
              <Braces className="mr-1.5 h-3.5 w-3.5" />
              {t('common.action.insertVariable')}
            </Button>

            {varPickerOpen && (
              <div className="absolute right-0 top-full z-50 mt-1 max-h-80 w-96 overflow-y-auto rounded-md border bg-popover shadow-lg">
                {VARIABLES.map((group) => (
                  <div key={group.group}>
                    <div className="sticky top-0 bg-muted/80 px-3 py-1.5 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground backdrop-blur-sm">
                      {group.group}
                    </div>
                    {group.items.map((item) => (
                      <button
                        key={item.variable}
                        type="button"
                        onClick={() => insertVariable(item.variable)}
                        className="flex w-full flex-col items-start gap-0.5 px-3 py-2 text-left hover:bg-muted/60"
                      >
                        <code className="text-xs font-medium text-primary">{item.variable}</code>
                        <span className="text-[11px] text-muted-foreground">{item.description}</span>
                      </button>
                    ))}
                  </div>
                ))}
              </div>
            )}
          </div>

          <div ref={savePickerRef} className="relative">
            <Button size="sm" onClick={handleOpenSavePicker} disabled={savingVersion}>
              {savingVersion ? (
                <RefreshCw className="mr-1.5 h-3.5 w-3.5 animate-spin" />
              ) : (
                <ChevronDown className="mr-1.5 h-3.5 w-3.5" />
              )}
              Salvar
            </Button>

            {savePickerOpen && (
              <div className="absolute right-0 top-full z-50 mt-1 w-80 rounded-md border bg-popover shadow-lg">
                <div className="border-b p-3">
                  <p className="mb-2 text-xs font-semibold text-foreground">Nova versão</p>
                  <input
                    type="text"
                    value={newVersionName}
                    onChange={(e) => setNewVersionName(e.target.value)}
                    placeholder="Nome da versão"
                    className="mb-2 w-full rounded-md border bg-background px-2.5 py-1.5 text-sm outline-none focus:ring-1 focus:ring-ring"
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') {
                        void handleSaveVersion()
                      }
                    }}
                    autoFocus
                  />
                  <label className="mb-3 flex cursor-pointer items-center gap-2 text-xs text-foreground">
                    <input
                      type="checkbox"
                      checked={newVersionActivate}
                      onChange={(e) => setNewVersionActivate(e.target.checked)}
                      className="h-3.5 w-3.5 rounded border-border"
                    />
                    Ativar esta versão
                  </label>
                  <Button
                    size="sm"
                    className="w-full"
                    onClick={handleSaveVersion}
                    disabled={savingVersion || !newVersionName.trim()}
                  >
                    {savingVersion ? 'Salvando...' : 'Salvar versão'}
                  </Button>
                </div>

                {versions.length > 0 && (
                  <div className="max-h-56 overflow-y-auto">
                    <p className="px-3 pb-1 pt-2 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
                      Versões salvas
                    </p>
                    {versions.map((version) => (
                      <div
                        key={version.id}
                        className="flex items-start justify-between gap-2 px-3 py-2 hover:bg-muted/40"
                      >
                        <div className="min-w-0 flex-1">
                          <div className="flex items-center gap-1.5">
                            <span className="truncate text-xs font-medium text-foreground">{version.name}</span>
                            {version.isActive && (
                              <span className="shrink-0 rounded-full bg-primary/15 px-1.5 py-0.5 text-[9px] font-semibold text-primary">
                                Ativa
                              </span>
                            )}
                          </div>
                          <span className="text-[10px] text-muted-foreground">{formatDate(version.createdAt)}</span>
                        </div>
                        <div className="flex shrink-0 items-center gap-1">
                          {!version.isActive && (
                            <button
                              type="button"
                              onClick={() => void handleActivate(version)}
                              className="rounded px-2 py-1 text-[10px] font-medium text-primary hover:bg-primary/10"
                            >
                              Ativar
                            </button>
                          )}
                          <button
                            type="button"
                            onClick={() => void handleDelete(version)}
                            className="rounded p-1 text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
                          >
                            <Trash2 className="h-3 w-3" />
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="flex min-h-0 flex-1">
        <div className="flex w-2/5 flex-col border-r">
          <div className="shrink-0 border-b bg-muted/30 px-3 py-1.5 text-[11px] font-medium text-muted-foreground">
            HTML
          </div>
          <div className="min-h-0 flex-1">
            <Editor
              height="100%"
              defaultLanguage="html"
              value={template}
              onChange={(v) => setTemplate(v ?? '')}
              onMount={handleEditorMount}
              options={{
                minimap: { enabled: false },
                fontSize: 12,
                lineNumbers: 'on',
                wordWrap: 'on',
                scrollBeyondLastLine: false,
                tabSize: 2,
              }}
              theme="vs"
            />
          </div>
        </div>

        <div className="flex w-3/5 flex-col">
          <div className="shrink-0 border-b bg-muted/30 px-3 py-1.5 text-[11px] font-medium text-muted-foreground">
            Pré-visualização — dados de exemplo
          </div>
          <div className="min-h-0 flex-1">
            {previewHtml ? (
              <iframe
                srcDoc={previewHtml}
                className="h-full w-full border-0"
                title="Preview da proposta"
                sandbox="allow-same-origin"
              />
            ) : (
              <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
                A pré-visualização aparece aqui após editar o template.
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
