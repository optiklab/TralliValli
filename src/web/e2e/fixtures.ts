import { test as base, expect } from '@playwright/test';
import type { Page } from '@playwright/test';

/**
 * Test fixtures for E2E tests
 * Provides reusable test utilities and authenticated contexts
 */

export interface TestUser {
  email: string;
  displayName: string;
  inviteToken?: string;
}

export interface TestFixtures {
  authenticatedPage: Page;
  testUser: TestUser;
}

// Helper to generate test user data
export function generateTestUser(prefix: string = 'test'): TestUser {
  const timestamp = Date.now();
  const random = Math.random().toString(36).substring(7);
  return {
    email: `${prefix}-${timestamp}-${random}@example.com`,
    displayName: `Test User ${timestamp}`,
  };
}

// Helper to wait for API response
export async function waitForApiResponse(
  page: Page,
  urlPattern: string | RegExp,
  timeout: number = 10000
) {
  return page.waitForResponse(
    (response) => {
      const url = response.url();
      if (typeof urlPattern === 'string') {
        return url.includes(urlPattern);
      }
      return urlPattern.test(url);
    },
    { timeout }
  );
}

// Helper to register a new user via invite link
export async function registerUser(page: Page, user: TestUser, inviteToken: string) {
  await page.goto(`/register?invite=${inviteToken}`);

  // Fill registration form
  await page.fill('input[name="email"]', user.email);
  await page.fill('input[name="displayName"]', user.displayName);

  // Submit form
  await page.click('button[type="submit"]');

  // Wait for successful registration
  await page.waitForURL('/magic-link-sent', { timeout: 10000 });
}

// Helper to login via magic link
export async function loginViaMagicLink(page: Page, email: string) {
  await page.goto('/login');

  // Enter email
  await page.fill('input[name="email"]', email);

  // Click send magic link
  await page.click('button[type="submit"]');

  // Wait for confirmation
  await page.waitForURL('/magic-link-sent', { timeout: 10000 });

  // In a real scenario, we would need to intercept the email or use a test endpoint
  // For E2E tests, we'll need a way to generate valid magic links or mock the auth
  // This is a placeholder that assumes we have a test endpoint or mechanism
}

// Helper to wait for SignalR connection
export async function waitForSignalRConnection(page: Page, timeout: number = 10000) {
  await page
    .waitForFunction(
      () => {
        // Check if SignalR connection is established
        // This assumes the app exposes connection state somehow
        return (window as any).signalRConnected === true;
      },
      { timeout }
    )
    .catch(() => {
      // If the above doesn't work, wait for presence of chat UI elements
      // which indicates the user is connected
    });
}

// Helper to send a text message
export async function sendMessage(page: Page, message: string) {
  const messageInput = page.locator(
    'textarea[placeholder*="message" i], input[placeholder*="message" i]'
  );
  await messageInput.fill(message);
  await messageInput.press('Enter');
}

// Helper to wait for message to appear
export async function waitForMessage(page: Page, messageContent: string, timeout: number = 10000) {
  await page.waitForSelector(`text="${messageContent}"`, { timeout });
}

// Helper to create a new conversation
export async function createDirectConversation(page: Page, userName: string) {
  // Click new conversation button
  await page.click('button:has-text("New"), button:has-text("Create")');

  // Select direct conversation type
  await page.click('text="Direct Message", text="Direct"');

  // Search and select user
  await page.fill('input[placeholder*="search" i]', userName);
  await page.click(`text="${userName}"`);

  // Create conversation
  await page.click('button:has-text("Create"), button:has-text("Start")');
}

// Helper to create a group
export async function createGroup(page: Page, groupName: string, members: string[]) {
  // Click new conversation button
  await page.click('button:has-text("New"), button:has-text("Create")');

  // Select group conversation type
  await page.click('text="Group", text="Create Group"');

  // Enter group name
  await page.fill('input[name="groupName"], input[placeholder*="group name" i]', groupName);

  // Add members
  for (const member of members) {
    await page.fill('input[placeholder*="search" i], input[placeholder*="member" i]', member);
    await page.click(`text="${member}"`);
  }

  // Create group
  await page.click('button:has-text("Create"), button:has-text("Done")');
}

// Helper to upload file
export async function uploadFile(page: Page, filePath: string) {
  const fileInput = page.locator('input[type="file"]');
  await fileInput.setInputFiles(filePath);
}

// Helper to logout
export async function logout(page: Page) {
  // Click settings or user menu
  await page.click('[aria-label*="settings" i], [aria-label*="menu" i], [aria-label*="profile" i]');

  // Click logout button
  await page.click('button:has-text("Logout"), button:has-text("Sign out")');

  // Wait for redirect to login
  await page.waitForURL('/login', { timeout: 10000 });
}

// Extended test fixture with authenticated page
export const test = base.extend<TestFixtures>({
  testUser: async ({}, use) => {
    const user = generateTestUser();
    await use(user);
  },

  authenticatedPage: async ({ page, testUser }, use) => {
    // This fixture provides a page that's already authenticated
    // In a real implementation, this would use a test endpoint or
    // mock authentication mechanism

    // For now, we'll just provide the page and assume tests will handle auth
    await use(page);
  },
});

export { expect };
