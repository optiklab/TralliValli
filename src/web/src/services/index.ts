// Export your API services from this folder
// Example: export { default as apiClient } from './apiClient';

export {
  SignalRService,
  ConnectionState,
  type ChatClientHandlers,
  type ConnectionStateHandler,
  type SignalRServiceOptions,
} from './signalr';

export { apiClient, default as api } from './api';

export {
  OfflineStorage,
  type QueuedOutgoingMessage,
  type SyncMetadata,
  type ConflictResolutionOptions,
} from './offlineStorage';

export {
  FileUploadService,
  fileUploadService,
  type UploadOptions,
  type UploadResult,
} from './fileUpload';

export {
  CryptoKeyExchange,
  type KeyPair,
  type ExportedKeyPair,
  type StoredKeyPair,
} from './cryptoKeyExchange';

export {
  encrypt,
  decrypt,
  generateKey,
  importKey,
  exportKey,
  encryptToBase64,
  decryptFromBase64,
  type EncryptionResult,
  type EncryptedData,
} from './aesGcmEncryption';

export {
  KeyManagementService,
  type StoredConversationKey,
  type KeyRotationRecord,
  type ConversationKeyInfo,
} from './keyManagement';

export {
  MessageEncryptionService,
  type MessageEncryptionResult,
  type MessageDecryptionResult,
} from './messageEncryption';

export {
  FileEncryptionService,
  type FileEncryptionProgress,
  type EncryptFileResult,
  type DecryptFileResult,
  type EncryptedFileMetadata,
} from './fileEncryption';

export {
  FileDownloadService,
  type DownloadProgress,
  type DownloadOptions,
} from './fileDownload';

