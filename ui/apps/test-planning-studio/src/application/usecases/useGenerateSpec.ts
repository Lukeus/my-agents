import { ref, type Ref } from 'vue';
import { z } from 'zod';
import { TestPlanningClient } from '@agents/api-client';
import { TestSpecSchema, type TestSpec } from '@agents/agent-domain';

// Request schema with Zod
const GenerateSpecRequestSchema = z.object({
  featureName: z.string().min(1, 'Feature name is required'),
  featureDescription: z.string().min(10, 'Feature description must be at least 10 characters'),
  acceptanceCriteria: z.array(z.string()).optional(),
  testingStrategy: z.enum(['bdd', 'tdd', 'e2e', 'unit', 'integration']).optional(),
});

type GenerateSpecRequest = z.infer<typeof GenerateSpecRequestSchema>;

interface UseGenerateSpecReturn {
  generatedSpec: Ref<TestSpec | null>;
  generating: Ref<boolean>;
  error: Ref<string | null>;
  generateSpec: (request: GenerateSpecRequest) => Promise<TestSpec | null>;
  clearGenerated: () => void;
}

const client = new TestPlanningClient(import.meta.env.VITE_TESTPLANNING_API_URL);

export function useGenerateSpec(): UseGenerateSpecReturn {
  const generatedSpec = ref<TestSpec | null>(null);
  const generating = ref(false);
  const error = ref<string | null>(null);

  const generateSpec = async (request: GenerateSpecRequest): Promise<TestSpec | null> => {
    generating.value = true;
    error.value = null;

    try {
      // Validate request with Zod
      const validatedRequest = GenerateSpecRequestSchema.parse(request);

      const result = await client.generateSpec(validatedRequest);
      if (result.isSuccess && result.output) {
        // Validate response with Zod
        const validatedSpec = TestSpecSchema.parse(result.output);
        generatedSpec.value = validatedSpec;
        return validatedSpec;
      } else {
        error.value = result.errorMessage || 'Failed to generate test spec';
        generatedSpec.value = null;
        return null;
      }
    } catch (err) {
      if (err instanceof z.ZodError) {
        error.value = `Validation error: ${err.errors.map((e) => e.message).join(', ')}`;
      } else {
        error.value = err instanceof Error ? err.message : 'Failed to generate test spec';
      }
      generatedSpec.value = null;
      return null;
    } finally {
      generating.value = false;
    }
  };

  const clearGenerated = () => {
    generatedSpec.value = null;
    error.value = null;
  };

  return {
    generatedSpec,
    generating,
    error,
    generateSpec,
    clearGenerated,
  };
}

// Export the schema for use in components
export { GenerateSpecRequestSchema };
export type { GenerateSpecRequest };
