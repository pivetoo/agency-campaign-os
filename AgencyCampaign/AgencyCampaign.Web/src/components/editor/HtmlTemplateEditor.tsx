import { useCallback, useEffect, useRef, useState } from 'react'
import Editor, { type OnMount } from '@monaco-editor/react'
import { Button, useApi } from 'archon-ui'
import { Braces } from 'lucide-react'

export interface TemplateVariable {
  key: string
  label: string
  description?: string
}

export interface TemplateVariableGroup {
  group: string
  items: TemplateVariable[]
}

export interface HtmlTemplateEditorProps {
  value: string
  onChange: (next: string) => void
  variables: TemplateVariableGroup[]
  fetchPreview: (template: string) => Promise<string | null | undefined>
  toolbarLeftSlot?: React.ReactNode
  toolbarRightSlot?: React.ReactNode
  height?: string
  previewDebounceMs?: number
  insertVariableLabel?: string
  previewEmptyLabel?: string
  previewSubtitle?: string
}

export default function HtmlTemplateEditor({
  value,
  onChange,
  variables,
  fetchPreview,
  toolbarLeftSlot,
  toolbarRightSlot,
  height = 'calc(100vh - 100px)',
  previewDebounceMs = 900,
  insertVariableLabel = 'Inserir variável',
  previewEmptyLabel = 'A pré-visualização aparece aqui após editar o template.',
  previewSubtitle = 'Pré-visualização — dados de exemplo',
}: HtmlTemplateEditorProps) {
  const [previewHtml, setPreviewHtml] = useState<string>('')
  const [pickerOpen, setPickerOpen] = useState(false)

  const editorRef = useRef<Parameters<OnMount>[0] | null>(null)
  const pickerRef = useRef<HTMLDivElement | null>(null)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const { execute: runPreview } = useApi<string | null>({ showErrorMessage: false })

  const requestPreview = useCallback(async (template: string) => {
    const result = await runPreview(() => fetchPreview(template).then((html) => html ?? ''))
    if (typeof result === 'string') {
      setPreviewHtml(result)
    }
  }, [fetchPreview, runPreview])

  useEffect(() => {
    if (!value) {
      setPreviewHtml('')
      return
    }
    if (debounceRef.current) {
      clearTimeout(debounceRef.current)
    }
    debounceRef.current = setTimeout(() => {
      void requestPreview(value)
    }, previewDebounceMs)
    return () => {
      if (debounceRef.current) {
        clearTimeout(debounceRef.current)
      }
    }
  }, [value, previewDebounceMs, requestPreview])

  useEffect(() => {
    if (!pickerOpen) return
    const handleClickOutside = (event: MouseEvent) => {
      if (pickerRef.current && !pickerRef.current.contains(event.target as Node)) {
        setPickerOpen(false)
      }
    }
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setPickerOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    document.addEventListener('keydown', handleEscape)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
      document.removeEventListener('keydown', handleEscape)
    }
  }, [pickerOpen])

  const handleEditorMount: OnMount = (editorInstance) => {
    editorRef.current = editorInstance
  }

  const insertVariable = (key: string) => {
    const token = `{{ ${key} }}`
    setPickerOpen(false)
    const ed = editorRef.current
    if (!ed) {
      onChange(value + token)
      return
    }
    const selection = ed.getSelection()
    if (!selection) {
      onChange(value + token)
      return
    }
    ed.executeEdits('', [{ range: selection, text: token, forceMoveMarkers: true }])
    ed.focus()
  }

  return (
    <div className="flex flex-col overflow-hidden" style={{ height }}>
      <div className="flex shrink-0 items-center justify-between gap-3 border-b bg-background px-4 py-2.5">
        <div className="flex items-center gap-1.5">{toolbarLeftSlot}</div>

        <div className="flex items-center gap-2">
          <div ref={pickerRef} className="relative">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setPickerOpen((open) => !open)}
            >
              <Braces className="mr-1.5 h-3.5 w-3.5" />
              {insertVariableLabel}
            </Button>

            {pickerOpen ? (
              variables.length === 0 ? (
                <div className="absolute right-0 top-full z-50 mt-1 w-72 rounded-md border bg-popover p-3 text-xs text-muted-foreground shadow-lg">
                  Nenhuma variável disponível.
                </div>
              ) : (
                <div className="absolute right-0 top-full z-50 mt-1 max-h-80 w-96 overflow-y-auto rounded-md border bg-popover shadow-lg">
                  {variables.map((group) => (
                    <div key={group.group}>
                      <div className="sticky top-0 bg-muted/80 px-3 py-1.5 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground backdrop-blur-sm">
                        {group.group}
                      </div>
                      {group.items.map((variable) => (
                        <button
                          key={variable.key}
                          type="button"
                          onClick={() => insertVariable(variable.key)}
                          className="flex w-full flex-col items-start gap-0.5 px-3 py-2 text-left hover:bg-muted/60"
                        >
                          <code className="text-xs font-medium text-primary">{`{{ ${variable.key} }}`}</code>
                          {variable.label ? (
                            <span className="text-[11px] font-medium text-foreground">{variable.label}</span>
                          ) : null}
                          {variable.description ? (
                            <span className="text-[11px] text-muted-foreground">{variable.description}</span>
                          ) : null}
                        </button>
                      ))}
                    </div>
                  ))}
                </div>
              )
            ) : null}
          </div>

          {toolbarRightSlot}
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
              value={value}
              onChange={(next) => onChange(next ?? '')}
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
            {previewSubtitle}
          </div>
          <div className="min-h-0 flex-1">
            {previewHtml ? (
              <iframe
                srcDoc={previewHtml}
                className="h-full w-full border-0"
                title="Pré-visualização"
                sandbox="allow-same-origin"
              />
            ) : (
              <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
                {previewEmptyLabel}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
