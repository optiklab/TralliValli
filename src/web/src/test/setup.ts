import '@testing-library/jest-dom';
import 'fake-indexeddb/auto';
import _sodium from 'libsodium-wrappers';

// Initialize libsodium for tests
await _sodium.ready;
