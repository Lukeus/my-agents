<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { AppCard, AppButton, AppBadge, AppInput, AppAlert, AppSpinner, AppEmptyState } from '@agents/design-system';
import { useListAgents } from '@/application/usecases/useListAgents';

const router = useRouter();
const { agents, loading, error, fetchAgents } = useListAgents();
const searchQuery = ref('');

const filteredAgents = ref(agents);

const filterAgents = () => {
  if (!searchQuery.value) {
    filteredAgents.value = agents;
    return;
  }

  const query = searchQuery.value.toLowerCase();
  filteredAgents.value = ref(
    agents.value.filter(
      (agent) =>
        agent.name.toLowerCase().includes(query) ||
        agent.description.toLowerCase().includes(query) ||
        agent.capabilities.some((cap) => cap.toLowerCase().includes(query))
    )
  );
};

onMounted(() => {
  fetchAgents();
});

const viewAgentDetail = (agentName: string) => {
  router.push(`/agents/${encodeURIComponent(agentName)}`);
};

const getStatusVariant = (status: string) => {
  switch (status) {
    case 'active':
      return 'success';
    case 'unhealthy':
      return 'danger';
    default:
      return 'secondary';
  }
};
</script>

<template>
  <div>
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-[--color-text-primary] mb-2">Agents</h1>
      <p class="text-[--color-text-secondary]">Manage and monitor all available agents</p>
    </div>

    <!-- Search Bar -->
    <div class="mb-6">
      <AppInput
        v-model="searchQuery"
        placeholder="Search agents by name, description, or capabilities..."
        @input="filterAgents"
      />
    </div>

    <!-- Error State -->
    <AppAlert v-if="error" variant="danger" class="mb-8">
      {{ error }}
    </AppAlert>

    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center py-12 gap-3">
      <AppSpinner />
      <span class="text-[--color-text-secondary]">Loading agents...</span>
    </div>

    <!-- Agents Table -->
    <AppCard v-else>
      <div class="overflow-x-auto">
        <table class="w-full">
          <thead class="border-b border-[--color-border]">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-semibold text-[--color-text-primary]">Agent</th>
              <th class="text-left py-3 px-4 text-sm font-semibold text-[--color-text-primary]">Description</th>
              <th class="text-left py-3 px-4 text-sm font-semibold text-[--color-text-primary]">Status</th>
              <th class="text-left py-3 px-4 text-sm font-semibold text-[--color-text-primary]">Version</th>
              <th class="text-left py-3 px-4 text-sm font-semibold text-[--color-text-primary]">Capabilities</th>
              <th class="text-right py-3 px-4 text-sm font-semibold text-[--color-text-primary]">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="agent in filteredAgents"
              :key="agent.name"
              class="border-b border-[--color-border] hover:bg-[--color-surface-hover] transition-colors cursor-pointer"
              @click="viewAgentDetail(agent.name)"
            >
              <td class="py-4 px-4">
                <div class="font-semibold text-[--color-text-primary]">{{ agent.name }}</div>
              </td>
              <td class="py-4 px-4">
                <div class="text-[--color-text-secondary] text-sm max-w-xs truncate">{{ agent.description }}</div>
              </td>
              <td class="py-4 px-4">
                <AppBadge :variant="getStatusVariant(agent.status)" show-dot>
                  {{ agent.status }}
                </AppBadge>
              </td>
              <td class="py-4 px-4">
                <span class="font-mono text-sm text-[--color-text-secondary]">{{ agent.version }}</span>
              </td>
              <td class="py-4 px-4">
                <div class="flex flex-wrap gap-1">
                  <AppBadge
                    v-for="capability in agent.capabilities.slice(0, 2)"
                    :key="capability"
                    variant="info"
                    size="sm"
                  >
                    {{ capability }}
                  </AppBadge>
                  <AppBadge v-if="agent.capabilities.length > 2" variant="secondary" size="sm">
                    +{{ agent.capabilities.length - 2 }}
                  </AppBadge>
                </div>
              </td>
              <td class="py-4 px-4 text-right">
                <AppButton
                  size="sm"
                  variant="secondary"
                  @click.stop="viewAgentDetail(agent.name)"
                >
                  View Details
                </AppButton>
              </td>
            </tr>
          </tbody>
        </table>

        <AppEmptyState
          v-if="filteredAgents.length === 0 && !loading"
          icon="🔍"
          :title="searchQuery ? 'No agents match your search' : 'No agents available'"
          :description="searchQuery ? 'Try adjusting your search criteria.' : 'There are no agents configured in the system.'"
        >
          <AppButton v-if="!searchQuery" @click="fetchAgents">Retry</AppButton>
        </AppEmptyState>
      </div>
    </AppCard>
  </div>
</template>
