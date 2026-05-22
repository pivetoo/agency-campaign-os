export function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export function formatCurrencyShort(value: number): string {
  if (value >= 1_000_000) return `R$ ${(value / 1_000_000).toFixed(1)}M`
  if (value >= 1_000) return `R$ ${(value / 1_000).toFixed(0)}k`
  return `R$ ${value.toFixed(0)}`
}

export function formatNumber(value?: number | null, fallback = '-'): string {
  if (value == null) return fallback
  return value.toLocaleString('pt-BR')
}

export function formatPercent(value?: number | null, fallback = '-', fractionDigits = 2): string {
  if (value == null) return fallback
  return `${value.toFixed(fractionDigits)}%`
}

export function formatDate(value?: string | null, fallback = '-'): string {
  if (!value) return fallback
  return new Date(value).toLocaleDateString('pt-BR')
}

export function formatDateTime(value?: string | null, fallback = '-'): string {
  if (!value) return fallback
  return new Date(value).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function dateInputToIso(value: string): string {
  if (!value) return ''
  return `${value}T12:00:00.000Z`
}

export function todayDateInput(): string {
  const d = new Date()
  const yyyy = d.getFullYear()
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${yyyy}-${mm}-${dd}`
}

export function isoToDateInput(value?: string | null): string {
  if (!value) return ''
  return value.split('T')[0] ?? ''
}
