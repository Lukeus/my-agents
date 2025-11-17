import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { TestPlanningClient } from '@agents/api-client';
import type { TestSpec } from '@agents/agent-domain';
import { z } from 'zod';
import { appConfig } from '@agents/shared';

// Zod schema for TestSpec validation
const testSpecSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1).max(200),
  description: z.string().optional(),
  feature: z.string().min(1),
  scenarios: z.array(z.object({
    name: z.string(),
    steps: z.array(z.string()),
    expectedResult: z.string()
  })).min(1),
  createdAt: z.string().datetime().optional(),
  updatedAt: z.string().datetime().optional()
});

const createTestSpecSchema = testSpecSchema.omit({ id: true, createdAt: true, updatedAt: true });

type CreateTestSpecInput = z.infer<typeof createTestSpecSchema>;

// Initialize client
const client = new TestPlanningClient(appConfig.api.testPlanning);

export const useTestSpecStore = defineStore('testSpecs', () => {
  // State
  const specs = ref<TestSpec[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const lastFetched = ref<Date | null>(null);
  
  // Cache configuration
  const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes

  // Computed
  const shouldRefetch = computed(() => {
    if (!lastFetched.value) return true;
    return Date.now() - lastFetched.value.getTime() > CACHE_DURATION;
  });

  const getSpecById = computed(() => {
    return (id: string) => specs.value.find(spec => spec.id === id);
  });

  // Actions
  const handleError = (err: unknown, operation: string): string => {
    const message = err instanceof Error ? err.message : `Failed to ${operation}`;
    error.value = message;
    console.error(`[testSpecStore] ${operation} failed:`, err);
    return message;
  };

  const fetchSpecs = async (force = false): Promise<void> => {
    if (!force && !shouldRefetch.value) {
      return;
    }

    loading.value = true;
    error.value = null;

    try {
      const result = await client.listTestSpecs();
      
      if (result.isSuccess && result.output) {
        // Validate response data with Zod
        const validatedSpecs = z.array(testSpecSchema).safeParse(result.output);
        
        if (validatedSpecs.success) {
          specs.value = validatedSpecs.data;
          lastFetched.value = new Date();
        } else {
          throw new Error('Invalid test spec data received from API');
        }
      } else {
        throw new Error(result.errorMessage || 'Failed to fetch test specs');
      }
    } catch (err) {
      handleError(err, 'fetch test specs');
      specs.value = [];
    } finally {
      loading.value = false;
    }
  };

  const getSpec = async (id: string): Promise<TestSpec | null> => {
    // Check cache first
    const cached = getSpecById.value(id);
    if (cached) {
      return cached;
    }

    loading.value = true;
    error.value = null;

    try {
      const result = await client.getTestSpec(id);
      
      if (result.isSuccess && result.output) {
        // Validate with Zod
        const validated = testSpecSchema.safeParse(result.output);
        
        if (validated.success) {
          // Update cache
          const index = specs.value.findIndex(s => s.id === id);
          if (index >= 0) {
            specs.value[index] = validated.data;
          } else {
            specs.value.push(validated.data);
          }
          return validated.data;
        } else {
          throw new Error('Invalid test spec data received');
        }
      } else {
        throw new Error(result.errorMessage || 'Failed to fetch test spec');
      }
    } catch (err) {
      handleError(err, 'fetch test spec');
      return null;
    } finally {
      loading.value = false;
    }
  };

  const createSpec = async (spec: CreateTestSpecInput): Promise<TestSpec | null> => {
    // Validate input with Zod
    const validation = createTestSpecSchema.safeParse(spec);
    if (!validation.success) {
      error.value = `Validation error: ${validation.error.errors.map(e => e.message).join(', ')}`;
      return null;
    }

    // Optimistic update
    const tempId = crypto.randomUUID();
    const tempSpec: TestSpec = {
      ...validation.data,
      id: tempId,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    } as TestSpec;
    
    specs.value.push(tempSpec);
    loading.value = true;
    error.value = null;

    try {
      const result = await client.createTestSpec(tempSpec);
      
      if (result.isSuccess && result.output) {
        // Validate response
        const validated = testSpecSchema.safeParse(result.output);
        
        if (validated.success) {
          // Replace temp with actual
          const index = specs.value.findIndex(s => s.id === tempId);
          if (index >= 0) {
            specs.value[index] = validated.data;
          }
          return validated.data;
        } else {
          throw new Error('Invalid response from create API');
        }
      } else {
        throw new Error(result.errorMessage || 'Failed to create test spec');
      }
    } catch (err) {
      // Rollback optimistic update
      specs.value = specs.value.filter(s => s.id !== tempId);
      handleError(err, 'create test spec');
      return null;
    } finally {
      loading.value = false;
    }
  };

  const updateSpec = async (id: string, updates: Partial<TestSpec>): Promise<TestSpec | null> => {
    // Find existing spec
    const existingIndex = specs.value.findIndex(s => s.id === id);
    if (existingIndex < 0) {
      error.value = 'Test spec not found';
      return null;
    }

    // Store original for rollback
    const original = { ...specs.value[existingIndex] };
    
    // Optimistic update
    const updated = { ...original, ...updates, updatedAt: new Date().toISOString() };
    specs.value[existingIndex] = updated as TestSpec;
    
    loading.value = true;
    error.value = null;

    try {
      const result = await client.updateTestSpec(id, updated as TestSpec);
      
      if (result.isSuccess && result.output) {
        // Validate response
        const validated = testSpecSchema.safeParse(result.output);
        
        if (validated.success) {
          specs.value[existingIndex] = validated.data;
          return validated.data;
        } else {
          throw new Error('Invalid response from update API');
        }
      } else {
        throw new Error(result.errorMessage || 'Failed to update test spec');
      }
    } catch (err) {
      // Rollback optimistic update
      specs.value[existingIndex] = original;
      handleError(err, 'update test spec');
      return null;
    } finally {
      loading.value = false;
    }
  };

  const deleteSpec = async (id: string): Promise<boolean> => {
    // Find spec for optimistic removal
    const index = specs.value.findIndex(s => s.id === id);
    if (index < 0) {
      error.value = 'Test spec not found';
      return false;
    }

    // Store for rollback
    const removed = specs.value[index];
    
    // Optimistic removal
    specs.value.splice(index, 1);
    loading.value = true;
    error.value = null;

    try {
      const result = await client.deleteTestSpec(id);
      
      if (result.isSuccess) {
        return true;
      } else {
        throw new Error(result.errorMessage || 'Failed to delete test spec');
      }
    } catch (err) {
      // Rollback optimistic removal
      specs.value.splice(index, 0, removed);
      handleError(err, 'delete test spec');
      return false;
    } finally {
      loading.value = false;
    }
  };

  const invalidateCache = () => {
    lastFetched.value = null;
  };

  const clearError = () => {
    error.value = null;
  };

  return {
    // State
    specs,
    loading,
    error,
    lastFetched,
    
    // Computed
    shouldRefetch,
    getSpecById,
    
    // Actions
    fetchSpecs,
    getSpec,
    createSpec,
    updateSpec,
    deleteSpec,
    invalidateCache,
    clearError
  };
});
