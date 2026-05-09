# Kanvas — Sistema Operacional para Agências de Marketing de Influência

O Kanvas (codinome `agency-campaign-os`) é um sistema operacional completo para agências que trabalham com marketing de influência. Ele cobre o ciclo de ponta a ponta: prospecção comercial, gestão de propostas, operação de campanhas com creators, controle financeiro e auditoria.

## Sumário

- [Para quem é](#para-quem-é)
- [Conceitos chave](#conceitos-chave)
- [Módulos do produto](#módulos-do-produto)
- [Fluxos de negócio principais](#fluxos-de-negócio-principais)
- [Arquitetura no ecossistema](#arquitetura-no-ecossistema)
- [Multi-tenant](#multi-tenant)
- [Identidade, sessão e permissões](#identidade-sessão-e-permissões)
- [Integrações externas e automações](#integrações-externas-e-automações)
- [Notificações](#notificações)
- [Auditoria](#auditoria)
- [Stack técnica](#stack-técnica)
- [Estrutura do repositório](#estrutura-do-repositório)
- [Como rodar localmente](#como-rodar-localmente)
- [Deploy em produção](#deploy-em-produção)
- [Convenções de projeto](#convenções-de-projeto)

## Para quem é

O Kanvas atende agências que:

- prospectam marcas e gerenciam negociações com pipeline próprio;
- contratam creators (Instagram, TikTok, YouTube, entre outros) para campanhas;
- precisam acompanhar entregas, aprovações da marca e publicação;
- precisam gerar propostas formatadas, com versionamento e link público para a marca aprovar;
- querem controle financeiro vinculado às propostas e campanhas, com fluxo de caixa e aging;
- gerenciam um time interno comercial, operacional e financeiro com perfis distintos.

## Conceitos chave

- **Contrato**: entidade central no `IdentityManagement` que representa uma agência cliente. Cada agência roda em seu próprio contrato; um login só atua sobre um contrato por vez.
- **Tenant**: cada contrato tem um banco de dados PostgreSQL isolado. O Kanvas é multi-tenant por banco, não por linha. Detalhes em [Multi-tenant](#multi-tenant).
- **Pipeline comercial**: configurável por contrato. A agência define os estágios (`Lead`, `Qualificação`, `Proposta`, `Negociação`, `Fechado`...) e o comportamento final de cada um (`Sucesso`, `Perda` ou `Em andamento`).
- **Oportunidade**: registro central do funil. Vincula marca, responsável, origem, tags, propostas, follow-ups, comentários e histórico de estágio.
- **Proposta**: documento formal enviado para a marca. Tem versões, link público com tracking de visualização, suporte a PDF, e quando aprovada vira insumo para a Campanha.
- **Campanha**: execução real do trabalho. Reúne creators, entregas (`Deliverables`), documentos, status e histórico. Cada entrega pode ter link público de aprovação para a marca antes da publicação.
- **Creator**: pessoa cadastrada na base da agência, com handles sociais, métricas, histórico de campanhas e dados de pagamento.
- **Lançamento financeiro**: entrada (`a receber`) ou saída (`a pagar`) com vínculo opcional a proposta, campanha, creator ou marca. Pode ser gerada automaticamente quando uma proposta é aprovada.
- **Automação**: regra do tipo gatilho-ação que liga eventos do Kanvas (`proposta enviada`, `entrega aprovada`, etc.) a pipelines configurados no `IntegrationPlataform`.

## Módulos do produto

A navegação é organizada em três módulos top-level (visíveis no switcher da navbar): `Sistema`, `Configuração` e `Auditoria`. Dentro de `Sistema` e `Configuração`, a agenda de menu segue grupos consistentes (`Geral`, `Comercial`, `Operação`, `Finanças`).

### Sistema

#### Geral

- **Dashboard**: visão consolidada com KPIs (campanhas ativas, marcas, creators, entregas pendentes), receita dos últimos 12 meses, distribuição do pipeline, plataformas mais usadas, crescimento de creators e indicadores de saúde da operação.
- **Usuários**: gerenciamento de quem tem acesso ao contrato ativo. Disponível apenas para perfis `root`. O administrador cria usuários no `IdentityManagement` e atribui papéis do contrato. Detalhes em [Identidade, sessão e permissões](#identidade-sessão-e-permissões).

#### Comercial

- **Pipeline**: kanban com os estágios configurados; arrasta-e-solta para mover oportunidades.
- **Oportunidades**: listagem completa com filtros, drill-down em detalhe (proposta, follow-ups, comentários, histórico de estágio, negociação, aprovações).
- **Propostas**: gestão de todas as propostas, geração de PDF, envio de link público, conversão em campanha quando aprovada.
- **Aprovações**: fila de aprovações internas (descontos, condições especiais).
- **Atividades**: follow-ups com prazo e responsável; alerta de pendências em atraso.

#### Operação

- **Marcas**: cadastro de marcas-cliente, com contatos e histórico de campanhas.
- **Creators**: base completa com handles sociais (Instagram, TikTok, YouTube), métricas, histórico de campanhas, faturamento gerado e taxa de entrega no prazo. Tela `Creator 360` consolida tudo.
- **Campanhas**: gestão de cada campanha com creators, documentos (briefings, contratos) e entregas. Cada entrega passa por aprovação da marca antes da publicação.
- **Aprovações**: fila de aprovações operacionais (entregas pendentes, status de creator).

#### Finanças

- **Contas a receber**: lançamentos pendentes de recebimento, com vínculo à proposta/campanha.
- **Contas a pagar**: lançamentos pendentes de pagamento.
- **Fluxo de caixa**: gráfico de entradas e saídas projetadas e realizadas, por dia/semana/mês.
- **Aging**: distribuição de pendências por faixa de atraso.

### Configuração

Cadastros administrativos que parametrizam o comportamento do Sistema:

- **Geral**: dados da agência (logo, dados fiscais, padrões de e-mail), integrações externas, templates de e-mail.
- **Comercial**: estágios do funil, origens de oportunidade, tags, templates de proposta, blocos de proposta.
- **Operação**: redes sociais (plataformas), status dos creators, tipos de entrega (`Deliverables`).
- **Finanças**: contas bancárias, subcategorias financeiras.

### Auditoria

Histórico completo de alterações em entidades sensíveis. Cada mudança gera entrada com o que foi alterado, quem fez e quando. A coleta é automática via `Archon`.

## Fluxos de negócio principais

### Funil comercial

1. **Captação**: a agência registra uma oportunidade vinculada a uma marca, escolhe a origem (LinkedIn, indicação, etc.) e atribui ao responsável comercial.
2. **Qualificação**: o responsável move a oportunidade pelos estágios do funil. Cada movimentação registra histórico.
3. **Proposta**: ao chegar no estágio de proposta, o usuário gera o documento (com itens, valor, validade), versiona e dispara o link público.
4. **Aprovação da marca**: a marca abre o link público, visualiza (tracking automático), aprova ou pede ajustes.
5. **Conversão**: proposta aprovada vira campanha automaticamente; o financeiro pode ser gerado em paralelo.

Eventos relevantes (`proposta enviada`, `proposta aprovada`, `oportunidade ganha`, `follow-up vencido`, etc.) podem disparar [automações](#integrações-externas-e-automações) que rodam pipelines no `IntegrationPlataform` (envio de e-mail, postagem em outro sistema).

### Operação da campanha

1. **Setup**: a campanha herda informações da proposta. O operacional cadastra os creators selecionados e os contratos/briefings.
2. **Execução**: para cada creator são criadas entregas (`Deliverables`) com tipo (post, story, reel, video), data prevista e valor combinado.
3. **Aprovação**: cada entrega gera link público de aprovação. A marca recebe, revisa e aprova antes da publicação.
4. **Publicação**: após aprovada, a entrega pode ser marcada como publicada com data de publicação registrada.
5. **Fechamento**: campanha fica disponível para análise (taxa de entrega no prazo, faturamento).

### Geração financeira

Quando uma proposta é aprovada e convertida em campanha, o sistema pode gerar automaticamente os lançamentos financeiros:

- entradas (`a receber`) baseadas no valor da proposta, parcelamento configurado e datas de vencimento;
- saídas (`a pagar`) para cada creator, conforme o valor combinado nas entregas.

A geração automática é configurável: a agência decide se quer auto-gerar ou criar manualmente.

## Arquitetura no ecossistema

O Kanvas é um consumidor real do stack `Archon` e se integra a outros dois sistemas do ecossistema:

```
                          IdentityManagement
                          (auth + users + roles)
                                  |
                                  | OAuth/OIDC
                                  v
+------------+   X-Integration-Secret   +------------------+
|   Kanvas   | -----------------------> |  IdentityMgmt    |
|   API      |    (gerenciar usuarios   |  REST API        |
|            |     do contrato ativo)   +------------------+
|            |
|            |   X-Integration-Secret   +------------------+
|            | -----------------------> | IntegrationPlat. |
|            |   (executar pipelines    |  REST API        |
|            |    de automacao)         +------------------+
+------------+
      |
      | Bearer JWT
      v
+------------+
| Kanvas Web |
| (SPA)      |
+------------+
```

### Backend

O `AgencyCampaign.Api` herda do `Archon.Framework`:

- `AddArchonApi(...)`: pipeline HTTP padronizado, envelope `ApiResponse`, multi-tenancy automática (resolução de tenant a partir do JWT ou `IntegrationSecret`), localizações, OpenAPI.
- `AddArchonPersistence(...)`: `DbContext` por tenant, `CrudService<T>`, auditoria automática via `auditentries` + `auditpropertychanges`, eventos de domínio.
- `AddArchonHangfire(...)`: jobs de background (geração financeira, automações).
- `AddArchonIdentityManagement(...)`: cliente HTTP para o `IdentityManagement` com cache de chaves de assinatura JWT e `IdentityUsersClient` (gerenciamento de usuários do contrato).

### Frontend

O `AgencyCampaign.Web` consome o pacote `archon-ui`:

- `AppLayout`, `Sidebar`, `Navbar`, `DataTable`, `PageLayout`, `Modal`, `Sheet`, `Toast` e demais componentes visuais.
- `AuthProvider`, `useAuth`, `usePermissions` e o cliente HTTP autenticado com refresh token automático.
- `UsersManagementPage`: tela plugável de gestão de usuários do contrato, importada diretamente.
- `i18n` com pt-BR padrão.

## Multi-tenant

Cada agência cliente tem:

- um **contrato** registrado no `IdentityManagement` (`contracts`);
- um **banco PostgreSQL próprio** referenciado em `tenantdatabases` (catálogo de tenants);
- um **`IntegrationSecret` exclusivo** que identifica o tenant em chamadas server-to-server.

Em runtime, toda requisição HTTP carrega o `tenant_id` (claim no JWT do usuário ou via header `X-Integration-Secret`). O `Archon` resolve o tenant uma vez e configura o `DbContext` da request apontando para o banco correto. O isolamento é físico, não por coluna `tenant_id`.

Detalhes adicionais em `.playbook/system-docs/identity-access-management.md`.

## Identidade, sessão e permissões

### Login

Login do usuário acontece sempre no `IdentityManagement` (`https://auth.<dominio>`). O fluxo OIDC redireciona o usuário para o IdM, autentica, e devolve para o Kanvas com `accessToken` (JWT) e `refreshToken`. O usuário escolhe o contrato ativo se tiver acesso a mais de um.

Claims relevantes no JWT:

- `user_id`, `username`, `email`, `name`
- `contract_id`, `tenant_id`
- `system_application_name`, `company_name`
- `azp` (cliente OIDC autorizado)
- `role_name`, `role_id`
- `root` (`true` quando o role é root)
- `permission` (uma claim por recurso autorizado)

### Permissões

O `Archon` valida acesso por endpoint via atributo `[RequireAccess]`. A regra é:

1. Se o JWT tem claim `root: true`, libera.
2. Senão, verifica se o JWT tem claim `permission` igual a `<controller>.<action>` em camelCase. Se sim, libera.
3. Senão, retorna `403 Forbidden`.

Server-to-server (Archon → IdM, por exemplo) usa header `X-Integration-Secret`. O `RequireAccessAttribute` aceita os dois mecanismos no mesmo atributo.

### Gestão de usuários

O Kanvas não tem CRUD próprio de usuário. A tela `Sistema → Geral → Usuários` (visível apenas para `root`) usa `IdentityUsersClient` do `Archon`, que chama o `IdentityManagement` via `IntegrationSecret`. Operações disponíveis:

- listar usuários do contrato ativo;
- criar usuário e atribuir um papel do contrato (operação atômica);
- alterar o papel do usuário no contrato;
- ativar/desativar usuário globalmente.

A tela é um componente plugável exportado pelo `archon-ui` (`UsersManagementPage`); qualquer outro sistema que use `Archon` ganha a mesma funcionalidade ao adicionar a rota.

## Integrações externas e automações

O Kanvas se integra ao mundo externo através do `IntegrationPlataform`. A agência cadastra integrações (por exemplo, um conector de e-mail SMTP, um conector com SendGrid, um conector com Make/Zapier) e o Kanvas dispara pipelines configurados.

### Cadastro de integrações

Cada integração registrada tem:

- `name`, `description`, `baseUrl` (quando aplicável);
- `parameters` (chaves de configuração; algumas marcadas como `secret`);
- `isActive`.

Integrações padrão usadas pelo Kanvas:

- `identity-management`: ponte server-to-server com o IdM (criar/listar usuários);
- `integration-platform`: ponte server-to-server com o IntegrationPlataform (executar pipelines);
- conectores de e-mail (envio transacional para a marca, alertas internos);
- outros conectores que a agência queira plugar.

### Automações

A aba `Sistema → Comercial/Operação` expõe ações que disparam eventos. A aba `Configuração → Comercial → Automações` permite vincular um evento a um pipeline:

| Evento (Kanvas) | Quando dispara |
|---|---|
| `proposal_sent` | Proposta gerada e enviada para a marca |
| `proposal_viewed` | Marca abriu o link público |
| `proposal_approved` | Marca aprovou a proposta |
| `opportunity_won` | Oportunidade movida para estágio de sucesso |
| `opportunity_lost` | Oportunidade movida para estágio de perda |
| `follow_up_overdue` | Follow-up vencido sem conclusão |
| `campaign_created` | Campanha criada (geralmente após `proposal_approved`) |
| `deliverable_pending_approval` | Entrega pronta aguardando marca |
| `deliverable_approved` | Marca aprovou a entrega |
| `deliverable_published` | Entrega marcada como publicada |
| `financial_entry_overdue` | Lançamento financeiro vencido |

Cada automação injeta um payload na execução do pipeline (variáveis no formato Mustache `{{proposal.name}}`, `{{brand.name}}`, etc.) que o `IntegrationPlataform` interpola antes de chamar o conector externo.

## Notificações

O Kanvas tem notificações in-app baseadas no padrão do `Archon`:

- a tabela `notifications` no banco do tenant guarda eventos relevantes para cada usuário;
- a navbar exibe um sino com badge de não-lidas;
- o frontend faz polling a cada 60 segundos para atualizar o sino sem reload da página;
- ao clicar, o usuário vê a lista, marca como lida e pode navegar para a entidade relacionada.

Eventos que geram notificação (Fase 1): proposta aprovada/recusada, deliverable aprovado/recusado, oportunidade aprovada internamente, atribuição de creator a campanha, lançamento financeiro vencido. Outros eventos podem ser ligados via automação.

## Auditoria

Toda alteração em entidades persistidas pelo `Archon.CrudService` gera, em transação:

- uma linha em `auditentries` com `entityname`, `entityid`, `changedat`, `changedby`, `correlationid` e tipo de operação;
- uma linha em `auditpropertychanges` por propriedade modificada, com valor antigo e novo serializados.

A aba `Auditoria` expõe esse histórico de forma legível, com filtros por entidade, usuário e período.

## Stack técnica

### Backend

- .NET 10 (`AgencyCampaign.Api/Application/Domain/Infrastructure/Testing`)
- Entity Framework Core 10 sobre PostgreSQL
- FluentMigrator para migrations
- Hangfire para jobs em background
- PDFsharp + MigraDoc (MIT) para geração de PDF de propostas
- Scrutor para auto-discovery de serviços
- Localization nativa do ASP.NET Core (`pt-BR`, `en-US`, `es-AR`)

### Frontend

- React 19, Vite 8, TypeScript 5.9
- Tailwind CSS 3
- Radix UI (componentes acessíveis)
- Nivo para gráficos (`@nivo/line`, `@nivo/bar`, `@nivo/pie`)
- React Joyride para tour pelo sistema
- React Router DOM
- Cliente HTTP central com refresh token automático e loader global

### Banco de dados

- PostgreSQL 17
- Banco por tenant
- Auditoria automática
- Migrations idempotentes via FluentMigrator

### Infra

- Docker Compose em VPS único
- Containers separados para `kanvas-api` e `kanvas-web`
- Nginx como proxy reverso, certificados via Certbot
- GitHub Actions para CI/CD com deploy automático

## Estrutura do repositório

```
agency-campaign-os/
|-- AgencyCampaign/
|   |-- AgencyCampaign.Api/             Bootstrap HTTP, controllers
|   |-- AgencyCampaign.Application/     Requests, contratos de servico
|   |-- AgencyCampaign.Domain/          Entidades, regras de dominio
|   |-- AgencyCampaign.Infrastructure/  EF, migrations, services, clients
|   |-- AgencyCampaign.Web/             SPA React (Vite)
|   |-- AgencyCampaign.Testing/         Testes unitarios e de integracao
|   `-- AgencyCampaign.slnx
`-- deploy/                              Docker Compose de producao
```

A solução referencia diretamente o `Archon.Framework` no monorepo (caminho relativo). O frontend referencia o pacote `archon-ui` via `file:../../../../frameworks/archon-ui`.

## Como rodar localmente

### Pré-requisitos

- .NET 10 SDK
- Node.js 20 LTS
- PostgreSQL 17 (local ou Docker)
- Acesso a uma instância rodando do `IdentityManagement` (local ou compartilhada)

### Backend

1. Configurar `AgencyCampaign.Api/appsettings.Development.json` com:
   - `TenantDatabases:default:ConnectionString` apontando para o Postgres local;
   - `Jwt:Issuer` e `Jwt:Audience` coerentes com o IdM em uso (`identity-management` e `agency-campaign` são os defaults);
   - `IdentityManagement:Authority` apontando para o IdM (ex.: `https://auth.mainstay.com.br`);
   - `RunMigrations: true` para aplicar migrations no boot.
2. Criar o banco vazio (PostgreSQL) — as migrations criam o schema.
3. `dotnet run --project AgencyCampaign/AgencyCampaign.Api`. A API sobe na porta padrão (8080) com OpenAPI em `/openapi/v1.json`.

### Frontend

1. Configurar `AgencyCampaign/AgencyCampaign.Web/.env`:
   - `VITE_API_BASE_URL=http://localhost:8080/api`
   - `VITE_IDENTITY_MANAGEMENT_URL=https://auth.mainstay.com.br` (ou o IdM local)
   - `VITE_OIDC_CLIENT_ID=<client_id_kanvas>`
2. `npm install`
3. `npm run dev` na pasta `AgencyCampaign.Web`. O frontend sobe em `http://localhost:5173`.

### Login local

O fluxo de login redireciona para o IdM. Para desenvolvimento, registre o `client_id` do Kanvas no IdM com `redirect_uri` apontando para `http://localhost:5173/callback`.

## Deploy em produção

O deploy é feito por GitHub Actions ao push na `main`. O workflow:

1. faz checkout do `archon-framework` e `archon-ui` no mesmo runner;
2. compila o backend (`AgencyCampaign.Api` + `Archon.*`) em imagem Docker;
3. compila o frontend (`AgencyCampaign.Web` + `archon-ui` rebuild) em imagem Docker servida por Nginx;
4. publica as duas imagens em `ghcr.io/<owner>/agency-campaign-api:latest` e `agency-campaign-web:latest`;
5. copia o `docker-compose.prod.yml` para o VPS;
6. executa `docker compose pull && docker compose up -d` no VPS.

O arquivo `appsettings.Production.json` vive no servidor (`/var/www/agency-campaign/config/`) e é montado como volume read-only no container da API. Não está no git por conter senhas reais.

Detalhes adicionais sobre rotação de segredos, backup e SSH: ver memória interna do projeto.

## Convenções de projeto

- **Idioma**: código em inglês (classes, métodos, variáveis); commits, mensagens ao usuário e documentação em pt-BR; logs técnicos em inglês.
- **Banco**: nomes de tabelas, colunas e índices em minúsculo, sem underscore (`opportunitysourceid`, `responsibleuserid`).
- **Migrations**: idempotentes (`if exists` antes de criar/dropar), com `Up` e `Down` coerentes.
- **Código**: KISS, sem over-engineering, comentários só quando o porquê for não-óbvio.
- **Sem emojis** em código, commits, documentação ou interface.
- **Commits** com prefixo semântico em pt-BR: `feat`, `fix`, `refactor`, `chore`, `docs`.

Mais detalhes em `/.claude/CLAUDE.md` e `.playbook/`.
