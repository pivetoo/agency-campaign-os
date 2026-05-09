import { defineConfig, devices } from '@playwright/test'
import * as path from 'node:path'
import * as dotenv from 'dotenv'

dotenv.config({ path: path.join(__dirname, '.env') })

const baseURL = process.env.E2E_BASE_URL ?? 'https://kanvas.mainstay.com.br'
const storageStatePath = path.join(__dirname, '.auth', 'user.json')

export default defineConfig({
  testDir: './tests',
  outputDir: './test-results',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: [
    ['list'],
    ['html', { outputFolder: './playwright-report', open: 'never' }],
  ],
  timeout: 60_000,
  expect: { timeout: 10_000 },
  use: {
    baseURL,
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    locale: 'pt-BR',
    timezoneId: 'America/Sao_Paulo',
  },
  projects: [
    {
      name: 'setup',
      testMatch: /auth\.setup\.ts/,
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'chromium',
      testMatch: /.*\.spec\.ts/,
      dependencies: ['setup'],
      use: {
        ...devices['Desktop Chrome'],
        storageState: storageStatePath,
      },
    },
  ],
})
