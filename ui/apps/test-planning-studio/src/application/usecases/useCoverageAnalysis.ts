import { ref, type Ref } from 'vue';
import { z } from 'zod';
import { TestPlanningClient } from '@agents/api-client';
import { TestCoverageSchema, type TestCoverage } from '@agents/agent-domain';

// Request schema with Zod
const CoverageAnalysisRequestSchema = z.object({
  projectPath: z.string().min(1, 'Project path is required'),
  includePatterns: z.array(z.string()).optional(),
  excludePatterns: z.array(z.string()).optional(),
});

type CoverageAnalysisRequest = z.infer<typeof CoverageAnalysisRequestSchema>;

interface UseCoverageAnalysisReturn {
  coverage: Ref<TestCoverage | null>;
  analyzing: Ref<boolean>;
  error: Ref<string | null>;
  analyzeCoverage: (request: CoverageAnalysisRequest) => Promise<TestCoverage | null>;
  clearCoverage: () => void;
}

const client = new TestPlanningClient(import.meta.env.VITE_TESTPLANNING_API_URL);

export function useCoverageAnalysis(): UseCoverageAnalysisReturn {
  const coverage = ref<TestCoverage | null>(null);
  const analyzing = ref(false);
  const error = ref<string | null>(null);

  const analyzeCoverage = async (request: CoverageAnalysisRequest): Promise<TestCoverage | null> => {
    analyzing.value = true;
    error.value = null;

    try {
      // Validate request with Zod
      const validatedRequest = CoverageAnalysisRequestSchema.parse(request);

      const result = await client.analyzeCoverage(validatedRequest);
      if (result.isSuccess && result.output) {
        // Validate response with Zod
        const validatedCoverage = TestCoverageSchema.parse(result.output);
        coverage.value = validatedCoverage;
        return validatedCoverage;
      } else {
        error.value = result.errorMessage || 'Failed to analyze coverage';
        coverage.value = null;
        return null;
      }
    } catch (err) {
      if (err instanceof z.ZodError) {
        error.value = `Validation error: ${err.errors.map((e) => e.message).join(', ')}`;
      } else {
        error.value = err instanceof Error ? err.message : 'Failed to analyze coverage';
      }
      coverage.value = null;
      return null;
    } finally {
      analyzing.value = false;
    }
  };

  const clearCoverage = () => {
    coverage.value = null;
    error.value = null;
  };

  return {
    coverage,
    analyzing,
    error,
    analyzeCoverage,
    clearCoverage,
  };
}

// Export the schema for use in components
export { CoverageAnalysisRequestSchema };
export type { CoverageAnalysisRequest };
