import { defineConfig, devices } from '@playwright/test';

/**
 * The API (run in the hermetic `E2E` environment) serves both the built SPA and `/api` on one
 * origin, so a single base URL drives the whole stack. CI starts that server and passes its URL as
 * `E2E_BASE_URL`; locally it defaults to http://localhost:5000.
 */
export default defineConfig({
  testDir: './specs',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:5000',
    trace: 'on-first-retry',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
});
