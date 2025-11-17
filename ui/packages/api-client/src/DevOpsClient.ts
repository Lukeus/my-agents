/**
 * DevOps Agent API Client
 * Maps to /api/devops endpoints
 */

import { BaseClient } from './BaseClient';
import {
  DevOpsRequestSchema,
  IssueRequestSchema,
  WorkflowTriggerRequestSchema,
  SprintAnalysisRequestSchema,
  IssueResultSchema,
  WorkflowResultSchema,
  SprintMetricsSchema,
  AgentResultSchema,
  AgentHealthSchema,
  type DevOpsRequest,
  type IssueRequest,
  type WorkflowTriggerRequest,
  type SprintAnalysisRequest,
  type IssueResult,
  type WorkflowResult,
  type SprintMetrics,
  type AgentResult,
  type AgentHealth,
} from '@agents/agent-domain';

export class DevOpsClient extends BaseClient {
  /**
   * Execute generic DevOps action
   * POST /api/devops/execute
   */
  async execute(request: DevOpsRequest): Promise<AgentResult> {
    DevOpsRequestSchema.parse(request);
    return this.post('/api/devops/execute', request, AgentResultSchema);
  }

  /**
   * Create GitHub issue
   * POST /api/devops/issues
   */
  async createIssue(request: IssueRequest): Promise<IssueResult> {
    IssueRequestSchema.parse(request);
    return this.post('/api/devops/issues', request, IssueResultSchema);
  }

  /**
   * Trigger GitHub workflow
   * POST /api/devops/workflows/trigger
   */
  async triggerWorkflow(request: WorkflowTriggerRequest): Promise<WorkflowResult> {
    WorkflowTriggerRequestSchema.parse(request);
    return this.post('/api/devops/workflows/trigger', request, WorkflowResultSchema);
  }

  /**
   * Analyze sprint metrics
   * POST /api/devops/sprints/analyze
   */
  async analyzeSprint(request: SprintAnalysisRequest): Promise<SprintMetrics> {
    SprintAnalysisRequestSchema.parse(request);
    return this.post('/api/devops/sprints/analyze', request, SprintMetricsSchema);
  }

  /**
   * Get issue by number
   * GET /api/devops/issues/{issueNumber}
   */
  async getIssue(issueNumber: number, repository: string): Promise<IssueResult> {
    return this.get(
      `/api/devops/issues/${issueNumber}?repository=${repository}`,
      IssueResultSchema
    );
  }

  /**
   * Get workflow status
   * GET /api/devops/workflows/{workflowId}
   */
  async getWorkflowStatus(workflowId: string): Promise<WorkflowResult> {
    return this.get(`/api/devops/workflows/${workflowId}`, WorkflowResultSchema);
  }

  /**
   * Get DevOps health status
   * GET /api/devops/health
   */
  async getHealth(): Promise<AgentHealth> {
    return this.get('/api/devops/health', AgentHealthSchema);
  }
}
