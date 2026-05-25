import { Input } from 'archon-ui'
import { formatCurrency } from '../../lib/format'

interface CurrencyInputProps {
  value: number
  onChange: (value: number) => void
  id?: string
  placeholder?: string
  disabled?: boolean
}

export default function CurrencyInput({ value, onChange, id, placeholder, disabled }: CurrencyInputProps) {
  const display = value > 0 ? formatCurrency(value) : ''

  const handleChange = (raw: string) => {
    const digits = raw.replace(/\D/g, '')
    onChange(digits ? Number(digits) / 100 : 0)
  }

  return (
    <Input
      id={id}
      inputMode="numeric"
      placeholder={placeholder ?? 'R$ 0,00'}
      value={display}
      onChange={(e) => handleChange(e.target.value)}
      disabled={disabled}
    />
  )
}
