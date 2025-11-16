import { ref, type Ref } from 'vue';
import { TestPlanningClient } from '@agents/api-client';
import type { TestSpec } from '@agents/agent-domain';

interface UseTestSpecsReturn {
  specs: Ref<TestSpec[]>;
  loading: Ref<boolean>;
  error: Ref<string | null>;
  fetchSpecs: () => Promise<void>;
  getSpec: (id: string) => Promise<TestSpec | null>;
  createSpec: (spec: Omit<TestSpec, 'id'>) => Promise<TestSpec | null>;
  updateSpec: (id: string, spec: Partial<TestSpec>) => Promise<TestSpec | null>;
  deleteSpec: (id: string) => Promise<boolean>;
}

const client = new TestPlanningClient(import.meta.env.VITE_TESTPLANNING_API_URL);

export function useTestSpecs(): UseTestSpecsReturn {
  const specs = ref<TestSpec[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);

  const fetchSpecs = async () => {
    loading.value = true;
    error.value = null;

    try {
      const result = await client.listTestSpecs();
      if (result.isSuccess && result.output) {
        specs.value = result.output;
      } else {
        error.value = result.errorMessage || 'Failed to fetch test specs';
        specs.value = [];
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to fetch test specs';
      specs.value = [];
    } finally {
      loading.value = false;
    }
  };

  const getSpec = async (id: string): Promise<TestSpec | null> => {
    loading.value = true;
    error.value = null;

    try {
      const result = await client.getTestSpec(id);
      if (result.isSuccess && result.output) {
        return result.output;
      } else {
        error.value = result.errorMessage || 'Failed to fetch test spec';
        return null;
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to fetch test spec';
      return null;
    } finally {
      loading.value = false;
    }
  };

  const createSpec = async (spec: Omit<TestSpec, 'id'>): Promise<TestSpec | null> => {
    loading.value = true;
    error.value = null;

    try {
      const result = await client.createTestSpec(spec as TestSpec);
      if (result.isSuccess && result.output) {
        await fetchSpecs(); // Refresh list
        return result.output;
      } else {
        error.value = result.errorMessage || 'Failed to create test spec';
        return null;
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to create test spec';
      return null;
    } finally {
      loading.value = false;
    }
  };

  const updateSpec = async (id: string, spec: Partial<TestSpec>): Promise<TestSpec | null> => {
    loading.value = true;
    error.value = null;

    try {
      const result = await client.updateTestSpec(id, spec as TestSpec);
      if (result.isSuccess && result.output) {
        await fetchSpecs(); // Refresh list
        return result.output;
      } else {
        error.value = result.errorMessage || 'Failed to update test spec';
        return null;
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to update test spec';
      return null;
    } finally {
      loading.value = false;
    }
  };

  const deleteSpec = async (id: string): Promise<boolean> => {
    loading.value = true;
    error.value = null;

    try {
      const result = await client.deleteTestSpec(id);
      if (result.isSuccess) {
        await fetchSpecs(); // Refresh list
        return true;
      } else {
        error.value = result.errorMessage || 'Failed to delete test spec';
        return false;
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to delete test spec';
      return false;
    } finally {
      loading.value = false;
    }
  };

  return {
    specs,
    loading,
    error,
    fetchSpecs,
    getSpec,
    createSpec,
    updateSpec,
    deleteSpec,
  };
}
