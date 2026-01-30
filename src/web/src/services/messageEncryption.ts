/**
 * Message Encryption Service
 *
 * Provides high-level encryption/decryption for messages using the key management service
 * and AES-GCM encryption.
 *
 * Features:
 * - Encrypt messages before sending
 * - Decrypt messages on display
 * - Handle decryption failures gracefully
 * - Integrate with key management
 */

import { KeyManagementService } from './keyManagement';
import { encryptToBase64, decryptFromBase64, type EncryptedData } from './aesGcmEncryption';

export interface MessageEncryptionResult {
  encryptedContent: string; // JSON string containing { iv, ciphertext, tag }
  success: boolean;
  error?: string;
}

export interface MessageDecryptionResult {
  content: string;
  success: boolean;
  error?: string;
}

/**
 * Message Encryption Service
 *
 * Handles encryption and decryption of message content using conversation keys.
 */
export class MessageEncryptionService {
  private keyManagementService: KeyManagementService;

  constructor(keyManagementService: KeyManagementService) {
    this.keyManagementService = keyManagementService;
  }

  /**
   * Encrypt a message for a conversation
   *
   * @param conversationId - The conversation ID
   * @param content - The plaintext message content
   * @returns Encryption result with encrypted content or error
   */
  async encryptMessage(
    conversationId: string,
    content: string
  ): Promise<MessageEncryptionResult> {
    try {
      // Get the conversation key
      const conversationKey = await this.keyManagementService.getConversationKey(conversationId);

      if (!conversationKey) {
        return {
          encryptedContent: '',
          success: false,
          error: 'No encryption key found for conversation',
        };
      }

      // Encrypt the message content
      const encrypted = await encryptToBase64(conversationKey, content);

      // Serialize the encrypted data as JSON
      const encryptedContent = JSON.stringify(encrypted);

      return {
        encryptedContent,
        success: true,
      };
    } catch (error) {
      return {
        encryptedContent: '',
        success: false,
        error: `Encryption failed: ${error instanceof Error ? error.message : 'Unknown error'}`,
      };
    }
  }

  /**
   * Decrypt a message for display
   *
   * @param conversationId - The conversation ID
   * @param encryptedContent - The encrypted message content (JSON string)
   * @returns Decryption result with plaintext content or error
   */
  async decryptMessage(
    conversationId: string,
    encryptedContent: string
  ): Promise<MessageDecryptionResult> {
    try {
      // Handle empty or invalid encrypted content
      if (!encryptedContent || encryptedContent.trim() === '') {
        return {
          content: '',
          success: false,
          error: 'No encrypted content provided',
        };
      }

      // Get the conversation key
      const conversationKey = await this.keyManagementService.getConversationKey(conversationId);

      if (!conversationKey) {
        return {
          content: '',
          success: false,
          error: 'No decryption key found for conversation',
        };
      }

      // Parse the encrypted data
      let encrypted: EncryptedData;
      try {
        encrypted = JSON.parse(encryptedContent) as EncryptedData;
      } catch (parseError) {
        return {
          content: '',
          success: false,
          error: 'Invalid encrypted content format',
        };
      }

      // Validate encrypted data structure
      if (!encrypted.iv || !encrypted.ciphertext || !encrypted.tag) {
        return {
          content: '',
          success: false,
          error: 'Incomplete encrypted data',
        };
      }

      // Decrypt the message content
      const decryptedBytes = await decryptFromBase64(conversationKey, encrypted);

      // Convert bytes to string
      const content = new TextDecoder().decode(decryptedBytes);

      return {
        content,
        success: true,
      };
    } catch (error) {
      return {
        content: '',
        success: false,
        error: `Decryption failed: ${error instanceof Error ? error.message : 'Unknown error'}`,
      };
    }
  }

  /**
   * Encrypt a message or return the plaintext if encryption is not available
   * 
   * This is a helper method for scenarios where encryption is optional (e.g., during migration)
   * 
   * @param conversationId - The conversation ID
   * @param content - The plaintext message content
   * @returns Encryption result
   */
  async encryptMessageOrFallback(
    conversationId: string,
    content: string
  ): Promise<MessageEncryptionResult> {
    const result = await this.encryptMessage(conversationId, content);
    
    // If encryption fails, return the plaintext (for backward compatibility)
    if (!result.success) {
      return {
        encryptedContent: content,
        success: false,
        error: result.error,
      };
    }
    
    return result;
  }

  /**
   * Decrypt a message or return a placeholder on failure
   * 
   * This is a helper method that always returns a displayable string
   * 
   * @param conversationId - The conversation ID
   * @param encryptedContent - The encrypted message content
   * @param fallbackPlaintext - Optional plaintext fallback (for messages sent before encryption)
   * @returns Decrypted content or placeholder
   */
  async decryptMessageOrPlaceholder(
    conversationId: string,
    encryptedContent: string,
    fallbackPlaintext?: string
  ): Promise<string> {
    // If we have plaintext fallback and no encrypted content, use plaintext
    if (fallbackPlaintext && (!encryptedContent || encryptedContent.trim() === '')) {
      return fallbackPlaintext;
    }

    const result = await this.decryptMessage(conversationId, encryptedContent);
    
    if (result.success) {
      return result.content;
    }
    
    // If decryption fails, return placeholder
    return '[Unable to decrypt message]';
  }
}
