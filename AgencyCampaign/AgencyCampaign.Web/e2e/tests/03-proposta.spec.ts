import { test, expect } from '@playwright/test'
import { env } from '../fixtures/env'

test.describe('Proposta - share link publico', () => {
  test('gera share link em proposta existente e abre em contexto sem auth', async ({ page, browser }) => {
    await page.goto('/comercial/propostas')
    await expect(page.getByRole('heading', { name: /Propostas/i })).toBeVisible({ timeout: 20_000 })

    // pega a primeira linha da tabela; se nao houver, encerra com skip
    const firstRow = page.locator('table tbody tr').first()
    const hasRow = await firstRow.isVisible().catch(() => false)
    test.skip(!hasRow, 'Sem propostas cadastradas no ambiente para testar share link.')

    await firstRow.dblclick()
    await expect(page).toHaveURL(/\/comercial\/propostas\/\d+/, { timeout: 20_000 })

    const shareTab = page.getByRole('tab', { name: /Compartilhar|Link|P[uú]blico/i }).first()
    if (await shareTab.count()) {
      await shareTab.click()
    }

    const generateBtn = page.getByRole('button', { name: /Gerar link p[uú]blico/i }).first()
    await expect(generateBtn).toBeVisible({ timeout: 15_000 })
    await generateBtn.click()

    // o token aparece como /p/<token> em algum lugar visivel da pagina (input, link ou texto)
    const tokenLocator = page.locator('text=/\\/p\\/[A-Za-z0-9_-]{8,}/').first()
    await expect(tokenLocator).toBeVisible({ timeout: 15_000 })
    const tokenText = await tokenLocator.innerText()
    const match = tokenText.match(/\/p\/([A-Za-z0-9_-]{8,})/)
    expect(match, 'esperava extrair token /p/<token> da pagina').not.toBeNull()
    const token = match![1]

    // abrir em contexto SEM auth para validar acesso publico
    const anonymousContext = await browser.newContext()
    const anonymousPage = await anonymousContext.newPage()
    await anonymousPage.goto(`${env.baseURL}/p/${token}`)
    await expect(anonymousPage.locator('body')).toContainText(/Proposta|Aprovar|Aceitar|Total/i, { timeout: 20_000 })

    // garantir que a pagina nao redirecionou para login
    expect(anonymousPage.url()).toContain('/p/')
    await anonymousContext.close()
  })
})
