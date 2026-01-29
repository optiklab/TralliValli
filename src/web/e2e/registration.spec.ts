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

    // Navigate to registration page with invite token
    await page.goto(`/register?invite=${inviteToken}`);

    // Verify registration page loaded
    await expect(page.locator('h1, h2')).toContainText(/register|sign up/i);

    // Check invite token is pre-filled or validated
    const inviteInput = page.locator('input[name="inviteToken"], input[value*="invite"]');
    if ((await inviteInput.count()) > 0) {
      await expect(inviteInput).toHaveValue(inviteToken);
    }

    // Fill in email
    await page.fill('input[name="email"], input[type="email"]', testUser.email);

    // Fill in display name
    await page.fill(
      'input[name="displayName"], input[placeholder*="name" i]',
      testUser.displayName
    );

    // Submit registration form
    await page.click('button[type="submit"]');

    // Wait for success - should redirect to magic link sent page
    await page.waitForURL(/magic-link-sent|check-email/i, { timeout: 15000 });

    // Verify confirmation message
    await expect(page.locator('body')).toContainText(/check.*email|magic link sent/i);
  });

  test('should show error with invalid invite link', async ({ page, testUser }) => {
    const invalidToken = 'invalid-token-xyz';

    // Navigate to registration page with invalid token
    await page.goto(`/register?invite=${invalidToken}`);

    // Fill form
    await page.fill('input[name="email"], input[type="email"]', testUser.email);
    await page.fill(
      'input[name="displayName"], input[placeholder*="name" i]',
      testUser.displayName
    );

    // Submit form
    await page.click('button[type="submit"]');

    // Should show error message
    await expect(page.locator('text=/invalid|expired|error/i')).toBeVisible({ timeout: 10000 });
  });

  test('should validate email format during registration', async ({ page, testUser }) => {
    const inviteToken = 'test-invite-token-' + Date.now();

    await page.goto(`/register?invite=${inviteToken}`);

    // Enter invalid email
    await page.fill('input[name="email"], input[type="email"]', 'invalid-email');
    await page.fill(
      'input[name="displayName"], input[placeholder*="name" i]',
      testUser.displayName
    );

    // Submit form
    await page.click('button[type="submit"]');

    // Should show validation error
    const errorMessage = page.locator('text=/invalid.*email|email.*format|valid email/i');
    await expect(errorMessage).toBeVisible({ timeout: 5000 });
  });

  test('should require display name during registration', async ({ page, testUser }) => {
    const inviteToken = 'test-invite-token-' + Date.now();

    await page.goto(`/register?invite=${inviteToken}`);

    // Enter email but not display name
    await page.fill('input[name="email"], input[type="email"]', testUser.email);

    // Submit form
    await page.click('button[type="submit"]');

    // Should show validation error for display name
    const errorMessage = page.locator('text=/name.*required|display name/i');
    await expect(errorMessage).toBeVisible({ timeout: 5000 });
  });
});
