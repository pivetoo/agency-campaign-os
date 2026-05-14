import { test, expect } from '@playwright/test'
import { env } from '../fixtures/env'
import { expectPageTitle, proposal, publicProposal } from '../fixtures/helpers'

test.describe('Proposta - share link publico', () => {
  test('gera share link em proposta existente e abre em contexto sem auth', async ({ page, browser }) => {
    await page.goto('/comercial/propostas')
    await expectPageTitle(page, /Propostas/i)

    const firstRow = page.locator('table tbody tr').first()
    const hasRow = await firstRow.isVisible().catch(() => false)
    test.skip(!hasRow, 'Sem propostas cadastradas no ambiente para testar share link.')

    await firstRow.dblclick()
    await expect(page).toHaveURL(/\/comercial\/propostas\/\d+/, { timeout: 20_000 })

    const generateBtn = proposal.generateLinkButton(page)
    await expect(generateBtn).toBeVisible({ timeout: 15_000 })

    // intercepta a resposta da API que cria o share link e extrai o token
    const sharePromise = page.waitForResponse(
      (response) =>
        /\/share-links\/Create/i.test(response.url()) && response.request().method() === 'POST',
      { timeout: 15_000 }
    )
    await generateBtn.click()
    const shareResponse = await sharePromise
    expect(shareResponse.ok(), `share-links/Create retornou ${shareResponse.status()}`).toBeTruthy()

    const body = await shareResponse.json()
    const token: string | undefined = body?.data?.token ?? body?.token
    expect(token, 'esperava token na resposta de share-links/Create').toBeTruthy()

    // abrir em contexto SEM auth para validar acesso publico
    const anonymousContext = await browser.newContext()
    const anonymousPage = await anonymousContext.newPage()
    await anonymousPage.goto(`${env.baseURL}/p/${token}`)
    await expect(publicProposal.page(anonymousPage)).toBeVisible({ timeout: 20_000 })
    expect(anonymousPage.url()).toContain('/p/')
    await anonymousContext.close()
  })
})
