import { expect, test } from '@playwright/test';

// Drives the full stack (React SPA → API → SQL Server) in the hermetic E2E environment, mirroring
// the location-search and location-intelligence BDD features through the browser.
test.describe('Locations & discovery', () => {
  test('sign in, search, persist, and view AI intelligence', async ({ page }) => {
    await page.goto('/');

    // Offline sign-in (E2E environment accepts the email as the token).
    await page.getByTestId('signin-email').fill('amara@cyclesync.example');
    await page.getByTestId('signin-submit').click();
    await expect(page.getByTestId('current-user')).toBeVisible();

    // Search Azure Maps (offline gazetteer) and confirm the destination is returned.
    await page.getByTestId('search-input').fill('Lisbon');
    await page.getByTestId('search-submit').click();
    const results = page.getByTestId('search-results');
    await expect(results).toContainText('Lisbon, Portugal');
    await expect(results).toContainText('Portugal');

    // Persist the selection; it appears in the saved list (visible to all users).
    await page.getByTestId('select-Lisbon, Portugal').click();
    const savedLink = page.getByTestId('location-Lisbon, Portugal');
    await expect(savedLink).toBeVisible();

    // Open details: AI intelligence is shown with confidence and a generation timestamp.
    await savedLink.click();
    await expect(page.getByTestId('intel-climate')).not.toBeEmpty();
    await expect(page.getByTestId('intel-best-times')).not.toBeEmpty();
    await expect(page.getByTestId('intel-confidence')).toContainText(/Low|Medium|High/);
    await expect(page.getByTestId('intel-generated')).toContainText('Generated');
  });
});
