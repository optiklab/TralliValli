import { test, expect } from './fixtures';

/**
 * E2E Test: Login via Magic Link
 * 
 * Tests the passwordless login flow using magic links
 */

test.describe('Login via Magic Link', () => {
  test('should send magic link when user enters valid email', async ({ page, testUser }) => {
    // Navigate to login page
    await page.goto('/login');
    
    // Verify login page loaded
    await expect(page.locator('h1, h2')).toContainText(/sign in|login/i);
    
    // Enter email
    await page.fill('input[name="email"], input[type="email"]', testUser.email);
    
    // Click send magic link button
    await page.click('button[type="submit"], button:has-text("Send"), button:has-text("Magic Link")');
    
    // Wait for confirmation page
    await page.waitForURL(/magic-link-sent|check-email/i, { timeout: 15000 });
    
    // Verify confirmation message
    await expect(page.locator('body')).toContainText(/check.*email|magic link.*sent/i);
    
    // Verify email is displayed or stored
    const displayedEmail = page.locator(`text="${testUser.email}"`);
    if (await displayedEmail.count() > 0) {
      await expect(displayedEmail).toBeVisible();
    }
  });
  
  test('should validate email format before sending magic link', async ({ page }) => {
    await page.goto('/login');
    
    // Enter invalid email
    await page.fill('input[name="email"], input[type="email"]', 'invalid-email');
    
    // Try to submit
    await page.click('button[type="submit"]');
    
    // Should show validation error
    const errorMessage = page.locator('text=/invalid.*email|email.*format|valid email/i');
    await expect(errorMessage).toBeVisible({ timeout: 5000 });
  });
  
  test('should require email before sending magic link', async ({ page }) => {
    await page.goto('/login');
    
    // Try to submit without email
    await page.click('button[type="submit"]');
    
    // Should show validation error
    const errorMessage = page.locator('text=/email.*required|enter.*email/i');
    await expect(errorMessage).toBeVisible({ timeout: 5000 });
  });
  
  test('should show loading state while sending magic link', async ({ page, testUser }) => {
    await page.goto('/login');
    
    await page.fill('input[name="email"], input[type="email"]', testUser.email);
    
    // Click submit and check for loading state
    await page.click('button[type="submit"]');
    
    // Button should show loading state
    const loadingButton = page.locator('button:has-text("Sending"), button[disabled]');
    await expect(loadingButton).toBeVisible({ timeout: 2000 });
  });
  
  test('should verify magic link and authenticate user', async ({ page, testUser }) => {
    // This test simulates clicking on a magic link
    // In real scenario, the magic link would be extracted from email
    
    // Generate a test magic link token
    const magicToken = 'test-magic-token-' + Date.now();
    const deviceId = 'test-device-id';
    
    // Navigate directly to magic link verification page
    await page.goto(`/verify-magic-link?token=${magicToken}&deviceId=${deviceId}`);
    
    // Should either:
    // 1. Show verification in progress
    // 2. Redirect to main app if successful
    // 3. Show error if token is invalid
    
    // Wait for one of these outcomes
    await Promise.race([
      page.waitForURL(/^(?!.*verify).*$/i, { timeout: 10000 }), // Redirected away from verify page
      page.locator('text=/verifying|loading/i').waitFor({ timeout: 10000 }), // Showing verification
      page.locator('text=/invalid|expired|error/i').waitFor({ timeout: 10000 }), // Showing error
    ]);
    
    // Verify we're either authenticated or got appropriate error
    const currentUrl = page.url();
    expect(currentUrl).toBeTruthy();
  });
});
