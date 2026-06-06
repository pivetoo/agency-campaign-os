# Design: Módulo de Relatórios (Kanvas / AgencyCampaign)

- Data: 2026-06-05
- Status: aprovado (design) — pronto para plano de implementação
- Escopo: relatórios padrões (fase 1). Relatórios personalizados ficam para a fase 2.

## 1. Contexto e objetivo

O hub `/relatorios` hoje é apenas um placeholder (`EmptyState` "Relatórios em breve").
O objetivo da fase 1 é entregar um módulo de Relatórios real, organizado na sidebar
em três áreas separadas — **Comercial**, **Produção** e **Financeiro** — com um conjunto
curado de relatórios padrões por área. A personalização de relatórios é uma fase 2 e
não é projetada em detalhe agora, mas a arquitetura da fase 1 deve deixá-la viável.

### Achado que orienta o design

O sistema já tem bastante infraestrutura de relatório, então "criar relatórios padrões"
é, em grande parte, **surfar o que já existe** e **organizar tudo num lugar só**:

- Backend financeiro com 6 relatórios e export CSV pt-BR (com proteção anti-injection)
  em `FinancialReportsController` / `FinancialReportService`. Só 2 têm tela
  (Fluxo de Caixa em `/financeiro/fluxo-caixa`, Aging em `/financeiro/aging`);
  **4 estão prontos no backend e sem UI** (Projeção de fluxo, DRE/competência,
  Rentabilidade por campanha, Retenções fiscais).
- Comercial com agregações prontas em `OpportunitiesController`
  (`Board`, `Forecast`, `Analytics`, `Insights`, `Dashboard`) e uma tela
  `/comercial/analytics` (funil/conversão, ciclo, motivos win/loss, top performers).
- Produção com relatório público de campanha (`/r/:token`) e métricas sociais completas
  em `CampaignDeliverable` (likes, comments, views, reach, impressions, saves, shares,
  engagement rate), além de SLA computado (`SlaStatus`), rodadas de revisão
  (`DeliverableContentVersion.RoundNumber`) e licenças (`DeliverableContentLicense`).
- Padrão técnico já estabelecido: tela de relatório = filtros + gráfico Nivo + tabela +
  export CSV (ver `CashFlow.tsx` / `Aging.tsx`); utilitário `CsvWriter` reutilizável;
  gráficos via Nivo (`@nivo/line`, `@nivo/pie`) e wrappers do archon-ui
  (`LineChart`, `BarChart`, `PieChart`, `ChartContainer`).
- PDF: PuppeteerSharp já está presente na imagem (usado nas propostas), com
  `CHROMIUM_EXECUTABLE_PATH` configurado — reutilizável para PDF de relatórios.

## 2. Decisões (validadas com o usuário)

1. **Centralizar tudo em Relatórios.** As telas de relatório que hoje vivem fora do hub
   (Fluxo de Caixa e Aging no Financeiro; Análise comercial em `/comercial/analytics`)
   migram para o módulo Relatórios. No fluxo operacional ficam, no máximo, atalhos/redirect.
2. **Conjunto curado (~5-6 por área).** Priorizar os relatórios de maior uso recorrente.
3. **Personalização fica para a fase 2.** Projetar os padrões de forma extensível
   (catálogo + contrato de export) sem construir o engine de custom report agora.
4. **Arquitetura A — telas dedicadas + toolkit compartilhado.** Cada relatório é uma rota
   própria montada sobre um kit reutilizável; não usar engine genérico orientado a metadados
   (over-engineering, dado que custom foi adiado).
5. **Export CSV + PDF desde já.** CSV reusa o `CsvWriter` existente; PDF reusa o
   PuppeteerSharp das propostas, com um renderizador genérico único.

## 3. Catálogo curado (fase 1)

Legenda de status:
- ✅ pronto no backend (falta só tela)
- 🟡 tela já existe (em outro módulo) — será portada/centralizada
- 🆕 novo (dados já existem; precisa query + tela)

### Comercial (portar `/comercial/analytics` + queries novas)

| Relatório | Status | Fonte de dados / reuso |
|---|---|---|
| Funil de conversão (pipeline por estágio) | 🟡 | reusa `OpportunitiesController.Analytics` (ConversionByStage) |
| Ganhos × Perdas por motivo | 🟡 | reusa `Analytics` (WinReasons/LossReasons) |
| Forecast / previsão ponderada | ✅ | reusa `OpportunitiesController.Forecast` |
| Metas × Realizado (período/vendedor) | 🆕 | `CommercialGoal` + oportunidades ganhas; modelo `CommercialGoalProgress` já existe |
| Propostas: emitidas × aceitas | 🆕 | `Proposal.Status` + `ProposalStatusHistory` |
| Ranking por marca | 🆕 | `Opportunity.BrandId` agregado |

Observação: funil, win/loss, ciclo e top performers já estão consolidados na tela
`/comercial/analytics`. Ela é **portada como está** (vira uma ou duas entradas no catálogo:
"Funil & Conversão" e, se desejado no plano, "Ganhos × Perdas" como recorte). Os itens 🆕
são endpoints novos no `CommercialReportService`.

### Produção (tudo novo — backend + tela)

| Relatório | Status | Fonte de dados |
|---|---|---|
| Performance de campanhas (alcance/engajamento/EMV) | 🆕 | agrega métricas de `CampaignDeliverable` (o que hoje só existe por campanha no link público) |
| Desempenho por creator | 🆕 | `CampaignCreator` + métricas de entregáveis |
| Produção por plataforma | 🆕 | `CampaignDeliverable.PlatformId` + métricas |
| Entregáveis: prazo × atraso (SLA) | 🆕 | `DueAt`/`PublishedAt`/`Status`/`SlaStatus` |
| Tempo de aprovação / rodadas de revisão | 🆕 | `DeliverableApproval` + `DeliverableContentVersion.RoundNumber` |
| Licenças de conteúdo (expiração) | 🆕 | `DeliverableContentLicense.ExpiresAt` + `ComputeStatus` |

### Financeiro (5 prontos no backend + portar 2 telas)

| Relatório | Status | Fonte de dados / reuso |
|---|---|---|
| Fluxo de Caixa | 🟡 | tela existente em `/financeiro/fluxo-caixa` (portar) |
| Aging / vencimentos | 🟡 | tela existente em `/financeiro/aging` (portar) |
| Projeção de fluxo (12 semanas) | ✅ | `FinancialReports/cashflow-projection` |
| DRE / Resultado por competência | ✅ | `FinancialReports/accrual-result` |
| Rentabilidade por campanha | ✅ | `FinancialReports/campaign-profitability` |
| Retenções fiscais por creator | ✅ | `FinancialReports/tax-withholding` |

Primeiros "novos" do Financeiro (entram se quisermos ir além dos 6, ou na 1b/1c):
Receita por marca, Despesas por categoria/subcategoria, Repasses a creators por período.
Todos com dados já existentes (alguns exigem JOIN `Campaign.BrandId`).

## 4. Arquitetura

### 4.1 Navegação e rotas

- `Relatórios` deixa de ser rota única e passa a um **módulo com `subGroups`**, usando o
  mesmo mecanismo que o módulo Configuração já usa em `AgencyCampaignLayout.tsx`
  (`configSubGroupDefs` → `subGroups`).
- Posição: mantém-se no grupo operacional (`group: 'op'`), logo abaixo do módulo Financeiro
  e antes do separador para os módulos de sistema.
- Três subgrupos, reaproveitando labels existentes:
  - Comercial → `nav.group.commercial`
  - Produção → `nav.group.operations`
  - Financeiro → `nav.group.finance`
- Convenção de rota: `/relatorios/<área>/<slug>`
  (ex.: `/relatorios/financeiro/dre`, `/relatorios/producao/entregaveis-sla`,
  `/relatorios/comercial/metas`).
- `/relatorios` vira **landing** com cards por área, lidos do catálogo (4.2).
- Centralização e compatibilidade:
  - As rotas antigas `/financeiro/fluxo-caixa`, `/financeiro/aging` e `/comercial/analytics`
    viram **redirects** para as novas rotas em `/relatorios/...` (preserva links, bookmarks e
    specs E2E existentes).
  - As entradas correspondentes somem dos grupos operacionais da sidebar (Financeiro/Comercial),
    pois passam a viver sob Relatórios.

### 4.2 Toolkit de relatório + catálogo (frontend)

- `ReportLayout` (novo, em `src/modules/Reports/_shared/`): cabeçalho (título + descrição),
  barra de filtros (children), área de gráfico, tabela (`DataTable`), botões Export CSV/PDF,
  estados loading/empty/error via `useApi`. É a generalização do que `CashFlow.tsx` e
  `Aging.tsx` já fazem hoje.
- Filtros compartilhados (em `_shared/filters/`): período (from/to), granularidade
  (Day/Week/Month) e seletores de Marca / Vendedor / Campanha / Creator / Plataforma.
  Todos os dropdowns dinâmicos usam `SearchableSelect`. Datas/valores sempre via `lib/format`.
- `catalog.ts` (novo, em `src/modules/Reports/`): array de descritores
  `{ id, area: 'comercial' | 'producao' | 'financeiro', title, description, icon, path, requires: string[] }`.
  Fonte única que alimenta: (a) os `subGroups` da sidebar, (b) a landing com cards,
  (c) breadcrumbs. É a semente dos relatórios personalizados da fase 2 (custom = novos
  descritores definidos pelo usuário).

### 4.3 Serviços de relatório (backend) + reuso

- Padrão: espelha `IFinancialReportService` / `FinancialReportService`.
- Serviços novos nomeados para o auto-DI do Archon (classe terminando em `Service`,
  namespace contendo `Services`): `CommercialReportService`, `ProductionReportService`,
  em `AgencyCampaign.Infrastructure/Services/`.
- Comercial reaproveita o que já existe em `OpportunitiesController` (funil, forecast,
  win/loss, performers). Endpoints novos somente para Metas × Realizado, Propostas
  emitidas × aceitas e Ranking por marca.
- Financeiro reaproveita os 6 endpoints já existentes; o trabalho é só de UI (4 telas novas)
  + portar 2 telas.
- Produção é o trabalho novo: `ProductionReportService` com métodos para performance de
  campanha, desempenho por creator, produção por plataforma, SLA de entregáveis, tempo/rodadas
  de aprovação e licenças. DTOs novos em `AgencyCampaign.Application/Models/Production/Reports/`
  (e `.../Commercial/Reports/` para os comerciais novos).
- Controllers novos: `CommercialReportsController`, `ProductionReportsController`,
  cada endpoint com `[RequireAccess(...)]`.
- Convenção Archon a respeitar: migrations não fazem parte (sem schema novo na fase 1);
  rotas com template iniciando em `{` ganham `[action]/` (atenção no client HTTP);
  colunas de data sempre `TIMESTAMPTZ` (não aplicável aqui pois não há migration nova).

### 4.4 Exportação CSV + PDF (contrato unificado)

- **Contrato `ReportTable`**: todo relatório expõe uma forma tabular
  `{ metadados (título, período, filtros aplicados, geradoEm), colunas, linhas, totais }`.
  CSV e PDF derivam da MESMA estrutura — consistência e DRY, e extensível a custom reports.
- CSV: reusa `CsvWriter` (separador `;`, vírgula decimal, UTF-8 com BOM, anti-injection) e o
  padrão de `FinancialReportExportService`. Cada endpoint de relatório ganha um irmão `/export`.
- PDF: um **`ReportPdfService` genérico** (em `Infrastructure/Services/`) reusando o
  PuppeteerSharp/Chrome já presente. Template único: cabeçalho com a marca Mainstay
  (primary navy `#1F3B61`, secondary cyan `#00B3C7`) + período/filtros + KPIs + tabela.
  Um único renderizador para todos os relatórios (sem template por relatório).
- Gráfico embutido no PDF é evolução futura (frontend envia PNG do Nivo). PDF v1 = KPIs + tabela.

### 4.5 Permissões / catálogo de acesso

- Novos descritores de acesso `commercialReports.*` e `productionReports.*` (o financeiro
  reusa `financialReports.*`), sincronizados ao IdM pelo mecanismo de sync de access resources
  do Archon.
- A sidebar já filtra rotas por `requires` (`filterItems` / `toNavRoutes`), então cada usuário
  só enxerga os relatórios que pode acessar. Como os relatórios são gateados por acesso, exibir
  valores financeiros aqui é apropriado (diferente da dashboard compartilhada, que não mostra
  receita).

### 4.6 i18n

- Nova chave `nav.module.reports` (hoje "Relatórios" está hardcoded no layout) + títulos e
  descrições dos relatórios. Idiomas pt-BR / en-US / es-AR (i18n do archon-ui).
  Os labels dos subgrupos reaproveitam chaves existentes.

### 4.7 Testes

- Backend: unitário + EF InMemory (padrão do projeto; sem Testcontainers/WebApplicationFactory),
  espelhando `FinancialReportServiceTests` — agregações, conjuntos vazios, exclusão de estornos,
  bordas de período. PDF: testar a montagem do HTML do template sem subir o Chrome no unit.
- E2E Playwright: specs por área (navegar, aplicar filtro, render, baixar CSV e PDF), estendendo
  a suíte atual (que já tem `29-relatorios-financeiros.spec.ts`). Os redirects das rotas antigas
  precisam continuar passando.
- TDD nos serviços novos; root-cause-first; não hackear teste/fixture para forçar verde.

### 4.8 Sequenciamento (dentro da fase 1)

Entrega em três fatias, validando o shell cedo com o que é mais barato:

- **1a — Shell + Financeiro:** navegação subGroups, `ReportLayout`, filtros, `catalog.ts`,
  landing, contrato de export CSV+PDF; surfar os 4 financeiros prontos + portar Fluxo de
  Caixa/Aging. Resultado: Relatórios já funcional com 6 relatórios financeiros.
- **1b — Comercial:** portar `/comercial/analytics` + página de Forecast (reuso) +
  3 endpoints novos (Metas × Realizado, Propostas, Ranking por marca).
- **1c — Produção:** `ProductionReportService` e telas novas (o mais pesado).

## 5. Fora de escopo (YAGNI — fase 1)

- Engine de relatórios personalizados (fase 2).
- Agendamento / geração em background de relatórios.
- Template de PDF por relatório (usamos um único renderizador genérico).
- Gráfico embutido no PDF.

## 6. Pontos a verificar no plano de implementação

- Confirmar que o componente de ModuleNav renderiza `subGroups` para um módulo do grupo `op`
  (hoje só o Configuração usa subGroups, e ele está no grupo `sys`). Se o render acoplar
  subGroups ao grupo `sys`, ajustar o componente de sidebar ou a posição de Relatórios.
- Confirmar o split da tela `/comercial/analytics`: portar inteira como "Funil & Conversão"
  vs. recortar "Ganhos × Perdas" em entrada separada no catálogo.
- Confirmar a estratégia de redirect das rotas antigas (componente de redirect vs. alias de rota).
- Validar a forma exata do PNG de gráfico caso decidamos antecipar gráfico no PDF.

## 7. Critérios de sucesso (fase 1)

- Sidebar mostra "Relatórios" com três subgrupos (Comercial, Produção, Financeiro), cada um
  listando apenas os relatórios que o usuário tem permissão de ver.
- `/relatorios` mostra landing com cards por área.
- Os 6 relatórios financeiros estão acessíveis sob Relatórios (4 novos + 2 portados),
  com export CSV e PDF.
- Rotas antigas redirecionam sem quebrar links/specs.
- Comercial e Produção entregues conforme o catálogo curado, com export CSV e PDF.
- Suíte unitária e E2E verde; sem hack de teste.
