import { httpClient } from 'archon-ui'

// Baixa um CSV de relatorio com guarda contra erro-como-blob: um 403/500 com responseType blob vem como
// JSON; nesse caso lemos o corpo como texto e lancamos, em vez de baixar um .csv contendo o erro.
export async function downloadCsvReport(url: string, fileName: string): Promise<void> {
  const response = await httpClient.get<Blob>(url, { responseType: 'blob' })
  const blob = response.data as Blob | undefined
  if (!blob) return
  if (!blob.type || !blob.type.includes('csv')) {
    const text = await blob.text().catch(() => '')
    throw new Error(text || 'Falha ao exportar o relatorio.')
  }
  const objectUrl = window.URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = objectUrl
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  window.URL.revokeObjectURL(objectUrl)
}
