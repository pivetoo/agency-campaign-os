import { test, expect } from '../fixtures/test'
import { crud, rowWithText } from '../fixtures/helpers'

// Cobre: detalhe da oportunidade + tabs internas + criar follow-up

test.describe('Oportunidade - detalhe e follow-up', () => {
  test('cria oportunidade, abre detalhe, adiciona follow-up', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const oppName = `E2E OppFollow ${stamp}`
    const followUpSubject = `E2E FollowUp ${stamp}`

    // 1) cria oportunidade
    await page.goto('/comercial/oportunidades')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const oppModal = page.getByRole('dialog').filter({ hasText: /Nova oportunidade/i })
    await expect(oppModal).toBeVisible({ timeout: 10_000 })
    await oppModal.getByLabel(/Nome da oportunidade/i).fill(oppName)
    await oppModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await oppModal.locator('#opportunity-estimated-value').fill('10000')
    await oppModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(oppModal).toBeHidden({ timeout: 15_000 })

    // 2) abrir detalhe — botao "Abrir" da linha (botao com texto "Abrir")
    const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

    const row = rowWithText(page, oppName).first()
    await expect(row).toBeVisible({ timeout: 15_000 })
    const openBtn = row.locator('button').filter({ hasText: /Abrir/i }).first()
    if (await openBtn.count()) {
      await openBtn.click()
    } else {
      // fallback: doubleclick na linha
      await row.dblclick()
    }
    await page.waitForURL(/\/comercial\/oportunidades\/\d+/, { timeout: 15_000 })

    // 3) clicar na tab "Atividades" / "Follow-ups"
    const activityTab = page.getByRole('tab', { name: /Atividades|Follow|Acompanhamento/i }).first()
    if (await activityTab.count()) {
      await activityTab.click()
      await page.waitForTimeout(300)
    }

    // 4) criar follow-up
    await page.getByRole('button', { name: /Novo follow-up|Novo follow/i }).first().click()
    const fuModal = page.getByRole('dialog').filter({ hasText: /Novo follow-up/i })
    await expect(fuModal).toBeVisible({ timeout: 10_000 })

    const fc = (label: string) =>
      fuModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()

    await fc('Assunto').locator('input').first().fill(followUpSubject)
    const tomorrow = new Date(); tomorrow.setDate(tomorrow.getDate() + 3)
    await fc('Prazo').locator('input').first().fill(tomorrow.toISOString().slice(0, 10))

    await fuModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(fuModal).toBeHidden({ timeout: 15_000 })

    // 5) follow-up aparece
    await expect(page.getByText(followUpSubject).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })
})
