# Relatórios — Fatia 1a (Shell + Financeiro com CSV) — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Entregar o módulo Relatórios do Kanvas com a sidebar separada em Comercial/Produção/Financeiro (subGroups), exibindo os 6 relatórios financeiros (4 novos + 2 portados) com export CSV.

**Architecture:** Telas dedicadas sobre um toolkit compartilhado (`ReportLayout` + filtros). Um catálogo (`catalog.tsx`) é a fonte única que alimenta a sidebar (subGroups) e a landing. O backend dos 6 relatórios financeiros já existe (controllers + DTOs + CSV); este plano é quase todo frontend, com um único acréscimo de backend: o endpoint de export CSV da Projeção de Fluxo (que ainda não existe). O export PDF é o Plano B (fatia 1a, separado).

**Tech Stack:** React 18 + TypeScript + Vite, archon-ui (PageLayout, Card, DataTable, SearchableSelect, Button, Badge, useApi, usePermissions, httpClient), Nivo (`@nivo/line`). Backend .NET 10 (Archon `ApiControllerBase`, EF), testes backend xUnit + EF InMemory, E2E Playwright.

**Pré-requisitos de contexto (ler antes de começar):**
- Spec: `AgencyCampaign/docs/superpowers/specs/2026-06-05-relatorios-kanvas-design.md`
- Padrão de tela de relatório existente: `AgencyCampaign.Web/src/modules/Main/Financial/CashFlow.tsx` e `Aging.tsx`
- Service existente: `AgencyCampaign.Web/src/services/financialReportService.ts`
- Sidebar/rotas: `AgencyCampaign.Web/src/layouts/AgencyCampaignLayout.tsx`, `AgencyCampaign.Web/src/routes/index.tsx`
- Backend financeiro: `AgencyCampaign.Api/Controllers/FinancialReportsController.cs`, `AgencyCampaign.Infrastructure/Services/FinancialReportService.cs`, `FinancialReportExportService.cs`, `AgencyCampaign.Application/Models/Financial/FinancialModels.cs`, `AgencyCampaign.Application/Export/CsvWriter.cs`

**Convenções obrigatórias (do repositório):**
- Imports em UMA linha só (`import { A, B, C } from 'x'`), independente da quantidade.
- Formatação via `src/lib/format.ts` (`formatCurrency`, `formatDate`, etc.) — nunca redefinir local.
- Todo dropdown dinâmico usa `SearchableSelect`.
- C#: chaves `{ }` em todo if/for/foreach; sem `JsonPropertyName` (web defaults camelCase); serviços novos terminam em `Service` e ficam em namespace `...Services` (auto-DI do Archon).
- Commits em pt-BR com prefixo `feat`/`fix`/`refactor`, sem `Co-Authored-By`, sem emoji.

**Fatos load-bearing (já verificados no código):**
- Permissão efetiva do Archon = `controllerName.actionName` em camelCase. Para os relatórios financeiros: `financialReports.getCashFlow`, `financialReports.getAging`, `financialReports.getCashFlowProjection`, `financialReports.getAccrualResult`, `financialReports.getCampaignProfitability`, `financialReports.getTaxWithholding`. São essas as strings de `requires` no catálogo.
- O `ModulePanel` do archon-ui renderiza `subGroups` para qualquer módulo, inclusive `group: 'op'` (o `group` só controla o separador op/sys na barra de ícones). Logo, Relatórios pode ser `group: 'op'` com `subGroups`.
- Tipos do archon-ui: `NavModule { key; label; icon; group: 'op'|'sys'; routes?: NavRoute[]; subGroups?: NavSubGroup[] }`, `NavSubGroup { label; routes: NavRoute[] }`, `NavRoute { key; label; path; icon; badge? }`.
- `DataTableColumn { key; title; dataIndex?; render?(value, record, index); width?; hiddenBelow?; primary?; cardTag? }`; `DataTable` props incluem `columns`, `data`, `rowKey`, `loading?`, `emptyText?`.
- Backend: endpoints normais retornam `Http200(result)`; export retorna `SendCsv(byte[], "nome.csv")`. Ambos helpers de `ApiControllerBase`.

---

## Part A — Catálogo e navegação

### Task 1: Criar o catálogo de relatórios

**Files:**
- Create: `AgencyCampaign.Web/src/modules/Reports/catalog.tsx`

- [ ] **Step 1: Criar o catálogo**

O catálogo é a fonte única. Na fatia 1a só a área `financeiro` tem itens; `comercial` e `producao` ficam vazias (serão preenchidas nos planos 1b/1c). Os `path` apontam para as novas rotas; `requires` usa a permissão derivada do Archon.

```tsx
import type { ReactNode } from 'react'
import { TrendingUp, Hourglass, LineChart, Scale, PiggyBank, Receipt } from 'lucide-react'

export type ReportArea = 'comercial' | 'producao' | 'financeiro'

export interface ReportCatalogEntry {
  id: string
  area: ReportArea
  title: string
  description: string
  icon: ReactNode
  path: string
  requires?: string[]
}

export const reportCatalog: ReportCatalogEntry[] = [
  {
    id: 'financeiro-fluxo-caixa',
    area: 'financeiro',
    title: 'Fluxo de Caixa',
    description: 'Entradas e saídas previstas e realizadas por período.',
    icon: <TrendingUp size={20} />,
    path: '/relatorios/financeiro/fluxo-caixa',
    requires: ['financialReports.getCashFlow'],
  },
  {
    id: 'financeiro-aging',
    area: 'financeiro',
    title: 'Aging',
    description: 'Títulos a vencer e vencidos por faixa de atraso.',
    icon: <Hourglass size={20} />,
    path: '/relatorios/financeiro/aging',
    requires: ['financialReports.getAging'],
  },
  {
    id: 'financeiro-projecao',
    area: 'financeiro',
    title: 'Projeção de Fluxo',
    description: 'Saldo projetado semana a semana (horizonte de 12 semanas).',
    icon: <LineChart size={20} />,
    path: '/relatorios/financeiro/projecao',
    requires: ['financialReports.getCashFlowProjection'],
  },
  {
    id: 'financeiro-resultado',
    area: 'financeiro',
    title: 'Resultado (Competência)',
    description: 'Receita menos despesa no regime de competência (DRE).',
    icon: <Scale size={20} />,
    path: '/relatorios/financeiro/resultado',
    requires: ['financialReports.getAccrualResult'],
  },
  {
    id: 'financeiro-rentabilidade',
    area: 'financeiro',
    title: 'Rentabilidade por Campanha',
    description: 'Receita, custos e margem consolidados por campanha.',
    icon: <PiggyBank size={20} />,
    path: '/relatorios/financeiro/rentabilidade',
    requires: ['financialReports.getCampaignProfitability'],
  },
  {
    id: 'financeiro-retencoes',
    area: 'financeiro',
    title: 'Retenções Fiscais',
    description: 'Imposto retido na fonte por creator no período.',
    icon: <Receipt size={20} />,
    path: '/relatorios/financeiro/retencoes',
    requires: ['financialReports.getTaxWithholding'],
  },
]

export const reportAreaLabels: Record<ReportArea, string> = {
  comercial: 'Comercial',
  producao: 'Produção',
  financeiro: 'Financeiro',
}

export const reportAreaOrder: ReportArea[] = ['comercial', 'producao', 'financeiro']
```

- [ ] **Step 2: Verificar compilação de tipos**

Run: `cd AgencyCampaign.Web && npx tsc --noEmit`
Expected: PASS (sem erros novos). `lucide-react` já é dependência (usado no layout).

- [ ] **Step 3: Commit**

```bash
git add AgencyCampaign.Web/src/modules/Reports/catalog.tsx
git commit -m "feat: adiciona catalogo de relatorios do Kanvas"
```

---

### Task 2: Registrar rotas dos relatórios e redirects

**Files:**
- Modify: `AgencyCampaign.Web/src/routes/index.tsx`

- [ ] **Step 1: Importar as novas telas**

Adicionar imports (em UMA linha cada) junto aos demais imports de módulos, logo após a linha `import Reports from '../modules/Reports'` (linha 57):

```tsx
import ReportProjection from '../modules/Reports/Financial/Projection'
import ReportAccrualResult from '../modules/Reports/Financial/AccrualResult'
import ReportCampaignProfitability from '../modules/Reports/Financial/CampaignProfitability'
import ReportTaxWithholding from '../modules/Reports/Financial/TaxWithholding'
```

(As telas Fluxo de Caixa e Aging reaproveitam os componentes já existentes `FinancialCashFlow` e `FinancialAging`, já importados nas linhas 37-38.)

- [ ] **Step 2: Trocar a rota antiga `relatorios` pelas rotas do módulo + redirects**

Substituir a linha 152 (`<Route path="relatorios" element={<Reports />} />`) por:

```tsx
          <Route path="relatorios" element={<Reports />} />
          <Route path="relatorios/financeiro/fluxo-caixa" element={<FinancialCashFlow />} />
          <Route path="relatorios/financeiro/aging" element={<FinancialAging />} />
          <Route path="relatorios/financeiro/projecao" element={<ReportProjection />} />
          <Route path="relatorios/financeiro/resultado" element={<ReportAccrualResult />} />
          <Route path="relatorios/financeiro/rentabilidade" element={<ReportCampaignProfitability />} />
          <Route path="relatorios/financeiro/retencoes" element={<ReportTaxWithholding />} />
```

Substituir as rotas antigas de fluxo de caixa e aging (linhas 128-129) por redirects que preservam links/bookmarks/E2E:

```tsx
          <Route path="financeiro/fluxo-caixa" element={<Navigate to="/relatorios/financeiro/fluxo-caixa" replace />} />
          <Route path="financeiro/aging" element={<Navigate to="/relatorios/financeiro/aging" replace />} />
```

(`Navigate` já está importado na linha 1.)

- [ ] **Step 3: Verificar compilação**

Run: `cd AgencyCampaign.Web && npx tsc --noEmit`
Expected: FAIL — os 4 módulos novos (`Reports/Financial/*`) ainda não existem. Isto confirma que as rotas estão referenciando os arquivos certos. Seguir para a Part D criará esses arquivos; o `tsc` volta a passar ao fim da Task 12. NÃO commitar ainda — esta task fica como parte do mesmo commit da Task 3.

> Nota para o executor: se preferir manter a árvore compilável a cada passo, faça a Task 2 e a Task 3 e depois as Tasks 9-12 antes de rodar `tsc` final. As Tasks 2-3 e 9-12 formam um bloco coeso de "rotas + telas".

---

### Task 3: Converter Relatórios em módulo com subGroups na sidebar

**Files:**
- Modify: `AgencyCampaign.Web/src/layouts/AgencyCampaignLayout.tsx`

- [ ] **Step 1: Importar o catálogo**

Adicionar (em uma linha) após o import de `logoAgencyCampaign` (linha 16):

```tsx
import { reportCatalog, reportAreaOrder, reportAreaLabels, type ReportArea } from '../modules/Reports/catalog'
```

- [ ] **Step 2: Remover Fluxo de Caixa e Aging do grupo Financeiro (op)**

No `opModuleDefs`, no grupo `financas`, remover estas duas linhas (128-129), pois passam a viver em Relatórios:

```tsx
      { key: 'financeiro-fluxo-caixa', label: t('nav.item.cashFlow'), path: '/financeiro/fluxo-caixa', icon: <TrendingUp size={20} />, requires: ['financialReports.getCashFlow'] },
      { key: 'financeiro-aging', label: t('nav.item.aging'), path: '/financeiro/aging', icon: <Hourglass size={20} />, requires: ['financialReports.getAging'] },
```

Após a remoção, `TrendingUp` e `Hourglass` ficam sem uso no layout (só eram usados nessas duas linhas). Remover ambos da linha 15 de import de `lucide-react` para evitar erro de import não usado (lint/tsc com `noUnusedLocals`). Conferir com:

Run: `cd AgencyCampaign.Web && grep -n "TrendingUp\|Hourglass" src/layouts/AgencyCampaignLayout.tsx`
Expected: nenhuma ocorrência além da própria linha de import — então removê-los do import.

- [ ] **Step 3: Construir o módulo Relatórios a partir do catálogo (subGroups)**

Substituir o bloco `relatoriosRoutes` + `relatoriosModule` (linhas 176-186) por:

```tsx
  const reportNavItems = (area: ReportArea): NavItem[] =>
    reportCatalog
      .filter((entry) => entry.area === area)
      .map((entry) => ({ key: entry.id, label: entry.title, path: entry.path, icon: entry.icon, requires: entry.requires }))

  const relatoriosSubGroups = reportAreaOrder
    .map((area) => ({ label: reportAreaLabels[area], routes: toNavRoutes(reportNavItems(area)) }))
    .filter((group) => group.routes.length > 0)

  const relatoriosModule = relatoriosSubGroups.length > 0
    ? [{ key: 'relatorios', label: 'Relatórios', icon: <FileBarChart2 size={20} />, group: 'op' as const, subGroups: relatoriosSubGroups }]
    : []
```

(O `moduleNav` na linha 194 já faz `[...opModules, ...relatoriosModule, ...sysModules]` — nada muda ali. A posição de Relatórios continua logo após Financeiro, antes do separador.)

- [ ] **Step 4: Adicionar breadcrumbs das novas rotas**

No `routeMap` do `breadcrumbs` (após a linha 229, `'/relatorios': 'Relatórios',`), adicionar:

```tsx
      '/relatorios/financeiro/fluxo-caixa': 'Fluxo de Caixa',
      '/relatorios/financeiro/aging': 'Aging',
      '/relatorios/financeiro/projecao': 'Projeção de Fluxo',
      '/relatorios/financeiro/resultado': 'Resultado (Competência)',
      '/relatorios/financeiro/rentabilidade': 'Rentabilidade por Campanha',
      '/relatorios/financeiro/retencoes': 'Retenções Fiscais',
```

- [ ] **Step 5: Verificar compilação dos tipos do layout**

Run: `cd AgencyCampaign.Web && npx tsc --noEmit 2>&1 | grep -i layout`
Expected: nenhuma linha sobre `AgencyCampaignLayout.tsx` (o erro restante é só dos módulos `Reports/Financial/*` ainda inexistentes, que serão criados na Part D). Se aparecer erro no layout (ex.: `subGroups` incompatível), revisar a forma de `relatoriosSubGroups` contra o tipo `NavModule`/`NavSubGroup`.

> Commit deste bloco (Tasks 2-3) acontece junto com as telas, ao fim da Task 13, quando `tsc` passa inteiro.

---

## Part B — Toolkit de relatório

### Task 4: Criar o componente `ReportLayout`

**Files:**
- Create: `AgencyCampaign.Web/src/modules/Reports/_shared/ReportLayout.tsx`

- [ ] **Step 1: Implementar o ReportLayout**

Generaliza o padrão de `CashFlow.tsx`/`Aging.tsx`: `PageLayout` + um `Card` com a barra de filtros e os botões de export, e o conteúdo (gráfico/tabela) como `children`. O botão PDF só aparece quando `onExportPdf` é passado (o Plano B liga isso; na fatia 1a passamos só `onExportCsv`).

```tsx
import type { ReactNode } from 'react'
import { useState } from 'react'
import { PageLayout, Card, CardContent, Button } from 'archon-ui'
import { Download, FileText } from 'lucide-react'

interface ReportLayoutProps {
  title: string
  subtitle?: string
  filters?: ReactNode
  onRefresh?: () => void
  onExportCsv?: () => void | Promise<void>
  onExportPdf?: () => void | Promise<void>
  children: ReactNode
}

export default function ReportLayout({ title, subtitle, filters, onRefresh, onExportCsv, onExportPdf, children }: ReportLayoutProps) {
  const [exporting, setExporting] = useState<'csv' | 'pdf' | null>(null)

  const runExport = async (kind: 'csv' | 'pdf', fn?: () => void | Promise<void>) => {
    if (!fn) return
    setExporting(kind)
    try {
      await fn()
    } finally {
      setExporting(null)
    }
  }

  return (
    <PageLayout title={title} subtitle={subtitle} onRefresh={onRefresh} showDefaultActions={false}>
      <Card>
        <CardContent className="pt-4 space-y-4">
          <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
            <div className="flex flex-1 flex-wrap items-end gap-3">{filters}</div>
            <div className="flex shrink-0 items-center gap-2">
              {onExportCsv && (
                <Button variant="outline" size="sm" disabled={exporting !== null} onClick={() => void runExport('csv', onExportCsv)}>
                  <Download className="mr-1.5 h-4 w-4" />CSV
                </Button>
              )}
              {onExportPdf && (
                <Button variant="outline" size="sm" disabled={exporting !== null} onClick={() => void runExport('pdf', onExportPdf)}>
                  <FileText className="mr-1.5 h-4 w-4" />PDF
                </Button>
              )}
            </div>
          </div>
          {children}
        </CardContent>
      </Card>
    </PageLayout>
  )
}
```

- [ ] **Step 2: Verificar compilação**

Run: `cd AgencyCampaign.Web && npx tsc --noEmit 2>&1 | grep -i ReportLayout`
Expected: nenhuma linha de erro sobre `ReportLayout.tsx`.

> Nota: se `Button` não aceitar `variant="outline"` (variantes do archon-ui podem diferir), trocar para `variant="secondary"`. Confirmar variantes em `frameworks/archon-ui/src/components/ui/button.tsx` se houver erro de tipo.

- [ ] **Step 3: Commit**

```bash
git add AgencyCampaign.Web/src/modules/Reports/_shared/ReportLayout.tsx
git commit -m "feat: adiciona ReportLayout compartilhado dos relatorios"
```

---

### Task 5: Criar filtro de período compartilhado

**Files:**
- Create: `AgencyCampaign.Web/src/modules/Reports/_shared/ReportFilters.tsx`

> Escopo enxuto (YAGNI): na fatia 1a só o filtro de período é reaproveitado (telas Resultado e Retenções). O Fluxo de Caixa portado mantém sua granularidade inline e a Projeção usa "semanas"; um filtro de granularidade compartilhado entra quando uma tela rebuilt precisar dele (Plano B).

- [ ] **Step 1: Implementar o filtro de período**

```tsx
import { Input } from 'archon-ui'

interface PeriodFilterProps {
  from: string
  to: string
  onChange: (range: { from: string; to: string }) => void
}

export function ReportPeriodFilter({ from, to, onChange }: PeriodFilterProps) {
  return (
    <>
      <div className="space-y-1">
        <label className="text-xs text-muted-foreground">De</label>
        <Input type="date" value={from} onChange={(e) => onChange({ from: e.target.value, to })} />
      </div>
      <div className="space-y-1">
        <label className="text-xs text-muted-foreground">Até</label>
        <Input type="date" value={to} onChange={(e) => onChange({ from, to: e.target.value })} />
      </div>
    </>
  )
}
```

- [ ] **Step 2: Verificar compilação**

Run: `cd AgencyCampaign.Web && npx tsc --noEmit 2>&1 | grep -i ReportFilters`
Expected: nenhuma linha de erro sobre `ReportFilters.tsx`.

- [ ] **Step 3: Commit**

```bash
git add AgencyCampaign.Web/src/modules/Reports/_shared/ReportFilters.tsx
git commit -m "feat: adiciona filtro de periodo compartilhado dos relatorios"
```

---

## Part C — Service, tipos e CSV da Projeção (backend)

### Task 6: Adicionar tipos e getters no financialReportService

**Files:**
- Modify: `AgencyCampaign.Web/src/services/financialReportService.ts`

- [ ] **Step 1: Adicionar as interfaces dos 3 relatórios sem tipo no frontend**

Inserir após a interface `CashFlowProjection` (linha 54), antes de `const BASE_URL`:

```ts
export interface AccrualResult {
  from: string
  to: string
  revenue: number
  expense: number
  result: number
}

export interface CampaignProfitabilityLine {
  campaignId: number
  campaignName?: string | null
  revenue: number
  creatorCost: number
  otherCost: number
  margin: number
  marginPercent: number
}

export interface CampaignProfitabilityReport {
  generatedAt: string
  lines: CampaignProfitabilityLine[]
  totalRevenue: number
  totalCreatorCost: number
  totalOtherCost: number
  totalMargin: number
}

export interface TaxWithholdingLine {
  creatorId: number
  creatorName?: string | null
  document?: string | null
  taxRegime?: number | null
  grossAmount: number
  taxWithheld: number
  netAmount: number
  paymentCount: number
}

export interface TaxWithholdingReport {
  generatedAt: string
  from: string
  to: string
  lines: TaxWithholdingLine[]
  totalGross: number
  totalWithheld: number
  totalNet: number
}
```

- [ ] **Step 2: Adicionar os getters e o export CSV da projeção no objeto `financialReportService`**

Adicionar dentro do objeto `financialReportService` (após `getCashFlowProjection`, linha 93):

```ts
  async getAccrualResult(from: string, to: string): Promise<AccrualResult | null> {
    const params = new URLSearchParams({ from, to })
    const response = await httpClient.get<AccrualResult>(`${BASE_URL}/accrual-result?${params.toString()}`)
    return response.data ?? null
  },

  async getCampaignProfitability(): Promise<CampaignProfitabilityReport | null> {
    const response = await httpClient.get<CampaignProfitabilityReport>(`${BASE_URL}/campaign-profitability`)
    return response.data ?? null
  },

  async getTaxWithholding(from: string, to: string): Promise<TaxWithholdingReport | null> {
    const params = new URLSearchParams({ from, to })
    const response = await httpClient.get<TaxWithholdingReport>(`${BASE_URL}/tax-withholding?${params.toString()}`)
    return response.data ?? null
  },

  exportCashFlowProjection(weeks = 12): Promise<void> {
    return downloadCsvReport(`${BASE_URL}/cashflow-projection/export?weeks=${weeks}`, 'projecao-fluxo-caixa.csv')
  },
```

- [ ] **Step 3: Verificar compilação**

Run: `cd AgencyCampaign.Web && npx tsc --noEmit 2>&1 | grep -i financialReportService`
Expected: nenhuma linha de erro sobre `financialReportService.ts`.

- [ ] **Step 4: Commit**

```bash
git add AgencyCampaign.Web/src/services/financialReportService.ts
git commit -m "feat: adiciona getters de resultado, rentabilidade e retencoes no service de relatorios"
```

---

### Task 7: Adicionar export CSV da Projeção de Fluxo (backend, TDD)

O endpoint `cashflow-projection` existe (GET), mas não tem `/export` CSV. Adicionar seguindo o padrão dos demais exports.

**Files:**
- Modify: `AgencyCampaign.Application/Services/IFinancialReportExportService.cs`
- Modify: `AgencyCampaign.Infrastructure/Services/FinancialReportExportService.cs`
- Modify: `AgencyCampaign.Api/Controllers/FinancialReportsController.cs`
- Test: `AgencyCampaign.Tests/.../FinancialReportExportServiceTests.cs` (localizar o arquivo de teste existente desse serviço)

- [ ] **Step 1: Escrever o teste que falha**

Localizar o arquivo de testes do `FinancialReportExportService` (procurar por `FinancialReportExportServiceTests`). Adicionar um teste que verifica que o CSV da projeção tem cabeçalho e uma linha por semana. Modelar pelos testes existentes do mesmo arquivo (mesma forma de montar o `IFinancialReportService`/InMemory já usada lá). Esqueleto do caso:

```csharp
[Fact]
public async Task ExportCashFlowProjection_EmitsHeaderAndWeekRows()
{
    // Arrange: usar o mesmo setup de DbContext InMemory + dados dos demais testes deste arquivo.
    // Garantir ao menos 1 conta ativa e 1 lançamento pendente futuro para gerar semanas.
    var service = CreateExportService(); // helper já existente no arquivo de teste

    // Act
    byte[] bytes = await service.ExportCashFlowProjection(weeks: 4, cancellationToken: default);
    string csv = System.Text.Encoding.UTF8.GetString(bytes);

    // Assert
    Assert.Contains("Semana", csv);          // cabeçalho
    Assert.Contains("Saldo projetado", csv); // cabeçalho
    Assert.True(bytes.Length > 3);           // não vazio (mais que o BOM)
}
```

- [ ] **Step 2: Rodar o teste e confirmar a falha**

Run: `dotnet test --filter FullyQualifiedName~ExportCashFlowProjection`
Expected: FAIL — `ExportCashFlowProjection` não existe em `IFinancialReportExportService`.

- [ ] **Step 3: Declarar o método na interface**

Em `IFinancialReportExportService.cs`, adicionar (modelando pelas assinaturas existentes):

```csharp
Task<byte[]> ExportCashFlowProjection(int weeks, CancellationToken cancellationToken = default);
```

- [ ] **Step 4: Implementar no serviço**

Em `FinancialReportExportService.cs`, adicionar o método (mesmo padrão dos outros: chama o report service, monta linhas, usa `CsvWriter.Build` e `Bytes`):

```csharp
public async Task<byte[]> ExportCashFlowProjection(int weeks, CancellationToken cancellationToken = default)
{
    CashFlowProjectionModel projection = await reportService.GetCashFlowProjection(weeks, cancellationToken);

    List<IReadOnlyList<string>> rows = projection.Series
        .Select(week => (IReadOnlyList<string>)
        [
            Date(week.WeekStart),
            Money(week.Inflow),
            Money(week.Outflow),
            Money(week.Net),
            Money(week.ProjectedBalance)
        ])
        .ToList();

    string csv = CsvWriter.Build(["Semana", "Entrada", "Saida", "Liquido", "Saldo projetado"], rows);
    return Bytes(csv);
}
```

- [ ] **Step 5: Adicionar o endpoint no controller**

Em `FinancialReportsController.cs`, adicionar (mesmo padrão de `ExportAccrualResult`):

```csharp
[RequireAccess("financialReports.getCashFlowProjection.description")]
[GetEndpoint("cashflow-projection/export")]
public async Task<IActionResult> ExportCashFlowProjection([FromQuery] int weeks, CancellationToken cancellationToken)
{
    int horizon = weeks <= 0 ? 12 : weeks;
    byte[] csv = await exportService.ExportCashFlowProjection(horizon, cancellationToken);
    return SendCsv(csv, "projecao-fluxo-caixa.csv");
}
```

- [ ] **Step 6: Rodar o teste e confirmar que passa**

Run: `dotnet test --filter FullyQualifiedName~ExportCashFlowProjection`
Expected: PASS.

- [ ] **Step 7: Rodar a suíte do serviço para não regredir**

Run: `dotnet test --filter FullyQualifiedName~FinancialReportExportServiceTests`
Expected: PASS (todos).

- [ ] **Step 8: Commit**

```bash
git add AgencyCampaign.Application/Services/IFinancialReportExportService.cs AgencyCampaign.Infrastructure/Services/FinancialReportExportService.cs AgencyCampaign.Api/Controllers/FinancialReportsController.cs AgencyCampaign.Tests
git commit -m "feat: adiciona export CSV da projecao de fluxo de caixa"
```

---

## Part D — Telas (landing + 4 novas + portar 2)

### Task 8: Landing do módulo Relatórios

**Files:**
- Modify (substituir): `AgencyCampaign.Web/src/modules/Reports/index.tsx`

- [ ] **Step 1: Substituir o placeholder por uma landing com cards por área**

Lê o catálogo, filtra por permissão (igual ao layout), agrupa por área e mostra cards clicáveis. Áreas sem relatório acessível não aparecem (na fatia 1a só Financeiro tem itens).

```tsx
import { useNavigate } from 'react-router-dom'
import { PageLayout, Card, CardContent, usePermissions } from 'archon-ui'
import { reportCatalog, reportAreaOrder, reportAreaLabels, type ReportCatalogEntry } from './catalog'

export default function Reports() {
  const navigate = useNavigate()
  const { isRoot, hasAnyPermission } = usePermissions()

  const canSee = (entry: ReportCatalogEntry) => isRoot || !entry.requires || hasAnyPermission(entry.requires)

  return (
    <PageLayout title="Relatórios" subtitle="Central de relatórios do Kanvas" showDefaultActions={false}>
      <div className="space-y-8">
        {reportAreaOrder.map((area) => {
          const entries = reportCatalog.filter((entry) => entry.area === area && canSee(entry))
          if (entries.length === 0) {
            return null
          }
          return (
            <section key={area} className="space-y-3">
              <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">{reportAreaLabels[area]}</h2>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {entries.map((entry) => (
                  <Card key={entry.id} className="cursor-pointer transition-colors hover:border-primary/40" onClick={() => navigate(entry.path)}>
                    <CardContent className="flex items-start gap-3 pt-5 pb-5">
                      <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-primary/15 text-primary">{entry.icon}</span>
                      <div className="space-y-1">
                        <p className="text-sm font-semibold">{entry.title}</p>
                        <p className="text-xs text-muted-foreground">{entry.description}</p>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </section>
          )
        })}
      </div>
    </PageLayout>
  )
}
```

- [ ] **Step 2: Verificar compilação**

Run: `cd AgencyCampaign.Web && npx tsc --noEmit 2>&1 | grep -i 'Reports/index'`
Expected: nenhuma linha de erro.

- [ ] **Step 3: Commit**

```bash
git add AgencyCampaign.Web/src/modules/Reports/index.tsx
git commit -m "feat: transforma hub de relatorios em landing com cards por area"
```

---

### Task 9: Tela Projeção de Fluxo

**Files:**
- Create: `AgencyCampaign.Web/src/modules/Reports/Financial/Projection.tsx`

- [ ] **Step 1: Implementar a tela**

Filtro: horizonte (semanas) via `SearchableSelect`. Conteúdo: KPI de saldo de abertura, gráfico Nivo de saldo projetado e tabela das semanas. Export CSV via `exportCashFlowProjection`.

```tsx
import { useEffect, useMemo, useState } from 'react'
import { useApi, DataTable, SearchableSelect, type DataTableColumn } from 'archon-ui'
import { ResponsiveLine } from '@nivo/line'
import { financialReportService, type CashFlowProjection, type CashFlowProjectionWeek } from '../../../services/financialReportService'
import { formatCurrency, formatDate } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'

const WEEK_OPTIONS = [{ value: '8', label: '8 semanas' }, { value: '12', label: '12 semanas' }, { value: '26', label: '26 semanas' }]

export default function Projection() {
  const [weeks, setWeeks] = useState(12)
  const [data, setData] = useState<CashFlowProjection | null>(null)
  const { execute } = useApi<CashFlowProjection | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => financialReportService.getCashFlowProjection(weeks))
    setData(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [weeks])

  const chartData = useMemo(() => {
    if (!data || data.series.length === 0) {
      return []
    }
    return [{
      id: 'Saldo projetado',
      color: '#1F3B61',
      data: data.series.map((week) => ({ x: formatDate(week.weekStart), y: week.projectedBalance })),
    }]
  }, [data])

  const columns: DataTableColumn<CashFlowProjectionWeek>[] = [
    { key: 'weekStart', title: 'Semana', dataIndex: 'weekStart', primary: true, render: (value: string) => formatDate(value) },
    { key: 'inflow', title: 'Entrada', dataIndex: 'inflow', render: (value: number) => formatCurrency(value) },
    { key: 'outflow', title: 'Saída', dataIndex: 'outflow', render: (value: number) => formatCurrency(value) },
    { key: 'net', title: 'Líquido', dataIndex: 'net', render: (value: number) => formatCurrency(value) },
    { key: 'projectedBalance', title: 'Saldo projetado', dataIndex: 'projectedBalance', render: (value: number) => formatCurrency(value) },
  ]

  const filters = (
    <div className="space-y-1">
      <label className="text-xs text-muted-foreground">Horizonte</label>
      <SearchableSelect value={String(weeks)} onValueChange={(v) => setWeeks(Number(v))} options={WEEK_OPTIONS} />
    </div>
  )

  return (
    <ReportLayout title="Projeção de Fluxo" subtitle="Saldo projetado semana a semana" filters={filters} onRefresh={() => void load()} onExportCsv={() => financialReportService.exportCashFlowProjection(weeks)}>
      <div className="rounded-md border p-3">
        <p className="text-xs text-muted-foreground">Saldo de abertura</p>
        <p className="text-lg font-semibold text-primary">{formatCurrency(data?.openingBalance ?? 0)}</p>
      </div>
      <div style={{ height: 320 }}>
        {chartData.length === 0 ? (
          <p className="flex h-full items-center justify-center text-sm text-muted-foreground">Nenhum dado projetado.</p>
        ) : (
          <ResponsiveLine
            data={chartData}
            margin={{ top: 20, right: 30, bottom: 50, left: 70 }}
            xScale={{ type: 'point' }}
            yScale={{ type: 'linear', min: 'auto', max: 'auto' }}
            axisBottom={{ tickRotation: -30 }}
            axisLeft={{ format: (value: number) => `R$ ${(value / 1000).toFixed(0)}k` }}
            colors={(serie) => (serie.color as string) ?? '#1F3B61'}
            pointSize={6}
            useMesh
            enableArea
            areaOpacity={0.1}
          />
        )}
      </div>
      <DataTable columns={columns} data={data?.series ?? []} rowKey="weekStart" emptyText="Nenhum dado projetado." />
    </ReportLayout>
  )
}
```

- [ ] **Step 2: Verificar compilação**

Run: `cd AgencyCampaign.Web && npx tsc --noEmit 2>&1 | grep -i 'Projection'`
Expected: nenhuma linha de erro.

- [ ] **Step 3: Commit**

```bash
git add AgencyCampaign.Web/src/modules/Reports/Financial/Projection.tsx
git commit -m "feat: adiciona tela de projecao de fluxo de caixa em relatorios"
```

---

### Task 10: Tela Resultado (Competência / DRE)

**Files:**
- Create: `AgencyCampaign.Web/src/modules/Reports/Financial/AccrualResult.tsx`

- [ ] **Step 1: Implementar a tela**

Filtro: período (from/to). Conteúdo: três KPIs (Receita, Despesa, Resultado).

```tsx
import { useEffect, useState } from 'react'
import { useApi } from 'archon-ui'
import { financialReportService, type AccrualResult as AccrualResultModel } from '../../../services/financialReportService'
import { formatCurrency } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'
import { ReportPeriodFilter } from '../_shared/ReportFilters'

function defaultRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

export default function AccrualResult() {
  const [range, setRange] = useState(defaultRange())
  const [data, setData] = useState<AccrualResultModel | null>(null)
  const { execute } = useApi<AccrualResultModel | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => financialReportService.getAccrualResult(new Date(range.from).toISOString(), new Date(range.to).toISOString()))
    setData(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range.from, range.to])

  const filters = <ReportPeriodFilter from={range.from} to={range.to} onChange={setRange} />

  return (
    <ReportLayout title="Resultado (Competência)" subtitle="Receita menos despesa no regime de competência" filters={filters} onRefresh={() => void load()} onExportCsv={() => financialReportService.exportAccrualResult(new Date(range.from).toISOString(), new Date(range.to).toISOString())}>
      <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
        <div className="rounded-md border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wide">Receita</p>
          <p className="text-2xl font-semibold text-emerald-600">{formatCurrency(data?.revenue ?? 0)}</p>
        </div>
        <div className="rounded-md border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wide">Despesa</p>
          <p className="text-2xl font-semibold text-destructive">{formatCurrency(data?.expense ?? 0)}</p>
        </div>
        <div className="rounded-md border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wide">Resultado</p>
          <p className="text-2xl font-semibold text-primary">{formatCurrency(data?.result ?? 0)}</p>
        </div>
      </div>
    </ReportLayout>
  )
}
```

- [ ] **Step 2: Verificar compilação**

Run: `cd AgencyCampaign.Web && npx tsc --noEmit 2>&1 | grep -i 'AccrualResult'`
Expected: nenhuma linha de erro.

- [ ] **Step 3: Commit**

```bash
git add AgencyCampaign.Web/src/modules/Reports/Financial/AccrualResult.tsx
git commit -m "feat: adiciona tela de resultado por competencia em relatorios"
```

---

### Task 11: Tela Rentabilidade por Campanha

**Files:**
- Create: `AgencyCampaign.Web/src/modules/Reports/Financial/CampaignProfitability.tsx`

- [ ] **Step 1: Implementar a tela**

Sem filtro de período (acumulado). Conteúdo: tabela por campanha + KPIs de totais.

```tsx
import { useEffect, useState } from 'react'
import { useApi, DataTable, Badge, type DataTableColumn } from 'archon-ui'
import { financialReportService, type CampaignProfitabilityReport, type CampaignProfitabilityLine } from '../../../services/financialReportService'
import { formatCurrency, formatPercent } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'

export default function CampaignProfitability() {
  const [data, setData] = useState<CampaignProfitabilityReport | null>(null)
  const { execute } = useApi<CampaignProfitabilityReport | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => financialReportService.getCampaignProfitability())
    setData(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const columns: DataTableColumn<CampaignProfitabilityLine>[] = [
    { key: 'campaignName', title: 'Campanha', dataIndex: 'campaignName', primary: true, render: (value?: string | null) => value || '—' },
    { key: 'revenue', title: 'Receita', dataIndex: 'revenue', render: (value: number) => formatCurrency(value) },
    { key: 'creatorCost', title: 'Custo creator', dataIndex: 'creatorCost', hiddenBelow: 'md', render: (value: number) => formatCurrency(value) },
    { key: 'otherCost', title: 'Outros custos', dataIndex: 'otherCost', hiddenBelow: 'lg', render: (value: number) => formatCurrency(value) },
    { key: 'margin', title: 'Margem', dataIndex: 'margin', render: (value: number) => formatCurrency(value) },
    { key: 'marginPercent', title: 'Margem %', dataIndex: 'marginPercent', cardTag: true, render: (value: number) => <Badge variant={value >= 0 ? 'success' : 'destructive'}>{formatPercent(value / 100)}</Badge> },
  ]

  return (
    <ReportLayout title="Rentabilidade por Campanha" subtitle="Receita, custos e margem por campanha" onRefresh={() => void load()} onExportCsv={() => financialReportService.exportCampaignProfitability()}>
      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Receita total</p>
          <p className="text-lg font-semibold text-emerald-600">{formatCurrency(data?.totalRevenue ?? 0)}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Custo creators</p>
          <p className="text-lg font-semibold text-destructive">{formatCurrency(data?.totalCreatorCost ?? 0)}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Outros custos</p>
          <p className="text-lg font-semibold text-destructive">{formatCurrency(data?.totalOtherCost ?? 0)}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Margem total</p>
          <p className="text-lg font-semibold text-primary">{formatCurrency(data?.totalMargin ?? 0)}</p>
        </div>
      </div>
      <DataTable columns={columns} data={data?.lines ?? []} rowKey="campaignId" emptyText="Nenhuma campanha com lançamentos." />
    </ReportLayout>
  )
}
```

- [ ] **Step 2: Verificar compilação**

Run: `cd AgencyCampaign.Web && npx tsc --noEmit 2>&1 | grep -i 'CampaignProfitability'`
Expected: nenhuma linha de erro. (Confirmar que `formatPercent` existe em `lib/format.ts`; existe conforme a regra de helpers centralizados. Se a assinatura esperar 0-1, passamos `value/100`.)

- [ ] **Step 3: Commit**

```bash
git add AgencyCampaign.Web/src/modules/Reports/Financial/CampaignProfitability.tsx
git commit -m "feat: adiciona tela de rentabilidade por campanha em relatorios"
```

---

### Task 12: Tela Retenções Fiscais

**Files:**
- Create: `AgencyCampaign.Web/src/modules/Reports/Financial/TaxWithholding.tsx`

- [ ] **Step 1: Implementar a tela**

Filtro: período (from/to). Conteúdo: tabela por creator + KPIs de totais. O `taxRegime` é numérico (enum); mapear para rótulo.

```tsx
import { useEffect, useState } from 'react'
import { useApi, DataTable, type DataTableColumn } from 'archon-ui'
import { financialReportService, type TaxWithholdingReport, type TaxWithholdingLine } from '../../../services/financialReportService'
import { formatCurrency } from '../../../lib/format'
import ReportLayout from '../_shared/ReportLayout'
import { ReportPeriodFilter } from '../_shared/ReportFilters'

const TAX_REGIME_LABELS: Record<number, string> = { 1: 'Pessoa Física', 2: 'MEI', 3: 'Simples Nacional', 4: 'Lucro Presumido/Real' }

function defaultRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

export default function TaxWithholding() {
  const [range, setRange] = useState(defaultRange())
  const [data, setData] = useState<TaxWithholdingReport | null>(null)
  const { execute } = useApi<TaxWithholdingReport | null>({ showErrorMessage: true })

  const load = async () => {
    const result = await execute(() => financialReportService.getTaxWithholding(new Date(range.from).toISOString(), new Date(range.to).toISOString()))
    setData(result)
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range.from, range.to])

  const columns: DataTableColumn<TaxWithholdingLine>[] = [
    { key: 'creatorName', title: 'Creator', dataIndex: 'creatorName', primary: true, render: (value?: string | null) => value || '—' },
    { key: 'document', title: 'Documento', dataIndex: 'document', hiddenBelow: 'md', render: (value?: string | null) => value || '—' },
    { key: 'taxRegime', title: 'Regime', dataIndex: 'taxRegime', hiddenBelow: 'lg', render: (value?: number | null) => (value != null ? TAX_REGIME_LABELS[value] ?? '—' : '—') },
    { key: 'grossAmount', title: 'Bruto', dataIndex: 'grossAmount', render: (value: number) => formatCurrency(value) },
    { key: 'taxWithheld', title: 'Retido', dataIndex: 'taxWithheld', render: (value: number) => formatCurrency(value) },
    { key: 'netAmount', title: 'Líquido', dataIndex: 'netAmount', render: (value: number) => formatCurrency(value) },
    { key: 'paymentCount', title: 'Qtd', dataIndex: 'paymentCount', hiddenBelow: 'sm' },
  ]

  const filters = <ReportPeriodFilter from={range.from} to={range.to} onChange={setRange} />

  return (
    <ReportLayout title="Retenções Fiscais" subtitle="Imposto retido na fonte por creator" filters={filters} onRefresh={() => void load()} onExportCsv={() => financialReportService.exportTaxWithholding(new Date(range.from).toISOString(), new Date(range.to).toISOString())}>
      <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Bruto total</p>
          <p className="text-lg font-semibold">{formatCurrency(data?.totalGross ?? 0)}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Retido total</p>
          <p className="text-lg font-semibold text-destructive">{formatCurrency(data?.totalWithheld ?? 0)}</p>
        </div>
        <div className="rounded-md border p-3">
          <p className="text-xs text-muted-foreground">Líquido total</p>
          <p className="text-lg font-semibold text-emerald-600">{formatCurrency(data?.totalNet ?? 0)}</p>
        </div>
      </div>
      <DataTable columns={columns} data={data?.lines ?? []} rowKey="creatorId" emptyText="Nenhuma retenção no período." />
    </ReportLayout>
  )
}
```

- [ ] **Step 2: Verificar compilação completa**

Run: `cd AgencyCampaign.Web && npx tsc --noEmit`
Expected: PASS (todos os módulos `Reports/Financial/*` existem agora; as rotas da Task 2 resolvem). Se restar erro, é o ponto exato a corrigir.

- [ ] **Step 3: Commit**

```bash
git add AgencyCampaign.Web/src/modules/Reports/Financial/TaxWithholding.tsx
git commit -m "feat: adiciona tela de retencoes fiscais em relatorios"
```

---

### Task 13: Portar Fluxo de Caixa e Aging (adicionar export CSV)

As telas já existem e já foram religadas às novas rotas (Task 2). Falta dar a elas o botão de export CSV (hoje não têm), para consistência com as demais. Mudança mínima: adicionar um botão de export no header de cada uma, reutilizando os métodos `exportCashFlow`/`exportAging` que já existem no service.

**Files:**
- Modify: `AgencyCampaign.Web/src/modules/Main/Financial/CashFlow.tsx`
- Modify: `AgencyCampaign.Web/src/modules/Main/Financial/Aging.tsx`

- [ ] **Step 1: Adicionar export CSV ao Fluxo de Caixa**

Em `CashFlow.tsx`, importar `Button` (adicionar à linha 2 de import do archon-ui) e `Download` de `lucide-react`, e adicionar um botão dentro do grid de filtros (após o bloco da granularidade, dentro da primeira `div` grid, linha ~136). Acrescentar uma coluna com o botão:

```tsx
            <div className="flex items-end">
              <Button variant="outline" size="sm" onClick={() => void financialReportService.exportCashFlow(new Date(range.from).toISOString(), new Date(range.to).toISOString(), granularity)}>
                <Download className="mr-1.5 h-4 w-4" />Exportar CSV
              </Button>
            </div>
```

Ajustar a linha 2 para incluir `Button` (uma linha):

```tsx
import { PageLayout, Card, CardContent, useApi, SearchableSelect, Input, Button, useI18n } from 'archon-ui'
```

E adicionar (uma linha) o import do ícone:

```tsx
import { Download } from 'lucide-react'
```

- [ ] **Step 2: Adicionar export CSV ao Aging**

Em `Aging.tsx`, importar `Button` (linha 2) e `Download`, e adicionar um botão de export no topo. Como o Aging usa `PageLayout` sem barra de filtros, inserir o botão num cabeçalho de ação acima do grid de totais (antes da `<div className="grid grid-cols-1 gap-3 md:grid-cols-2 mb-4">`, linha 36):

```tsx
      <div className="mb-3 flex justify-end">
        <Button variant="outline" size="sm" onClick={() => void financialReportService.exportAging()}>
          <Download className="mr-1.5 h-4 w-4" />Exportar CSV
        </Button>
      </div>
```

Ajustar a linha 2 (uma linha):

```tsx
import { PageLayout, Card, CardContent, useApi, Badge, Button, useI18n } from 'archon-ui'
```

E o import do ícone:

```tsx
import { Download } from 'lucide-react'
```

- [ ] **Step 3: Verificar compilação**

Run: `cd AgencyCampaign.Web && npx tsc --noEmit`
Expected: PASS.

- [ ] **Step 4: Commit (inclui o bloco de rotas/sidebar das Tasks 2-3)**

```bash
git add AgencyCampaign.Web/src/routes/index.tsx AgencyCampaign.Web/src/layouts/AgencyCampaignLayout.tsx AgencyCampaign.Web/src/modules/Main/Financial/CashFlow.tsx AgencyCampaign.Web/src/modules/Main/Financial/Aging.tsx
git commit -m "feat: centraliza fluxo de caixa e aging no modulo relatorios com export CSV"
```

---

## Part E — E2E e verificação final

### Task 14: Spec E2E do módulo Relatórios

**Files:**
- Modify/criar: `AgencyCampaign.Web/e2e/tests/29-relatorios-financeiros.spec.ts` (já existe — estender) e revisar specs que navegavam para `/financeiro/fluxo-caixa` ou `/financeiro/aging` (agora redirecionam).

- [ ] **Step 1: Localizar o harness de login da suíte**

Run: `cd AgencyCampaign.Web && ls e2e && grep -rl "login\|auth\|storageState" e2e | head`
Expected: identificar o helper/fixture de autenticação usado pelos specs existentes (ex.: um `beforeEach` ou `storageState`). Reusar exatamente esse mecanismo no novo teste — não criar login novo.

- [ ] **Step 2: Escrever o spec de navegação + render + export**

Modelar pelos specs existentes (mesmo import de fixtures e login). Esqueleto dos casos a cobrir:

```ts
// Reusar o harness/login da suíte existente (ver specs vizinhos).
test('Relatorios: navega pelo painel e abre os 6 relatorios financeiros', async ({ page }) => {
  await page.goto('/relatorios')
  // a landing mostra a secao Financeiro com os cards
  await expect(page.getByText('Fluxo de Caixa')).toBeVisible()
  await expect(page.getByText('Projeção de Fluxo')).toBeVisible()
  await expect(page.getByText('Rentabilidade por Campanha')).toBeVisible()

  // abre cada relatorio pela rota direta e confirma que renderiza
  for (const path of [
    '/relatorios/financeiro/fluxo-caixa',
    '/relatorios/financeiro/aging',
    '/relatorios/financeiro/projecao',
    '/relatorios/financeiro/resultado',
    '/relatorios/financeiro/rentabilidade',
    '/relatorios/financeiro/retencoes',
  ]) {
    await page.goto(path)
    await expect(page.locator('h1, [data-page-title]')).toBeVisible()
  }
})

test('Relatorios: rota antiga de fluxo de caixa redireciona para relatorios', async ({ page }) => {
  await page.goto('/financeiro/fluxo-caixa')
  await expect(page).toHaveURL(/\/relatorios\/financeiro\/fluxo-caixa$/)
})

test('Relatorios: exporta CSV do resultado por competencia', async ({ page }) => {
  await page.goto('/relatorios/financeiro/resultado')
  const downloadPromise = page.waitForEvent('download')
  await page.getByRole('button', { name: /CSV/i }).click()
  const download = await downloadPromise
  expect(download.suggestedFilename()).toContain('.csv')
})
```

Ajustar seletores (`h1`/título) ao que `PageLayout` realmente renderiza — inspecionar um spec existente que valide título de página para usar o mesmo seletor.

- [ ] **Step 3: Rodar os specs do módulo**

Run: `cd AgencyCampaign.Web && npx playwright test 29-relatorios-financeiros`
Expected: PASS. Se algum spec antigo navegava direto para `/financeiro/fluxo-caixa` esperando a tela (e não o redirect), atualizar para a nova URL.

- [ ] **Step 4: Commit**

```bash
git add AgencyCampaign.Web/e2e
git commit -m "test: cobre navegacao, redirect e export do modulo relatorios"
```

---

### Task 15: Verificação final

- [ ] **Step 1: Typecheck + build do frontend**

Run: `cd AgencyCampaign.Web && npx tsc --noEmit && npm run build`
Expected: PASS sem erros.

- [ ] **Step 2: Lint do frontend**

Run: `cd AgencyCampaign.Web && npm run lint`
Expected: PASS (corrigir o que aparecer; atenção a imports multi-linha — devem ficar em uma linha).

- [ ] **Step 3: Testes backend**

Run: `dotnet test --filter FullyQualifiedName~FinancialReport`
Expected: PASS (inclui o novo teste da projeção).

- [ ] **Step 4: Smoke manual (opcional, recomendado)**

Subir o app e conferir: a sidebar mostra "Relatórios" com subgrupo "Financeiro" listando os 6 relatórios; cada um abre, filtra e exporta CSV; rotas antigas redirecionam; a landing `/relatorios` mostra os cards.

- [ ] **Step 5: Marcar conclusão**

A fatia 1a (shell + Financeiro com CSV) está completa. Próximo: Plano B (export PDF unificado).

---

## Cobertura do spec (rastreabilidade)

- Navegação subGroups por área (spec §4.1): Tasks 1, 3.
- Centralização + redirects (spec §2.1, §4.1): Tasks 2, 13.
- Toolkit + catálogo (spec §4.2): Tasks 1, 4, 5.
- Surfar os 4 prontos + portar 2 (spec §3 Financeiro, §4.8 fatia 1a): Tasks 6, 8-13.
- Export CSV (spec §4.4, parte): Tasks 6, 7, 13.
- Permissões por área (spec §4.5): `requires` no catálogo (Task 1) + filtro na sidebar/landing (Tasks 3, 8).
- Testes (spec §4.7): Tasks 7 (unit), 14 (E2E).

## Fora deste plano (vai para o Plano B — também fatia 1a)

- Contrato `ReportTable` unificado (backend) e `ReportPdfService` (PuppeteerSharp) + extração do `PuppeteerPdfRenderer` compartilhado.
- Botão/endpoints de export PDF em todos os relatórios financeiros (o `ReportLayout` já tem o gancho `onExportPdf`).
