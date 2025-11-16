# My-Agents UI

Multi-application frontend for the my-agents AI orchestration framework.

## Tech Stack

- **Vue 3.5.24** - Progressive JavaScript framework
- **Tailwind CSS 4** - Utility-first CSS with design tokens
- **TypeScript 5.6** - Type-safe JavaScript
- **Vite** - Fast build tool and dev server
- **Pinia** - State management
- **pnpm** - Fast, efficient package manager
- **Turborepo** - Build system for monorepos

## Architecture

This is a monorepo workspace containing:

### Packages (Shared Libraries)
- `@agents/design-system` - Tailwind 4 tokens + Vue components
- `@agents/agent-domain` - TypeScript domain contracts
- `@agents/api-client` - API clients for backend services
- `@agents/layout-shell` - Shared layout & navigation

### Apps (Independent Applications)
- `agents-console` - Global agents dashboard & orchestration
- `test-planning-studio` - Test plan & spec-centered UI
- `devops-agent-explorer` - DevOps automations & pipelines
- `notification-center` - Notifications & alerting

## Prerequisites

- **Node.js** 20+ ([Download](https://nodejs.org/))
- **pnpm** 9+ (will be installed automatically via corepack)
- **Backend running** - Ensure .NET Aspire AppHost is running

## Getting Started

### 1. Enable pnpm (if not already enabled)

```powershell
corepack enable
corepack prepare pnpm@9.0.0 --activate
```

### 2. Install dependencies

```powershell
cd ui
pnpm install
```

### 3. Start development

```powershell
# Start all apps
pnpm dev

# Start specific app
pnpm --filter @agents/agents-console dev
```

### 4. Build for production

```powershell
# Build all apps
pnpm build

# Build specific app
pnpm --filter @agents/agents-console build
```

## Workspace Commands

```powershell
# Install dependencies
pnpm install

# Run dev servers for all apps
pnpm dev

# Build all apps and packages
pnpm build

# Run linting
pnpm lint

# Run tests
pnpm test

# Clean all build artifacts
pnpm clean
```

## App URLs (Development)

- **Agents Console**: http://localhost:5173
- **Test Planning Studio**: http://localhost:5174
- **DevOps Explorer**: http://localhost:5175
- **Notification Center**: http://localhost:5176

## Backend API URLs (Development)

- **Notification API**: http://localhost:7268
- **DevOps API**: http://localhost:7108
- **TestPlanning API**: http://localhost:7010
- **Implementation API**: http://localhost:5253
- **ServiceDesk API**: http://localhost:7145
- **BimClassification API**: http://localhost:7220

## Project Structure

```
ui/
├── package.json              # Root workspace config
├── pnpm-workspace.yaml       # pnpm workspace definition
├── turbo.json                # Turborepo config
├── packages/                 # Shared packages
│   ├── design-system/
│   ├── agent-domain/
│   ├── api-client/
│   └── layout-shell/
└── apps/                     # Independent applications
    ├── agents-console/
    ├── test-planning-studio/
    ├── devops-agent-explorer/
    └── notification-center/
```

## Development Workflow

1. **Start backend** - Run the .NET Aspire AppHost
2. **Start frontend** - `cd ui && pnpm dev`
3. **Open browser** - Navigate to app URLs above
4. **Make changes** - Hot reload is enabled
5. **Build** - `pnpm build` when ready to deploy

## Clean Architecture

Each app follows clean architecture principles:

```
apps/[app-name]/src/
├── domain/          # Domain types (usually imported from @agents/agent-domain)
├── application/     # Use cases as composables/stores
│   └── usecases/
├── infrastructure/  # App-specific infra (localStorage, flags)
└── presentation/    # Pages & components
    ├── pages/
    └── components/
```

## Adding a New Package

```powershell
cd packages
mkdir my-package
cd my-package
pnpm init
```

## Adding a New App

```powershell
cd apps
pnpm create vite my-app --template vue-ts
cd my-app
pnpm install
```

## Documentation

- [Implementation Plan](../docs/ui-implementation-plan.md)
- [Backend Architecture](../docs/architecture.md)
- [Agent Development Guide](../docs/agent-development.md)

## Troubleshooting

### pnpm not found
```powershell
corepack enable
corepack prepare pnpm@9.0.0 --activate
```

### Port already in use
Change the port in `vite.config.ts`:
```ts
export default defineConfig({
  server: { port: 5177 } // Use different port
})
```

### CORS errors
Ensure backend CORS is configured to allow your frontend origin.

## Contributing

1. Create a feature branch
2. Make your changes
3. Run tests: `pnpm test`
4. Run linting: `pnpm lint`
5. Commit and push
6. Create a pull request

## License

MIT - See [LICENSE](../LICENSE) for details.
