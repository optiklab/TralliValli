import { test, expect } from './fixtures';

/**
 * E2E Test: Registration via Invite Link
 *
 * Tests the complete user registration flow using an invite link
 */

test.describe('Registration via Invite Link', () => {
  test('should successfully register a new user with valid invite link', async ({
    page,
    testUser,
  }) => {
    // Generate a test invite token (in real scenario, this would come from admin API)
    const inviteToken = 'test-invite-token-' + Date.now();

    // Mock the invite validation API to return success
    await page.route('**/auth/invite/**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          isValid: true,
          message: 'Invite is valid.',
          expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(),
        }),
      });
    });

    // Mock the registration API to return success
    await page.route('**/auth/register', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: 'mock-access-token',
          refreshToken: 'mock-refresh-token',
          expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
          refreshExpiresAt: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
        }),
      });
    });

    // Navigate to registration page with invite token
    await page.goto(`/register?invite=${inviteToken}`);

    // Verify registration page loaded
    await expect(page.locator('h1, h2')).toContainText(/register|sign up|create.*account/i);

    // Check invite token is pre-filled or validated
    const inviteInput = page.locator('input[name="inviteToken"], input[value*="invite"]');
    if ((await inviteInput.count()) > 0) {
      await expect(inviteInput).toHaveValue(inviteToken);
    }

    // Wait for invite validation to complete and email input to be enabled
    const emailInput = page.locator('input[name="email"], input[type="email"]');
    await emailInput.waitFor({ state: 'visible', timeout: 10000 });
    await expect(emailInput).toBeEnabled({ timeout: 10000 });

    // Fill in email
    await page.fill('input[name="email"], input[type="email"]', testUser.email);

    // Fill in display name
    await page.fill(
      'input[name="displayName"], input[placeholder*="name" i]',
      testUser.displayName
    );

    // Submit registration form
    await page.click('button[type="submit"]');

    // Wait for success - the app should process the registration
    // Since we mocked the API, we should check for successful state changes
    // The component calls onSuccess or navigates, but since we don't have routing,
    // we can check if the error is not shown and form is submitted
    await page.waitForTimeout(1000);

    // Verify no error message is shown
    const errorElement = page.locator('text=/error|failed/i').first();
    const hasError = await errorElement.isVisible().catch(() => false);
    expect(hasError).toBe(false);
  });

  test('should show error with invalid invite link', async ({ page, testUser }) => {
    const invalidToken = 'invalid-token-xyz';

    // Mock the invite validation API to return failure
    await page.route('**/auth/invite/**', async (route) => {
      await route.fulfill({
        status: 404,
        contentType: 'application/json',
        body: JSON.stringify({
          isValid: false,
          message: 'Invalid or expired invite token.',
        }),
      });
    });

    // Navigate to registration page with invalid token
    await page.goto(`/register?invite=${invalidToken}`);

    // Wait for invite validation to complete
    await page.waitForTimeout(1000);

    // Check if error message is displayed (use first() to handle multiple matches)
    await expect(page.locator('text=/invalid|expired/i').first()).toBeVisible({ timeout: 5000 });
    
    // Verify that the email input is disabled due to invalid invite
    const emailInput = page.locator('input[name="email"], input[type="email"]');
    await expect(emailInput).toBeDisabled();
  });

  test('should validate email format during registration', async ({ page, testUser }) => {
    const inviteToken = 'test-invite-token-' + Date.now();

    // Mock the invite validation API to return success
    await page.route('**/auth/invite/**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          isValid: true,
          message: 'Invite is valid.',
          expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(),
        }),
      });
    });

    await page.goto(`/register?invite=${inviteToken}`);

    // Wait for invite validation to complete and email input to be enabled
    const emailInput = page.locator('input[name="email"], input[type="email"]');
    await emailInput.waitFor({ state: 'visible', timeout: 10000 });
    await expect(emailInput).toBeEnabled({ timeout: 10000 });

    // Enter invalid email
    await page.fill('input[name="email"], input[type="email"]', 'invalid-email');
    await page.fill(
      'input[name="displayName"], input[placeholder*="name" i]',
      testUser.displayName
    );

    // Submit form
    await page.click('button[type="submit"]');

    // Should show validation error - the component shows "Please enter a valid email address"
    const errorMessage = page.locator('text=/valid email address/i');
    await expect(errorMessage).toBeVisible({ timeout: 5000 });
  });

  test('should require display name during registration', async ({ page, testUser }) => {
    const inviteToken = 'test-invite-token-' + Date.now();

    // Mock the invite validation API to return success
    await page.route('**/auth/invite/**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          isValid: true,
          message: 'Invite is valid.',
          expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(),
        }),
      });
    });

    await page.goto(`/register?invite=${inviteToken}`);

    // Wait for invite validation to complete and email input to be enabled
    const emailInput = page.locator('input[name="email"], input[type="email"]');
    await emailInput.waitFor({ state: 'visible', timeout: 10000 });
    await expect(emailInput).toBeEnabled({ timeout: 10000 });

    // Enter email but not display name
    await page.fill('input[name="email"], input[type="email"]', testUser.email);

    // Submit form
    await page.click('button[type="submit"]');

    // Should show validation error for display name
    const errorMessage = page.locator('text=/name.*required|display name/i');
    await expect(errorMessage).toBeVisible({ timeout: 5000 });
  });
});
