# Testes E2E do Kanvas

Testes de ponta a ponta com Playwright, simulando um QA real abrindo o navegador, autenticando e usando a aplicacao.

## Setup (uma vez)

```bash
cd AgencyCampaign/AgencyCampaign.Web
npm install
npm run test:e2e:install
cp e2e/.env.example e2e/.env
```

Edite `e2e/.env` com as credenciais do usuario de teste.

## Rodar

```bash
# headless, terminal
npm run test:e2e

# com UI interativa do Playwright (recomendado para debugar)
npm run test:e2e:ui

# com browser visivel (modo "QA assistindo")
npm run test:e2e:headed

# abrir relatorio HTML do ultimo run
npm run test:e2e:report
```

## O que cobre

| Spec | Fluxo |
|---|---|
| `auth.setup.ts` | Login OIDC contra IdentityManagement, salva storageState |
| `01-smoke.spec.ts` | Dashboard renderiza KPIs apos login |
| `02-pipeline.spec.ts` | Cria oportunidade nova, arrasta entre estagios, valida persistencia apos reload |
| `03-proposta.spec.ts` | Gera share link em proposta existente, abre em contexto sem auth, valida render publico |

## Ambiente alvo

Por padrao roda contra `https://kanvas.mainstay.com.br`. Para apontar para outro ambiente, mude `E2E_BASE_URL` no `.env`.

## Quando algo falhar

- Trace e video sao salvos em `e2e/test-results/` apenas quando o teste falha.
- Relatorio HTML em `e2e/playwright-report/index.html`.
- `npm run test:e2e:report` abre o relatorio no navegador.

## Limites conhecidos

- Spec da proposta usa a primeira proposta cadastrada no ambiente. Se nao houver propostas, o teste e pulado (`skip`) com mensagem clara.
- Roda em producao por padrao. Cada execucao cria uma oportunidade real chamada `E2E QA Lead <timestamp>`. Vale criar um filtro/limpeza periodica ou apontar para um ambiente de staging.
