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
   * Alias for listSpecs() for consistency
   */
  async listTestSpecs(): Promise<AgentResult<TestSpec[]>> {
    try {
      const specs = await this.listSpecs();
      return {
        isSuccess: true,
        output: specs,
        errorMessage: null,
      };
    } catch (error) {
      return {
        isSuccess: false,
        output: null,
        errorMessage: error instanceof Error ? error.message : 'Failed to fetch specs',
      };
    }
  }

  /**
   * Get test spec by ID (wrapper with AgentResult)
   */
  async getTestSpec(id: string): Promise<AgentResult<TestSpec>> {
    try {
      const spec = await this.getSpecById(id);
      return {
        isSuccess: true,
        output: spec,
        errorMessage: null,
      };
    } catch (error) {
      return {
        isSuccess: false,
        output: null,
        errorMessage: error instanceof Error ? error.message : 'Failed to fetch spec',
      };
    }
  }

  /**
   * Create test spec (wrapper with AgentResult)
   */
  async createTestSpec(spec: TestSpec): Promise<AgentResult<TestSpec>> {
    try {
      // Note: In a real implementation, this would POST to /api/testplanning/specs
      // For now, we'll use generateSpec as a placeholder
      const result = await this.generateSpec(spec.feature || '', spec.description || '');
      return {
        isSuccess: true,
        output: result,
        errorMessage: null,
      };
    } catch (error) {
      return {
        isSuccess: false,
        output: null,
        errorMessage: error instanceof Error ? error.message : 'Failed to create spec',
      };
    }
  }

  /**
   * Update test spec (wrapper with AgentResult)
   */
  async updateTestSpec(id: string, spec: TestSpec): Promise<AgentResult<TestSpec>> {
    try {
      const updated = await this.updateSpec(id, spec);
      return {
        isSuccess: true,
        output: updated,
        errorMessage: null,
      };
    } catch (error) {
      return {
        isSuccess: false,
        output: null,
        errorMessage: error instanceof Error ? error.message : 'Failed to update spec',
      };
    }
  }

  /**
   * Delete test spec (wrapper with AgentResult)
   */
  async deleteTestSpec(id: string): Promise<AgentResult<void>> {
    try {
      await this.deleteSpec(id);
      return {
        isSuccess: true,
        output: undefined,
        errorMessage: null,
      };
    } catch (error) {
      return {
        isSuccess: false,
        output: undefined,
        errorMessage: error instanceof Error ? error.message : 'Failed to delete spec',
      };
    }
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
