/**
 * Test Planning agent domain types
 * Mirrors C# from src/Application/Agents.Application.TestPlanning/
 */

import { z } from 'zod';

// Test spec type
export const TestSpecTypeSchema = z.enum(['unit', 'integration', 'e2e', 'performance', 'security']);

// Test status
export const TestStatusSchema = z.enum(['draft', 'review', 'approved', 'implemented', 'deprecated']);

// Coverage level
export const CoverageLevelSchema = z.enum(['basic', 'comprehensive', 'exhaustive']);

// Test Planning Request
export const TestPlanningRequestSchema = z.object({
  type: z.enum(['generate_spec', 'create_strategy', 'analyze_coverage']),
  input: z.string().min(1, 'Input is required'),
  featureDescription: z.string().optional(),
  existingSpecs: z.array(z.string()).optional(),
  coverageLevel: CoverageLevelSchema.default('comprehensive'),
});

// Test Spec
export const TestSpecSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  description: z.string(),
  type: TestSpecTypeSchema,
  status: TestStatusSchema,
  scenarios: z.array(z.object({
    name: z.string(),
    given: z.string(),
    when: z.string(),
    then: z.string(),
    priority: z.enum(['low', 'medium', 'high']),
  })),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  createdBy: z.string(),
});

// Test Strategy
export const TestStrategySchema = z.object({
  id: z.string().uuid(),
  feature: z.string(),
  objectives: z.array(z.string()),
  scope: z.object({
    included: z.array(z.string()),
    excluded: z.array(z.string()),
  }),
  testTypes: z.array(TestSpecTypeSchema),
  estimatedEffort: z.string(), // Duration
  riskLevel: z.enum(['low', 'medium', 'high']),
  createdAt: z.string().datetime(),
});

// Test Coverage Analysis
export const TestCoverageSchema = z.object({
  totalTests: z.number().int().min(0),
  passingTests: z.number().int().min(0),
  failingTests: z.number().int().min(0),
  coveragePercentage: z.number().min(0).max(100),
  coverageByType: z.record(TestSpecTypeSchema, z.number().min(0).max(100)),
  gaps: z.array(z.object({
    area: z.string(),
    severity: z.enum(['low', 'medium', 'high']),
    recommendation: z.string(),
  })),
});

// Test Generation Result
export const TestGenerationResultSchema = z.object({
  specId: z.string().uuid(),
  generatedTests: z.number().int().min(0),
  testFiles: z.array(z.object({
    fileName: z.string(),
    content: z.string(),
    testCount: z.number().int().min(0),
  })),
  estimatedCoverage: z.number().min(0).max(100),
});

// Type exports
export type TestSpecType = z.infer<typeof TestSpecTypeSchema>;
export type TestStatus = z.infer<typeof TestStatusSchema>;
export type CoverageLevel = z.infer<typeof CoverageLevelSchema>;
export type TestPlanningRequest = z.infer<typeof TestPlanningRequestSchema>;
export type TestSpec = z.infer<typeof TestSpecSchema>;
export type TestStrategy = z.infer<typeof TestStrategySchema>;
export type TestCoverage = z.infer<typeof TestCoverageSchema>;
export type TestGenerationResult = z.infer<typeof TestGenerationResultSchema>;
