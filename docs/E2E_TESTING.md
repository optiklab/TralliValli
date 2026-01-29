# E2E Testing Documentation

This document provides comprehensive information about End-to-End (E2E) testing for TralliValli.

## Overview

The E2E test suite uses **Playwright** to test the complete user journey through the TralliValli chat application. The tests run against a real environment with all services (MongoDB, RabbitMQ, Redis, API, and Web frontend).

## Test Coverage

The E2E test suite includes 40+ test cases covering all acceptance criteria:

### ✅ Registration Flow (4 tests)
- Successfully register with valid invite link
- Error handling for invalid invite links
- Email format validation
- Display name requirement validation

### ✅ Login Flow (5 tests)
- Send magic link for valid email
- Email format validation
- Email requirement validation
- Loading state display
- Magic link verification

### ✅ Messaging (7 tests)
- Create new direct conversation
- Send text messages
- Multi-line message support (Shift+Enter)
- Real-time message delivery
- Typing indicators
- Emoji support
- Message timestamps

### ✅ Group Management (7 tests)
- Create new groups
- Group name requirement validation
- Add members to existing groups
- Display group members list
- Send messages in groups
- Display group name in header
- Show member count
- Leave group functionality

### ✅ File Upload (8 tests)
- Open file picker
- Upload and send text files
- Upload and send images
- Display file size
- Remove attached files
- Multiple file uploads
- Upload progress indicators
- File download links

### ✅ Logout (9 tests)
- Successful logout and redirect
- Clear session data
- Prevent access to protected routes
- Logout confirmation dialogs
- SignalR disconnection
- Re-login after logout
- Offline logout handling
- Persistent logout state

## Architecture

```
TralliValli/
├── src/web/
│   ├── e2e/
│   │   ├── fixtures.ts           # Shared test utilities and fixtures
│   │   ├── registration.spec.ts  # Registration tests
│   │   ├── login.spec.ts         # Login tests
│   │   ├── messaging.spec.ts     # Messaging tests
│   │   ├── groups.spec.ts        # Group management tests
│   │   ├── file-upload.spec.ts   # File upload tests
│   │   ├── logout.spec.ts        # Logout tests
│   │   └── README.md             # E2E test documentation
│   └── playwright.config.ts      # Playwright configuration
├── docker-compose.e2e.yml        # E2E test environment
└── .github/workflows/ci.yml      # CI pipeline with E2E tests
```

## Running E2E Tests

### Prerequisites

1. **Node.js 18+** installed
2. **Docker and Docker Compose** installed
3. **All dependencies installed**: `cd src/web && npm install`
4. **Playwright browsers installed**: `npx playwright install chromium`

### Local Development

```bash
# Terminal 1: Start Docker services
docker-compose -f docker-compose.e2e.yml up -d

# Terminal 2: Start API backend (configure env vars to point to Docker services)
export MONGODB_CONNECTION_STRING="mongodb://admin:password@localhost:27018/tralivali_e2e?authSource=admin"
export RABBITMQ_CONNECTION_STRING="amqp://admin:password@localhost:5673/"
export REDIS_CONNECTION_STRING="localhost:6380,password=password"
cd src/TraliVali.Api
dotnet run

# Terminal 3: Start web frontend
cd src/web
npm run dev

# Terminal 4: Run E2E tests
cd src/web
npm run test:e2e
```

### Quick Test Commands

```bash
# Run all E2E tests
npm run test:e2e

# Run with UI mode (interactive)
npm run test:e2e:ui

# Run in debug mode
npm run test:e2e:debug

# Run with browser visible
npm run test:e2e:headed

# View test report
npm run test:e2e:report

# Run specific test file
npx playwright test e2e/login.spec.ts

# Run tests matching pattern
npx playwright test --grep "login"
```

## CI/CD Integration

The E2E tests are integrated into GitHub Actions CI pipeline:

### CI Workflow
1. **Build backend**: Compile .NET projects
2. **Start services**: Launch Docker Compose with MongoDB, RabbitMQ, Redis
3. **Install dependencies**: Install Node.js packages and Playwright browsers
4. **Run E2E tests**: Execute full test suite
5. **Upload artifacts**: Save test reports, videos, and screenshots
6. **Cleanup**: Stop Docker services

### Viewing CI Results
- Test reports are uploaded as GitHub Actions artifacts
- Videos and screenshots are available for failed tests
- Check the "Actions" tab in GitHub repository

## Test Environment

### Docker Services

The `docker-compose.e2e.yml` file defines isolated services for testing:

- **MongoDB**: Port 27018 (isolated from dev on 27017)
- **RabbitMQ**: Ports 5673 (AMQP) and 15673 (Management)
- **Redis**: Port 6380 (isolated from dev on 6379)

### Environment Variables

```bash
# E2E Test Database
MONGODB_CONNECTION_STRING=mongodb://admin:password@localhost:27018/tralivali_e2e?authSource=admin

# E2E Test Message Queue
RABBITMQ_CONNECTION_STRING=amqp://admin:password@localhost:5673/

# E2E Test Cache
REDIS_CONNECTION_STRING=localhost:6380,password=password

# Web Application URL
E2E_BASE_URL=http://localhost:5173
```

## Writing Tests

### Test Structure

```typescript
import { test, expect } from './fixtures';

test.describe('Feature Name', () => {
  test('should do something', async ({ page }) => {
    // Navigate to page
    await page.goto('/');
    
    // Interact with elements
    await page.click('button');
    await page.fill('input', 'value');
    
    // Assert expectations
    await expect(page.locator('text=Success')).toBeVisible();
  });
});
```

### Using Test Fixtures

```typescript
test('should use test user', async ({ page, testUser }) => {
  // testUser is automatically generated for each test
  await page.fill('input[name="email"]', testUser.email);
  await page.fill('input[name="displayName"]', testUser.displayName);
});
```

### Helper Functions

The `fixtures.ts` file provides reusable helpers:

```typescript
import { 
  generateTestUser,
  registerUser,
  loginViaMagicLink,
  sendMessage,
  createGroup,
  uploadFile,
  logout
} from './fixtures';
```

## Best Practices

### 1. Test Isolation
- Each test should be independent
- Use unique test data (timestamps, random IDs)
- Don't rely on other tests' state

### 2. Waiting Strategies
```typescript
// ✅ Good: Wait for element to be visible
await expect(page.locator('button')).toBeVisible();

// ❌ Bad: Fixed timeout
await page.waitForTimeout(5000);
```

### 3. Selectors
```typescript
// ✅ Good: User-facing selectors
await page.click('button:has-text("Send")');
await page.fill('input[name="email"]', 'test@example.com');

// ❌ Bad: Implementation-specific selectors
await page.click('.btn-class-xyz-123');
```

### 4. Test Data
```typescript
// ✅ Good: Dynamic test data
const message = `Test message ${Date.now()}`;

// ❌ Bad: Static test data (causes conflicts)
const message = 'Test message';
```

## Debugging

### Debug Specific Test
```bash
npx playwright test --debug e2e/login.spec.ts
```

### View Test Trace
```bash
npx playwright show-trace trace.zip
```

### Take Screenshots
```typescript
await page.screenshot({ path: 'debug-screenshot.png' });
```

### Console Logs
```typescript
page.on('console', msg => console.log('PAGE LOG:', msg.text()));
```

## Troubleshooting

### Tests Timeout
- Increase timeout in `playwright.config.ts`
- Check that services are running: `docker-compose -f docker-compose.e2e.yml ps`
- Verify network connectivity

### Services Not Starting
```bash
# Check logs
docker-compose -f docker-compose.e2e.yml logs

# Restart services
docker-compose -f docker-compose.e2e.yml restart

# Clean restart
docker-compose -f docker-compose.e2e.yml down -v
docker-compose -f docker-compose.e2e.yml up -d
```

### Browser Issues
```bash
# Reinstall browsers
npx playwright install --force chromium
```

### Port Conflicts
If ports are already in use, update `docker-compose.e2e.yml` to use different ports.

## Performance

### Test Execution Time
- Full suite: ~5-10 minutes (depending on hardware)
- Individual test: ~10-30 seconds
- Parallel execution: Disabled to avoid conflicts

### Optimization Tips
1. Run tests serially to avoid resource conflicts
2. Use `.only()` for debugging specific tests
3. Skip slow tests during development with `.skip()`
4. Use `--grep` to run subset of tests

## Maintenance

### Updating Tests
When UI changes:
1. Update selectors in test files
2. Update helper functions in `fixtures.ts`
3. Run tests to verify changes
4. Update documentation if needed

### Adding New Tests
1. Create new spec file in `e2e/` directory
2. Import fixtures: `import { test, expect } from './fixtures';`
3. Write test cases
4. Run tests: `npm run test:e2e`
5. Update this documentation

## Resources

- [Playwright Documentation](https://playwright.dev/)
- [Playwright Best Practices](https://playwright.dev/docs/best-practices)
- [Playwright API Reference](https://playwright.dev/docs/api/class-playwright)
- [Debugging Guide](https://playwright.dev/docs/debug)

## Support

For issues or questions:
1. Check troubleshooting section above
2. Review Playwright documentation
3. Check GitHub Actions logs for CI failures
4. Review test reports and screenshots

## Summary

The E2E test suite provides comprehensive coverage of all user-facing features in TralliValli. Tests run against a real Docker Compose environment and are integrated into the CI pipeline for continuous validation. The suite is designed to be reliable, maintainable, and easy to extend.
