import { useCallback, useEffect, useRef, useState } from 'react'
import Editor from '@monaco-editor/react'
import { Button, useApi, useI18n } from 'archon-ui'
import { Save, RefreshCw, ChevronDown, ChevronUp } from 'lucide-react'
import { agencySettingsService } from '../../../services/agencySettingsService'
import type { ProposalLayout } from '../../../services/agencySettingsService'
import type { AgencySettings } from '../../../types/agencySettings'

const DEBOUNCE_MS = 900

const VARIABLES = [
  {
    group: 'Agência',
    items: [
      { variable: '{{agency.primaryColor}}', description: 'Cor primária (hex)' },
      { variable: '{{agency.logo}}', description: 'Logo (img ou texto)' },
      { variable: '{{agency.name}}', description: 'Nome da agência' },
      { variable: '{{agency.displayName}}', description: 'Nome fantasia ou nome' },
      { variable: '{{agency.emailHtml}}', description: 'E-mail (div ou vazio)' },
      { variable: '{{agency.email}}', description: 'E-mail (texto)' },
      { variable: '{{agency.phone}}', description: 'Telefone' },
      { variable: '{{agency.address}}', description: 'Endereço' },
      { variable: '{{agency.document}}', description: 'CNPJ/documento' },
      { variable: '{{agency.documentSuffix}}', description: '" (CNPJ)" ou vazio' },
      { variable: '{{agency.nameWithDocument}}', description: 'Nome + documento' },
      { variable: '{{agency.contactLine}}', description: 'Email · Fone · Endereço' },
    ],
  },
  {
    group: 'Proposta',
    items: [
      { variable: '{{proposal.name}}', description: 'Título da proposta' },
      { variable: '{{proposal.descriptionHtml}}', description: 'Descrição (p tag ou vazio)' },
      { variable: '{{proposal.description}}', description: 'Descrição (texto)' },
      { variable: '{{proposal.date}}', description: 'Data de emissão' },
      { variable: '{{proposal.client}}', description: 'Nome do cliente' },
      { variable: '{{proposal.clientRow}}', description: 'Linha de tabela do cliente' },
      { variable: '{{proposal.owner}}', description: 'Responsável interno' },
      { variable: '{{proposal.ownerRow}}', description: 'Linha de tabela do responsável' },
    ],
  },
  {
    group: 'Itens e totais',
    items: [
      { variable: '{{proposal.items}}', description: 'Tabela de itens' },
      { variable: '{{proposal.totals}}', description: 'Bloco de total' },
      { variable: '{{proposal.totalFormatted}}', description: 'Valor total formatado' },
      { variable: '{{proposal.validityHtml}}', description: 'Validade (p tag ou vazio)' },
      { variable: '{{proposal.notesHtml}}', description: 'Observações (bloco ou vazio)' },
    ],
  },
]

export default function ProposalTemplate() {
  const { t } = useI18n()
  const [template, setTemplate] = useState<string>('')
  const [previewHtml, setPreviewHtml] = useState<string>('')
  const [layouts, setLayouts] = useState<ProposalLayout[]>([])
  const [variablesOpen, setVariablesOpen] = useState(false)

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

  const handleLayoutLoad = (key: string) => {
    const layout = layouts.find((l) => l.key === key)
    if (layout) {
      setTemplate(layout.template)
    }
  }

  const handleSave = async () => {
    await runSave(() => agencySettingsService.saveProposalTemplate(template))
  }

  return (
    <div className="flex h-full flex-col overflow-hidden">
      <div className="flex shrink-0 items-center justify-between gap-3 border-b bg-background px-4 py-3">
        <div className="flex items-center gap-2">
          <span className="text-sm font-semibold text-foreground">{t('configuration.proposalTemplate.title')}</span>
          <span className="text-xs text-muted-foreground">{t('configuration.proposalTemplate.subtitle')}</span>
        </div>
        <div className="flex items-center gap-2">
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
          <Button size="sm" onClick={handleSave} disabled={saving}>
            {saving ? <RefreshCw className="mr-1.5 h-3.5 w-3.5 animate-spin" /> : <Save className="mr-1.5 h-3.5 w-3.5" />}
            Salvar
          </Button>
        </div>
      </div>

      <div className="shrink-0 border-b">
        <button
          type="button"
          onClick={() => setVariablesOpen((v) => !v)}
          className="flex w-full items-center gap-2 px-4 py-2 text-left text-xs text-muted-foreground hover:bg-muted/40"
        >
          {variablesOpen ? <ChevronUp className="h-3.5 w-3.5" /> : <ChevronDown className="h-3.5 w-3.5" />}
          <span className="font-medium">Variáveis disponíveis</span>
        </button>
        {variablesOpen && (
          <div className="flex flex-wrap gap-x-8 gap-y-3 border-t bg-muted/20 px-4 py-3">
            {VARIABLES.map((group) => (
              <div key={group.group} className="min-w-48">
                <p className="mb-1.5 text-xs font-semibold text-foreground">{group.group}</p>
                <ul className="space-y-0.5">
                  {group.items.map((item) => (
                    <li key={item.variable} className="flex items-baseline gap-2">
                      <code className="shrink-0 rounded bg-primary/10 px-1 py-0.5 text-[10px] font-mono text-primary">
                        {item.variable}
                      </code>
                      <span className="text-[10px] text-muted-foreground">{item.description}</span>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        )}
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
