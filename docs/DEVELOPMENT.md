# Local Development Guide

This guide covers everything you need to develop TralliValli locally, from setting up prerequisites to submitting pull requests.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Running the Application](#running-the-application)
- [Running Tests](#running-tests)
- [Code Style Guidelines](#code-style-guidelines)
- [Pull Request Process](#pull-request-process)
- [Troubleshooting](#troubleshooting)

## Prerequisites

Before you begin, ensure you have the following installed on your development machine:

### Required Software

1. **Docker Desktop** (version 20.10 or higher)
   - [Docker Desktop for Windows](https://docs.docker.com/desktop/install/windows-install/)
   - [Docker Desktop for Mac](https://docs.docker.com/desktop/install/mac-install/)
   - [Docker Engine for Linux](https://docs.docker.com/engine/install/)
   - Docker Compose v2.0 or higher (included with Docker Desktop)

2. **.NET 8 SDK**
   - Download from [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
   - Verify installation: `dotnet --version` (should show 8.x.x)

3. **Node.js 20** (LTS)
   - Download from [https://nodejs.org/](https://nodejs.org/)
   - Verify installation: `node --version` (should show v20.x.x)
   - npm comes bundled with Node.js

### Recommended Tools

- **IDE/Editor**: Visual Studio 2022, Visual Studio Code, or JetBrains Rider
- **Git**: For version control
- **MongoDB Compass**: For database inspection (optional)
- **Postman** or **Insomnia**: For API testing (optional)

### Verify Prerequisites

```bash
# Check Docker
docker --version
docker compose version

# Check .NET SDK
dotnet --version

# Check Node.js and npm
node --version
npm --version
```

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/optiklab/TralliValli.git
cd TralliValli
```

### 2. Set Up Environment Variables

Copy the example environment file and configure it:

```bash
cp .env.example .env
```

Edit `.env` if you need to change default values (optional for local development).

**Default configuration works out of the box** with:
- MongoDB on port 27017
- RabbitMQ on ports 5672 (AMQP) and 15672 (Management UI)
- Redis on port 6379

### 3. Start Docker Services

Start the required infrastructure services (MongoDB, RabbitMQ, Redis):

```bash
docker compose up -d
```

Verify services are running:

```bash
docker compose ps
```

All services should show status as "healthy" after ~30 seconds. Check logs if needed:

```bash
docker compose logs -f
```

## Running the Application

### Running the API (Backend)

#### Option 1: Using dotnet CLI (Recommended for Development)

```bash
cd src/TraliVali.Api
dotnet run
```

The API will start on:
- HTTP: `http://localhost:5248`
- HTTPS: `https://localhost:7053`
- Swagger UI: `http://localhost:5248/swagger`

#### Option 2: Debug Mode in Visual Studio

1. Open `TraliVali.slnx` in Visual Studio 2022
2. Set `TraliVali.Api` as the startup project
3. Press F5 to run with debugging, or Ctrl+F5 to run without debugging
4. The browser will open with Swagger UI

#### Option 3: Debug Mode in VS Code

1. Open the project folder in VS Code
2. Install the C# Dev Kit extension
3. Open `src/TraliVali.Api/Program.cs`
4. Press F5 to start debugging
5. Select ".NET Core" as the environment if prompted

#### Environment Variables for API

The API reads connection strings from environment variables or configuration. You can set them via:

**Environment Variables** (Recommended for local development):
```bash
export MONGODB_CONNECTION_STRING="mongodb://admin:password@localhost:27017/tralivali?authSource=admin"
export RABBITMQ_CONNECTION_STRING="amqp://admin:password@localhost:5672/"
export REDIS_CONNECTION_STRING="localhost:6379,password=password"
```

**Or appsettings.Development.json**:
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://admin:password@localhost:27017/tralivali?authSource=admin"
  },
  "RabbitMQ": {
    "ConnectionString": "amqp://admin:password@localhost:5672/"
  },
  "Redis": {
    "ConnectionString": "localhost:6379,password=password"
  }
}
```

These default values match the Docker Compose configuration.

### Running the Web Client (Frontend)

```bash
cd src/web
npm install  # First time only
npm run dev
```

The web application will start on `http://localhost:5173`

Open your browser and navigate to the URL shown in the terminal.

#### Development Mode Features

- **Hot Module Replacement (HMR)**: Changes are reflected immediately
- **React Fast Refresh**: Preserves component state during edits
- **TypeScript Type Checking**: Real-time type checking in the console

### Running Both Together

For full-stack development, you'll typically run both the API and web client:

**Terminal 1** - Docker Services:
```bash
docker compose up -d
```

**Terminal 2** - API Backend:
```bash
cd src/TraliVali.Api
dotnet run
```

**Terminal 3** - Web Frontend:
```bash
cd src/web
npm run dev
```

Now you can access:
- Web App: http://localhost:5173
- API: http://localhost:5248
- Swagger: http://localhost:5248/swagger
- RabbitMQ Management: http://localhost:15672 (admin/password)

## Running Tests

### .NET Backend Tests

#### Run All Tests

```bash
# From repository root
dotnet test TraliVali.slnx

# Or from test project directory
cd tests/TraliVali.Tests
dotnet test
```

#### Run Tests with Coverage

```bash
dotnet test TraliVali.slnx --collect:"XPlat Code Coverage"
```

#### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~TraliVali.Tests.Auth.LoginTests"
```

#### Run Tests by Category

```bash
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
```

#### Debug Tests in Visual Studio

1. Open Test Explorer (Test > Test Explorer)
2. Right-click on a test
3. Select "Debug"

### Web Frontend Tests

#### Unit Tests (Vitest)

```bash
cd src/web

# Run all unit tests
npm run test

# Run tests in watch mode
npm run test:watch

# Run tests with UI
npm run test:ui
```

#### E2E Tests (Playwright)

E2E tests require all services to be running. See [E2E_TESTING.md](./E2E_TESTING.md) for detailed instructions.

```bash
# Prerequisites: Start Docker services and API
docker compose -f docker-compose.e2e.yml up -d
cd src/TraliVali.Api && dotnet run

# In another terminal, run E2E tests
cd src/web

# Install Playwright browsers (first time only)
npx playwright install chromium

# Run E2E tests
npm run test:e2e

# Run with UI mode (interactive)
npm run test:e2e:ui

# Run in debug mode
npm run test:e2e:debug

# View test report
npm run test:e2e:report
```

#### Linting and Formatting

```bash
cd src/web

# Run ESLint
npm run lint

# Fix ESLint issues automatically
npm run lint:fix

# Format code with Prettier
npm run format
```

## Code Style Guidelines

### .NET Backend

The .NET codebase follows standard C# conventions and best practices:

#### Naming Conventions

- **Classes, Methods, Properties**: PascalCase
  ```csharp
  public class UserService
  {
      public string DisplayName { get; set; }
      public void SendMessage() { }
  }
  ```

- **Private fields**: _camelCase with underscore prefix
  ```csharp
  private readonly IUserRepository _userRepository;
  ```

- **Local variables, parameters**: camelCase
  ```csharp
  var userName = "John";
  public void ProcessUser(string userId) { }
  ```

- **Constants**: PascalCase or UPPER_CASE
  ```csharp
  public const int MaxRetries = 3;
  private const string DEFAULT_VALUE = "default";
  ```

#### Code Organization

- One class per file
- File name matches class name
- Use `namespace` file-scoped declarations (C# 10+)
- Group using statements logically
- Order members: fields → constructors → properties → methods

#### Best Practices

- Use dependency injection for services
- Async methods should be suffixed with `Async`
- Use nullable reference types (`string?` for nullable)
- Prefer `var` for local variables when type is obvious
- Use `readonly` for fields that don't change after construction
- Add XML documentation comments for public APIs

### TypeScript/React Frontend

The frontend follows modern React and TypeScript best practices with Prettier and ESLint enforcing code style.

#### Configuration

- **Prettier** (`.prettierrc`): Enforces consistent formatting
  - Semi-colons: Yes
  - Single quotes: Yes
  - Print width: 100
  - Tab width: 2 spaces
  - Trailing commas: ES5

- **ESLint** (`eslint.config.js`): Enforces code quality
  - Based on recommended rules + React hooks + TypeScript
  - Import ordering enforced
  - Accessibility rules (jsx-a11y)
  - No console logs (except warn/error)

#### Naming Conventions

- **Components**: PascalCase
  ```typescript
  function MessageList() { }
  const UserAvatar = () => { };
  ```

- **Files**: PascalCase for components, camelCase for utilities
  ```
  MessageList.tsx
  userService.ts
  ```

- **Variables, functions**: camelCase
  ```typescript
  const userName = 'John';
  function sendMessage() { }
  ```

- **Constants**: UPPER_CASE
  ```typescript
  const MAX_FILE_SIZE = 10_000_000;
  ```

- **Interfaces/Types**: PascalCase, prefer types over interfaces
  ```typescript
  type User = {
    id: string;
    name: string;
  };
  ```

#### React Best Practices

- Use functional components with hooks
- Prefer custom hooks for shared logic
- Use Zustand for state management
- Keep components focused and single-purpose
- Extract reusable logic into hooks or utility functions
- Use TypeScript for type safety

#### Import Order

ESLint enforces this order:
1. Built-in modules (e.g., `react`)
2. External modules (e.g., `zustand`)
3. Internal modules (e.g., `@/components`)
4. Parent imports
5. Sibling imports

#### File Structure

```
src/
├── components/     # Reusable UI components
├── pages/          # Page-level components
├── hooks/          # Custom React hooks
├── stores/         # Zustand stores
├── services/       # API services
├── utils/          # Utility functions
├── types/          # TypeScript type definitions
└── styles/         # Global styles
```

### General Guidelines

- **Write meaningful commit messages**: Use imperative mood, present tense
  ```
  Good: "Add user authentication feature"
  Bad: "Added stuff", "Fixed things"
  ```

- **Keep commits focused**: One logical change per commit
- **Write self-documenting code**: Clear variable/function names reduce need for comments
- **Test your changes**: Add tests for new features, update tests for changed behavior
- **Don't commit sensitive data**: No passwords, API keys, or secrets

## Pull Request Process

### 1. Create a Feature Branch

```bash
# Update your local main branch
git checkout main
git pull origin main

# Create a new branch
git checkout -b feature/your-feature-name
```

Branch naming conventions:
- `feature/description` - for new features
- `fix/description` - for bug fixes
- `docs/description` - for documentation
- `refactor/description` - for code refactoring

### 2. Make Your Changes

- Write clean, well-documented code
- Follow the code style guidelines above
- Add/update tests as needed
- Keep commits focused and atomic

### 3. Test Your Changes

Before submitting a PR, ensure:

```bash
# .NET tests pass
dotnet test TraliVali.slnx

# Web tests pass
cd src/web
npm run test
npm run lint

# Build succeeds
dotnet build TraliVali.slnx
cd src/web && npm run build
```

### 4. Commit Your Changes

```bash
git add .
git commit -m "Brief description of changes"
```

Write clear commit messages:
- First line: Brief summary (50 chars or less)
- Blank line
- Detailed explanation if needed (wrap at 72 chars)

Example:
```
Add user profile editing feature

- Add ProfileEdit component with form validation
- Implement PUT /api/users/profile endpoint
- Add unit tests for profile service
- Update API documentation
```

### 5. Push Your Branch

```bash
git push origin feature/your-feature-name
```

### 6. Create Pull Request

1. Go to the repository on GitHub
2. Click "Pull requests" > "New pull request"
3. Select your branch
4. Fill in the PR template:
   - **Title**: Clear, concise description
   - **Description**: What changes were made and why
   - **Testing**: How to test the changes
   - **Screenshots**: For UI changes (required)
   - **Checklist**: Complete all items

### 7. PR Review Process

- **Automated checks**: CI/CD pipeline runs automatically
  - .NET build and tests
  - Web linting and tests
  - E2E tests (if applicable)
- **Code review**: At least one approval required
- **Address feedback**: Make requested changes, push updates
- **Keep PR updated**: Resolve merge conflicts if needed

### PR Best Practices

- **Keep PRs small**: Easier to review, faster to merge
- **Link related issues**: Reference issue numbers (#123)
- **Respond to feedback**: Address all comments
- **Be respectful**: Professional and constructive communication
- **Update documentation**: If your changes affect docs
- **Add screenshots**: For all UI changes (required)
- **Test thoroughly**: Both locally and in the CI environment

### PR Checklist

Before submitting, verify:
- [ ] Code follows style guidelines
- [ ] All tests pass locally
- [ ] New tests added for new features
- [ ] Documentation updated (if applicable)
- [ ] No console.log or debug code left
- [ ] Commit messages are clear
- [ ] PR description is complete
- [ ] Screenshots attached (for UI changes)

## Troubleshooting

### Docker Issues

#### Ports Already in Use

```bash
# Find what's using the port
lsof -i :27017  # macOS/Linux
netstat -ano | findstr :27017  # Windows

# Change ports in .env file
MONGO_PORT=27018
RABBITMQ_PORT=5673
REDIS_PORT=6380
```

#### Services Not Starting

```bash
# Check logs
docker compose logs -f mongodb
docker compose logs -f rabbitmq
docker compose logs -f redis

# Restart services
docker compose restart

# Full reset (WARNING: deletes all data)
docker compose down -v
docker compose up -d
```

### .NET API Issues

#### Connection String Errors

Verify connection strings in `src/TraliVali.Api/appsettings.Development.json` match your Docker configuration.

#### Port Conflicts

Change the port in `launchSettings.json` or use:
```bash
dotnet run --urls "http://localhost:5000"
```

#### Missing Dependencies

```bash
dotnet restore TraliVali.slnx
```

### Web Client Issues

#### Port 5173 Already in Use

Vite will automatically use the next available port (5174, 5175, etc.)

#### Module Not Found Errors

```bash
cd src/web
rm -rf node_modules package-lock.json
npm install
```

#### Build Errors

```bash
# Clear TypeScript cache
rm -rf src/web/node_modules/.vite
npm run build
```

### Test Failures

#### E2E Tests Timeout

- Ensure all services are running
- Increase timeout in `playwright.config.ts`
- Check network connectivity

#### Unit Tests Fail

```bash
# Clear test cache
dotnet clean
dotnet test --no-build
```

## Additional Resources

- [Docker Compose Setup Guide](./docker-compose-setup.md)
- [E2E Testing Guide](./E2E_TESTING.md)
- [Project Roadmap](./PROJECT_ROADMAP.md)
- [Azure Blob Storage Configuration](./AZURE_BLOB_STORAGE_CONFIGURATION.md)
- [SSL Configuration](./SSL_CONFIGURATION.md)

## Getting Help

- **Documentation**: Check the `docs/` directory
- **Issues**: Search or create issues on GitHub
- **Discussions**: Use GitHub Discussions for questions

## Contributing

Thank you for contributing to TralliValli! Following these guidelines helps maintain code quality and makes the review process smooth and efficient.
