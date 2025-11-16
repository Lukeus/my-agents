# @agents/api-client

Type-safe HTTP clients for my-agents backend APIs with automatic Zod validation.

## Features

- **Type-safe** - Full TypeScript support with domain types
- **Zod validation** - Automatic validation of API responses
- **Error handling** - Structured error responses
- **Axios-based** - Reliable HTTP client with interceptors
- **Environment-agnostic** - Configure base URLs per environment

## Installation

This package is part of the monorepo and automatically available:

```ts
import { NotificationClient, DevOpsClient } from '@agents/api-client';
```

## Quick Start

### Notification Client

```ts
import { NotificationClient } from '@agents/api-client';

// Create client instance
const client = new NotificationClient('http://localhost:7268');

// Send notification
const result = await client.sendNotification({
  channel: 'email',
  recipient: 'user@example.com',
  subject: 'Test Notification',
  content: 'Hello from my-agents!',
  priority: 'normal',
});

console.log(result.isSuccess); // true
console.log(result.output); // Success message

// Get notification history
const history = await client.getHistory(10);

// Check health
const health = await client.getHealth();
```

### DevOps Client

```ts
import { DevOpsClient } from '@agents/api-client';

const client = new DevOpsClient('http://localhost:7108');

// Create GitHub issue
const issue = await client.createIssue({
  title: 'Bug: Login not working',
  description: 'Users cannot log in after deployment',
  priority: 'high',
  labels: ['bug', 'urgent'],
  repository: 'my-org/my-repo',
});

console.log(issue.issueNumber); // 123
console.log(issue.url); // https://github.com/...

// Trigger workflow
const workflow = await client.triggerWorkflow({
  workflowName: 'deploy.yml',
  branch: 'main',
  repository: 'my-org/my-repo',
});

// Analyze sprint
const metrics = await client.analyzeSprint({
  startDate: '2025-01-01T00:00:00Z',
  endDate: '2025-01-14T23:59:59Z',
  includeMetrics: true,
});

console.log(`Completed: ${metrics.completedIssues}/${metrics.totalIssues}`);
console.log(`Velocity: ${metrics.velocity}`);
```

### Test Planning Client

```ts
import { TestPlanningClient } from '@agents/api-client';

const client = new TestPlanningClient('http://localhost:7010');

// Generate test spec
const spec = await client.generateSpec(
  'User authentication with email and password',
  'comprehensive'
);

console.log(spec.title);
spec.scenarios.forEach(scenario => {
  console.log(`Test: ${scenario.name}`);
  console.log(`  Given: ${scenario.given}`);
  console.log(`  When: ${scenario.when}`);
  console.log(`  Then: ${scenario.then}`);
});

// List all specs
const allSpecs = await client.listSpecs();

// Generate tests from spec
const generated = await client.generateTests(spec.id);
generated.testFiles.forEach(file => {
  console.log(`${file.fileName}: ${file.testCount} tests`);
});
```

## Error Handling

All clients throw structured errors with the `ApiError` interface:

```ts
import { NotificationClient, type ApiError } from '@agents/api-client';

const client = new NotificationClient('http://localhost:7268');

try {
  await client.sendNotification(request);
} catch (error) {
  const apiError = error as ApiError;
  
  console.error('API Error:', apiError.message);
  console.error('Status:', apiError.status); // HTTP status code
  console.error('Code:', apiError.code); // Error code
  console.error('Details:', apiError.details); // Full response
}
```

## Authentication

Set authentication token for all requests:

```ts
const client = new NotificationClient('http://localhost:7268');

// Set token
client.setAuthToken('your-jwt-token');

// Make authenticated requests
await client.sendNotification(request);

// Clear token
client.clearAuthToken();
```

## Configuration

### Environment Variables

```env
# Development
VITE_NOTIFICATION_API_BASE_URL=http://localhost:7268
VITE_DEVOPS_API_BASE_URL=http://localhost:7108
VITE_TEST_PLANNING_API_BASE_URL=http://localhost:7010
VITE_IMPLEMENTATION_API_BASE_URL=http://localhost:5253
VITE_SERVICE_DESK_API_BASE_URL=http://localhost:7145
VITE_BIM_CLASSIFICATION_API_BASE_URL=http://localhost:7220

# Production
VITE_NOTIFICATION_API_BASE_URL=https://notification-api.prod.yourdomain.com
# ... etc
```

### Client Configuration

```ts
// Create client with custom config
const client = new NotificationClient(
  import.meta.env.VITE_NOTIFICATION_API_BASE_URL
);

// Configure timeout (default: 30s)
client.http.defaults.timeout = 60000; // 60 seconds

// Add custom headers
client.http.defaults.headers.common['X-Custom-Header'] = 'value';

// Add request interceptor
client.http.interceptors.request.use((config) => {
  config.headers['X-Request-ID'] = crypto.randomUUID();
  return config;
});
```

## Validation

All responses are automatically validated using Zod schemas:

```ts
// Response is validated automatically
const result = await client.sendNotification(request);
// result is typed as AgentResult and validated at runtime

// If validation fails, throws ZodError
try {
  const result = await client.sendNotification(request);
} catch (error) {
  if (error instanceof z.ZodError) {
    console.error('Validation failed:', error.errors);
  }
}
```

## Vue 3 Composables

Example composable for notifications:

```ts
// composables/useNotifications.ts
import { ref } from 'vue';
import { NotificationClient } from '@agents/api-client';

const client = new NotificationClient(import.meta.env.VITE_NOTIFICATION_API_BASE_URL);

export function useNotifications() {
  const isLoading = ref(false);
  const error = ref<Error | null>(null);

  const sendNotification = async (request: NotificationRequest) => {
    try {
      isLoading.value = true;
      error.value = null;
      const result = await client.sendNotification(request);
      return result;
    } catch (e) {
      error.value = e as Error;
      throw e;
    } finally {
      isLoading.value = false;
    }
  };

  const getHistory = async (limit = 50) => {
    try {
      isLoading.value = true;
      error.value = null;
      return await client.getHistory(limit);
    } catch (e) {
      error.value = e as Error;
      throw e;
    } finally {
      isLoading.value = false;
    }
  };

  return {
    isLoading,
    error,
    sendNotification,
    getHistory,
  };
}
```

## Available Clients

### NotificationClient

**Base URL**: Port 7268 (dev)

**Methods**:
- `sendNotification(request)` - Send notification
- `getHistory(limit?)` - Get notification history
- `getNotificationById(id)` - Get specific notification
- `retryNotification(id)` - Retry failed notification
- `getHealth()` - Get agent health
- `getStats()` - Get notification statistics

### DevOpsClient

**Base URL**: Port 7108 (dev)

**Methods**:
- `execute(request)` - Execute generic DevOps action
- `createIssue(request)` - Create GitHub issue
- `triggerWorkflow(request)` - Trigger GitHub workflow
- `analyzeSprint(request)` - Analyze sprint metrics
- `getIssue(issueNumber, repository)` - Get issue details
- `getWorkflowStatus(workflowId)` - Get workflow status
- `getHealth()` - Get agent health

### TestPlanningClient

**Base URL**: Port 7010 (dev)

**Methods**:
- `execute(request)` - Execute generic test planning action
- `generateSpec(description, level)` - Generate test spec
- `createStrategy(feature, objectives)` - Create test strategy
- `analyzeCoverage(testFiles)` - Analyze coverage
- `generateTests(specId)` - Generate tests from spec
- `getSpecById(id)` - Get test spec
- `listSpecs()` - List all specs
- `updateSpec(id, spec)` - Update spec
- `deleteSpec(id)` - Delete spec
- `getHealth()` - Get agent health

## Best Practices

1. **Create client instances once**
   ```ts
   // Good: Create once, reuse
   const client = new NotificationClient(baseUrl);
   
   // Avoid: Creating new instance per request
   ```

2. **Use environment variables for URLs**
   ```ts
   const client = new NotificationClient(
     import.meta.env.VITE_NOTIFICATION_API_BASE_URL
   );
   ```

3. **Handle errors appropriately**
   ```ts
   try {
     await client.sendNotification(request);
   } catch (error) {
     // Show user-friendly error message
     showNotification('Failed to send notification');
     // Log for debugging
     console.error(error);
   }
   ```

4. **Validate requests before sending**
   ```ts
   // Requests are auto-validated, but you can validate early
   const result = NotificationRequestSchema.safeParse(formData);
   if (!result.success) {
     showValidationErrors(result.error);
     return;
   }
   await client.sendNotification(result.data);
   ```

## TypeScript Support

All clients are fully typed:

```ts
import type { 
  NotificationRequest,
  AgentResult,
  ApiError 
} from '@agents/api-client';

const request: NotificationRequest = {
  channel: 'email', // TypeScript ensures valid channel
  recipient: 'user@example.com',
  subject: 'Test',
  content: 'Hello!',
};

const result: AgentResult = await client.sendNotification(request);
```

## References

- [Axios Documentation](https://axios-http.com/)
- [Zod Documentation](https://zod.dev)
- Backend APIs: `src/Presentation/Agents.API.*/`
