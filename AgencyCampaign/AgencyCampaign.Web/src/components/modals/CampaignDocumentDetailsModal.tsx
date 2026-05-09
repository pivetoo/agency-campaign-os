import { useEffect, useState } from 'react'
import {
  Badge,
  Button,
  Modal,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  useApi,
} from 'archon-ui'
import { Check, Clock, Download, ExternalLink } from 'lucide-react'
import { campaignDocumentService } from '../../services/campaignDocumentService'
import {
  campaignDocumentEventTypeLabels,
  campaignDocumentSignerRoleLabels,
  campaignDocumentStatusLabels,
  campaignDocumentTypeLabels,
  type CampaignDocument,
  type CampaignDocumentStatusValue,
} from '../../types/campaignDocument'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  documentId: number | null
}

const STATUS_VARIANTS: Record<CampaignDocumentStatusValue, 'default' | 'success' | 'warning' | 'destructive' | 'outline'> = {
  1: 'outline',
  2: 'outline',
  3: 'warning',
  4: 'warning',
  5: 'success',
  6: 'destructive',
  7: 'destructive',
}

export default function CampaignDocumentDetailsModal({ open, onOpenChange, documentId }: Props) {
  const [document, setDocument] = useState<CampaignDocument | null>(null)
  const { execute, loading } = useApi<CampaignDocument | null>({ showErrorMessage: true })

  useEffect(() => {
    if (!open || !documentId) {
      setDocument(null)
      return
    }
    void execute(() => campaignDocumentService.getById(documentId)).then((result) => {
      if (result) setDocument(result)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, documentId])

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '960px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>Detalhes do documento</ModalTitle>
        </ModalHeader>

        {loading && <p className="text-sm text-muted-foreground">Carregando...</p>}

        {document && (
          <div className="space-y-4">
            <div className="rounded-lg border bg-primary/5 p-4">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h3 className="text-base font-semibold">{document.title}</h3>
                  <p className="text-sm text-muted-foreground">
                    {campaignDocumentTypeLabels[document.documentType]}
                    {document.templateName ? ` · template "${document.templateName}"` : ''}
                  </p>
                </div>
                <Badge variant={STATUS_VARIANTS[document.status]}>
                  {campaignDocumentStatusLabels[document.status]}
                </Badge>
              </div>
              {document.provider && (
                <p className="mt-2 text-xs text-muted-foreground">
                  Provider: <span className="font-mono">{document.provider}</span>
                  {document.providerDocumentId ? ` · ${document.providerDocumentId}` : ''}
                </p>
              )}
              {document.signedDocumentUrl && (
                <a
                  href={document.signedDocumentUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="mt-2 inline-flex items-center gap-1 text-sm text-primary hover:underline"
                >
                  <Download className="h-4 w-4" /> Baixar PDF assinado
                </a>
              )}
              {document.documentUrl && !document.signedDocumentUrl && (
                <a
                  href={document.documentUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="mt-2 inline-flex items-center gap-1 text-sm text-primary hover:underline"
                >
                  <ExternalLink className="h-4 w-4" /> Abrir documento original
                </a>
              )}
            </div>

            <Tabs defaultValue="body">
              <TabsList>
                <TabsTrigger value="body">Conteúdo</TabsTrigger>
                <TabsTrigger value="signers">
                  Signatários ({document.signatures.length})
                </TabsTrigger>
                <TabsTrigger value="events">
                  Histórico ({document.events.length})
                </TabsTrigger>
              </TabsList>

              <TabsContent value="body" className="mt-3">
                {document.body ? (
                  <pre className="max-h-[420px] overflow-auto whitespace-pre-wrap rounded-md border bg-background p-4 font-mono text-xs">
                    {document.body}
                  </pre>
                ) : (
                  <p className="text-sm text-muted-foreground">
                    Este documento não foi gerado a partir de um template. Use a URL acima para acessar o conteúdo original.
                  </p>
                )}
              </TabsContent>

              <TabsContent value="signers" className="mt-3">
                {document.signatures.length === 0 ? (
                  <p className="text-sm text-muted-foreground">Nenhum signatário registrado ainda.</p>
                ) : (
                  <div className="space-y-2">
                    {document.signatures.map((signature) => (
                      <div
                        key={signature.id}
                        className="flex items-center justify-between rounded-md border bg-background p-3"
                      >
                        <div>
                          <div className="flex items-center gap-2">
                            <Badge variant="outline">
                              {campaignDocumentSignerRoleLabels[signature.role]}
                            </Badge>
                            <span className="text-sm font-medium">{signature.signerName}</span>
                          </div>
                          <p className="text-xs text-muted-foreground">{signature.signerEmail}</p>
                          {signature.signerDocumentNumber && (
                            <p className="text-xs text-muted-foreground">
                              Documento: {signature.signerDocumentNumber}
                            </p>
                          )}
                        </div>
                        <div className="text-right text-xs">
                          {signature.isSigned ? (
                            <Badge variant="success" className="gap-1">
                              <Check className="h-3 w-3" />
                              Assinou em {new Date(signature.signedAt!).toLocaleString('pt-BR')}
                            </Badge>
                          ) : (
                            <Badge variant="warning" className="gap-1">
                              <Clock className="h-3 w-3" />
                              Aguardando
                            </Badge>
                          )}
                          {signature.ipAddress && (
                            <p className="mt-1 font-mono text-muted-foreground">{signature.ipAddress}</p>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </TabsContent>

              <TabsContent value="events" className="mt-3">
                {document.events.length === 0 ? (
                  <p className="text-sm text-muted-foreground">Sem eventos registrados.</p>
                ) : (
                  <div className="relative space-y-3 border-l-2 border-primary/15 pl-4">
                    {document.events.map((event) => (
                      <div key={event.id} className="relative">
                        <div className="absolute -left-[1.4rem] top-1 h-3 w-3 rounded-full bg-primary/15" />
                        <div className="flex items-baseline justify-between gap-3">
                          <span className="text-sm font-medium">
                            {campaignDocumentEventTypeLabels[event.eventType]}
                          </span>
                          <span className="text-xs text-muted-foreground">
                            {new Date(event.occurredAt).toLocaleString('pt-BR')}
                          </span>
                        </div>
                        {event.description && (
                          <p className="text-xs text-muted-foreground">{event.description}</p>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </TabsContent>
            </Tabs>
          </div>
        )}

        <ModalFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Fechar
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}
