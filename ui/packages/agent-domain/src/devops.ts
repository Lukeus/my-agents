/**
 * DevOps agent domain types
 * Mirrors C# from src/Application/Agents.Application.DevOps/
 */

import { z } from 'zod';

// DevOps action types
export const DevOpsActionSchema = z.enum([
  'create_issue',
  'update_issue',
  'trigger_workflow',
  'analyze_sprint',
  'generate_report',
]);

// Issue priority
export const IssuePrioritySchema = z.enum(['low', 'medium', 'high', 'critical']);

// Issue status
export const IssueStatusSchema = z.enum(['open', 'in_progress', 'review', 'closed']);

// DevOps Request
export const DevOpsRequestSchema = z.object({
  action: DevOpsActionSchema,
  data: z.record(z.unknown()),
  repository: z.string().optional(),
  metadata: z.record(z.unknown()).optional(),
});

// Issue Request
export const IssueRequestSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  description: z.string(),
  priority: IssuePrioritySchema.default('medium'),
  labels: z.array(z.string()).default([]),
  assignee: z.string().optional(),
  repository: z.string(),
});

// Workflow Trigger Request
export const WorkflowTriggerRequestSchema = z.object({
  workflowName: z.string().min(1, 'Workflow name is required'),
  branch: z.string().default('main'),
  inputs: z.record(z.unknown()).optional(),
  repository: z.string(),
});

// Sprint Analysis Request
export const SprintAnalysisRequestSchema = z.object({
  sprintId: z.string().optional(),
  startDate: z.string().datetime(),
  endDate: z.string().datetime(),
  includeMetrics: z.boolean().default(true),
});

// Issue Result
export const IssueResultSchema = z.object({
  issueNumber: z.number().int().positive(),
  url: z.string().url(),
  status: IssueStatusSchema,
  createdAt: z.string().datetime(),
});

// Workflow Result
export const WorkflowResultSchema = z.object({
  workflowId: z.string(),
  runNumber: z.number().int().positive(),
  status: z.enum(['queued', 'in_progress', 'completed', 'failed']),
  url: z.string().url(),
  triggeredAt: z.string().datetime(),
});

// Sprint Metrics
export const SprintMetricsSchema = z.object({
  sprintId: z.string(),
  totalIssues: z.number().int().min(0),
  completedIssues: z.number().int().min(0),
  completionRate: z.number().min(0).max(100),
  averageCompletionTime: z.string(), // Duration
  velocity: z.number().min(0),
});

// Type exports
export type DevOpsAction = z.infer<typeof DevOpsActionSchema>;
export type IssuePriority = z.infer<typeof IssuePrioritySchema>;
export type IssueStatus = z.infer<typeof IssueStatusSchema>;
export type DevOpsRequest = z.infer<typeof DevOpsRequestSchema>;
export type IssueRequest = z.infer<typeof IssueRequestSchema>;
export type WorkflowTriggerRequest = z.infer<typeof WorkflowTriggerRequestSchema>;
export type SprintAnalysisRequest = z.infer<typeof SprintAnalysisRequestSchema>;
export type IssueResult = z.infer<typeof IssueResultSchema>;
export type WorkflowResult = z.infer<typeof WorkflowResultSchema>;
export type SprintMetrics = z.infer<typeof SprintMetricsSchema>;
