# Frontend Implementation Plan for `my-agents`

**Vue 3.5.24 • Tailwind CSS 4 • Clean Architecture • Multi-App Micro Frontend**

---

## Problem Statement

The `my-agents` project currently has a comprehensive **.NET 9** backend with **6 specialized agents** (Notification, DevOps, TestPlanning, Implementation, ServiceDesk, BimClassification) but **no user interface**. Users can only interact with agents via REST APIs using tools like Postman or curl. 

**Goal:** Build a reusable, multi-application frontend that:
- Provides user-friendly interfaces for interacting with agents
- Supports **multiple independent frontends** (apps) as micro frontends
- Uses **Vue 3.5.24** and **Tailwind CSS 4** with design tokens
- Follows **clean architecture** principles compatible with the existing backend
- Can be developed in a single monorepo but deployed independently

---

## Current State

### Backend Architecture
**Location:** `C:\Users\lukeu\source\repos\my-agents`

**Tech Stack:**
- **.NET 9** with Clean Architecture (Domain → Application → Infrastructure → Presentation)
- **6 agent microservices** with independent REST APIs:
  - Notification API (Port 7268) - `src/Presentation/Agents.API.Notification/`
  - DevOps API (Port 7108) - `src/Presentation/Agents.API.DevOps/`
  - TestPlanning API (Port 7010) - `src/Presentation/Agents.API.TestPlanning/`
  - Implementation API (Port 5253) - `src/Presentation/Agents.API.Implementation/`
  - ServiceDesk API (Port 7145) - `src/Presentation/Agents.API.ServiceDesk/`
  - BimClassification API (Port 7220) - `src/Presentation/Agents.API.BimClassification/`
- **Dapr** for event-driven communication (Redis locally, Azure Service Bus in production)
- **.NET Aspire** for local orchestration
- **Swagger/OpenAPI** documentation for all APIs
- **SQL Server 2017** + Cosmos DB for persistence

**API Structure:** Each agent follows a consistent pattern:
```csharp
// Controllers/NotificationController.cs
[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
    
    [HttpGet("health")]
    public IActionResult Health()
}
```

**Response Format:**
```csharp
public class AgentResult
{
    public bool IsSuccess { get; init; }
    public string? Output { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object> Metadata { get; init; }
    public TimeSpan Duration { get; init; }
}
```

### Frontend State
- **No UI folder exists** (`ui/` directory does not exist)
- **No package managers configured** (no package.json, pnpm-workspace.yaml)
- **CORS is enabled** in backend APIs (allows any origin in development)
- **No frontend CI/CD workflows** (existing workflows: `ci.yml`, `deploy-aks.yml`, `docker-build.yml`, `security-scan.yml`)

### Existing Documentation
- `docs/architecture.md` - Comprehensive backend architecture documentation
- `docs/agent-development.md` - Agent development guide
- Clean Architecture principles clearly documented

---

## Proposed Solution

### High-Level Architecture

Add a `ui/` folder to the root that contains a **monorepo workspace** with:
- **Shared packages** for cross-app concerns (design system, domain types, API clients)
- **Independent apps** that can be built and deployed separately
- **Turborepo** for incremental builds and task orchestration

```
my-agents/
  Agents.sln
  src/
    Domain/
    Application/
    Infrastructure/
    Presentation/
  
  ui/                          # ← NEW: Frontend monorepo
    package.json
    pnpm-workspace.yaml
    turbo.json
    
    packages/                  # ← Shared libraries
      design-system/           # Tailwind 4 tokens + Vue components
      agent-domain/            # TypeScript domain contracts
      api-client/              # Generated TS clients from OpenAPI
      layout-shell/            # Shared layout & navigation
    
    apps/                      # ← Independent applications
      agents-console/          # Global agents dashboard & orchestration
      test-planning-studio/    # Test plan & spec-centered UI
      devops-agent-explorer/   # DevOps automations & pipelines
      notification-center/     # Notifications & alerting
```

### Clean Architecture Mapping

**Frontend Clean Architecture:**
```
ui/
├── packages/
│   ├── agent-domain/          # Domain Layer (types, interfaces)
│   ├── api-client/            # Infrastructure Layer (API clients)
│   ├── design-system/         # Presentation Layer (UI components)
│   └── layout-shell/          # Presentation Layer (shared layouts)
│
└── apps/
    └── agents-console/
        └── src/
            ├── domain/        # App-specific domain (optional, usually shared)
            ├── application/   # Use cases as composables/stores
            │   └── usecases/
            │       ├── useListAgents.ts
            │       └── useRunAgent.ts
            ├── infrastructure/# App-specific infra (localStorage, flags)
            └── presentation/  # Pages & components
                ├── pages/
                └── components/
```

**Backend to Frontend Mapping:**
| Backend Layer | Frontend Equivalent |
|--------------|-------------------|
| `src/Domain/Agents.Domain.Core/` | `ui/packages/agent-domain/` |
| `src/Application/Agents.Application.*/` | `ui/apps/*/src/application/usecases/` |
| `src/Infrastructure/Agents.Infrastructure.LLM/` | `ui/packages/api-client/` |
| `src/Presentation/Agents.API.*/` | `ui/apps/*/src/presentation/` |

---

## Implementation Plan

### Phase 1: Workspace Foundation (Day 1-2)

**Goal:** Set up the monorepo structure with pnpm + Turborepo

**Tasks:**
1. **Create workspace structure**
   ```powershell
   mkdir ui
   cd ui
   mkdir packages, apps
   mkdir packages/design-system, packages/agent-domain, packages/api-client, packages/layout-shell
   ```

2. **Initialize root package.json**
   ```json
   {
     "name": "@agents/ui-root",
     "private": true,
     "packageManager": "pnpm@9.0.0",
     "scripts": {
       "dev": "turbo dev",
       "build": "turbo build",
       "lint": "turbo lint",
       "test": "turbo test"
     },
     "devDependencies": {
       "turbo": "^2.1.0",
       "typescript": "^5.6.0",
       "pnpm": "^9.0.0"
     }
   }
   ```

3. **Create pnpm-workspace.yaml**
   ```yaml
   packages:
     - "apps/*"
     - "packages/*"
   ```

4. **Create turbo.json**
   ```json
   {
     "$schema": "https://turbo.build/schema.json",
     "pipeline": {
       "build": {
         "dependsOn": ["^build"],
         "outputs": ["dist/**"]
       },
       "dev": { "cache": false },
       "lint": {},
       "test": {}
     }
   }
   ```

**Success Criteria:**
- ✅ `pnpm install` runs successfully
- ✅ Workspace structure is recognized by pnpm

---

### Phase 2: Design System Foundation (Day 2-3)

**Goal:** Create the shared design system with Tailwind 4 tokens

**Location:** `ui/packages/design-system/`

**Tasks:**
1. **Initialize package**
   ```json
   {
     "name": "@agents/design-system",
     "version": "0.0.1",
     "private": true,
     "type": "module",
     "exports": { ".": "./src/index.ts" },
     "dependencies": { "vue": "3.5.24" },
     "devDependencies": {
       "@vitejs/plugin-vue": "^5.1.0",
       "@tailwindcss/vite": "^4.1.0",
       "tailwindcss": "^4.1.0"
     }
   }
   ```

2. **Create Tailwind 4 tokens** (`src/tokens.css`):
   ```css
   @import "tailwindcss";
   
   @theme {
     /* Brand colors */
     --color-brand-500: oklch(0.70 0.12 250);
     --color-brand-600: oklch(0.62 0.14 250);
     
     /* Surfaces (dark theme) */
     --color-surface: oklch(0.15 0.02 260);
     --color-surface-elevated: oklch(0.18 0.03 260);
     --color-border-subtle: oklch(0.35 0.02 260);
     
     /* Semantic colors */
     --color-danger-500: oklch(0.68 0.20 24);
     --color-success-500: oklch(0.73 0.16 145);
     
     /* Radius */
     --radius-md: 0.5rem;
     --radius-lg: 0.75rem;
   }
   ```

3. **Create core components**:
   - `AppButton.vue` - Button with variants (primary, ghost, danger)
   - `AppCard.vue` - Card container
   - `AppInput.vue` - Input field
   - `AppBadge.vue` - Status badge

4. **Export plugin** (`src/index.ts`):
   ```ts
   import type { App } from "vue";
   import "./tokens.css";
   import AppButton from "./components/AppButton.vue";
   
   export { AppButton };
   
   export default {
     install(app: App) {
       app.component("AppButton", AppButton);
     }
   };
   ```

**Success Criteria:**
- ✅ Design tokens are defined and working
- ✅ Core components are documented
- ✅ Plugin can be imported by apps

---

### Phase 3: Domain Types & API Client (Day 3-4)

**Goal:** Create TypeScript domain contracts and API clients

#### 3.1 Domain Types (`ui/packages/agent-domain/`)

**Tasks:**
1. **Mirror C# domain types to TypeScript**:
   
   `src/agents.ts`:
   ```ts
   export type AgentCategory = "devops" | "test-planning" | "notification" | "implementation" | "servicedesk" | "bimclassification";
   
   export interface AgentSummary {
     name: string;
     displayName: string;
     description: string;
     category: AgentCategory;
     isEnabled: boolean;
   }
   
   export interface AgentRunRequest {
     agentName: string;
     input: string;
     contextId?: string;
   }
   
   export interface AgentResult {
     isSuccess: boolean;
     output?: string;
     errorMessage?: string;
     metadata: Record<string, unknown>;
     duration: string;
   }
   ```

2. **Create domain types for each agent**:
   - `src/notification.ts` - NotificationRequest, NotificationChannel
   - `src/devops.ts` - IssueRequest, WorkflowTrigger
   - `src/testplanning.ts` - TestSpec, TestStrategy
   - `src/implementation.ts` - CodeGenerationRequest
   - `src/servicedesk.ts` - TicketRequest, TriageResult

#### 3.2 API Client (`ui/packages/api-client/`)

**Tasks:**
1. **Create base HTTP client** (`src/BaseClient.ts`):
   ```ts
   import axios, { AxiosInstance } from "axios";
   
   export class BaseClient {
     protected readonly http: AxiosInstance;
     
     constructor(baseUrl: string) {
       this.http = axios.create({
         baseURL: baseUrl,
         headers: { "Content-Type": "application/json" }
       });
     }
   }
   ```

2. **Create agent-specific clients**:
   
   `src/AgentsClient.ts`:
   ```ts
   import { BaseClient } from "./BaseClient";
   import type { AgentSummary, AgentRunRequest, AgentResult } from "@agents/agent-domain";
   
   export class AgentsClient extends BaseClient {
     async listAgents(): Promise<AgentSummary[]> {
       const res = await this.http.get("/api/agents");
       return res.data;
     }
     
     async runAgent(request: AgentRunRequest): Promise<AgentResult> {
       const res = await this.http.post("/api/agents/run", request);
       return res.data;
     }
   }
   ```

3. **Create clients for each API**:
   - `NotificationClient.ts` - `/api/notification/send`
   - `DevOpsClient.ts` - `/api/devops/execute`
   - `TestPlanningClient.ts` - `/api/testplanning/execute`
   - `ImplementationClient.ts` - `/api/implementation/execute`
   - `ServiceDeskClient.ts` - `/api/servicedesk/execute`
   - `BimClassificationClient.ts` - `/api/bimclassification/execute`

**Success Criteria:**
- ✅ Types match C# DTOs
- ✅ API clients can be instantiated
- ✅ Environment-specific base URLs are configurable

---

### Phase 4: First App - Agents Console (Day 5-7)

**Goal:** Build the foundational app that demonstrates the architecture

**Location:** `ui/apps/agents-console/`

**Routes:**
- `/` - Dashboard (agent list, recent runs)
- `/agents` - All agents list
- `/agents/:name` - Agent detail + quick run
- `/runs` - All runs history
- `/runs/:id` - Run detail (transcript)

**Tasks:**
1. **Initialize Vite + Vue app**:
   ```json
   {
     "name": "@agents/agents-console",
     "scripts": {
       "dev": "vite",
       "build": "vite build"
     },
     "dependencies": {
       "vue": "3.5.24",
       "vue-router": "^4.4.5",
       "pinia": "^2.2.6",
       "@agents/design-system": "workspace:*",
       "@agents/agent-domain": "workspace:*",
       "@agents/api-client": "workspace:*"
     }
   }
   ```

2. **Create vite.config.ts**:
   ```ts
   import { defineConfig } from "vite";
   import vue from "@vitejs/plugin-vue";
   import tailwindcss from "@tailwindcss/vite";
   
   export default defineConfig({
     plugins: [vue(), tailwindcss()],
     resolve: { alias: { "@": "/src" } },
     server: { port: 5173 }
   });
   ```

3. **Create application layer** (`src/application/usecases/`):
   
   `useListAgents.ts`:
   ```ts
   import { ref, computed } from "vue";
   import { AgentsClient } from "@agents/api-client";
   import type { AgentSummary } from "@agents/agent-domain";
   
   const client = new AgentsClient(import.meta.env.VITE_AGENTS_API_BASE_URL);
   
   export function useListAgents() {
     const agents = ref<AgentSummary[]>([]);
     const isLoading = ref(false);
     const error = ref<unknown>(null);
     
     const load = async () => {
       try {
         isLoading.value = true;
         agents.value = await client.listAgents();
       } catch (e) {
         error.value = e;
       } finally {
         isLoading.value = false;
       }
     };
     
     return { agents, isLoading, error, load };
   }
   ```

4. **Create presentation layer** (`src/presentation/pages/`):
   
   `DashboardPage.vue`:
   ```vue
   <script setup lang="ts">
   import { onMounted } from "vue";
   import { useListAgents } from "@/application/usecases/useListAgents";
   import { AppButton } from "@agents/design-system";
   
   const { agents, isLoading, load } = useListAgents();
   onMounted(load);
   </script>
   
   <template>
     <main class="page-container">
       <header class="flex items-center justify-between">
         <h1 class="text-xl font-semibold">Agent Console</h1>
         <AppButton size="sm">New Session</AppButton>
       </header>
       
       <section class="card">
         <div v-if="isLoading">Loading...</div>
         <ul v-else>
           <li v-for="agent in agents" :key="agent.name" class="card">
             <h2>{{ agent.displayName }}</h2>
             <p>{{ agent.description }}</p>
           </li>
         </ul>
       </section>
     </main>
   </template>
   ```

5. **Configure environment variables** (`.env`):
   ```env
   VITE_AGENTS_API_BASE_URL=http://localhost:7268
   ```

**Success Criteria:**
- ✅ `pnpm dev` starts the app on port 5173
- ✅ Dashboard renders without errors
- ✅ Can fetch agents list from backend
- ✅ Tailwind styles are applied

---

### Phase 5: Additional Apps (Day 8-10)

**Goal:** Scaffold remaining apps following the same pattern

#### 5.1 Test Planning Studio (`ui/apps/test-planning-studio/`)

**Routes:**
- `/` - Test plans list
- `/plans/:id` - Test plan detail
- `/plans/:id/spec` - Spec editor with agent integration

**Key Features:**
- Rich text editor for test specs
- Agent-powered spec generation
- Test coverage visualization

#### 5.2 DevOps Agent Explorer (`ui/apps/devops-agent-explorer/`)

**Routes:**
- `/` - DevOps dashboard
- `/configs` - Config explorer
- `/runs` - DevOps runs history

**Key Features:**
- Pipeline visualizations
- Issue creation forms
- Sprint analytics

#### 5.3 Notification Center (`ui/apps/notification-center/`)

**Routes:**
- `/` - Notifications dashboard
- `/alerts/:id` - Alert detail

**Key Features:**
- Multi-channel notification viewer
- Filter by agent/severity
- Acknowledge/dismiss actions

**Success Criteria:**
- ✅ Each app can be built independently
- ✅ Each app has its own environment config
- ✅ Shared packages work across all apps

---

### Phase 6: Shared Layout & Navigation (Day 11-12)

**Goal:** Extract common layout patterns into `packages/layout-shell/`

**Tasks:**
1. **Create layout components**:
   - `AppShell.vue` - Top nav + sidebar + content area
   - `TopNav.vue` - Global navigation with app switcher
   - `Sidebar.vue` - Per-app navigation
   - `RouteGuard.ts` - Authentication guards (future)

2. **Create app switcher**:
   ```vue
   <template>
     <nav class="flex gap-4">
       <a href="http://localhost:5173">Console</a>
       <a href="http://localhost:5174">Test Planning</a>
       <a href="http://localhost:5175">DevOps</a>
       <a href="http://localhost:5176">Notifications</a>
     </nav>
   </template>
   ```

3. **Integrate into all apps**:
   ```vue
   <script setup>
   import { AppShell } from "@agents/layout-shell";
   </script>
   
   <template>
     <AppShell>
       <router-view />
     </AppShell>
   </template>
   ```

**Success Criteria:**
- ✅ Consistent navigation across apps
- ✅ App switcher works
- ✅ Layout is responsive

---

### Phase 7: Build & Deployment (Day 13-15)

**Goal:** Set up independent build and deployment for each app

#### 7.1 Dockerfiles

**Create per-app Dockerfile** (`ui/apps/agents-console/Dockerfile`):
```dockerfile
FROM node:24-alpine AS build
WORKDIR /src

# Copy workspace files
COPY ../../package.json ../../pnpm-workspace.yaml ../../turbo.json ./
COPY ../../pnpm-lock.yaml ./
COPY ../../packages ./packages
COPY . .

RUN npm install -g pnpm && pnpm install --frozen-lockfile
RUN pnpm turbo build --filter @agents/agents-console

FROM nginx:1.27-alpine
COPY --from=build /src/apps/agents-console/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

#### 7.2 GitHub Actions Workflows

**Create workflow per app** (`.github/workflows/agents-console-ui.yml`):
```yaml
name: Build & Deploy Agents Console UI

on:
  push:
    branches: [main]
    paths:
      - "ui/apps/agents-console/**"
      - "ui/packages/**"

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - uses: pnpm/action-setup@v4
        with:
          version: 9
      
      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: pnpm
      
      - name: Install deps
        run: cd ui && pnpm install --frozen-lockfile
      
      - name: Build app
        run: cd ui && pnpm turbo build --filter @agents/agents-console
      
      - name: Build Docker image
        run: |
          docker build -t ghcr.io/${{ github.repository }}/agents-console:${{ github.sha }} \
            ui/apps/agents-console
```

#### 7.3 Kubernetes Manifests

**Create deployment** (`k8s/ui/agents-console/deployment.yaml`):
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: agents-console-ui
spec:
  replicas: 2
  selector:
    matchLabels:
      app: agents-console-ui
  template:
    metadata:
      labels:
        app: agents-console-ui
    spec:
      containers:
      - name: agents-console
        image: ghcr.io/your-org/agents-console:latest
        ports:
        - containerPort: 80
        env:
        - name: VITE_AGENTS_API_BASE_URL
          value: "http://notification-api:7268"
---
apiVersion: v1
kind: Service
metadata:
  name: agents-console-ui
spec:
  selector:
    app: agents-console-ui
  ports:
  - port: 80
    targetPort: 80
```

**Success Criteria:**
- ✅ Each app builds successfully in Docker
- ✅ GitHub Actions workflows trigger on path changes
- ✅ Images are pushed to container registry
- ✅ Apps can be deployed to AKS

---

### Phase 8: Integration & Testing (Day 16-17)

**Goal:** Ensure backend-frontend integration works end-to-end

**Tasks:**
1. **Update backend CORS configuration** for production:
   ```csharp
   // Program.cs
   builder.Services.AddCors(options =>
   {
       options.AddDefaultPolicy(policy =>
       {
           policy.WithOrigins(
               "http://localhost:5173",  // agents-console
               "http://localhost:5174",  // test-planning-studio
               "http://localhost:5175",  // devops-agent-explorer
               "http://localhost:5176"   // notification-center
           )
           .AllowAnyMethod()
           .AllowAnyHeader();
       });
   });
   ```

2. **Add API health checks to frontend**:
   ```ts
   export function useHealthCheck(apiUrl: string) {
     const isHealthy = ref(false);
     
     const check = async () => {
       try {
         await axios.get(`${apiUrl}/health`);
         isHealthy.value = true;
       } catch {
         isHealthy.value = false;
       }
     };
     
     return { isHealthy, check };
   }
   ```

3. **Add error boundaries**:
   ```vue
   <script setup>
   import { onErrorCaptured } from "vue";
   
   const error = ref(null);
   onErrorCaptured((err) => {
     error.value = err;
     return false;
   });
   </script>
   ```

4. **Create integration test suite**:
   - Test each app can fetch from backend
   - Test agent execution flows
   - Test error handling

**Success Criteria:**
- ✅ All apps work with local backend (Aspire)
- ✅ Error states are handled gracefully
- ✅ Loading states are consistent
- ✅ Network failures don't crash apps

---

## File Structure Summary

```
my-agents/
├── ui/
│   ├── package.json
│   ├── pnpm-workspace.yaml
│   ├── turbo.json
│   │
│   ├── packages/
│   │   ├── design-system/
│   │   │   ├── package.json
│   │   │   └── src/
│   │   │       ├── tokens.css
│   │   │       ├── components/
│   │   │       │   ├── AppButton.vue
│   │   │       │   ├── AppCard.vue
│   │   │       │   └── AppInput.vue
│   │   │       └── index.ts
│   │   │
│   │   ├── agent-domain/
│   │   │   ├── package.json
│   │   │   └── src/
│   │   │       ├── agents.ts
│   │   │       ├── notification.ts
│   │   │       ├── devops.ts
│   │   │       └── index.ts
│   │   │
│   │   ├── api-client/
│   │   │   ├── package.json
│   │   │   └── src/
│   │   │       ├── BaseClient.ts
│   │   │       ├── AgentsClient.ts
│   │   │       ├── NotificationClient.ts
│   │   │       └── index.ts
│   │   │
│   │   └── layout-shell/
│   │       ├── package.json
│   │       └── src/
│   │           ├── AppShell.vue
│   │           ├── TopNav.vue
│   │           └── index.ts
│   │
│   └── apps/
│       ├── agents-console/
│       │   ├── package.json
│       │   ├── vite.config.ts
│       │   ├── Dockerfile
│       │   ├── .env
│       │   └── src/
│       │       ├── main.ts
│       │       ├── App.vue
│       │       ├── router/
│       │       ├── application/
│       │       │   └── usecases/
│       │       │       ├── useListAgents.ts
│       │       │       └── useRunAgent.ts
│       │       └── presentation/
│       │           ├── pages/
│       │           │   ├── DashboardPage.vue
│       │           │   └── AgentsListPage.vue
│       │           └── components/
│       │
│       ├── test-planning-studio/
│       ├── devops-agent-explorer/
│       └── notification-center/
│
├── .github/
│   └── workflows/
│       ├── agents-console-ui.yml
│       ├── test-planning-studio-ui.yml
│       └── ...
│
└── k8s/
    └── ui/
        ├── agents-console/
        │   ├── deployment.yaml
        │   └── service.yaml
        └── ...
```

---

## API Integration Details

### Environment Variables per App

Each app needs environment-specific API URLs:

**Development** (`.env`):
```env
# agents-console
VITE_AGENTS_API_BASE_URL=http://localhost:7268

# test-planning-studio
VITE_TEST_PLANNING_API_BASE_URL=http://localhost:7010

# devops-agent-explorer
VITE_DEVOPS_API_BASE_URL=http://localhost:7108

# notification-center
VITE_NOTIFICATIONS_API_BASE_URL=http://localhost:7268
```

**Production** (Kubernetes ConfigMap):
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: agents-console-config
data:
  VITE_AGENTS_API_BASE_URL: "https://agents-api.prod.yourdomain.com"
```

### Backend API Endpoints to Use

Based on current backend structure:

| Agent | Endpoint | Method | Request Body |
|-------|----------|--------|--------------|
| Notification | `/api/notification/send` | POST | `{ channel, recipient, subject, content }` |
| DevOps | `/api/devops/execute` | POST | `{ action, data }` |
| TestPlanning | `/api/testplanning/execute` | POST | `{ type, input }` |
| Implementation | `/api/implementation/execute` | POST | `{ action, specification }` |
| ServiceDesk | `/api/servicedesk/execute` | POST | `{ action, ticketData }` |
| BimClassification | `/api/bimclassification/execute` | POST | `{ elementId, properties }` |

All endpoints return:
```json
{
  "isSuccess": true,
  "output": "...",
  "metadata": {},
  "duration": "00:00:02.1234567"
}
```

---

## Success Metrics

### Phase Completion Criteria

**Phase 1-2 (Foundation + Design System):**
- [ ] Workspace runs `pnpm install` successfully
- [ ] Design tokens are applied across sample pages
- [ ] Core components render correctly

**Phase 3-4 (Domain + First App):**
- [ ] TypeScript types match C# DTOs
- [ ] Agents Console fetches real data from backend
- [ ] Dashboard displays agents list

**Phase 5-6 (Additional Apps + Layout):**
- [ ] All 4 apps can be developed independently
- [ ] Shared layout provides consistent navigation
- [ ] App switcher works between apps

**Phase 7-8 (Deployment + Integration):**
- [ ] Each app builds a Docker image
- [ ] GitHub Actions workflows pass
- [ ] Apps integrate with backend via Aspire locally
- [ ] Apps can be deployed to Kubernetes

---

## Timeline

**Total Estimated Time:** 15-17 working days

| Phase | Days | Key Deliverable |
|-------|------|----------------|
| 1. Workspace Foundation | 1-2 | Monorepo structure |
| 2. Design System | 2-3 | Tailwind 4 tokens + components |
| 3. Domain & API Client | 3-4 | TypeScript domain + API clients |
| 4. First App (Console) | 5-7 | Working agents console |
| 5. Additional Apps | 8-10 | 3 more apps scaffolded |
| 6. Shared Layout | 11-12 | Consistent navigation |
| 7. Build & Deployment | 13-15 | Docker + CI/CD |
| 8. Integration & Testing | 16-17 | E2E integration |

---

## Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Backend API changes break frontend | High | Keep domain types in sync; document contract changes |
| Monorepo complexity | Medium | Use Turborepo; keep packages small and focused |
| CORS issues in production | High | Configure CORS properly; test early |
| State management complexity | Medium | Use Pinia for shared state; keep stores simple |
| Different Node versions | Low | Use `.nvmrc` file; document in README |

---

## Next Steps

1. **Review & Approve** this plan
2. **Set up development environment**:
   - Install Node.js 20+
   - Install pnpm 9+
   - Ensure backend is running (Aspire AppHost)
3. **Execute Phase 1** (Workspace Foundation)
4. **Iterate** through phases, validating success criteria

---

## References

- [Vue 3 Documentation](https://vuejs.org)
- [Tailwind CSS 4 Documentation](https://tailwindcss.com/docs/v4-beta)
- [Turborepo Documentation](https://turbo.build/repo/docs)
- [Pinia Documentation](https://pinia.vuejs.org)
- Backend: `docs/architecture.md`, `docs/agent-development.md`

---

**Document Version:** 1.0  
**Last Updated:** 2025-11-16  
**Owner:** Frontend Team
