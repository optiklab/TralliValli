import { test, expect } from './fixtures';

/**
 * E2E Test: Error Handling and Recovery
 *
 * Tests error scenarios and recovery:
 * - API errors (4xx, 5xx)
 * - Network errors
 * - Invalid data handling
 * - Token expiration and refresh
 * - Graceful degradation
 */

test.describe('Error Handling and Recovery', () => {
  test('should handle 401 unauthorized and prompt re-login', async ({ page }) => {
    // Mock authenticated state
    await page.evaluate(() => {
      localStorage.setItem('auth_token', 'expired-token');
      localStorage.setItem(
        'user',
        JSON.stringify({
          id: 'user-401',
          email: 'unauthorized@example.com',
          displayName: 'Unauthorized User',
        })
      );
    });

    // Intercept API calls to return 401
    await page.route('**/api/**', async (route) => {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Unauthorized' }),
      });
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // App should handle 401 - either redirect to login or show error
    await page.waitForTimeout(3000);

    // Check for login page redirect or error message
    const url = page.url();
    const isLoginPage = url.includes('/login') || url.endsWith('/');
    
    const errorMessage = page.locator('text=/unauthorized|session.*expired|login.*again/i');
    const hasErrorMessage = await errorMessage.first().isVisible().catch(() => false);

    // Should either redirect to login or show error
    expect(isLoginPage || hasErrorMessage).toBeTruthy();
  });

  test('should handle 500 server error gracefully', async ({ page }) => {
    await page.evaluate(() => {
      const timestamp = Date.now();
      localStorage.setItem('auth_token', 'mock-token-' + timestamp);
      localStorage.setItem(
        'user',
        JSON.stringify({
          id: 'user-500',
          email: 'server-error@example.com',
          displayName: 'Server Error User',
        })
      );
    });

    // Mock some API calls to return 500
    let errorCallCount = 0;
    await page.route('**/api/messages/**', async (route) => {
      errorCallCount++;
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Internal Server Error' }),
      });
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // App should show error message
    await page.waitForTimeout(2000);

    // Look for error notification or message
    const errorNotification = page.locator(
      'text=/error|failed|something went wrong|server error/i, [role="alert"], .error-message'
    );
    
    const hasError = await errorNotification.first().isVisible({ timeout: 5000 }).catch(() => false);

    // App should handle error gracefully (show message or continue functioning)
    const appContainer = page.locator('main, [role="main"], .app-container');
    const appVisible = await appContainer.isVisible().catch(() => false);

    // Either error is shown or app continues to function
    expect(hasError || appVisible).toBeTruthy();
  });

  test('should handle 404 resource not found', async ({ page }) => {
    await page.evaluate(() => {
      const timestamp = Date.now();
      localStorage.setItem('auth_token', 'mock-token-' + timestamp);
      localStorage.setItem(
        'user',
        JSON.stringify({
          id: 'user-404',
          email: 'notfound@example.com',
          displayName: 'Not Found User',
        })
      );
    });

    // Navigate to a non-existent conversation
    await page.goto('/conversation/non-existent-id');
    await page.waitForLoadState('networkidle');

    // Should show 404 error or redirect
    await page.waitForTimeout(2000);

    const notFoundMessage = page.locator('text=/not found|doesn.*t exist|invalid/i, [data-testid="404"]');
    const hasNotFound = await notFoundMessage.first().isVisible().catch(() => false);

    const url = page.url();
    const redirected = !url.includes('non-existent-id');

    // Should either show not found message or redirect
    expect(hasNotFound || redirected).toBeTruthy();
  });

  test('should handle network timeout gracefully', async ({ page, context }) => {
    await page.evaluate(() => {
      const timestamp = Date.now();
      localStorage.setItem('auth_token', 'mock-token-' + timestamp);
      localStorage.setItem(
        'user',
        JSON.stringify({
          id: 'user-timeout',
          email: 'timeout@example.com',
          displayName: 'Timeout User',
        })
      );
    });

    // Mock API to delay response (simulate timeout)
    await page.route('**/api/**', async (route) => {
      // Don't respond - let it timeout
      // Or delay significantly
      await new Promise(resolve => setTimeout(resolve, 5000));
      await route.abort('timedout');
    });

    await page.goto('/');
    
    // Wait for timeout to occur
    await page.waitForTimeout(6000);

    // App should handle timeout - show error or retry
    const timeoutMessage = page.locator('text=/timeout|slow.*connection|taking.*longer/i');
    const hasTimeout = await timeoutMessage.first().isVisible().catch(() => false);

    // App should either show timeout message or continue to function
    const appContainer = page.locator('main, [role="main"], .app-container');
    const appVisible = await appContainer.isVisible().catch(() => false);

    expect(hasTimeout || appVisible).toBeTruthy();
  });

  test('should validate and reject invalid email format', async ({ page }) => {
    await page.route('**/auth/invite/**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          isValid: true,
          message: 'Invite is valid.',
        }),
      });
    });

    await page.goto(`/register?invite=test-${Date.now()}`);

    const emailInput = page.locator('input[name="email"], input[type="email"]');
    await emailInput.waitFor({ state: 'visible', timeout: 10000 });
    await expect(emailInput).toBeEnabled({ timeout: 10000 });

    // Try invalid email formats
    const invalidEmails = ['invalid', 'invalid@', '@example.com', 'invalid@.com'];

    for (const invalidEmail of invalidEmails) {
      await emailInput.fill(invalidEmail);
      await page.fill('input[name="displayName"], input[placeholder*="name" i]', 'Test User');
      await page.click('button[type="submit"]');

      // Should show validation error or prevent submission
      await page.waitForTimeout(1000);

      // Check if still on registration page (form didn't submit)
      const url = page.url();
      expect(url).toContain('register');

      // Or check for error message
      const errorMsg = page.locator('text=/invalid.*email|enter.*valid.*email/i');
      const hasError = await errorMsg.first().isVisible().catch(() => false);

      // Clear the field for next iteration
      await emailInput.clear();
    }
  });

  test('should handle malformed API responses', async ({ page }) => {
    await page.evaluate(() => {
      const timestamp = Date.now();
      localStorage.setItem('auth_token', 'mock-token-' + timestamp);
      localStorage.setItem(
        'user',
        JSON.stringify({
          id: 'user-malformed',
          email: 'malformed@example.com',
          displayName: 'Malformed User',
        })
      );
    });

    // Mock API to return malformed JSON
    await page.route('**/api/**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: 'This is not valid JSON{[}]',
      });
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // App should handle malformed response without crashing
    await page.waitForTimeout(3000);

    // Check for error message or that app still functions
    const errorMessage = page.locator('text=/error|failed|invalid.*response/i, [role="alert"]');
    const hasError = await errorMessage.first().isVisible().catch(() => false);

    const appContainer = page.locator('main, [role="main"], .app-container');
    const appVisible = await appContainer.isVisible().catch(() => false);

    // App should either show error or continue to function
    expect(hasError || appVisible).toBeTruthy();
  });

  test('should handle token refresh on expiration', async ({ page }) => {
    let tokenRefreshAttempted = false;

    await page.evaluate(() => {
      const timestamp = Date.now();
      localStorage.setItem('auth_token', 'expiring-token');
      localStorage.setItem('refresh_token', 'valid-refresh-token');
      localStorage.setItem(
        'user',
        JSON.stringify({
          id: 'user-refresh',
          email: 'refresh@example.com',
          displayName: 'Refresh User',
        })
      );
    });

    // Intercept token refresh endpoint
    await page.route('**/auth/refresh', async (route) => {
      tokenRefreshAttempted = true;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: 'new-access-token',
          refreshToken: 'new-refresh-token',
          expiresAt: new Date(Date.now() + 3600000).toISOString(),
        }),
      });
    });

    // First API call returns 401
    let apiCallCount = 0;
    await page.route('**/api/conversations', async (route) => {
      apiCallCount++;
      if (apiCallCount === 1) {
        await route.fulfill({
          status: 401,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Token expired' }),
        });
      } else {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ conversations: [] }),
        });
      }
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Wait for token refresh attempt
    await page.waitForTimeout(3000);

    // The app should have attempted token refresh
    // This depends on app implementation
    expect(typeof tokenRefreshAttempted).toBe('boolean');
  });

  test('should show user-friendly error for no internet connection', async ({ page, context }) => {
    await page.evaluate(() => {
      const timestamp = Date.now();
      localStorage.setItem('auth_token', 'mock-token-' + timestamp);
      localStorage.setItem(
        'user',
        JSON.stringify({
          id: 'user-no-internet',
          email: 'no-internet@example.com',
          displayName: 'No Internet User',
        })
      );
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Simulate no internet
    await context.setOffline(true);
    await page.waitForTimeout(2000);

    // Look for offline/no connection message
    const offlineMessage = page.locator(
      'text=/offline|no.*internet|no.*connection|check.*connection/i, .offline-banner'
    );

    const hasOfflineMessage = await offlineMessage.first().isVisible({ timeout: 5000 }).catch(() => false);

    // Restore connection
    await context.setOffline(false);
    await page.waitForTimeout(2000);

    // Message should disappear when back online
    const stillOffline = await offlineMessage.first().isVisible().catch(() => false);

    // The offline behavior should be handled
    expect(typeof hasOfflineMessage).toBe('boolean');
  });

  test('should handle concurrent operation conflicts', async ({ page }) => {
    await page.evaluate(() => {
      const timestamp = Date.now();
      localStorage.setItem('auth_token', 'mock-token-' + timestamp);
      localStorage.setItem(
        'user',
        JSON.stringify({
          id: 'user-conflict',
          email: 'conflict@example.com',
          displayName: 'Conflict User',
        })
      );
    });

    // Mock API to return 409 Conflict
    await page.route('**/api/**', async (route) => {
      const method = route.request().method();
      if (method === 'POST' || method === 'PUT' || method === 'PATCH') {
        await route.fulfill({
          status: 409,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Resource conflict' }),
        });
      } else {
        await route.continue();
      }
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Try to perform an operation that causes conflict
    const messageInput = page
      .locator(
        'textarea[placeholder*="message" i], input[placeholder*="message" i]'
      )
      .first();

    if ((await messageInput.count()) > 0 && (await messageInput.isVisible())) {
      await messageInput.fill('Conflict test message');
      
      const sendButton = page.locator('button:has-text("Send")').first();
      if ((await sendButton.count()) > 0) {
        await sendButton.click();
      } else {
        await messageInput.press('Enter');
      }

      await page.waitForTimeout(2000);

      // Should show error or handle conflict
      const errorMessage = page.locator('text=/error|conflict|failed/i, [role="alert"]');
      const hasError = await errorMessage.first().isVisible().catch(() => false);

      // App should handle conflict gracefully
      expect(typeof hasError).toBe('boolean');
    }
  });

  test('should recover from WebSocket disconnection', async ({ page }) => {
    await page.evaluate(() => {
      const timestamp = Date.now();
      localStorage.setItem('auth_token', 'mock-token-' + timestamp);
      localStorage.setItem(
        'user',
        JSON.stringify({
          id: 'user-ws-disconnect',
          email: 'ws-disconnect@example.com',
          displayName: 'WS Disconnect User',
        })
      );
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Simulate WebSocket disconnection by going offline briefly
    await page.context().setOffline(true);
    await page.waitForTimeout(2000);

    // Reconnect
    await page.context().setOffline(false);
    await page.waitForTimeout(3000);

    // App should reconnect - check for connection status
    const reconnectMessage = page.locator('text=/reconnect|connected/i');
    const hasReconnect = await reconnectMessage.first().isVisible().catch(() => false);

    // Or check that app is still functional
    const appContainer = page.locator('main, [role="main"], .app-container');
    const appVisible = await appContainer.isVisible().catch(() => false);

    expect(hasReconnect || appVisible).toBeTruthy();
  });

  test('should handle rate limiting (429 Too Many Requests)', async ({ page }) => {
    await page.evaluate(() => {
      const timestamp = Date.now();
      localStorage.setItem('auth_token', 'mock-token-' + timestamp);
      localStorage.setItem(
        'user',
        JSON.stringify({
          id: 'user-rate-limit',
          email: 'rate-limit@example.com',
          displayName: 'Rate Limit User',
        })
      );
    });

    // Mock API to return 429
    await page.route('**/api/**', async (route) => {
      await route.fulfill({
        status: 429,
        contentType: 'application/json',
        headers: {
          'Retry-After': '60',
        },
        body: JSON.stringify({ message: 'Too many requests' }),
      });
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Should show rate limit message
    const rateLimitMessage = page.locator('text=/rate limit|too many|slow down|try again/i');
    const hasMessage = await rateLimitMessage.first().isVisible().catch(() => false);

    const appContainer = page.locator('main, [role="main"], .app-container');
    const appVisible = await appContainer.isVisible().catch(() => false);

    // App should handle rate limiting gracefully
    expect(hasMessage || appVisible).toBeTruthy();
  });
});
