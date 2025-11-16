# @agents/agent-domain

TypeScript domain contracts with Zod validation for my-agents. Mirrors the C# domain layer.

## Features

- **Zod schemas** for runtime validation
- **Type-safe** TypeScript types inferred from schemas
- **Mirrors C# domain** - keeps frontend and backend in sync
- **Validation helpers** built-in

## Installation

This package is part of the monorepo and automatically available:

```ts
import { AgentSummarySchema, type AgentSummary } from '@agents/agent-domain';
```

## Core Types

### Agent Types

```ts
import { 
  AgentSummarySchema, 
  AgentRunRequestSchema,
  AgentResultSchema,
  type AgentSummary,
  type AgentRunRequest,
  type AgentResult
} from '@agents/agent-domain';

// Validate API response
const result = AgentSummarySchema.parse(apiResponse);

// Type-safe objects
const request: AgentRunRequest = {
  agentName: 'notification-agent',
  input: JSON.stringify({ channel: 'email' }),
};

// Validate before sending
AgentRunRequestSchema.parse(request);
```

### Notification Types

```ts
import { 
  NotificationRequestSchema,
  type NotificationRequest 
} from '@agents/agent-domain';

const notification: NotificationRequest = {
  channel: 'email',
  recipient: 'user@example.com',
  subject: 'Test',
  content: 'Hello!',
  priority: 'normal',
};

// Validate
NotificationRequestSchema.parse(notification);
```

### DevOps Types

```ts
import { 
  IssueRequestSchema,
  WorkflowTriggerRequestSchema,
  type IssueRequest 
} from '@agents/agent-domain';

const issue: IssueRequest = {
  title: 'Bug fix needed',
  description: 'Details...',
  priority: 'high',
  labels: ['bug', 'urgent'],
  repository: 'my-repo',
};

IssueRequestSchema.parse(issue);
```

### Test Planning Types

```ts
import { 
  TestSpecSchema,
  TestPlanningRequestSchema,
  type TestSpec 
} from '@agents/agent-domain';

const spec: TestSpec = {
  id: crypto.randomUUID(),
  title: 'User Login Tests',
  description: 'Test user authentication flow',
  type: 'integration',
  status: 'draft',
  scenarios: [
    {
      name: 'Successful login',
      given: 'User has valid credentials',
      when: 'User submits login form',
      then: 'User is redirected to dashboard',
      priority: 'high',
    },
  ],
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  createdBy: 'user@example.com',
};

TestSpecSchema.parse(spec);
```

## Validation Patterns

### Basic Validation

```ts
import { AgentResultSchema } from '@agents/agent-domain';

// Parse (throws on error)
const result = AgentResultSchema.parse(data);

// Safe parse (returns { success, data, error })
const result = AgentResultSchema.safeParse(data);
if (result.success) {
  console.log(result.data);
} else {
  console.error(result.error);
}
```

### Partial Validation

```ts
// Validate only some fields
const PartialRequest = AgentRunRequestSchema.partial();
PartialRequest.parse({ agentName: 'test' }); // OK
```

### Array Validation

```ts
import { AgentSummarySchema } from '@agents/agent-domain';

const agents = z.array(AgentSummarySchema).parse(apiResponse);
```

### Custom Validation

```ts
const CustomRequest = AgentRunRequestSchema.extend({
  customField: z.string().min(5),
});
```

## Using with Forms

```vue
<script setup lang="ts">
import { ref, computed } from 'vue';
import { NotificationRequestSchema } from '@agents/agent-domain';

const formData = ref({
  channel: 'email',
  recipient: '',
  subject: '',
  content: '',
});

const errors = ref<Record<string, string>>({});

const validate = () => {
  const result = NotificationRequestSchema.safeParse(formData.value);
  
  if (!result.success) {
    errors.value = result.error.flatten().fieldErrors;
    return false;
  }
  
  errors.value = {};
  return true;
};

const submit = () => {
  if (validate()) {
    // Send validated data
  }
};
</script>
```

## Type Guards

```ts
import { AgentCategorySchema } from '@agents/agent-domain';

function isValidCategory(value: unknown): value is AgentCategory {
  return AgentCategorySchema.safeParse(value).success;
}

if (isValidCategory(input)) {
  // TypeScript knows input is AgentCategory
}
```

## Schema Composition

```ts
import { AgentResultSchema, createTypedAgentResultSchema } from '@agents/agent-domain';
import { NotificationResultSchema } from '@agents/agent-domain';

// Create a typed result schema
const NotificationAgentResult = createTypedAgentResultSchema(NotificationResultSchema);

type NotificationAgentResult = z.infer<typeof NotificationAgentResult>;
```

## Available Schemas

### Core
- `AgentSummarySchema`
- `AgentRunRequestSchema`
- `AgentResultSchema`
- `AgentContextSchema`
- `AgentRunSchema`
- `AgentHealthSchema`

### Notification
- `NotificationRequestSchema`
- `NotificationResultSchema`
- `NotificationHistorySchema`

### DevOps
- `DevOpsRequestSchema`
- `IssueRequestSchema`
- `WorkflowTriggerRequestSchema`
- `SprintAnalysisRequestSchema`
- `IssueResultSchema`
- `WorkflowResultSchema`
- `SprintMetricsSchema`

### Test Planning
- `TestPlanningRequestSchema`
- `TestSpecSchema`
- `TestStrategySchema`
- `TestCoverageSchema`
- `TestGenerationResultSchema`

## Best Practices

1. **Always validate external data**
   ```ts
   const data = AgentResultSchema.parse(apiResponse);
   ```

2. **Use safeParse for user input**
   ```ts
   const result = schema.safeParse(userInput);
   if (!result.success) {
     showErrors(result.error);
   }
   ```

3. **Infer types from schemas**
   ```ts
   type MyType = z.infer<typeof MySchema>;
   ```

4. **Share schemas between frontend and backend validations**

## Migration from C#

| C# | TypeScript + Zod |
|----|------------------|
| `public class Agent` | `export const AgentSchema = z.object({...})` |
| `public enum Status` | `export const StatusSchema = z.enum([...])` |
| `[Required]` | `.min(1, 'Required')` |
| `[EmailAddress]` | `.email()` |
| `[Range(0, 100)]` | `.min(0).max(100)` |

## References

- [Zod Documentation](https://zod.dev)
- Backend C# domain: `src/Domain/`
