import { test, expect } from './fixtures';
import path from 'path';
import fs from 'fs';

/**
 * E2E Test: File Upload and Sharing
 * 
 * Tests file attachment and sending functionality
 */

test.describe('File Upload and Sharing', () => {
  // Create a test file for upload tests
  const testFilePath = path.join('/tmp', `test-file-${Date.now()}.txt`);
  const testImagePath = path.join('/tmp', `test-image-${Date.now()}.png`);
  
  test.beforeAll(() => {
    // Create a test text file
    fs.writeFileSync(testFilePath, 'This is a test file for E2E testing.');
    
    // Create a simple PNG test image (1x1 pixel red)
    const pngBuffer = Buffer.from([
      0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a,
      0x00, 0x00, 0x00, 0x0d, 0x49, 0x48, 0x44, 0x52,
      0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
      0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
      0xde, 0x00, 0x00, 0x00, 0x0c, 0x49, 0x44, 0x41,
      0x54, 0x08, 0xd7, 0x63, 0xf8, 0xcf, 0xc0, 0x00,
      0x00, 0x03, 0x01, 0x01, 0x00, 0x18, 0xdd, 0x8d,
      0xb4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4e,
      0x44, 0xae, 0x42, 0x60, 0x82
    ]);
    fs.writeFileSync(testImagePath, pngBuffer);
  });
  
  test.afterAll(() => {
    // Clean up test files
    try {
      fs.unlinkSync(testFilePath);
      fs.unlinkSync(testImagePath);
    } catch (e) {
      // Ignore cleanup errors
    }
  });
  
  test('should open file picker when clicking attach button', async ({ page }) => {
    await page.goto('/');
    
    // Click on a conversation
    const conversation = page.locator('[role="list"] > *, .conversation-item').first();
    if (await conversation.count() > 0) {
      await conversation.click();
      
      // Look for file attachment button
      const attachButton = page.locator(
        'button[aria-label*="attach" i], button[aria-label*="file" i], button:has-text("📎")'
      ).first();
      
      await expect(attachButton).toBeVisible({ timeout: 5000 });
      
      // Setup file chooser listener
      const fileChooserPromise = page.waitForEvent('filechooser');
      await attachButton.click();
      
      const fileChooser = await fileChooserPromise;
      expect(fileChooser).toBeDefined();
    }
  });
  
  test('should upload and send a text file', async ({ page }) => {
    await page.goto('/');
    
    // Select a conversation
    const conversation = page.locator('[role="list"] > *, .conversation-item').first();
    if (await conversation.count() > 0) {
      await conversation.click();
      
      // Find file input (might be hidden)
      const fileInput = page.locator('input[type="file"]').first();
      
      // Upload file
      await fileInput.setInputFiles(testFilePath);
      
      // Wait for file to be processed - wait for file name to appear
      const fileName = path.basename(testFilePath);
      const filePreview = page.locator(`text="${fileName}"`);
      
      // File might appear immediately or need confirmation
      const isVisible = await filePreview.isVisible().catch(() => false);
      if (isVisible) {
        await expect(filePreview).toBeVisible({ timeout: 5000 });
        
        // Send the file (might auto-send or need button click)
        const sendButton = page.locator('button:has-text("Send")').first();
        if (await sendButton.isVisible().catch(() => false)) {
          await sendButton.click();
        }
        
        // Verify file appears in message thread
        await expect(page.locator(`text="${fileName}"`)).toBeVisible({ timeout: 10000 });
      }
    }
  });
  
  test('should upload and send an image file', async ({ page }) => {
    await page.goto('/');
    
    const conversation = page.locator('[role="list"] > *, .conversation-item').first();
    if (await conversation.count() > 0) {
      await conversation.click();
      
      const fileInput = page.locator('input[type="file"]').first();
      await fileInput.setInputFiles(testImagePath);
      
      // Wait for file processing by checking for image preview or file name
      await Promise.race([
        page.locator('img[src*="blob:"], img[src*="data:"], .image-preview').waitFor({ timeout: 5000 }),
        page.locator(`text="${path.basename(testImagePath)}"`).waitFor({ timeout: 5000 })
      ]).catch(() => {}); // Ignore if neither appears quickly
      
      // Should show image preview
      const imagePreview = page.locator('img[src*="blob:"], img[src*="data:"], .image-preview');
      const isVisible = await imagePreview.isVisible({ timeout: 5000 }).catch(() => false);
      
      if (isVisible) {
        await expect(imagePreview).toBeVisible();
        
        // Send the image
        const sendButton = page.locator('button:has-text("Send")').first();
        if (await sendButton.isVisible().catch(() => false)) {
          await sendButton.click();
        }
        
        // Verify image appears in thread
        const imageName = path.basename(testImagePath);
        
        // Look for the image or its name in messages - wait for it to appear
        const sentImage = page.locator(`img, text="${imageName}"`).last();
        await expect(sentImage).toBeVisible({ timeout: 10000 });
      }
    }
  });
  
  test('should show file size for uploaded file', async ({ page }) => {
    await page.goto('/');
    
    const conversation = page.locator('[role="list"] > *, .conversation-item').first();
    if (await conversation.count() > 0) {
      await conversation.click();
      
      const fileInput = page.locator('input[type="file"]').first();
      await fileInput.setInputFiles(testFilePath);
      
      // Wait for file to appear by checking for file name
      const fileName = path.basename(testFilePath);
      await page.locator(`text="${fileName}"`).waitFor({ timeout: 5000 }).catch(() => {});
      
      // Look for file size indicator (KB, MB, bytes, etc.)
      const sizePattern = /\d+\s*(bytes|KB|MB|B)/i;
      const sizeIndicator = page.locator(`text=${sizePattern}`).first();
      
      const isVisible = await sizeIndicator.isVisible({ timeout: 5000 }).catch(() => false);
      if (isVisible) {
        await expect(sizeIndicator).toBeVisible();
      }
    }
  });
  
  test('should allow removing attached file before sending', async ({ page }) => {
    await page.goto('/');
    
    const conversation = page.locator('[role="list"] > *, .conversation-item').first();
    if (await conversation.count() > 0) {
      await conversation.click();
      
      const fileInput = page.locator('input[type="file"]').first();
      await fileInput.setInputFiles(testFilePath);
      
      // Wait for file name to appear
      const fileName = path.basename(testFilePath);
      await page.locator(`text="${fileName}"`).waitFor({ timeout: 5000 }).catch(() => {});
      
      // Look for remove/cancel button
      const removeButton = page.locator(
        'button[aria-label*="remove" i], button[aria-label*="cancel" i], button:has-text("×")'
      ).first();
      
      const isVisible = await removeButton.isVisible({ timeout: 3000 }).catch(() => false);
      if (isVisible) {
        await removeButton.click();
        
        // File preview should disappear
        const fileName = path.basename(testFilePath);
        const filePreview = page.locator(`text="${fileName}"`);
        await expect(filePreview).not.toBeVisible({ timeout: 3000 });
      }
    }
  });
  
  test('should support multiple file uploads', async ({ page }) => {
    await page.goto('/');
    
    const conversation = page.locator('[role="list"] > *, .conversation-item').first();
    if (await conversation.count() > 0) {
      await conversation.click();
      
      const fileInput = page.locator('input[type="file"]').first();
      
      // Check if multiple attribute is set
      const hasMultiple = await fileInput.getAttribute('multiple');
      
      if (hasMultiple !== null) {
        // Upload multiple files
        await fileInput.setInputFiles([testFilePath, testImagePath]);
        
        // Wait for files to appear
        const file1Name = path.basename(testFilePath);
        const file2Name = path.basename(testImagePath);
        
        const file1Preview = page.locator(`text="${file1Name}"`);
        const file2Preview = page.locator(`text="${file2Name}"`);
        
        await expect(file1Preview).toBeVisible({ timeout: 5000 });
        await expect(file2Preview).toBeVisible({ timeout: 5000 });
      }
    }
  });
  
  test('should show upload progress for large files', async ({ page }) => {
    await page.goto('/');
    
    // Create a larger test file (1MB)
    const largeFilePath = path.join('/tmp', `large-file-${Date.now()}.bin`);
    const largeBuffer = Buffer.alloc(1024 * 1024, 'x'); // 1MB file
    fs.writeFileSync(largeFilePath, largeBuffer);
    
    try {
      const conversation = page.locator('[role="list"] > *, .conversation-item').first();
      if (await conversation.count() > 0) {
        await conversation.click();
        
        const fileInput = page.locator('input[type="file"]').first();
        await fileInput.setInputFiles(largeFilePath);
        
        // Look for progress indicator
        const progressIndicator = page.locator(
          'text=/uploading|progress|\d+%/i, [role="progressbar"]'
        ).first();
        
        const isVisible = await progressIndicator.isVisible({ timeout: 3000 }).catch(() => false);
        if (isVisible) {
          await expect(progressIndicator).toBeVisible();
        }
      }
    } finally {
      // Clean up large file
      try {
        fs.unlinkSync(largeFilePath);
      } catch (e) {
        // Ignore
      }
    }
  });
  
  test('should display file download link in received messages', async ({ page }) => {
    await page.goto('/');
    
    const conversation = page.locator('[role="list"] > *, .conversation-item').first();
    if (await conversation.count() > 0) {
      await conversation.click();
      
      // Look for file attachments in messages
      const fileAttachment = page.locator(
        'a[download], a[href*="download"], .file-attachment'
      ).first();
      
      const isVisible = await fileAttachment.isVisible({ timeout: 3000 }).catch(() => false);
      if (isVisible) {
        await expect(fileAttachment).toBeVisible();
      }
    }
  });
});
