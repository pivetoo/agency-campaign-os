import FinancialEntriesPage from './EntriesPage'

export default function FinancialPayables() {
  return (
    <FinancialEntriesPage
      type={2}
      title="Contas a pagar"
      subtitle="Repasses para creators, fornecedores, impostos e despesas operacionais"
    />
  )
}
