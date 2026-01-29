import { test, expect } from '../fixtures';

/**
 * E2E Test: Messaging Features
 * 
 * Tests creating conversations, sending messages, and real-time message delivery
 */

test.describe('Messaging - Direct Conversations', () => {
  test('should create a new direct conversation', async ({ page }) => {
    // Note: This test assumes user is already authenticated
    // In real scenario, would need to authenticate first
    
    await page.goto('/');
    
    // Look for new conversation button
    const newConversationBtn = page.locator(
      'button:has-text("New"), button:has-text("Create"), button:has-text("Start Chat")'
    ).first();
    
    await newConversationBtn.click();
    
    // Should show conversation creation dialog/modal
    await expect(
      page.locator('text=/new conversation|create chat|direct message/i')
    ).toBeVisible({ timeout: 5000 });
    
    // Look for option to create direct message
    const directOption = page.locator('text=/direct|dm|one-on-one/i').first();
    if (await directOption.count() > 0) {
      await directOption.click();
    }
    
    // Should show user search/selection interface
    const searchInput = page.locator('input[placeholder*="search" i], input[placeholder*="user" i]').first();
    await expect(searchInput).toBeVisible({ timeout: 5000 });
  });
  
  test('should send a text message in conversation', async ({ page }) => {
    await page.goto('/');
    
    // Assume we have at least one conversation or create one
    // Click on first conversation in list
    const firstConversation = page.locator('[role="list"] > *, .conversation-item').first();
    if (await firstConversation.count() > 0) {
      await firstConversation.click();
    }
    
    // Find message input
    const messageInput = page.locator(
      'textarea[placeholder*="message" i], input[placeholder*="message" i], textarea[name="message"]'
    ).first();
    
    await expect(messageInput).toBeVisible({ timeout: 5000 });
    
    // Type a message
    const testMessage = `Test message at ${new Date().toISOString()}`;
    await messageInput.fill(testMessage);
    
    // Send message (Enter key or send button)
    const sendButton = page.locator('button:has-text("Send"), button[type="submit"]').first();
    if (await sendButton.count() > 0) {
      await sendButton.click();
    } else {
      await messageInput.press('Enter');
    }
    
    // Verify message appears in thread
    await expect(page.locator(`text="${testMessage}"`)).toBeVisible({ timeout: 10000 });
    
    // Input should be cleared after sending
    await expect(messageInput).toHaveValue('');
  });
  
  test('should send message with Shift+Enter for newline and Enter to send', async ({ page }) => {
    await page.goto('/');
    
    const messageInput = page.locator('textarea[placeholder*="message" i]').first();
    if (await messageInput.count() === 0) {
      test.skip();
      return;
    }
    
    await messageInput.fill('Line 1');
    
    // Shift+Enter should add newline
    await messageInput.press('Shift+Enter');
    await messageInput.type('Line 2');
    
    // Verify multiline content
    const value = await messageInput.inputValue();
    expect(value).toContain('\n');
    
    // Enter without shift should send
    await messageInput.press('Enter');
    
    // Message should be sent
    await expect(messageInput).toHaveValue('');
  });
});

test.describe('Messaging - Real-time Message Delivery', () => {
  test('should receive messages in real-time', async ({ page, context }) => {
    // This test simulates two users in the same conversation
    // Open second page (simulating another user)
    const page2 = await context.newPage();
    
    // Both pages navigate to app
    await page.goto('/');
    await page2.goto('/');
    
    // Both select the same conversation
    const conversationSelector = '[role="list"] > *, .conversation-item';
    await page.locator(conversationSelector).first().click();
    await page2.locator(conversationSelector).first().click();
    
    // User 1 sends a message
    const testMessage = `Real-time test ${Date.now()}`;
    const messageInput = page.locator('textarea[placeholder*="message" i]').first();
    await messageInput.fill(testMessage);
    await messageInput.press('Enter');
    
    // User 2 should see the message appear in real-time
    await expect(page2.locator(`text="${testMessage}"`)).toBeVisible({ timeout: 15000 });
    
    await page2.close();
  });
  
  test('should show typing indicator when user is typing', async ({ page, context }) => {
    const page2 = await context.newPage();
    
    await page.goto('/');
    await page2.goto('/');
    
    // Both select the same conversation
    await page.locator('[role="list"] > *, .conversation-item').first().click();
    await page2.locator('[role="list"] > *, .conversation-item').first().click();
    
    // User 1 starts typing
    const messageInput = page.locator('textarea[placeholder*="message" i]').first();
    await messageInput.fill('Typing...');
    
    // User 2 should see typing indicator
    const typingIndicator = page2.locator('text=/typing|is typing|\\.\\.\\.$/i');
    await expect(typingIndicator).toBeVisible({ timeout: 5000 });
    
    await page2.close();
  });
});

test.describe('Messaging - Message Features', () => {
  test('should support emoji in messages', async ({ page }) => {
    await page.goto('/');
    
    const firstConversation = page.locator('[role="list"] > *, .conversation-item').first();
    if (await firstConversation.count() > 0) {
      await firstConversation.click();
    }
    
    const messageInput = page.locator('textarea[placeholder*="message" i]').first();
    
    // Look for emoji picker button
    const emojiButton = page.locator('button[aria-label*="emoji" i], button:has-text("😀")').first();
    if (await emojiButton.count() > 0) {
      await emojiButton.click();
      
      // Emoji picker should appear
      await expect(page.locator('.emoji-picker, [role="dialog"]')).toBeVisible({ timeout: 3000 });
      
      // Select an emoji
      const emoji = page.locator('.emoji-picker button, .emoji-picker span').first();
      await emoji.click();
      
      // Emoji should be added to input
      const value = await messageInput.inputValue();
      expect(value.length).toBeGreaterThan(0);
    }
  });
  
  test('should display message timestamp', async ({ page }) => {
    await page.goto('/');
    
    const firstConversation = page.locator('[role="list"] > *, .conversation-item').first();
    if (await firstConversation.count() > 0) {
      await firstConversation.click();
    }
    
    // Send a message
    const messageInput = page.locator('textarea[placeholder*="message" i]').first();
    const testMessage = `Timestamp test ${Date.now()}`;
    await messageInput.fill(testMessage);
    await messageInput.press('Enter');
    
    // Wait for message to appear
    await page.locator(`text="${testMessage}"`).waitFor({ timeout: 5000 });
    
    // Look for timestamp (various formats: HH:MM, time ago, etc.)
    const timestampPattern = /\d{1,2}:\d{2}|minute|hour|second|just now|am|pm/i;
    const timestamp = page.locator(`text=${timestampPattern}`).first();
    await expect(timestamp).toBeVisible({ timeout: 3000 });
  });
});
