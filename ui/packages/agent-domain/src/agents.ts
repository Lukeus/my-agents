/**
 * Agent domain types with Zod validation
 * Mirrors the C# domain from src/Application/Agents.Application.Core/
 */

import { z } from 'zod';

// Enums and constants
export const AgentCategorySchema = z.enum([
  'devops',
  'test-planning',
  'notification',
  'implementation',
  'servicedesk',
  'bimclassification',
]);

export const AgentStatusSchema = z.enum(['enabled', 'disabled', 'maintenance']);

export const AgentRunStatusSchema = z.enum(['pending', 'running', 'succeeded', 'failed']);

export const AgentHealthStatusSchema = z.enum(['healthy', 'degraded', 'unhealthy']);

// Agent Summary
export const AgentSummarySchema = z.object({
  name: z.string(),
  displayName: z.string(),
  description: z.string(),
  category: AgentCategorySchema,
  isEnabled: z.boolean(),
  status: AgentStatusSchema,
});

// Agent Run Request
export const AgentRunRequestSchema = z.object({
  agentName: z.string().min(1, 'Agent name is required'),
  input: z.string(),
  contextId: z.string().optional(),
  metadata: z.record(z.unknown()).optional(),
});

// Agent Result (mirrors C# AgentResult)
export const AgentResultSchema = z.object({
  isSuccess: z.boolean(),
  output: z.string().optional(),
  errorMessage: z.string().optional(),
  metadata: z.record(z.unknown()).optional(),
  duration: z.string(), // ISO duration string from TimeSpan
});

// Typed Agent Result with generic data
export const createTypedAgentResultSchema = <T extends z.ZodTypeAny>(dataSchema: T) =>
  AgentResultSchema.extend({
    data: dataSchema.optional(),
  });

// Agent Context
export const AgentContextSchema = z.object({
  initiatedBy: z.string(),
  correlationId: z.string().optional(),
  timestamp: z.string().datetime(),
});

// Agent Run (history record)
export const AgentRunSchema = z.object({
  id: z.string().uuid(),
  agentName: z.string(),
  startedAt: z.string().datetime(),
  completedAt: z.string().datetime().optional(),
  status: AgentRunStatusSchema,
  input: z.string(),
  output: z.string().optional(),
  errorMessage: z.string().optional(),
  duration: z.string().optional(),
  initiatedBy: z.string(),
});

// Agent Health
export const AgentHealthSchema = z.object({
  agentName: z.string(),
  status: AgentHealthStatusSchema,
  lastChecked: z.string().datetime(),
  message: z.string().optional(),
});

// Type exports (inferred from schemas)
export type AgentCategory = z.infer<typeof AgentCategorySchema>;
export type AgentStatus = z.infer<typeof AgentStatusSchema>;
export type AgentRunStatus = z.infer<typeof AgentRunStatusSchema>;
export type AgentHealthStatus = z.infer<typeof AgentHealthStatusSchema>;

export type AgentSummary = z.infer<typeof AgentSummarySchema>;
export type AgentRunRequest = z.infer<typeof AgentRunRequestSchema>;
export type AgentResult = z.infer<typeof AgentResultSchema>;
export type AgentContext = z.infer<typeof AgentContextSchema>;
export type AgentRun = z.infer<typeof AgentRunSchema>;
export type AgentHealth = z.infer<typeof AgentHealthSchema>;

// Helper type for typed results
export type TypedAgentResult<T> = AgentResult & { data?: T };
