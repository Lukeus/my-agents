import { computed, watch } from 'vue';
import { useTestSpecStore } from '@/stores/testSpecStore';
import { z } from 'zod';

/**
 * Composable for working with test specs
 * Uses Pinia store for state management with caching and optimistic updates
 * 
 * @example
 * ```ts
 * const { specs, loading, error, fetchSpecs, createSpec } = useTestSpecs();
 * 
 * onMounted(async () => {
 *   await fetchSpecs();
 * });
 * ```
 */
export function useTestSpecs() {
  const store = useTestSpecStore();

  // Reactive state from store
  const specs = computed(() => store.specs);
  const loading = computed(() => store.loading);
  const error = computed(() => store.error);
  const lastFetched = computed(() => store.lastFetched);

  /**
   * Fetch test specs with optional force refresh
   * Uses cache if data was fetched within the last 5 minutes
   */
  const fetchSpecs = async (force = false): Promise<void> => {
    await store.fetchSpecs(force);
    
    // Show notification on error
    if (store.error) {
      notifyError('Failed to fetch test specs', store.error);
    }
  };

  /**
   * Get a single test spec by ID
   * Checks cache first, fetches from API if not found
   */
  const getSpec = async (id: string) => {
    const result = await store.getSpec(id);
    
    if (store.error) {
      notifyError('Failed to fetch test spec', store.error);
    }
    
    return result;
  };

  /**
   * Create a new test spec with optimistic updates
   * Automatically validates input with Zod before API call
   */
  const createSpec = async (spec: {
    name: string;
    description?: string;
    feature: string;
    scenarios: Array<{
      name: string;
      steps: string[];
      expectedResult: string;
    }>;
  }) => {
    const result = await store.createSpec(spec);
    
    if (result) {
      notifySuccess('Test spec created', `Successfully created "${result.name}"`);
    } else if (store.error) {
      notifyError('Failed to create test spec', store.error);
    }
    
    return result;
  };

  /**
   * Update an existing test spec with optimistic updates
   * Automatically rolls back on failure
   */
  const updateSpec = async (id: string, updates: Partial<{
    name: string;
    description?: string;
    feature: string;
    scenarios: Array<{
      name: string;
      steps: string[];
      expectedResult: string;
    }>;
  }>) => {
    const result = await store.updateSpec(id, updates);
    
    if (result) {
      notifySuccess('Test spec updated', `Successfully updated "${result.name}"`);
    } else if (store.error) {
      notifyError('Failed to update test spec', store.error);
    }
    
    return result;
  };

  /**
   * Delete a test spec with optimistic removal
   * Automatically rolls back on failure
   */
  const deleteSpec = async (id: string) => {
    const spec = store.getSpecById(id);
    const result = await store.deleteSpec(id);
    
    if (result) {
      notifySuccess('Test spec deleted', spec ? `Deleted "${spec.name}"` : 'Test spec deleted');
    } else if (store.error) {
      notifyError('Failed to delete test spec', store.error);
    }
    
    return result;
  };

  /**
   * Invalidate cache and force refresh
   */
  const refreshSpecs = async () => {
    store.invalidateCache();
    await fetchSpecs(true);
  };

  /**
   * Clear any error state
   */
  const clearError = () => {
    store.clearError();
  };

  // Auto-clear errors after 5 seconds
  watch(() => store.error, (newError) => {
    if (newError) {
      setTimeout(() => {
        if (store.error === newError) {
          clearError();
        }
      }, 5000);
    }
  });

  return {
    // State
    specs,
    loading,
    error,
    lastFetched,
    
    // Actions
    fetchSpecs,
    getSpec,
    createSpec,
    updateSpec,
    deleteSpec,
    refreshSpecs,
    clearError,
    
    // Utilities
    getSpecById: store.getSpecById,
    shouldRefetch: computed(() => store.shouldRefetch)
  };
}

// Notification helpers (to be implemented with actual notification system)
function notifySuccess(title: string, message: string) {
  console.log(`✅ ${title}: ${message}`);
  // TODO: Integrate with actual notification system from @agents/design-system
  // notify({ type: 'success', title, message, duration: 3000 });
}

function notifyError(title: string, message: string) {
  console.error(`❌ ${title}: ${message}`);
  // TODO: Integrate with actual notification system from @agents/design-system
  // notify({ type: 'error', title, message, duration: 5000 });
}
