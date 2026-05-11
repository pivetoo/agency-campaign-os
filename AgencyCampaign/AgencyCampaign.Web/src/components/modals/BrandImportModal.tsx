import { useRef, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, ModalBody, Button } from 'archon-ui'
import { Upload, FileSpreadsheet, X, Download } from 'lucide-react'

interface BrandImportModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

const CSV_TEMPLATE_HEADERS = 'nome,nome_fantasia,documento,contato,email,observacoes'
const CSV_TEMPLATE_EXAMPLE = 'Marca Exemplo,Nome Fantasia,12.345.678/0001-99,João Silva,joao@empresa.com,Observação opcional'

function downloadTemplate() {
  const content = `${CSV_TEMPLATE_HEADERS}\n${CSV_TEMPLATE_EXAMPLE}\n`
  const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'modelo_marcas.csv'
  a.click()
  URL.revokeObjectURL(url)
}

export default function BrandImportModal({ open, onOpenChange }: BrandImportModalProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [dragging, setDragging] = useState(false)

  const handleFile = (f: File) => {
    if (f.name.endsWith('.csv') || f.type === 'text/csv') {
      setFile(f)
    }
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setDragging(false)
    const dropped = e.dataTransfer.files[0]
    if (dropped) handleFile(dropped)
  }

  const handleClose = (val: boolean) => {
    if (!val) setFile(null)
    onOpenChange(val)
  }

  return (
    <Modal open={open} onOpenChange={handleClose}>
      <ModalContent size="md">
        <ModalHeader>
          <ModalTitle>Importar marcas</ModalTitle>
        </ModalHeader>

        <ModalBody>
          <div className="flex flex-col gap-4">
            {/* Drop zone */}
            <div
              onClick={() => inputRef.current?.click()}
              onDragOver={(e) => { e.preventDefault(); setDragging(true) }}
              onDragLeave={() => setDragging(false)}
              onDrop={handleDrop}
              className={`relative flex cursor-pointer flex-col items-center justify-center gap-3 rounded-lg border-2 border-dashed px-6 py-10 text-center transition-colors
                ${dragging ? 'border-primary bg-primary/5' : 'border-border hover:border-primary/50 hover:bg-muted/30'}
                ${file ? 'border-[#1d6f42]/50 bg-[#1d6f42]/5' : ''}`}
            >
              <input
                ref={inputRef}
                type="file"
                accept=".csv,text/csv"
                className="hidden"
                onChange={(e) => { const f = e.target.files?.[0]; if (f) handleFile(f) }}
              />

              {file ? (
                <>
                  <FileSpreadsheet size={32} className="text-[#1d6f42]" />
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-foreground">{file.name}</span>
                    <button
                      type="button"
                      onClick={(e) => { e.stopPropagation(); setFile(null) }}
                      className="rounded p-0.5 text-muted-foreground hover:text-destructive"
                    >
                      <X size={14} />
                    </button>
                  </div>
                  <span className="text-xs text-muted-foreground">
                    {(file.size / 1024).toFixed(1)} KB
                  </span>
                </>
              ) : (
                <>
                  <Upload size={28} className="text-muted-foreground" />
                  <div>
                    <p className="text-sm font-medium text-foreground">
                      Arraste o arquivo ou clique para selecionar
                    </p>
                    <p className="mt-0.5 text-xs text-muted-foreground">Somente arquivos .csv</p>
                  </div>
                </>
              )}
            </div>

            {/* Template download */}
            <div className="flex items-center justify-between rounded-lg border border-border bg-muted/20 px-4 py-3">
              <div className="min-w-0">
                <p className="text-sm font-medium">Modelo do CSV</p>
                <p className="text-xs text-muted-foreground">
                  Baixe o modelo com os campos esperados
                </p>
              </div>
              <button
                type="button"
                onClick={downloadTemplate}
                className="ml-4 inline-flex shrink-0 items-center gap-1.5 rounded-md border border-[#1d6f42]/40 px-3 py-1.5 text-xs font-medium text-[#1d6f42] transition-colors hover:bg-[#1d6f42]/8 hover:border-[#1d6f42]"
              >
                <Download size={13} />
                Baixar modelo
              </button>
            </div>
          </div>
        </ModalBody>

        <ModalFooter>
          <Button variant="outline" onClick={() => handleClose(false)}>
            Cancelar
          </Button>
          <Button disabled={!file}>
            Importar
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}
