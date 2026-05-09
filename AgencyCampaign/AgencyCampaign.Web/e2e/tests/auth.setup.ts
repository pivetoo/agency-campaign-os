import { test as setup, expect } from '@playwright/test'
import * as path from 'node:path'
import * as fs from 'node:fs'
import { fileURLToPath } from 'node:url'
import { env, assertCredentials } from '../fixtures/env'

const here = path.dirname(fileURLToPath(import.meta.url))
const storageStatePath = path.join(here, '..', '.auth', 'user.json')

setup('autenticar via OIDC e salvar storageState', async ({ page }) => {
  assertCredentials()

  fs.mkdirSync(path.dirname(storageStatePath), { recursive: true })

  await page.goto('/')

  await page.waitForLoadState('domcontentloaded')

  await page.waitForURL(/auth\.mainstay\.com\.br/, { timeout: 20_000 })

  const emailField = page
    .locator('input[type="email"], input[name="email"], input[name="username"], input[placeholder="E-mail" i]')
    .first()
  const passwordField = page
    .locator('input[type="password"], input[name="password"], input[placeholder="Senha" i]')
    .first()

  await emailField.waitFor({ state: 'visible', timeout: 15_000 })
  await emailField.fill(env.user)
  await passwordField.fill(env.password)

  const submitButton = page.getByRole('button', { name: /Entrar|Login/i }).first()
  await submitButton.click()

  // tres caminhos possiveis: redirecionou pro Kanvas (ok), erro do IdM, ou nada acontece (timeout)
  const redirected = page.waitForURL(/kanvas\.mainstay\.com\.br/, { timeout: 30_000 })
  const idmError = page
    .locator('text=/credenciais|inv[aá]lid|incorret|n[aã]o possui|inativo|n[aã]o autorizado/i')
    .first()
    .waitFor({ state: 'visible', timeout: 30_000 })

  await Promise.race([redirected, idmError])

  if (page.url().includes('auth.mainstay.com.br')) {
    const errorText = await page
      .locator('text=/credenciais|inv[aá]lid|incorret|n[aã]o possui|inativo|n[aã]o autorizado/i')
      .first()
      .innerText()
      .catch(() => 'mensagem desconhecida')
    throw new Error(
      `IdentityManagement rejeitou o login do usuario ${env.user}. Mensagem do IdM: "${errorText.trim()}". ` +
        `Verifique se o usuario existe, esta ativo e tem pelo menos um contrato ativo associado ao Kanvas.`
    )
  }

  await expect(page).toHaveURL(/kanvas\.mainstay\.com\.br\//, { timeout: 30_000 })

  await expect(page.getByText(/Campanhas ativas|Marcas|Creators/i).first()).toBeVisible({ timeout: 30_000 })

  await page.context().storageState({ path: storageStatePath })
})
