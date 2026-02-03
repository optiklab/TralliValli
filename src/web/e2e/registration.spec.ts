import { test, expect } from './fixtures';

/**
 * E2E Test: Registration via Invite Link
 *
 * Tests the complete user registration flow using an invite link
 */

/**
 * Helper to mock system status (bootstrapped)
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
async function mockSystemStatus(page: any, isBootstrapped = true) {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  await page.route('**/auth/system-status', async (route: any) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        isBootstrapped,
        requiresInvite: isBootstrapped,
      }),
    });
  });
}

/**
 * Helper to mock successful invite validation
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
async function mockValidInvite(page: any) {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  await page.route('**/auth/invite/**', async (route: any) => {
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
}

/**
 * Helper to mock invalid invite validation
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
async function mockInvalidInvite(page: any) {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  await page.route('**/auth/invite/**', async (route: any) => {
    await route.fulfill({
      status: 404,
      contentType: 'application/json',
      body: JSON.stringify({
        isValid: false,
        message: 'Invalid or expired invite token.',
      }),
    });
  });
}

/**
 * Helper to mock successful registration
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
async function mockSuccessfulRegistration(page: any) {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  await page.route('**/auth/register', async (route: any) => {
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
}

test.describe('Registration via Invite Link', () => {
  test('should successfully register a new user with valid invite link', async ({
    page,
    testUser,
  }) => {
    // Generate a test invite token (in real scenario, this would come from admin API)
    const inviteToken = 'test-invite-token-' + Date.now();

    // Mock system status and invite validation
    await mockSystemStatus(page);
    await mockValidInvite(page);
    await mockSuccessfulRegistration(page);

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
    const submitButton = page.locator('button[type="submit"]');
    await submitButton.click();

    // Wait for registration request to complete
    await page.waitForResponse('**/auth/register', { timeout: 5000 });

    // Verify no error message is shown
    const errorElement = page.locator('text=/error|failed/i').first();
    const hasError = await errorElement.isVisible().catch(() => false);
    expect(hasError).toBe(false);
  });

  test('should show error with invalid invite link', async ({ page, testUser }) => {
    const invalidToken = 'invalid-token-xyz';

    // Mock system status and invalid invite
    await mockSystemStatus(page);
    await mockInvalidInvite(page);

    // Navigate to registration page with invalid token
    await page.goto(`/register?invite=${invalidToken}`);

    // Wait for validation response
    await page.waitForResponse('**/auth/invite/**', { timeout: 5000 });

    // Check if error message is displayed (use first() to handle multiple matches)
    await expect(page.locator('text=/invalid|expired/i').first()).toBeVisible({ timeout: 5000 });

    // Verify that the email input is disabled due to invalid invite
    const emailInput = page.locator('input[name="email"], input[type="email"]');
    await expect(emailInput).toBeDisabled();
  });

  test('should validate email format during registration', async ({ page, testUser }) => {
    const inviteToken = 'test-invite-token-' + Date.now();

    // Mock system status and invite validation
    await mockSystemStatus(page);
    await mockValidInvite(page);

    // Mock the registration API - track if it gets called
    let registrationAttempts = 0;
    await page.route('**/auth/register', async (route: any) => {
      registrationAttempts++;
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

    // The email input should show HTML5 validation error or the form should show a custom error
    // Check if registration API was not called (form validation prevented submission)
    // Wait for potential API call with a small timeout, expect it to timeout
    const registrationRequest = page.waitForRequest('**/auth/register', { timeout: 2000 });
    await expect(registrationRequest).rejects.toThrow();

    // The email input should still be visible (not navigated away)
    await expect(emailInput).toBeVisible();

    // Verify registration was not attempted
    expect(registrationAttempts).toBe(0);
  });

  test('should require display name during registration', async ({ page, testUser }) => {
    const inviteToken = 'test-invite-token-' + Date.now();

    // Mock system status and invite validation
    await mockSystemStatus(page);
    await mockValidInvite(page);

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

  test('should allow registration without invite link', async ({ page, testUser }) => {
    // Mock system status and registration
    await mockSystemStatus(page);
    await mockSuccessfulRegistration(page);

    // Navigate to registration page without invite token
    await page.goto('/register');

    // Wait for system status check to complete
    const emailInput = page.locator('input[name="email"], input[type="email"]');
    await emailInput.waitFor({ state: 'visible', timeout: 10000 });

    // Verify invite field is optional (has "(optional)" text)
    const inviteLabel = page.locator('text=/invite.*optional/i');
    await expect(inviteLabel).toBeVisible();

    // Fill in email and display name (no invite token)
    await page.fill('input[name="email"], input[type="email"]', testUser.email);
    await page.fill(
      'input[name="displayName"], input[placeholder*="name" i]',
      testUser.displayName
    );

    // Submit registration form
    const submitButton = page.locator('button[type="submit"]');
    await submitButton.click();

    // Wait for registration request to complete
    await page.waitForResponse('**/auth/register', { timeout: 5000 });

    // Verify no error message is shown
    const errorElement = page.locator('text=/error|failed/i').first();
    const hasError = await errorElement.isVisible().catch(() => false);
    expect(hasError).toBe(false);
  });
});
