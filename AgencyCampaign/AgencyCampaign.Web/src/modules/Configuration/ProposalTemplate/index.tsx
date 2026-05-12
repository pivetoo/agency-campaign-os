import { useCallback, useEffect, useRef, useState } from 'react'
import Editor from '@monaco-editor/react'
import type { editor } from 'monaco-editor'
import { Button, useApi, useI18n } from 'archon-ui'
import { Braces, RefreshCw, Save } from 'lucide-react'
import { agencySettingsService } from '../../../services/agencySettingsService'
import type { ProposalLayout } from '../../../services/agencySettingsService'
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

export default function ProposalTemplate() {
  const { t } = useI18n()
  const [template, setTemplate] = useState<string>('')
  const [previewHtml, setPreviewHtml] = useState<string>('')
  const [layouts, setLayouts] = useState<ProposalLayout[]>([])
  const [varPickerOpen, setVarPickerOpen] = useState(false)

  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null)
  const varPickerRef = useRef<HTMLDivElement | null>(null)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const { execute: fetchLayouts } = useApi<ProposalLayout[]>({ showErrorMessage: true })
  const { execute: fetchSettings } = useApi<AgencySettings | null>({ showErrorMessage: true })
  const { execute: runSave, loading: saving } = useApi<AgencySettings | null>({
    showSuccessMessage: true,
    showErrorMessage: true,
  })
  const { execute: runPreview } = useApi<{ html: string }>({ showErrorMessage: false })

  const load = useCallback(async () => {
    const [layoutsResult, settingsResult] = await Promise.all([
      fetchLayouts(() => agencySettingsService.getProposalLayouts()),
      fetchSettings(() => agencySettingsService.get()),
    ])
    if (layoutsResult) {
      setLayouts(layoutsResult)
    }
    if (settingsResult) {
      const saved = settingsResult.proposalHtmlTemplate
      if (saved) {
        setTemplate(saved)
      } else if (layoutsResult && layoutsResult.length > 0) {
        setTemplate(layoutsResult[0].template)
      }
    }
  }, [fetchLayouts, fetchSettings])

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
    if (!varPickerOpen) {
      return
    }
    const handleClickOutside = (event: MouseEvent) => {
      if (varPickerRef.current && !varPickerRef.current.contains(event.target as Node)) {
        setVarPickerOpen(false)
      }
    }
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setVarPickerOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    document.addEventListener('keydown', handleEscape)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
      document.removeEventListener('keydown', handleEscape)
    }
  }, [varPickerOpen])

  const handleEditorMount = (editorInstance: editor.IStandaloneCodeEditor) => {
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

  const handleSave = async () => {
    await runSave(() => agencySettingsService.saveProposalTemplate(template))
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

          <Button size="sm" onClick={handleSave} disabled={saving}>
            {saving ? (
              <RefreshCw className="mr-1.5 h-3.5 w-3.5 animate-spin" />
            ) : (
              <Save className="mr-1.5 h-3.5 w-3.5" />
            )}
            Salvar
          </Button>
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
