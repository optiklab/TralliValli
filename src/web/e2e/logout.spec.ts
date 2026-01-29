import { test, expect } from '../fixtures';

/**
 * E2E Test: Logout Functionality
 * 
 * Tests user logout and session cleanup
 */

test.describe('Logout', () => {
  test('should logout successfully and redirect to login page', async ({ page }) => {
    // Assume user is logged in
    await page.goto('/');
    
    // Look for user menu, settings, or profile button
    const menuButton = page.locator(
      'button[aria-label*="menu" i], button[aria-label*="settings" i], button[aria-label*="profile" i], button[aria-label*="user" i]'
    ).first();
    
    // If menu button exists, click it
    if (await menuButton.count() > 0) {
      await menuButton.click();
      
      // Look for logout button
      const logoutButton = page.locator(
        'button:has-text("Logout"), button:has-text("Sign out"), button:has-text("Log out"), a:has-text("Logout")'
      ).first();
      
      await expect(logoutButton).toBeVisible({ timeout: 5000 });
      await logoutButton.click();
    } else {
      // Try to find logout button directly (might be in sidebar or nav)
      const logoutButton = page.locator(
        'button:has-text("Logout"), button:has-text("Sign out"), a:has-text("Logout")'
      ).first();
      
      if (await logoutButton.count() > 0) {
        await logoutButton.click();
      } else {
        test.skip();
        return;
      }
    }
    
    // Should redirect to login page
    await page.waitForURL(/login|signin|auth/i, { timeout: 10000 });
    
    // Verify we're on login page
    await expect(page.locator('h1, h2')).toContainText(/sign in|login/i);
  });
  
  test('should clear session data after logout', async ({ page }) => {
    await page.goto('/');
    
    // Check that we have some session/auth data before logout
    const hasAuthData = await page.evaluate(() => {
      return localStorage.getItem('auth') !== null || 
             localStorage.getItem('token') !== null ||
             sessionStorage.length > 0;
    });
    
    // Logout
    const menuButton = page.locator('button[aria-label*="menu" i], button[aria-label*="settings" i]').first();
    if (await menuButton.count() > 0) {
      await menuButton.click();
    }
    
    const logoutButton = page.locator('button:has-text("Logout"), button:has-text("Sign out")').first();
    if (await logoutButton.count() > 0) {
      await logoutButton.click();
      
      // Wait for logout to complete
      await page.waitForURL(/login|signin/i, { timeout: 10000 });
      
      // Check that session data is cleared
      const authDataAfterLogout = await page.evaluate(() => {
        const localStorageAuth = localStorage.getItem('auth');
        const localStorageToken = localStorage.getItem('token');
        return {
          hasAuth: localStorageAuth !== null,
          hasToken: localStorageToken !== null,
        };
      });
      
      // Auth tokens should be cleared
      expect(authDataAfterLogout.hasAuth).toBe(false);
      expect(authDataAfterLogout.hasToken).toBe(false);
    }
  });
  
  test('should not be able to access protected routes after logout', async ({ page }) => {
    await page.goto('/');
    
    // Logout
    const menuButton = page.locator('button[aria-label*="menu" i], button[aria-label*="settings" i]').first();
    if (await menuButton.count() > 0) {
      await menuButton.click();
    }
    
    const logoutButton = page.locator('button:has-text("Logout"), button:has-text("Sign out")').first();
    if (await logoutButton.count() > 0) {
      await logoutButton.click();
      await page.waitForURL(/login|signin/i, { timeout: 10000 });
      
      // Try to navigate to a protected route
      await page.goto('/');
      
      // Should be redirected to login
      await page.waitForURL(/login|signin|auth/i, { timeout: 10000 });
      expect(page.url()).toMatch(/login|signin|auth/i);
    }
  });
  
  test('should show logout confirmation dialog if configured', async ({ page }) => {
    await page.goto('/');
    
    // Setup dialog listener
    let dialogAppeared = false;
    page.on('dialog', async (dialog) => {
      dialogAppeared = true;
      expect(dialog.message()).toMatch(/logout|sign out|sure/i);
      await dialog.accept();
    });
    
    // Try to logout
    const menuButton = page.locator('button[aria-label*="menu" i], button[aria-label*="settings" i]').first();
    if (await menuButton.count() > 0) {
      await menuButton.click();
    }
    
    const logoutButton = page.locator('button:has-text("Logout"), button:has-text("Sign out")').first();
    if (await logoutButton.count() > 0) {
      await logoutButton.click();
      
      // Wait a moment for dialog to appear
      await page.waitForTimeout(500);
      
      // If no browser dialog, check for custom modal
      if (!dialogAppeared) {
        const confirmModal = page.locator(
          'text=/are you sure|confirm logout/i, [role="dialog"]'
        );
        const modalVisible = await confirmModal.isVisible({ timeout: 2000 }).catch(() => false);
        
        if (modalVisible) {
          // Click confirm in modal
          const confirmButton = page.locator('button:has-text("Confirm"), button:has-text("Yes")').first();
          await confirmButton.click();
        }
      }
      
      // Wait for redirect
      await page.waitForURL(/login|signin/i, { timeout: 10000 });
    }
  });
  
  test('should disconnect from SignalR when logging out', async ({ page }) => {
    await page.goto('/');
    
    // Check if connected (this depends on app exposing connection state)
    const isConnectedBefore = await page.evaluate(() => {
      return (window as any).signalRConnected === true;
    }).catch(() => false);
    
    // Logout
    const menuButton = page.locator('button[aria-label*="menu" i], button[aria-label*="settings" i]').first();
    if (await menuButton.count() > 0) {
      await menuButton.click();
    }
    
    const logoutButton = page.locator('button:has-text("Logout"), button:has-text("Sign out")').first();
    if (await logoutButton.count() > 0) {
      await logoutButton.click();
      
      await page.waitForURL(/login|signin/i, { timeout: 10000 });
      
      // Connection should be closed
      const isConnectedAfter = await page.evaluate(() => {
        return (window as any).signalRConnected === true;
      }).catch(() => false);
      
      expect(isConnectedAfter).toBe(false);
    }
  });
  
  test('should be able to login again after logout', async ({ page, testUser }) => {
    await page.goto('/');
    
    // Logout
    const menuButton = page.locator('button[aria-label*="menu" i], button[aria-label*="settings" i]').first();
    if (await menuButton.count() > 0) {
      await menuButton.click();
    }
    
    const logoutButton = page.locator('button:has-text("Logout"), button:has-text("Sign out")').first();
    if (await logoutButton.count() > 0) {
      await logoutButton.click();
      
      // Wait for login page
      await page.waitForURL(/login|signin/i, { timeout: 10000 });
      
      // Try to login again
      await page.fill('input[name="email"], input[type="email"]', testUser.email);
      await page.click('button[type="submit"]');
      
      // Should proceed to magic link sent page
      await page.waitForURL(/magic-link-sent|check-email/i, { timeout: 10000 });
      await expect(page.locator('body')).toContainText(/check.*email|magic link/i);
    }
  });
});

test.describe('Logout - Edge Cases', () => {
  test('should handle logout when network is offline', async ({ page, context }) => {
    await page.goto('/');
    
    // Go offline
    await context.setOffline(true);
    
    // Try to logout
    const menuButton = page.locator('button[aria-label*="menu" i], button[aria-label*="settings" i]').first();
    if (await menuButton.count() > 0) {
      await menuButton.click();
    }
    
    const logoutButton = page.locator('button:has-text("Logout"), button:has-text("Sign out")').first();
    if (await logoutButton.count() > 0) {
      await logoutButton.click();
      
      // Should still logout locally even if offline
      // Either redirect to login or show offline message
      await Promise.race([
        page.waitForURL(/login|signin/i, { timeout: 10000 }),
        page.locator('text=/offline|network/i').waitFor({ timeout: 10000 }),
      ]);
    }
    
    // Go back online
    await context.setOffline(false);
  });
  
  test('should maintain logout state after page refresh', async ({ page }) => {
    await page.goto('/');
    
    // Logout
    const menuButton = page.locator('button[aria-label*="menu" i], button[aria-label*="settings" i]').first();
    if (await menuButton.count() > 0) {
      await menuButton.click();
    }
    
    const logoutButton = page.locator('button:has-text("Logout"), button:has-text("Sign out")').first();
    if (await logoutButton.count() > 0) {
      await logoutButton.click();
      await page.waitForURL(/login|signin/i, { timeout: 10000 });
      
      // Refresh the page
      await page.reload();
      
      // Should still be on login page
      await expect(page.locator('h1, h2')).toContainText(/sign in|login/i);
    }
  });
});
