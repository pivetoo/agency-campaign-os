import { test, expect } from '../fixtures/test'
import { env } from '../fixtures/env'
import { createProposalForNewOpportunity, generateShareLinkToken, publicProposal, publicProposalDecision, opportunityDetail, rowWithText, sendProposalViaModal } from '../fixtures/helpers'

// Cobre o A1: quando o cliente aceita a proposta no link público, a oportunidade
// vinculada é fechada como ganha automaticamente. Também exercita o aceite público
// (que o spec 03 não cobre, parando na abertura do /p/:token).

test.describe('Proposta - aceite público fecha a oportunidade (A1)', () => {
  test('cliente aceita no link público e a oportunidade fecha como ganha', async ({ page, browser }) => {
    const stamp = Date.now()
    const oppName = `E2E AcceptWin ${stamp}`

    // oportunidade + proposta + envio + share link
    await createProposalForNewOpportunity(page, oppName)
    await sendProposalViaModal(page)
    const token = await generateShareLinkToken(page)

    // cliente aceita no contexto anônimo (sem auth)
    const anonContext = await browser.newContext()
    const anon = await anonContext.newPage()
    await anon.goto(`${env.baseURL}/p/${token}`)
    await expect(publicProposal.page(anon)).toBeVisible({ timeout: 20_000 })
    await publicProposalDecision.nameInput(anon).fill('Cliente E2E')
    await publicProposalDecision.acceptButton(anon).click()
    await expect(publicProposalDecision.accepted(anon)).toBeVisible({ timeout: 20_000 })
    await anonContext.close()

    // back-office: a oportunidade vinculada deve estar fechada (ganha)
    await page.goto('/comercial/oportunidades')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

    const targetRow = rowWithText(page, oppName).first()
    await expect(targetRow).toBeVisible({ timeout: 15_000 })
    const openBtn = targetRow.locator('button').filter({ hasText: /Abrir/i }).first()
    if (await openBtn.count()) {
      await openBtn.click()
    } else {
      await targetRow.dblclick()
    }
    await page.waitForURL(/\/comercial\/oportunidades\/\d+/, { timeout: 15_000 })

    await expect
      .poll(async () => opportunityDetail.currentStage(page).getAttribute('data-closed'), { timeout: 20_000 })
      .toBe('true')
  })
})
