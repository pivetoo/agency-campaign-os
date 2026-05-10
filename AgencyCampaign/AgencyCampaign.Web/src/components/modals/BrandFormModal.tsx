import { useEffect, useRef, useState } from 'react'
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, useApi } from 'archon-ui'
import { ImagePlus, Trash2 } from 'lucide-react'
import { brandService, resolveBrandLogoUrl, type CreateBrandRequest, type UpdateBrandRequest } from '../../services/brandService'
import type { Brand } from '../../types/brand'
import { cleanFormPayload } from '../../lib/cleanFormPayload'

interface BrandFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  brand: Brand | null
  onSuccess: () => void
}

const initialFormData: CreateBrandRequest = {
  name: '',
  tradeName: '',
  document: '',
  contactName: '',
  contactEmail: '',
  notes: '',
}

const ACCEPTED_TYPES = ['image/png', 'image/jpeg', 'image/jpg', 'image/webp']
const MAX_BYTES = 2 * 1024 * 1024

export default function BrandFormModal({ open, onOpenChange, brand, onSuccess }: BrandFormModalProps) {
  const isEditing = !!brand
  const [formData, setFormData] = useState<CreateBrandRequest>(initialFormData)
  const [isActive, setIsActive] = useState(true)
  const [pendingLogo, setPendingLogo] = useState<File | null>(null)
  const [logoPreview, setLogoPreview] = useState<string | null>(null)
  const [logoError, setLogoError] = useState<string | null>(null)
  const [shouldRemoveLogo, setShouldRemoveLogo] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    if (brand) {
      setFormData({
        name: brand.name,
        tradeName: brand.tradeName || '',
        document: brand.document || '',
        contactName: brand.contactName || '',
        contactEmail: brand.contactEmail || '',
        notes: brand.notes || '',
      })
      setIsActive(brand.isActive)
    } else {
      setFormData(initialFormData)
      setIsActive(true)
    }

    setPendingLogo(null)
    setLogoPreview(brand?.logoUrl ? resolveBrandLogoUrl(brand.logoUrl) ?? null : null)
    setLogoError(null)
    setShouldRemoveLogo(false)
  }, [brand, open])

  const handleSelectLogo = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    setLogoError(null)

    if (!file) {
      return
    }

    if (!ACCEPTED_TYPES.includes(file.type)) {
      setLogoError('Formato invalido. Use PNG, JPG ou WEBP.')
      return
    }

    if (file.size > MAX_BYTES) {
      setLogoError('Arquivo excede o limite de 2MB.')
      return
    }

    setPendingLogo(file)
    setShouldRemoveLogo(false)
    setLogoPreview(URL.createObjectURL(file))
  }

  const handleRemoveLogo = () => {
    setPendingLogo(null)
    setLogoPreview(null)
    setLogoError(null)
    setShouldRemoveLogo(!!brand?.logoUrl)
  }

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    const cleaned = cleanFormPayload(formData)

    const result = await execute(async () => {
      const saved = isEditing
        ? await brandService.update(brand.id, {
            id: brand.id,
            ...cleaned,
            isActive,
          } satisfies UpdateBrandRequest)
        : await brandService.create(cleaned)

      const savedBrand = saved.data
      if (!savedBrand) {
        return saved
      }

      if (pendingLogo) {
        return await brandService.uploadLogo(savedBrand.id, pendingLogo)
      }

      if (shouldRemoveLogo && isEditing) {
        return await brandService.removeLogo(savedBrand.id)
      }

      return saved
    })

    if (result !== null) {
      onSuccess()
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '960px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar marca' : 'Nova marca'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div style={{ display: 'grid', gridTemplateColumns: '160px 1fr', gap: '1.5rem', alignItems: 'start' }}>
            <div className="space-y-2">
              <label className="text-sm font-medium">Logo</label>
              <div
                className="relative flex items-center justify-center overflow-hidden rounded-lg border border-dashed bg-muted/30"
                style={{ width: 160, height: 160 }}
              >
                {logoPreview ? (
                  <img src={logoPreview} alt="Logo da marca" className="h-full w-full object-contain" />
                ) : (
                  <div className="flex flex-col items-center gap-1 text-xs text-muted-foreground">
                    <ImagePlus className="h-6 w-6" />
                    <span>Sem logo</span>
                  </div>
                )}
              </div>
              <input
                ref={fileInputRef}
                type="file"
                accept={ACCEPTED_TYPES.join(',')}
                onChange={handleSelectLogo}
                className="hidden"
              />
              <div className="flex flex-col gap-1">
                <Button type="button" variant="outline" size="sm" onClick={() => fileInputRef.current?.click()}>
                  {logoPreview ? 'Trocar logo' : 'Enviar logo'}
                </Button>
                {logoPreview && (
                  <Button type="button" variant="ghost" size="sm" onClick={handleRemoveLogo}>
                    <Trash2 className="mr-1 h-3 w-3" /> Remover
                  </Button>
                )}
              </div>
              <p className="text-xs text-muted-foreground">PNG, JPG ou WEBP. Maximo 2MB.</p>
              {logoError && <p className="text-xs text-destructive">{logoError}</p>}
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
              <div className="space-y-2">
                <label className="text-sm font-medium">Nome</label>
                <Input value={formData.name} onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))} required />
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium">Nome fantasia</label>
                <Input value={formData.tradeName || ''} onChange={(e) => setFormData((prev) => ({ ...prev, tradeName: e.target.value }))} />
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium">Documento</label>
                <Input value={formData.document || ''} onChange={(e) => setFormData((prev) => ({ ...prev, document: e.target.value }))} />
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium">Contato</label>
                <Input value={formData.contactName || ''} onChange={(e) => setFormData((prev) => ({ ...prev, contactName: e.target.value }))} />
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium">E-mail</label>
                <Input type="email" value={formData.contactEmail || ''} onChange={(e) => setFormData((prev) => ({ ...prev, contactEmail: e.target.value }))} />
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium">Observacoes</label>
                <Input value={formData.notes || ''} onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))} />
              </div>
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '1rem' }}>
            <div>
              {isEditing && (
                <div className="flex items-center gap-2">
                  <Checkbox checked={isActive} onCheckedChange={(checked) => setIsActive(!!checked)} />
                  <span className="text-sm">Ativa</span>
                </div>
              )}
            </div>

            <ModalFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
              <Button type="submit" disabled={loading}>{loading ? 'Salvando...' : 'Salvar'}</Button>
            </ModalFooter>
          </div>
        </form>
      </ModalContent>
    </Modal>
  )
}
