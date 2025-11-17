import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { 
  AgentSummarySchema, 
  AgentHealthSchema,
  type AgentSummary, 
  type AgentHealth 
} from '@agents/agent-domain';

const CACHE_DURATION_MS = 5 * 60 * 1000; // 5 minutes

export const useAgentsStore = defineStore('agents', () => {
  // State
  const agents = ref<AgentSummary[]>([]);
  const agentHealthMap = ref<Map<string, AgentHealth>>(new Map());
  const loading = ref(false);
  const error = ref<string | null>(null);
  const lastFetch = ref<number | null>(null);

  // Computed
  const activeAgents = computed(() =>
    agents.value.filter(agent => agent.status === 'enabled')
  );

  const isCacheValid = computed(() => {
    if (!lastFetch.value) return false;
    return Date.now() - lastFetch.value < CACHE_DURATION_MS;
  });

  const agentsCount = computed(() => agents.value.length);
  const activeAgentsCount = computed(() => activeAgents.value.length);

  // Mock agent data (will be replaced with real API calls)
  // Using Zod to validate at runtime
  const createMockAgent = (data: unknown): AgentSummary => {
    return AgentSummarySchema.parse(data);
  };

  const mockAgents: AgentSummary[] = [
    createMockAgent({
      name: 'notification',
      displayName: 'Notification Agent',
      description: 'Multi-channel notifications via email, SMS, Teams, and Slack',
      category: 'notification',
      isEnabled: true,
      status: 'enabled',
    }),
    createMockAgent({
      name: 'devops',
      displayName: 'DevOps Agent',
      description: 'Issue management, workflow automation, and sprint analytics',
      category: 'devops',
      isEnabled: true,
      status: 'enabled',
    }),
    createMockAgent({
      name: 'testplanning',
      displayName: 'Test Planning Agent',
      description: 'Generate test specifications, coverage analysis, and BDD scenarios',
      category: 'test-planning',
      isEnabled: true,
      status: 'enabled',
    }),
    createMockAgent({
      name: 'implementation',
      displayName: 'Implementation Agent',
      description: 'Code generation, refactoring assistance, and best practices',
      category: 'implementation',
      isEnabled: true,
      status: 'enabled',
    }),
    createMockAgent({
      name: 'servicedesk',
      displayName: 'Service Desk Agent',
      description: 'Support ticket management, auto-response, and knowledge base',
      category: 'servicedesk',
      isEnabled: true,
      status: 'enabled',
    }),
    createMockAgent({
      name: 'bimclassification',
      displayName: 'BIM Classification Agent',
      description: 'BIM data classification, metadata extraction, and organization',
      category: 'bimclassification',
      isEnabled: true,
      status: 'enabled',
    }),
  ];

  // Actions
  async function fetchAgents(force = false) {
    if (!force && isCacheValid.value) {
      return; // Use cached data
    }

    loading.value = true;
    error.value = null;

    try {
      // TODO: Replace with actual API call
      // const response = await agentsClient.listAgents();
      // Validate response with Zod:
      // agents.value = z.array(AgentSummarySchema).parse(response);

      // For now, use mock data (already validated via createMockAgent)
      await new Promise(resolve => setTimeout(resolve, 500)); // Simulate API delay
      agents.value = mockAgents;
      lastFetch.value = Date.now();

      // Fetch health status for each agent
      if (import.meta.env.VITE_ENABLE_HEALTH_CHECKS === 'true') {
        await fetchAllHealthStatuses();
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to fetch agents';
      console.error('Error fetching agents:', err);
    } finally {
      loading.value = false;
    }
  }

  async function fetchAgentHealth(agentName: string): Promise<AgentHealth> {
    const apiUrlMap: Record<string, string> = {
      notification: import.meta.env.VITE_NOTIFICATION_API_URL,
      devops: import.meta.env.VITE_DEVOPS_API_URL,
      testplanning: import.meta.env.VITE_TESTPLANNING_API_URL,
      implementation: import.meta.env.VITE_IMPLEMENTATION_API_URL,
      servicedesk: import.meta.env.VITE_SERVICEDESK_API_URL,
      bimclassification: import.meta.env.VITE_BIMCLASSIFICATION_API_URL,
    };

    const apiUrl = apiUrlMap[agentName];
    if (!apiUrl) {
      return {
        agentName,
        status: 'unhealthy',
        lastChecked: new Date().toISOString(),
        message: 'Unknown agent',
      };
    }

    try {
      const response = await fetch(`${apiUrl}/health`, {
        method: 'GET',
        headers: { 'Accept': 'application/json' },
        signal: AbortSignal.timeout(5000), // 5s timeout
      });

      const isHealthy = response.ok;
      
      // Validate with Zod
      return AgentHealthSchema.parse({
        agentName,
        status: isHealthy ? 'healthy' : 'degraded',
        lastChecked: new Date().toISOString(),
      });
    } catch (err) {
      // Validate with Zod
      return AgentHealthSchema.parse({
        agentName,
        status: 'unhealthy' as const,
        lastChecked: new Date().toISOString(),
        message: err instanceof Error ? err.message : 'Health check failed',
      });
    }
  }

  async function fetchAllHealthStatuses() {
    const healthChecks = await Promise.allSettled(
      agents.value.map(agent => fetchAgentHealth(agent.name))
    );

    healthChecks.forEach((result, index) => {
      if (result.status === 'fulfilled') {
        agentHealthMap.value.set(agents.value[index].name, result.value);
      }
    });
  }

  function getAgentByName(name: string): AgentSummary | undefined {
    return agents.value.find(agent => agent.name === name);
  }

  function getAgentHealth(agentName: string): AgentHealth | undefined {
    return agentHealthMap.value.get(agentName);
  }

  function clearCache() {
    agents.value = [];
    agentHealthMap.value.clear();
    lastFetch.value = null;
    error.value = null;
  }

  return {
    // State
    agents,
    agentHealthMap,
    loading,
    error,
    lastFetch,

    // Computed
    activeAgents,
    isCacheValid,
    agentsCount,
    activeAgentsCount,

    // Actions
    fetchAgents,
    fetchAgentHealth,
    fetchAllHealthStatuses,
    getAgentByName,
    getAgentHealth,
    clearCache,
  };
});
