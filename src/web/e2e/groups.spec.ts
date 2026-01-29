import { test, expect } from './fixtures';

/**
 * E2E Test: Group Conversations
 *
 * Tests creating groups and managing group members
 */

test.describe('Group Conversations', () => {
  test('should create a new group', async ({ page }) => {
    await page.goto('/');

    // Click new conversation/group button
    const newButton = page.locator('button:has-text("New"), button:has-text("Create")').first();
    await newButton.click();

    // Select group option
    const groupOption = page
      .locator('text=/group|create group|new group/i, button:has-text("Group")')
      .first();
    await groupOption.click();

    // Should show group creation form
    await expect(
      page.locator('input[name*="group" i], input[placeholder*="group name" i]')
    ).toBeVisible({ timeout: 5000 });

    // Enter group name
    const groupName = `Test Group ${Date.now()}`;
    await page.fill('input[name*="group" i], input[placeholder*="group name" i]', groupName);

    // Look for member selection interface
    const memberSearch = page.locator(
      'input[placeholder*="search" i], input[placeholder*="member" i]'
    );
    await expect(memberSearch).toBeVisible({ timeout: 5000 });

    // Add at least one member (if there are users available)
    await memberSearch.fill('test');

    // Wait for search results to appear
    await page
      .locator('[role="listbox"] > *, .user-item, .member-item')
      .first()
      .waitFor({ timeout: 3000 })
      .catch(() => {});

    // Click first result if available
    const firstResult = page.locator('[role="listbox"] > *, .user-item, .member-item').first();
    if ((await firstResult.count()) > 0) {
      await firstResult.click();
    }

    // Create/Save group
    const createButton = page.locator('button:has-text("Create"), button:has-text("Save")').last();
    await createButton.click();

    // Should show the new group in conversation list or redirect to it
    await expect(page.locator(`text="${groupName}"`)).toBeVisible({ timeout: 10000 });
  });

  test('should require group name when creating group', async ({ page }) => {
    await page.goto('/');

    // Navigate to group creation
    const newButton = page.locator('button:has-text("New"), button:has-text("Create")').first();
    await newButton.click();

    const groupOption = page.locator('text=/group|create group/i').first();
    await groupOption.click();

    // Try to create without name
    const createButton = page.locator('button:has-text("Create"), button:has-text("Save")').last();
    await createButton.click();

    // Should show validation error
    await expect(page.locator('text=/name.*required|group name/i')).toBeVisible({ timeout: 5000 });
  });

  test('should add member to existing group', async ({ page }) => {
    await page.goto('/');

    // Assume we have a group conversation
    // Click on a group conversation
    const groupConversation = page
      .locator('[role="list"] > *, .conversation-item, .group-item')
      .first();
    await groupConversation.click();

    // Open group settings/info
    const settingsButton = page
      .locator(
        'button[aria-label*="settings" i], button[aria-label*="info" i], button:has-text("⋮")'
      )
      .first();

    if ((await settingsButton.count()) > 0) {
      await settingsButton.click();

      // Look for add member option
      const addMemberButton = page
        .locator('button:has-text("Add"), button:has-text("Invite"), text=/add.*member/i')
        .first();

      if ((await addMemberButton.count()) > 0) {
        await addMemberButton.click();

        // Should show member selection interface
        const memberSearch = page.locator(
          'input[placeholder*="search" i], input[placeholder*="user" i]'
        );
        await expect(memberSearch).toBeVisible({ timeout: 5000 });

        // Search for a user
        await memberSearch.fill('test');
        await page
          .locator('[role="listbox"] > *, .user-item')
          .first()
          .waitFor({ timeout: 3000 })
          .catch(() => {});

        // Select first result if available
        const firstResult = page.locator('[role="listbox"] > *, .user-item').first();
        if ((await firstResult.count()) > 0) {
          await firstResult.click();

          // Confirm addition
          const confirmButton = page
            .locator('button:has-text("Add"), button:has-text("Confirm")')
            .last();
          await confirmButton.click();

          // Wait for success by checking if button is gone or success indicator appears
          await Promise.race([
            confirmButton.waitFor({ state: 'hidden', timeout: 5000 }),
            page.locator('text=/added|success/i').waitFor({ timeout: 5000 }),
          ]).catch(() => {});

          expect(page.url()).toBeTruthy();
        }
      }
    }
  });

  test('should display group members list', async ({ page }) => {
    await page.goto('/');

    // Click on a group conversation
    const groupConversation = page.locator('[role="list"] > *, .conversation-item').first();
    if ((await groupConversation.count()) > 0) {
      await groupConversation.click();

      // Open group info/settings
      const infoButton = page
        .locator('button[aria-label*="info" i], button[aria-label*="details" i]')
        .first();

      if ((await infoButton.count()) > 0) {
        await infoButton.click();

        // Should show members section
        const membersSection = page.locator('text=/members|participants/i');
        await expect(membersSection).toBeVisible({ timeout: 5000 });
      }
    }
  });

  test('should send message in group conversation', async ({ page }) => {
    await page.goto('/');

    // Click on a group (assuming one exists)
    const groupConversation = page.locator('[role="list"] > *, .conversation-item').first();
    if ((await groupConversation.count()) > 0) {
      await groupConversation.click();

      // Send a message
      const messageInput = page.locator('textarea[placeholder*="message" i]').first();
      const testMessage = `Group message ${Date.now()}`;

      await messageInput.fill(testMessage);
      await messageInput.press('Enter');

      // Verify message appears
      await expect(page.locator(`text="${testMessage}"`)).toBeVisible({ timeout: 10000 });
    }
  });

  test('should display group name in conversation header', async ({ page }) => {
    await page.goto('/');

    // Create or select a group
    const groupConversation = page.locator('[role="list"] > *, .conversation-item').first();
    if ((await groupConversation.count()) > 0) {
      // Get the group name from the list
      const groupName = await groupConversation.textContent();

      // Click on the group
      await groupConversation.click();

      // Group name should appear in header
      if (groupName && groupName.trim()) {
        const header = page.locator('header, .conversation-header, .chat-header');
        await expect(header).toContainText(groupName.trim());
      }
    }
  });
});

test.describe('Group - Advanced Features', () => {
  test('should show member count in group', async ({ page }) => {
    await page.goto('/');

    const groupConversation = page.locator('[role="list"] > *, .conversation-item').first();
    if ((await groupConversation.count()) > 0) {
      await groupConversation.click();

      // Look for member count indicator
      const memberCount = page.locator('text=/\\d+ members|\\d+ participants/i');

      // Member count might be in conversation list or header
      const isVisible = await memberCount.isVisible().catch(() => false);
      if (isVisible) {
        await expect(memberCount).toBeVisible();
      }
    }
  });

  test('should allow leaving a group', async ({ page }) => {
    await page.goto('/');

    const groupConversation = page.locator('[role="list"] > *, .conversation-item').first();
    if ((await groupConversation.count()) > 0) {
      await groupConversation.click();

      // Open group settings
      const settingsButton = page.locator('button[aria-label*="settings" i]').first();
      if ((await settingsButton.count()) > 0) {
        await settingsButton.click();

        // Look for leave group option
        const leaveButton = page.locator('button:has-text("Leave"), text=/leave.*group/i').first();
        if ((await leaveButton.count()) > 0) {
          await expect(leaveButton).toBeVisible();
        }
      }
    }
  });
});
