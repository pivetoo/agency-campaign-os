import FinancialEntriesPage from './EntriesPage'

export default function FinancialReceivables() {
  return (
    <FinancialEntriesPage
      type={1}
      title="Contas a receber"
      subtitle="Lançamentos de entrada da agência: marca, bônus e ajustes"
    />
  )
}
