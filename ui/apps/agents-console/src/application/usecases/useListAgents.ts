import { ref, type Ref } from 'vue';
import { NotificationClient } from '@agents/api-client';
import type { AgentSummary } from '@agents/agent-domain';

interface UseListAgentsReturn {
  agents: Ref<AgentSummary[]>;
  loading: Ref<boolean>;
  error: Ref<string | null>;
  fetchAgents: () => Promise<void>;
}

const notificationClient = new NotificationClient(import.meta.env.VITE_NOTIFICATION_API_URL);

export function useListAgents(): UseListAgentsReturn {
  const agents = ref<AgentSummary[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);

  const fetchAgents = async () => {
    loading.value = true;
    error.value = null;

    try {
      // For now, we'll create a mock list of agents based on available services
      // In a real implementation, this would call an agents registry API
      const mockAgents: AgentSummary[] = [
        {
          name: 'Notification Agent',
          description: 'Handles multi-channel notifications (email, SMS, Teams, Slack)',
          version: '1.0.0',
          status: 'active',
          capabilities: ['email', 'sms', 'teams', 'slack'],
          healthEndpoint: `${import.meta.env.VITE_NOTIFICATION_API_URL}/health`,
        },
        {
          name: 'DevOps Agent',
          description: 'Manages issues, workflows, and sprint analytics',
          version: '1.0.0',
          status: 'active',
          capabilities: ['issue-management', 'workflow-automation', 'sprint-analytics'],
          healthEndpoint: `${import.meta.env.VITE_DEVOPS_API_URL}/health`,
        },
        {
          name: 'Test Planning Agent',
          description: 'Generates test specifications and coverage analysis',
          version: '1.0.0',
          status: 'active',
          capabilities: ['test-generation', 'coverage-analysis', 'bdd-scenarios'],
          healthEndpoint: `${import.meta.env.VITE_TESTPLANNING_API_URL}/health`,
        },
        {
          name: 'Implementation Agent',
          description: 'Assists with code implementation and refactoring',
          version: '1.0.0',
          status: 'active',
          capabilities: ['code-generation', 'refactoring', 'best-practices'],
          healthEndpoint: `${import.meta.env.VITE_IMPLEMENTATION_API_URL}/health`,
        },
        {
          name: 'Service Desk Agent',
          description: 'Handles support tickets and service requests',
          version: '1.0.0',
          status: 'active',
          capabilities: ['ticket-management', 'auto-response', 'knowledge-base'],
          healthEndpoint: `${import.meta.env.VITE_SERVICEDESK_API_URL}/health`,
        },
        {
          name: 'BIM Classification Agent',
          description: 'Classifies and organizes BIM data',
          version: '1.0.0',
          status: 'active',
          capabilities: ['classification', 'metadata-extraction', 'data-organization'],
          healthEndpoint: `${import.meta.env.VITE_BIMCLASSIFICATION_API_URL}/health`,
        },
      ];

      // Check health status for each agent (in parallel)
      const healthChecks = await Promise.allSettled(
        mockAgents.map(async (agent) => {
          try {
            const response = await fetch(agent.healthEndpoint);
            const isHealthy = response.ok;
            return {
              ...agent,
              status: isHealthy ? ('active' as const) : ('unhealthy' as const),
            };
          } catch {
            return {
              ...agent,
              status: 'unhealthy' as const,
            };
          }
        })
      );

      agents.value = healthChecks.map((result) =>
        result.status === 'fulfilled' ? result.value : mockAgents[0]
      );
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to fetch agents';
      console.error('Error fetching agents:', err);
    } finally {
      loading.value = false;
    }
  };

  return {
    agents,
    loading,
    error,
    fetchAgents,
  };
}
