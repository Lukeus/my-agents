/**
 * Test Planning Agent API Client
 * Maps to /api/testplanning endpoints
 */

import { BaseClient } from './BaseClient';
import {
  TestPlanningRequestSchema,
  TestSpecSchema,
  TestStrategySchema,
  TestCoverageSchema,
  TestGenerationResultSchema,
  AgentResultSchema,
  AgentHealthSchema,
  type TestPlanningRequest,
  type TestSpec,
  type TestStrategy,
  type TestCoverage,
  type TestGenerationResult,
  type AgentResult,
  type AgentHealth,
} from '@agents/agent-domain';
import { z } from 'zod';

export class TestPlanningClient extends BaseClient {
  /**
   * Execute generic test planning action
   * POST /api/testplanning/execute
   */
  async execute(request: TestPlanningRequest): Promise<AgentResult> {
    TestPlanningRequestSchema.parse(request);
    return this.post('/api/testplanning/execute', request, AgentResultSchema);
  }

  /**
   * Generate test spec from feature description
   * POST /api/testplanning/specs/generate
   */
  async generateSpec(featureDescription: string, coverageLevel = 'comprehensive'): Promise<TestSpec> {
    return this.post(
      '/api/testplanning/specs/generate',
      { featureDescription, coverageLevel },
      TestSpecSchema
    );
  }

  /**
   * Create test strategy
   * POST /api/testplanning/strategy
   */
  async createStrategy(feature: string, objectives: string[]): Promise<TestStrategy> {
    return this.post(
      '/api/testplanning/strategy',
      { feature, objectives },
      TestStrategySchema
    );
  }

  /**
   * Analyze test coverage
   * POST /api/testplanning/coverage/analyze
   */
  async analyzeCoverage(testFiles: string[]): Promise<TestCoverage> {
    return this.post(
      '/api/testplanning/coverage/analyze',
      { testFiles },
      TestCoverageSchema
    );
  }

  /**
   * Generate tests from spec
   * POST /api/testplanning/tests/generate
   */
  async generateTests(specId: string): Promise<TestGenerationResult> {
    return this.post(
      `/api/testplanning/tests/generate`,
      { specId },
      TestGenerationResultSchema
    );
  }

  /**
   * Get test spec by ID
   * GET /api/testplanning/specs/{id}
   */
  async getSpecById(id: string): Promise<TestSpec> {
    return this.get(`/api/testplanning/specs/${id}`, TestSpecSchema);
  }

  /**
   * List all test specs
   * GET /api/testplanning/specs
   */
  async listSpecs(): Promise<TestSpec[]> {
    return this.get('/api/testplanning/specs', z.array(TestSpecSchema));
  }

  /**
   * Update test spec
   * PUT /api/testplanning/specs/{id}
   */
  async updateSpec(id: string, spec: Partial<TestSpec>): Promise<TestSpec> {
    return this.put(`/api/testplanning/specs/${id}`, spec, TestSpecSchema);
  }

  /**
   * Delete test spec
   * DELETE /api/testplanning/specs/{id}
   */
  async deleteSpec(id: string): Promise<void> {
    await this.delete(`/api/testplanning/specs/${id}`, z.void());
  }

  /**
   * Get test planning health status
   * GET /api/testplanning/health
   */
  async getHealth(): Promise<AgentHealth> {
    return this.get('/api/testplanning/health', AgentHealthSchema);
  }
}
