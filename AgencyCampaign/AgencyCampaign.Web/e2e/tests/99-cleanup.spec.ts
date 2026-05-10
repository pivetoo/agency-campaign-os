import { test, expect } from '@playwright/test'

/**
 * Spec de manutencao opt-in: remove entidades criadas pelos specs E2E.
 *
 * Roda apenas com E2E_CLEANUP=1 (npm run test:e2e:cleanup).
 *
 * Estrategia: usa o accessToken do storageState para chamar os endpoints
 * REST diretamente. Filtra por prefixo "E2E " no campo `name` (ou
 * `title`/`subject`) e DELETE em best-effort.
 *
 * Entidades sem endpoint de DELETE (brands, creators, campaigns, proposals)
 * sao listadas no resumo; precisam ser desativadas manualmente via UI.
 */

const SHOULD_RUN = process.env.E2E_CLEANUP === '1'
const PREFIX = process.env.E2E_CLEANUP_PREFIX ?? 'E2E '

interface CleanupResult {
  entity: string
  total: number
  matched: number
  deleted: number
  failed: number
  errors: string[]
}

test.describe.configure({ mode: 'serial' })

test.describe('Cleanup E2E artifacts (opt-in)', () => {
  test.skip(!SHOULD_RUN, 'Defina E2E_CLEANUP=1 para executar a limpeza')
  test.setTimeout(180_000)

  test(`remove registros com prefixo "${PREFIX}"`, async ({ page, baseURL }) => {
    await page.goto('/')
    await page.waitForLoadState('domcontentloaded')

    const token = await page.evaluate(() => localStorage.getItem('@Archon:accessToken'))
    expect(token, '@Archon:accessToken nao encontrado em localStorage').toBeTruthy()

    const ctx = page.request
    const headers = { Authorization: `Bearer ${token}` }
    const root = baseURL ?? 'https://kanvas.mainstay.com.br'
    const results: CleanupResult[] = []

    const extractItems = (body: unknown): unknown[] => {
      if (Array.isArray(body)) return body
      const data = (body as { data?: unknown })?.data
      if (Array.isArray(data)) return data
      const items = (data as { items?: unknown[] })?.items ?? (body as { items?: unknown[] })?.items
      return Array.isArray(items) ? items : []
    }

    async function cleanup({
      entity,
      listUrl,
      deleteUrl,
      nameFields = ['name', 'title', 'subject'],
    }: {
      entity: string
      listUrl: string
      deleteUrl: (id: number) => string
      nameFields?: string[]
    }) {
      const result: CleanupResult = { entity, total: 0, matched: 0, deleted: 0, failed: 0, errors: [] }
      try {
        const resp = await ctx.get(`${root}${listUrl}`, { headers })
        if (!resp.ok()) {
          result.errors.push(`list ${resp.status()}`)
          results.push(result)
          return
        }
        const body = await resp.json()
        const items = extractItems(body) as Array<Record<string, unknown>>
        result.total = items.length

        for (const item of items) {
          const id = item.id as number | undefined
          if (typeof id !== 'number') continue
          const matchedField = nameFields
            .map((f) => item[f])
            .find((v) => typeof v === 'string' && (v as string).startsWith(PREFIX))
          if (!matchedField) continue
          result.matched++

          const dr = await ctx.delete(`${root}${deleteUrl(id)}`, { headers })
          if (dr.ok()) {
            result.deleted++
          } else {
            result.failed++
            const text = await dr.text().catch(() => '')
            result.errors.push(`id=${id} -> ${dr.status()} ${text.slice(0, 80)}`)
          }
        }
      } catch (err) {
        result.errors.push(String(err).slice(0, 120))
      }
      results.push(result)
    }

    // Ordem: dependentes primeiro
    // 1) Oportunidades — apaga em cascata comentarios/negociacoes/follow-ups
    //    Usa filtro `search` para nao pegar paginacao parcial.
    await cleanup({
      entity: 'Opportunities',
      listUrl: `/api/Opportunities/Get?pageSize=500&search=${encodeURIComponent(PREFIX)}`,
      deleteUrl: (id) => `/api/Opportunities/${id}`,
    })

    // 2) Templates de documento (E2E Template Doc...)
    await cleanup({
      entity: 'CampaignDocumentTemplates',
      listUrl: '/api/CampaignDocumentTemplates/Get?pageSize=500',
      deleteUrl: (id) => `/api/CampaignDocumentTemplates/Delete/${id}`,
    })

    // 3) Templates de e-mail
    await cleanup({
      entity: 'EmailTemplates',
      listUrl: '/api/EmailTemplates/Get?includeInactive=true',
      deleteUrl: (id) => `/api/EmailTemplates/Delete/${id}`,
    })

    // 4) Templates de proposta
    await cleanup({
      entity: 'ProposalTemplates',
      listUrl: '/api/ProposalTemplates/Get?includeInactive=true',
      deleteUrl: (id) => `/api/ProposalTemplates/${id}`,
    })

    // 5) Blocos de proposta
    await cleanup({
      entity: 'ProposalBlocks',
      listUrl: '/api/ProposalBlocks/Get?includeInactive=true',
      deleteUrl: (id) => `/api/ProposalBlocks/${id}`,
    })

    // 6) Cadastros simples
    await cleanup({
      entity: 'OpportunitySources',
      listUrl: '/api/OpportunitySources/Get?includeInactive=true',
      deleteUrl: (id) => `/api/OpportunitySources/Delete/${id}`,
    })
    await cleanup({
      entity: 'OpportunityTags',
      listUrl: '/api/OpportunityTags/Get?includeInactive=true',
      deleteUrl: (id) => `/api/OpportunityTags/Delete/${id}`,
    })
    await cleanup({
      entity: 'DeliverableKinds',
      listUrl: '/api/DeliverableKinds/Get',
      deleteUrl: (id) => `/api/DeliverableKinds/Delete/${id}`,
    })
    await cleanup({
      entity: 'CampaignCreatorStatuses',
      listUrl: '/api/CampaignCreatorStatuses/Get',
      deleteUrl: (id) => `/api/CampaignCreatorStatuses/Delete/${id}`,
    })
    await cleanup({
      entity: 'Platforms',
      listUrl: '/api/Platforms/Get',
      deleteUrl: (id) => `/api/Platforms/Delete/${id}`,
    })
    await cleanup({
      entity: 'CommercialPipelineStages',
      listUrl: '/api/CommercialPipelineStages/Get',
      deleteUrl: (id) => `/api/CommercialPipelineStages/Delete/${id}`,
    })
    await cleanup({
      entity: 'FinancialAccounts',
      listUrl: '/api/FinancialAccounts/Get',
      deleteUrl: (id) => `/api/FinancialAccounts/Delete/${id}`,
    })
    await cleanup({
      entity: 'FinancialSubcategories',
      listUrl: '/api/FinancialSubcategories/Get',
      deleteUrl: (id) => `/api/FinancialSubcategories/Delete/${id}`,
    })

    // Imprime resumo
    const lines: string[] = ['', `=== Cleanup summary (prefix="${PREFIX}") ===`]
    let totalDeleted = 0
    let totalFailed = 0
    for (const r of results) {
      lines.push(
        `  ${r.entity.padEnd(30)} matched=${String(r.matched).padStart(3)} deleted=${String(r.deleted).padStart(3)} failed=${String(r.failed).padStart(3)} total=${r.total}`,
      )
      if (r.errors.length > 0) {
        for (const e of r.errors.slice(0, 5)) lines.push(`    ! ${e}`)
        if (r.errors.length > 5) lines.push(`    ! ...mais ${r.errors.length - 5} erros`)
      }
      totalDeleted += r.deleted
      totalFailed += r.failed
    }
    lines.push('')
    lines.push(`Total deletado: ${totalDeleted}; Total falhou: ${totalFailed}`)
    lines.push('')
    lines.push('Entidades sem DELETE direto (Brands, Creators, Campaigns, Proposals)')
    lines.push('precisam ser desativadas manualmente na UI.')
    console.log(lines.join('\n'))
  })
})
