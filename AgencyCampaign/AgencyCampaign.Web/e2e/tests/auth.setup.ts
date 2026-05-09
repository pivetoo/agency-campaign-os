import { test as setup, expect } from '@playwright/test'
import * as path from 'node:path'
import * as fs from 'node:fs'
import { env, assertCredentials } from '../fixtures/env'

const storageStatePath = path.join(__dirname, '..', '.auth', 'user.json')

setup('autenticar via OIDC e salvar storageState', async ({ page }) => {
  assertCredentials()

  fs.mkdirSync(path.dirname(storageStatePath), { recursive: true })

  await page.goto('/')

  await page.waitForLoadState('domcontentloaded')

  await expect
    .poll(async () => page.url(), {
      timeout: 20_000,
      message: 'Esperava redirect do Kanvas para o IdentityManagement',
    })
    .not.toContain('kanvas.mainstay.com.br')

  const emailField = page
    .locator('input[type="email"], input[name="email"], input[name="username"], input#email')
    .first()
  const passwordField = page.locator('input[type="password"]').first()

  await emailField.waitFor({ state: 'visible', timeout: 15_000 })
  await emailField.fill(env.user)
  await passwordField.fill(env.password)

  const submitButton = page
    .locator('button[type="submit"], input[type="submit"], button:has-text("Entrar"), button:has-text("Login")')
    .first()
  await submitButton.click()

  await page.waitForURL(/kanvas\.mainstay\.com\.br/, { timeout: 30_000 })
  await expect(page).toHaveURL(/kanvas\.mainstay\.com\.br\//, { timeout: 30_000 })

  await expect(page.getByText(/Campanhas ativas|Marcas|Creators/i).first()).toBeVisible({ timeout: 30_000 })

  await page.context().storageState({ path: storageStatePath })
})
