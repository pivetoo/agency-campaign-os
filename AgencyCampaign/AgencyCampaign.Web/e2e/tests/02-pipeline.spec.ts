import { test, expect } from '@playwright/test'

test.describe('Pipeline comercial - criar oportunidade e arrastar entre estagios', () => {
  test('cria oportunidade e move para o proximo estagio', async ({ page }) => {
    const opportunityName = `E2E QA Lead ${Date.now()}`

    await page.goto('/comercial/pipeline')
    await expect(page.getByRole('heading', { name: /Pipeline Comercial/i })).toBeVisible({ timeout: 20_000 })

    // garantir que o board carregou (pelo menos 2 estagios para testar drag)
    const stageBadges = page.locator('section >> css=span.inline-flex')
    await expect.poll(async () => stageBadges.count(), { timeout: 15_000 }).toBeGreaterThanOrEqual(2)

    // abrir modal de novo lead
    await page.getByRole('button', { name: /Novo Lead|Cadastrar lead/i }).first().click()
    const modal = page.getByRole('dialog').filter({ hasText: /Nova oportunidade/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })

    // nome
    await modal.getByLabel(/Nome da oportunidade/i).fill(opportunityName)

    // marca (SearchableSelect: clicar no trigger e escolher a primeira opcao da lista)
    const brandTrigger = modal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first()
    await brandTrigger.click()
    const firstBrandOption = page.locator('[role="option"], [role="listbox"] [role="option"]').first()
    await expect(firstBrandOption).toBeVisible({ timeout: 10_000 })
    await firstBrandOption.click()

    // valor estimado (segundo Input numerico mais frequente)
    const valueInput = modal.locator(':text("Valor")').first().locator('..').locator('input').first()
    if (await valueInput.count()) {
      await valueInput.fill('5000')
    }

    // submit
    await modal.getByRole('button', { name: /Salvar|Criar|Confirmar/i }).first().click()

    // modal fecha
    await expect(modal).toBeHidden({ timeout: 15_000 })

    // card aparece no board
    const card = page.getByRole('button').filter({ hasText: opportunityName })
    await expect(card).toBeVisible({ timeout: 20_000 })

    // identificar a coluna de origem e a coluna alvo
    const sourceColumn = page.locator('section', { has: card })
    const allColumns = page.locator('main section[style*="border-top"]')
    const sourceIndex = await allColumns.evaluateAll(
      (cols, sourceEl) => cols.indexOf(sourceEl as HTMLElement),
      await sourceColumn.elementHandle()
    )
    expect(sourceIndex).toBeGreaterThanOrEqual(0)

    const targetIndex = sourceIndex === 0 ? 1 : 0
    const targetColumn = allColumns.nth(targetIndex)
    await expect(targetColumn).toBeVisible()

    // drag-and-drop HTML5 nativo — Playwright dragTo() nao dispara dataTransfer corretamente.
    // Disparamos os eventos manualmente com pausa entre eles para dar tempo ao React de processar
    // o setDraggedItem (state) antes do drop.
    const dispatchDragEvent = async (selector: string, eventName: 'dragstart' | 'dragend') => {
      await page.evaluate(
        ({ name, evt }) => {
          const cards = Array.from(document.querySelectorAll('main section[style*="border-top"] [draggable="true"]')) as HTMLElement[]
          const el = cards.find((card) => card.textContent?.includes(name))
          if (!el) throw new Error('card nao encontrado: ' + name)
          el.dispatchEvent(new DragEvent(evt, { bubbles: true, cancelable: true, dataTransfer: new DataTransfer() }))
        },
        { name: selector, evt: eventName }
      )
    }

    const dispatchDropEvent = async (idx: number, eventName: 'dragenter' | 'dragover' | 'drop') => {
      await page.evaluate(
        ({ index, evt }) => {
          const cols = Array.from(document.querySelectorAll('main section[style*="border-top"]')) as HTMLElement[]
          const el = cols[index]
          if (!el) throw new Error('coluna nao encontrada: ' + index)
          el.dispatchEvent(new DragEvent(evt, { bubbles: true, cancelable: true, dataTransfer: new DataTransfer() }))
        },
        { index: idx, evt: eventName }
      )
    }

    await dispatchDragEvent(opportunityName, 'dragstart')
    await page.waitForTimeout(80)
    await dispatchDropEvent(targetIndex, 'dragenter')
    await dispatchDropEvent(targetIndex, 'dragover')
    await page.waitForTimeout(80)
    await dispatchDropEvent(targetIndex, 'drop')
    await dispatchDragEvent(opportunityName, 'dragend')

    // o card deve estar agora dentro da coluna alvo
    await expect.poll(
      async () => targetColumn.getByText(opportunityName).count(),
      { timeout: 15_000, message: 'card deveria ter migrado para a coluna alvo' }
    ).toBeGreaterThan(0)

    // recarregar e validar persistencia
    await page.reload()
    await expect(page.getByRole('heading', { name: /Pipeline Comercial/i })).toBeVisible({ timeout: 20_000 })
    await expect(allColumns.nth(targetIndex).getByText(opportunityName)).toBeVisible({ timeout: 20_000 })
  })
})
