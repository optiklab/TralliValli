# TralliValli Web Application

A modern React web application built with Vite, TypeScript, and Tailwind CSS.

## Tech Stack

- **Framework**: React 19
- **Build Tool**: Vite 7
- **Language**: TypeScript (strict mode)
- **Styling**: Tailwind CSS v4
- **Code Quality**: ESLint + Prettier
- **Linting Config**: Airbnb-style rules (adapted for ESLint 9)

## Project Structure

```
src/
├── components/    # React components
├── hooks/         # Custom React hooks
├── services/      # API services and external integrations
├── stores/        # State management stores
├── types/         # TypeScript type definitions
├── utils/         # Utility functions
└── assets/        # Static assets (images, fonts, etc.)
```

## Getting Started

### Prerequisites

- Node.js 20.x or higher
- npm 10.x or higher

### Installation

```bash
cd src/web
npm install
```

### Development

Start the development server:

```bash
npm run dev
```

The application will be available at `http://localhost:5173`

### Building for Production

```bash
npm run build
```

The built files will be in the `dist/` directory.

### Preview Production Build

```bash
npm run preview
```

## Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint
- `npm run lint:fix` - Fix ESLint errors automatically
- `npm run format` - Format code with Prettier

## Environment Variables

Copy `.env.example` to `.env` and configure:

```env
VITE_API_URL=http://localhost:5000/api
VITE_SIGNALR_URL=http://localhost:5000/hub
```

## Path Aliases

The following path aliases are configured for easier imports:

- `@/*` - `src/*`
- `@components/*` - `src/components/*`
- `@hooks/*` - `src/hooks/*`
- `@services/*` - `src/services/*`
- `@stores/*` - `src/stores/*`
- `@types/*` - `src/types/*`
- `@utils/*` - `src/utils/*`

Example usage:

```typescript
import { MyComponent } from '@components/MyComponent';
import { useAuth } from '@hooks/useAuth';
```

## Code Style

- **ESLint**: Configured with recommended rules and Airbnb-inspired best practices
- **Prettier**: Enforces consistent code formatting
- **TypeScript**: Strict mode enabled for maximum type safety

## Contributing

1. Follow the existing code structure
2. Use TypeScript for all new files
3. Run `npm run lint` and `npm run format` before committing
4. Follow the established naming conventions
