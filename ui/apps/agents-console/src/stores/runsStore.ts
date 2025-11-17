import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { 
  AgentRunSchema, 
  AgentResultSchema,
  AgentRunRequestSchema,
  type AgentRun, 
  type AgentResult,
  type AgentRunRequest 
} from '@agents/agent-domain';
import { z } from 'zod';

const CACHE_DURATION_MS = 2 * 60 * 1000; // 2 minutes

export const useRunsStore = defineStore('runs', () => {
  // State
  const runs = ref<AgentRun[]>([]);
  const currentRun = ref<AgentRun | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const lastFetch = ref<number | null>(null);

  // Computed
  const recentRuns = computed(() =>
    runs.value.slice().sort((a, b) => 
      new Date(b.startedAt).getTime() - new Date(a.startedAt).getTime()
    ).slice(0, 10)
  );

  const succeededRuns = computed(() =>
    runs.value.filter(run => run.status === 'succeeded')
  );

  const failedRuns = computed(() =>
    runs.value.filter(run => run.status === 'failed')
  );

  const runningRuns = computed(() =>
    runs.value.filter(run => run.status === 'running')
  );

  const isCacheValid = computed(() => {
    if (!lastFetch.value) return false;
    return Date.now() - lastFetch.value < CACHE_DURATION_MS;
  });

  const runsCount = computed(() => runs.value.length);
  const successRate = computed(() => {
    if (runs.value.length === 0) return 0;
    return (succeededRuns.value.length / runs.value.length) * 100;
  });

  // Actions
  async function fetchRuns(force = false) {
    if (!force && isCacheValid.value) {
      return; // Use cached data
    }

    loading.value = true;
    error.value = null;

    try {
      // TODO: Replace with actual API call
      // const response = await runsClient.listRuns();
      // Validate response with Zod
      // runs.value = z.array(AgentRunSchema).parse(response);

      // For now, use mock data
      await new Promise(resolve => setTimeout(resolve, 300));
      
      const mockRunsData = [
        {
          id: crypto.randomUUID(),
          agentName: 'notification',
          startedAt: new Date(Date.now() - 1000 * 60 * 5).toISOString(),
          completedAt: new Date(Date.now() - 1000 * 60 * 4).toISOString(),
          status: 'succeeded',
          input: JSON.stringify({ channel: 'email', recipient: 'user@example.com', subject: 'Test' }),
          output: 'Email sent successfully',
          duration: '00:00:01.234',
          initiatedBy: 'admin',
        },
        {
          id: crypto.randomUUID(),
          agentName: 'testplanning',
          startedAt: new Date(Date.now() - 1000 * 60 * 15).toISOString(),
          completedAt: new Date(Date.now() - 1000 * 60 * 10).toISOString(),
          status: 'succeeded',
          input: JSON.stringify({ type: 'generate_spec', featureDescription: 'User authentication' }),
          output: 'Test specification generated',
          duration: '00:00:05.123',
          initiatedBy: 'developer',
        },
        {
          id: crypto.randomUUID(),
          agentName: 'devops',
          startedAt: new Date(Date.now() - 1000 * 60 * 30).toISOString(),
          completedAt: new Date(Date.now() - 1000 * 60 * 29).toISOString(),
          status: 'failed',
          input: JSON.stringify({ action: 'create_issue', data: { title: 'Bug fix' } }),
          errorMessage: 'Failed to connect to Azure DevOps',
          duration: '00:00:00.500',
          initiatedBy: 'system',
        },
      ];

      // Validate with Zod
      runs.value = z.array(AgentRunSchema).parse(mockRunsData);
      lastFetch.value = Date.now();
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to fetch runs';
      console.error('Error fetching runs:', err);
    } finally {
      loading.value = false;
    }
  }

  async function fetchRunById(id: string): Promise<AgentRun | null> {
    // Check cache first
    const cachedRun = runs.value.find(run => run.id === id);
    if (cachedRun) {
      currentRun.value = cachedRun;
      return cachedRun;
    }

    loading.value = true;
    error.value = null;

    try {
      // TODO: Replace with actual API call
      // const response = await runsClient.getRunById(id);
      // Validate with Zod
      // const run = AgentRunSchema.parse(response);

      // For now, return from cache or null
      currentRun.value = null;
      return null;
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to fetch run';
      console.error('Error fetching run:', err);
      return null;
    } finally {
      loading.value = false;
    }
  }

  async function executeAgent(request: AgentRunRequest): Promise<AgentResult> {
    // Validate request with Zod
    const validatedRequest = AgentRunRequestSchema.parse(request);

    loading.value = true;
    error.value = null;

    try {
      // Create optimistic run entry
      const runId = crypto.randomUUID();
      const newRun: AgentRun = AgentRunSchema.parse({
        id: runId,
        agentName: validatedRequest.agentName,
        startedAt: new Date().toISOString(),
        status: 'running',
        input: validatedRequest.input,
        initiatedBy: validatedRequest.metadata?.initiatedBy || 'unknown',
      });

      // Add to runs list optimistically
      runs.value = [newRun, ...runs.value];

      // TODO: Replace with actual API call
      // const apiUrlMap: Record<string, string> = { ... };
      // const response = await fetch(`${apiUrl}/execute`, { ... });
      // const result = AgentResultSchema.parse(await response.json());

      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      const mockResult = {
        isSuccess: Math.random() > 0.3, // 70% success rate
        output: 'Agent execution completed',
        duration: '00:00:02.000',
        metadata: {},
      };

      // Validate result with Zod
      const result = AgentResultSchema.parse(mockResult);

      // Update run with result
      const updatedRun: AgentRun = AgentRunSchema.parse({
        ...newRun,
        completedAt: new Date().toISOString(),
        status: result.isSuccess ? 'succeeded' : 'failed',
        output: result.output,
        errorMessage: result.errorMessage,
        duration: result.duration,
      });

      // Update in runs list
      const index = runs.value.findIndex(r => r.id === runId);
      if (index !== -1) {
        runs.value[index] = updatedRun;
      }

      return result;
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to execute agent';
      console.error('Error executing agent:', err);
      
      // Return failed result (validated with Zod)
      return AgentResultSchema.parse({
        isSuccess: false,
        errorMessage: error.value,
        duration: '00:00:00.000',
      });
    } finally {
      loading.value = false;
    }
  }

  function getRunsByAgent(agentName: string): AgentRun[] {
    return runs.value.filter(run => run.agentName === agentName);
  }

  function clearCache() {
    runs.value = [];
    currentRun.value = null;
    lastFetch.value = null;
    error.value = null;
  }

  return {
    // State
    runs,
    currentRun,
    loading,
    error,
    lastFetch,

    // Computed
    recentRuns,
    succeededRuns,
    failedRuns,
    runningRuns,
    isCacheValid,
    runsCount,
    successRate,

    // Actions
    fetchRuns,
    fetchRunById,
    executeAgent,
    getRunsByAgent,
    clearCache,
  };
});
