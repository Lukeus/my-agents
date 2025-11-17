# Agents Console

The main web application for managing and monitoring agents in the my-agents microservices platform.

## Features

- **Dashboard**: Overview of all agents with real-time health status
- **Agents Management**: Browse, search, and view detailed information about agents
- **Run History**: View execution history and results (coming soon)
- **Settings**: Configure agents and system preferences (coming soon)

## Architecture

Built with **Clean Architecture** principles:

```
src/
├── application/
│   └── usecases/          # Business logic composables
├── presentation/
│   ├── pages/             # Route-level components
│   └── components/        # Reusable UI components
├── router/                # Vue Router configuration
└── assets/                # Static assets and global styles
```

## Technology Stack

- **Vue 3.5.24** - Progressive JavaScript framework
- **TypeScript** - Type-safe development
- **Vue Router** - Client-side routing
- **Pinia** - State management
- **Tailwind CSS 4** - Utility-first CSS framework
- **Vite 6** - Fast build tool and dev server
- **Zod** - Runtime type validation

## Workspace Dependencies

- `@agents/design-system` - Shared UI components and design tokens
- `@agents/layout-shell` - Application shell and navigation
- `@agents/agent-domain` - Domain types and Zod schemas
- `@agents/api-client` - HTTP clients for backend APIs

## Development

```bash
# Install dependencies (from workspace root)
pnpm install

# Start dev server
pnpm dev

# Build for production
pnpm build

# Preview production build
pnpm preview

# Type checking
pnpm type-check

# Linting
pnpm lint
```

The dev server runs at http://localhost:5173

## Environment Variables

Configure API endpoints in `.env`:

```env
VITE_NOTIFICATION_API_URL=http://localhost:7268
VITE_DEVOPS_API_URL=http://localhost:7108
VITE_TESTPLANNING_API_URL=http://localhost:7010
VITE_IMPLEMENTATION_API_URL=http://localhost:5253
VITE_SERVICEDESK_API_URL=http://localhost:7145
VITE_BIMCLASSIFICATION_API_URL=http://localhost:7220
```

## Routes

- `/` - Dashboard with agent overview
- `/agents` - List of all agents
- `/agents/:name` - Agent details (coming soon)
- `/runs` - Execution history (coming soon)
- `/runs/:id` - Run details (coming soon)
- `/settings` - Application settings (coming soon)

## Integration with Backend

The console integrates with 6 .NET microservices:

1. **Notification Agent** (Port 7268) - Multi-channel notifications
2. **DevOps Agent** (Port 7108) - Issue tracking and workflows
3. **Test Planning Agent** (Port 7010) - Test generation and coverage
4. **Implementation Agent** (Port 5253) - Code assistance
5. **Service Desk Agent** (Port 7145) - Support tickets
6. **BIM Classification Agent** (Port 7220) - BIM data management

## Next Steps

- Implement agent detail pages with real-time metrics
- Add run history with logs and execution details
- Build interactive agent execution interface
- Add WebSocket support for real-time updates
- Implement user authentication and authorization
