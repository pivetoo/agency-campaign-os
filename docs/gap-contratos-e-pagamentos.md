# Gap: Assinatura Digital de Contrato + Antecipação de Pagamento

Documento de gap funcional. Lista o que falta no Kanvas para cobrir o que hoje é o problema #1 das agências de marketing de influência (e o que faz a Pora ser referência no segmento).

## Contexto

Agências de influência operam num fluxo onde:

1. Marca aprova campanha e paga depois (NET 30, 60 ou 90).
2. Creator entrega o conteúdo combinado.
3. Creator quer receber rápido (idealmente na entrega, não em 60 dias).
4. Agência fica no meio, tem que adiantar do próprio caixa ou perder o creator.

Os dois problemas operacionais que mais doem:

- **Contrato**: gerar, enviar, assinar, arquivar e auditar contrato com cada creator de cada campanha.
- **Pagamento**: coletar dados bancários e NF, calcular splits, antecipar valor, fazer repasse, conciliar.

Hoje o Kanvas não cobre nenhum dos dois.

## Estado atual do Kanvas

Olhando o domínio em `AgencyCampaign.Domain`:

- `Campaign`, `CampaignCreator`, `CampaignDeliverable` — modelagem operacional pronta.
- `Proposal` + `ProposalShareLink` + `ProposalView` — fluxo comercial pronto até a aprovação da proposta.
- `FinancialAccount`, `FinancialEntry`, `FinancialSubcategory`, `FinancialReport` — financeiro interno da agência pronto.
- **Não existe**: agregado de Contrato, agregado de Pagamento ao creator, integração com assinatura digital, integração com gateway financeiro/antecipação.

## O que falta — Contrato Digital

### Domínio (novo)

- Agregado `Contract` (raiz) com:
  - Vínculos: `CampaignId`, `CampaignCreatorId`, `BrandId`, `AgencyId` (tenant).
  - Status: `Draft`, `PendingSignature`, `PartiallySigned`, `Signed`, `Cancelled`, `Expired`.
  - Versão atual + histórico (`ContractVersion`).
  - Variáveis de preenchimento (valores, prazos, escopo, exclusividade, cláusula de uso de imagem).
  - `ContractSignature` por signatário (creator, agência, marca quando aplicável) com timestamp, IP, hash do documento assinado, provider externo.
  - Referência ao PDF gerado e ao PDF assinado final.
- `ContractTemplate` (raiz) com placeholders `{{}}` reaproveitando o padrão já usado em `EmailTemplate`.
- Value objects: `SignatureProvider`, `SignerRole`, `ContractStatus`.

### Application

- `ContractTemplateService` — CRUD de templates por agência.
- `ContractService` — gerar contrato a partir de template + dados da campanha/creator, enviar para assinatura, receber webhook de status, arquivar.
- `ContractSignatureWebhookHandler` — receber callbacks do provider (D4Sign, Clicksign, ZapSign, Autentique, Docusign).
- Geração de PDF: pode reusar `ProposalPdfService` como referência arquitetural.

### Infrastructure

- Cliente para provider de assinatura. **Decisão necessária**: qual provider integrar primeiro.
  - Candidatos brasileiros com API: **ZapSign** (barato, API simples), **Clicksign** (referência no jurídico), **D4Sign** (popular em PMEs), **Autentique** (free tier generoso).
  - Recomendação: começar com **ZapSign** ou **Autentique** pelo custo e API REST direta.
- Configuração: API key por tenant (ou global da agência-mãe), via `IntegrationSecret` do Archon.
- Storage do PDF: hoje o sistema não tem blob storage configurado. Precisa decidir entre filesystem local, S3/MinIO ou storage do próprio provider.

### API

- `ContractsController` — CRUD, gerar, enviar, baixar PDF, listar por campanha/creator.
- `ContractTemplatesController` — CRUD de templates.
- `ContractWebhooksController` — endpoint público (sem `[RequireAccess]`) para callback do provider, autenticado por secret/HMAC do provider.
- `ContractPublicController` — link público para o creator visualizar o contrato sem login.

### Frontend

- Módulo `Contracts/` em `AgencyCampaign.Web`.
- Lista de contratos por campanha, status visual (rascunho → enviado → assinado).
- Editor de template com preview de variáveis substituídas.
- Tela de detalhe com timeline de eventos (gerado → enviado → visualizado → assinado).
- Aba "Contrato" dentro da página de Campaign e dentro da página de CampaignCreator.

### Integrações cruzadas

- Quando uma `Proposal` é aprovada, sugerir geração automática de `Contract` correspondente (via `Automations` que já existe).
- Quando um `Contract` é assinado por todas as partes, disparar evento que destrava `Payment` (próxima seção).

## O que falta — Pagamento ao Creator e Antecipação

### Domínio (novo)

- Agregado `CreatorPayment` (raiz) com:
  - Vínculos: `CampaignCreatorId`, `ContractId` (opcional mas recomendado), `CreatorId`.
  - Status: `Pending`, `AwaitingDocuments`, `AwaitingInvoice`, `Scheduled`, `Anticipated`, `Paid`, `Failed`, `Cancelled`.
  - Valor bruto, descontos, valor líquido, data prevista, data efetiva.
  - Método: PIX, TED, conta digital interna.
  - `CreatorPaymentEvent` para auditoria de cada transição.
- `CreatorBankAccount` (entity de `Creator`): chave PIX, banco, agência, conta, tipo, CPF/CNPJ titular. Hoje provavelmente não está modelado.
- `CreatorInvoice` (NF do creator): número, série, valor, arquivo, data emissão, situação fiscal.
- Value objects: `PaymentStatus`, `PaymentMethod`, `AnticipationStatus`.

### Application

- `CreatorPaymentService` — criar pagamento a partir de campanha entregue, validar documentação (NF + dados bancários), agendar.
- `CreatorBankAccountService` — onboarding e atualização de dados bancários do creator (com link público para o creator preencher sozinho).
- `InvoiceCollectionService` — pedir NF, receber upload, validar.
- `PaymentExecutionService` — executar a transferência via gateway.
- `AnticipationService` — calcular taxa, aplicar adiantamento, liquidar quando a marca pagar.

### Infrastructure

- **Gateway de pagamento PIX/TED**. Candidatos:
  - **Asaas** — popular em PMEs brasileiras, API simples, suporta split.
  - **Iugu** — split nativo, conhecido em SaaS.
  - **Pagar.me** (Stone) — robusto, mais complexo.
  - **Celcoin** — BaaS, controle total mas exige mais compliance.
  - Recomendação: começar com **Asaas** pelo custo e onboarding rápido.
- **Antecipação** é mais complexa. Caminhos:
  - Não fazer antecipação própria — apenas integrar com FIDC/banco parceiro que faz isso (operação financeira regulada).
  - Repassar do caixa da agência (a agência já tem o dinheiro) — apenas controlar o fluxo, sem produto financeiro próprio.
  - Recomendação: **MVP só faz repasse normal**. Antecipação fica para uma v2 com parceiro financeiro, porque envolve risco de crédito, regulação BACEN e capital de giro.
- Storage de NFs e comprovantes (mesmo problema do contrato — precisa decidir blob storage).

### API

- `CreatorPaymentsController` — listar, agendar, cancelar, marcar como pago, anexar comprovante.
- `CreatorBankAccountsController` — CRUD interno + endpoint público para creator preencher via link com token.
- `CreatorInvoicesController` — upload, validação, status fiscal.
- `PaymentWebhooksController` — callbacks do gateway.

### Frontend

- Módulo `Payments/` (ou expansão de `Financial/`).
- Visão por campanha: quanto a agência deve a cada creator, status documentação, status pagamento.
- Tela do creator (pública, com token) para preencher dados bancários e enviar NF.
- Conciliação: relacionar entrada da marca com saídas para creators (repasse).

### Compliance e jurídico (não pode esquecer)

- LGPD: dados bancários e CPF do creator são dados pessoais sensíveis. Criptografia em repouso, masking em logs, política de retenção.
- Reter ISS / IR quando aplicável (depende do regime do creator — PF, MEI, ME).
- Emissão de informe de rendimentos para o creator no fim do ano.
- Termos de uso e contrato de prestação de serviço entre agência e Kanvas que deixe claro que **o Kanvas não é instituição financeira**.

## Roadmap sugerido (ordem de implementação)

1. **Contratos com assinatura digital (MVP)** — provider único (ZapSign ou Autentique), template básico, fluxo manual de gerar e enviar.
2. **Onboarding do creator com dados bancários** — link público, formulário, criptografia.
3. **CreatorPayment manual** — agência registra que pagou, sistema só controla status. Sem gateway ainda.
4. **Integração com gateway (Asaas)** — pagamento via PIX disparado pelo sistema.
5. **Coleta automática de NF** — pedir, receber, validar.
6. **Conciliação financeira** — ligar `FinancialEntry` (entrada da marca) com `CreatorPayment` (saída pro creator).
7. **Antecipação** (v2) — só depois de tudo acima funcionar bem, e com parceiro financeiro definido.

Cada etapa entrega valor isolado. Se parar no item 3, agência já tem mais do que tem hoje em planilha.

## Decisões pendentes (precisam ser tomadas antes de começar)

1. **Blob storage** — onde guardar PDFs assinados, NFs, comprovantes. Filesystem local, MinIO no VPS, S3, ou storage do provider.
2. **Provider de assinatura digital** — ZapSign vs Autentique vs Clicksign.
3. **Gateway de pagamento** — Asaas vs Iugu.
4. **Modelo de antecipação** — fazer com capital próprio da agência, não fazer, ou plugar parceiro financeiro.
5. **Multi-provider ou single-provider** — suporta vários providers de assinatura/pagamento por tenant ou padroniza um só. Recomendação: padronizar um por categoria no MVP, abstrair via interface só quando houver demanda real.

## Esforço estimado (alto nível)

- Contratos completo (MVP): 3-5 semanas de dev focado.
- Pagamentos manual + gateway: 4-6 semanas.
- Conciliação e relatórios: 2-3 semanas.
- Total para fechar o gap até item 6 do roadmap: **10-14 semanas** de trabalho focado, sem antecipação.

---

# Como validar isso sem conhecer 5 donos de agência

Este é um problema separado, mas crítico. Antes de gastar 14 semanas construindo, valide.

## Onde achar donos de agência de influência no Brasil

**LinkedIn** é o caminho mais óbvio e que mais funciona:

- Pesquisa: `"agência de influência" OR "agência de influenciadores" OR "marketing de influência"` filtrando por Brasil e cargo "Founder", "CEO", "Sócio", "Diretor".
- Vai aparecer muita gente. Entre 50-200 perfis acessíveis em São Paulo, Rio, BH, Curitiba, Porto Alegre.
- Mande pedido de conexão com nota personalizada curta. Algo do tipo: *"Oi [nome], tô estudando o mercado de gestão operacional pra agências de influência (contratos, repasses, propostas) e queria fazer 4-5 perguntas se topar uma call de 20 min. Sem pitch, só aprender."* — sem mencionar o produto.
- Taxa de resposta realista: 10-20%. Mandando 50 mensagens, você consegue 5-10 calls. Isso é mais que suficiente.

**Comunidades específicas**:

- Grupos de Facebook tipo "Agências de Marketing de Influência Brasil".
- Comunidades no Telegram/WhatsApp de profissionais de influência (peça pra alguém te indicar).
- Eventos: **Influency Day**, **iLumini**, **Fórum de Marketing de Influência**, **Web Summit Rio**. Vão presencialmente, networking direto.
- Slack/Discord de creators (donos de agência circulam lá).

**Caminho indireto**:

- Procure influenciadores médios (10k-500k seguidores) no Instagram. Veja na bio se mencionam agência. Você descobre nome da agência → procura o dono no LinkedIn.
- Listas públicas: o **Meio & Mensagem** publica rankings de agências de influência. **Capterra Brasil** lista clientes de plataformas concorrentes (Pora, Influency).
- **Pora e Influency divulgam cases** no site deles — os clientes que aparecem nesses cases são exatamente seu ICP.

**Caminho frio mas eficaz**:

- Cold email. Pega o site de 30-50 agências, acha o email do fundador (ou usa Hunter.io, RocketReach), manda mensagem curta pedindo 20 minutos.
- Taxa de resposta menor que LinkedIn (5-10%) mas escala.

## Como conduzir as conversas (importante)

Não é demo. É entrevista. Regras:

1. **Não mostre o Kanvas**. Se mostrar, todo mundo elogia por educação e você não aprende nada.
2. **Pergunte sobre o passado, não sobre o futuro**. "Como foi o último contrato que você fechou com creator?" é melhor que "você usaria um sistema de contrato?". Comportamento passado prevê melhor que opinião sobre futuro.
3. **Foque em dor, não em solução**. "Qual a parte mais chata da operação hoje?" "O que você tá fazendo em planilha que devia tá em sistema?" "Qual foi a última vez que você perdeu dinheiro por falha operacional?"
4. **Pergunte o que eles já tentaram**. "Já testou Pora? Influency? Por que não usa?" — descobre se o mercado já rejeitou soluções e por quê.
5. **Pergunte sobre disposição a pagar**. "Se existisse algo que resolvesse X, quanto valeria pra você por mês?"

Se 5-7 conversas mostrarem que **contratos e repasses** são realmente as dores principais, você tem sinal forte. Se aparecer outra dor que você não esperava, melhor ainda — descobriu algo novo antes de codar.

## Meta realista

- 1 semana mandando mensagens no LinkedIn.
- 2-3 semanas conduzindo as calls (5-10 conversas).
- 1 semana sintetizando.

**Total: ~1 mês de validação antes de mais qualquer linha de código nesse gap.**

Se a validação confirmar a hipótese, você começa a construir contratos com convicção e talvez já com 1-2 agências dispostas a serem design partners (te ajudam a moldar o produto em troca de uso gratuito ou desconto vitalício). Isso é ouro.
