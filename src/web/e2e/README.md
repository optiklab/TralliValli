# E2E Testing with Playwright

This directory contains End-to-End (E2E) tests for the TralliValli chat application using Playwright.

## Overview

The E2E tests cover the following scenarios:
- ✅ Registration via invite link
- ✅ Login via magic link
- ✅ Create direct conversation
- ✅ Send text message
- ✅ Receive message in real-time
- ✅ Create group
- ✅ Add member to group
- ✅ Upload and send file
- ✅ Logout

## Prerequisites

- Node.js 18+ installed
- Docker and Docker Compose installed (for running against Docker environment)
- All dependencies installed: `npm install`

## Installation

1. Install Playwright dependencies:
```bash
cd src/web
npm install
```

2. Install Playwright browsers:
```bash
npx playwright install chromium
```

## Running Tests

### Local Development

Run tests against your local development server:

```bash
# Start the dev server (in one terminal)
npm run dev

# Run E2E tests (in another terminal)
npm run test:e2e
```

### Against Docker Compose Environment

1. Start the Docker Compose services:
```bash
# From the repository root
docker-compose -f docker-compose.e2e.yml up -d

# Wait for services to be healthy
docker-compose -f docker-compose.e2e.yml ps
```

2. Start your API backend (if not containerized):
```bash
# Configure environment variables to point to Docker services
export MONGODB_CONNECTION_STRING="mongodb://admin:password@localhost:27018/tralivali_e2e?authSource=admin"
export RABBITMQ_CONNECTION_STRING="amqp://admin:password@localhost:5673/"
export REDIS_CONNECTION_STRING="localhost:6380,password=password"

# Start the API
cd src/TraliVali.Api
dotnet run
```

3. Start the web frontend:
```bash
cd src/web
npm run dev
```

4. Run the E2E tests:
```bash
npm run test:e2e
```

### Test Commands

```bash
# Run all E2E tests
npm run test:e2e

# Run tests in UI mode (interactive)
npm run test:e2e:ui

# Run tests in debug mode
npm run test:e2e:debug

# Run tests with browser visible (headed mode)
npm run test:e2e:headed

# Show test report
npm run test:e2e:report

# Run specific test file
npx playwright test e2e/login.spec.ts

# Run tests matching a pattern
npx playwright test --grep "login"
```

## Test Structure

```
e2e/
├── fixtures/
│   └── index.ts           # Shared test fixtures and helpers
├── registration.spec.ts   # Registration via invite link tests
├── login.spec.ts          # Magic link login tests
├── messaging.spec.ts      # Direct messaging and real-time tests
├── groups.spec.ts         # Group creation and management tests
├── file-upload.spec.ts    # File upload and sharing tests
└── logout.spec.ts         # Logout functionality tests
```

## Configuration

E2E test configuration is in `playwright.config.ts`:

- **Base URL**: `http://localhost:5173` (configurable via `E2E_BASE_URL` env var)
- **Timeout**: 60 seconds per test
- **Retries**: 2 on CI, 0 locally
- **Browsers**: Chromium (can be extended to Firefox, WebKit)
- **Reports**: HTML report and list format

### Environment Variables

```bash
# Base URL for the web application
E2E_BASE_URL=http://localhost:5173

# Set to true when running in CI
CI=true
```

## CI Integration

The E2E tests are integrated into the GitHub Actions CI workflow. See `.github/workflows/ci.yml` for the complete CI configuration.

### CI Workflow

1. Start Docker Compose services
2. Build and start API backend
3. Build and start web frontend
4. Run E2E tests
5. Upload test artifacts (reports, videos, screenshots)

## Writing Tests

### Basic Test Structure

```typescript
import { test, expect } from '../fixtures';

test.describe('Feature Name', () => {
  test('should do something', async ({ page }) => {
    await page.goto('/');
    
    // Interact with the page
    await page.click('button');
    
    // Assert expectations
    await expect(page.locator('text=Success')).toBeVisible();
  });
});
```

### Using Test Fixtures

```typescript
test('should use test user', async ({ page, testUser }) => {
  // testUser provides a unique test user for each test
  await page.fill('input[name="email"]', testUser.email);
});
```

### Helper Functions

The `fixtures/index.ts` file provides helper functions:

- `generateTestUser()` - Generate test user data
- `registerUser()` - Register a new user
- `loginViaMagicLink()` - Login via magic link
- `sendMessage()` - Send a message in conversation
- `createGroup()` - Create a group conversation
- `uploadFile()` - Upload a file attachment
- `logout()` - Logout the user

## Test Data

Tests use dynamically generated test data to avoid conflicts:
- Email addresses include timestamps
- Display names are unique per test run
- Group names include timestamps

## Debugging

### Debug a specific test:
```bash
npx playwright test --debug e2e/login.spec.ts
```

### View test trace:
```bash
npx playwright show-trace trace.zip
```

### Take screenshots during test development:
```typescript
await page.screenshot({ path: 'screenshot.png' });
```

## Troubleshooting

### Tests are failing with timeout

- Increase timeout in `playwright.config.ts`
- Check that services are running and healthy
- Verify network connectivity to services

### Services not starting

```bash
# Check service logs
docker-compose -f docker-compose.e2e.yml logs

# Restart services
docker-compose -f docker-compose.e2e.yml restart
```

### Browser not found

```bash
# Install browsers
npx playwright install chromium
```

## Best Practices

1. **Test Isolation**: Each test should be independent and not rely on other tests
2. **Clean State**: Use unique test data for each test run
3. **Wait for Elements**: Always wait for elements to be visible before interacting
4. **Descriptive Names**: Use clear, descriptive test names
5. **Page Object Pattern**: Consider using page objects for complex pages
6. **Minimal Assertions**: Focus on testing user-visible behavior
7. **Error Handling**: Tests should handle edge cases gracefully

## Resources

- [Playwright Documentation](https://playwright.dev/)
- [Best Practices](https://playwright.dev/docs/best-practices)
- [API Reference](https://playwright.dev/docs/api/class-playwright)
