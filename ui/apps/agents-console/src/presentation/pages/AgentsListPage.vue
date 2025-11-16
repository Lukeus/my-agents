<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { AppCard, AppButton, AppBadge, AppInput } from '@agents/design-system';
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
  <div class="max-w-7xl mx-auto">
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-text-primary mb-2">Agents</h1>
      <p class="text-text-secondary">Manage and monitor all available agents</p>
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
    <div v-if="error" class="mb-8 p-4 bg-danger-50 border border-danger-200 rounded-lg text-danger-700">
      {{ error }}
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-12">
      <div class="text-text-secondary">Loading agents...</div>
    </div>

    <!-- Agents Table -->
    <AppCard v-else>
      <div class="overflow-x-auto">
        <table class="w-full">
          <thead class="border-b border-surface-border">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-semibold text-text-primary">Agent</th>
              <th class="text-left py-3 px-4 text-sm font-semibold text-text-primary">Description</th>
              <th class="text-left py-3 px-4 text-sm font-semibold text-text-primary">Status</th>
              <th class="text-left py-3 px-4 text-sm font-semibold text-text-primary">Version</th>
              <th class="text-left py-3 px-4 text-sm font-semibold text-text-primary">Capabilities</th>
              <th class="text-right py-3 px-4 text-sm font-semibold text-text-primary">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="agent in filteredAgents"
              :key="agent.name"
              class="border-b border-surface-border hover:bg-surface-hover transition-colors cursor-pointer"
              @click="viewAgentDetail(agent.name)"
            >
              <td class="py-4 px-4">
                <div class="font-semibold text-text-primary">{{ agent.name }}</div>
              </td>
              <td class="py-4 px-4">
                <div class="text-text-secondary text-sm max-w-xs truncate">{{ agent.description }}</div>
              </td>
              <td class="py-4 px-4">
                <AppBadge :variant="getStatusVariant(agent.status)" show-dot>
                  {{ agent.status }}
                </AppBadge>
              </td>
              <td class="py-4 px-4">
                <span class="font-mono text-sm text-text-secondary">{{ agent.version }}</span>
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

        <div v-if="filteredAgents.length === 0 && !loading" class="text-center py-12">
          <p class="text-text-secondary mb-4">
            {{ searchQuery ? 'No agents match your search' : 'No agents available' }}
          </p>
          <AppButton v-if="!searchQuery" @click="fetchAgents">Retry</AppButton>
        </div>
      </div>
    </AppCard>
  </div>
</template>
