/**
 * @agents/api-client
 * HTTP clients for my-agents backend APIs with Zod validation
 */

// Base client and types
export { BaseClient, type ApiError } from './BaseClient';

// Agent-specific clients
export { NotificationClient, type NotificationStats } from './NotificationClient';
export { DevOpsClient } from './DevOpsClient';
export { TestPlanningClient } from './TestPlanningClient';

// Re-export domain types for convenience
export type {
  // Core
  AgentSummary,
  AgentRunRequest,
  AgentResult,
  AgentHealth,
  AgentRun,
  
  // Notification
  NotificationRequest,
  NotificationResult,
  NotificationHistory,
  NotificationChannel,
  
  // DevOps
  DevOpsRequest,
  IssueRequest,
  WorkflowTriggerRequest,
  IssueResult,
  WorkflowResult,
  SprintMetrics,
  
  // Test Planning
  TestPlanningRequest,
  TestSpec,
  TestStrategy,
  TestCoverage,
  TestGenerationResult,
} from '@agents/agent-domain';
