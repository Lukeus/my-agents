<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { AppCard, AppButton, AppBadge, AppInput, AppAlert, AppSpinner, AppEmptyState, AppSelect } from '@agents/design-system';
import { useAgentsStore } from '@/stores/agentsStore';
import AgentCard from '@/presentation/components/AgentCard.vue';
import StatusBadge from '@/presentation/components/StatusBadge.vue';
import type { AgentStatus } from '@agents/agent-domain';

const router = useRouter();
const agentsStore = useAgentsStore();
const searchQuery = ref('');
const statusFilter = ref<AgentStatus | 'all'>('all');
const viewMode = ref<'grid' | 'table'>('grid');

const filteredAgents = computed(() => {
  let result = agentsStore.agents;

  // Filter by search query
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase();
    result = result.filter(
      (agent) =>
        agent.name.toLowerCase().includes(query) ||
        agent.displayName.toLowerCase().includes(query) ||
        agent.description.toLowerCase().includes(query) ||
        agent.category.toLowerCase().includes(query)
    );
  }

  // Filter by status
  if (statusFilter.value !== 'all') {
    result = result.filter((agent) => agent.status === statusFilter.value);
  }

  return result;
});

const statusOptions = [
  { value: 'all', label: 'All Statuses' },
  { value: 'enabled', label: 'Enabled' },
  { value: 'disabled', label: 'Disabled' },
  { value: 'maintenance', label: 'Maintenance' },
];

onMounted(() => {
  agentsStore.fetchAgents();
});

const viewAgentDetail = (agentName: string) => {
  router.push(`/agents/${encodeURIComponent(agentName)}`);
};
</script>

<template>
  <div>
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-[--color-text-primary] mb-2">Agents</h1>
      <p class="text-[--color-text-secondary]">Manage and monitor all available agents</p>
    </div>

    <!-- Filters Bar -->
    <div class="flex flex-col md:flex-row gap-4 mb-6">
      <div class="flex-1">
        <AppInput
          v-model="searchQuery"
          placeholder="Search agents by name, description, or category..."
        />
      </div>
      <div class="w-full md:w-48">
        <AppSelect v-model="statusFilter" :options="statusOptions" />
      </div>
      <div class="flex gap-2">
        <AppButton
          :variant="viewMode === 'grid' ? 'primary' : 'ghost'"
          size="sm"
          @click="viewMode = 'grid'"
        >
          Grid
        </AppButton>
        <AppButton
          :variant="viewMode === 'table' ? 'primary' : 'ghost'"
          size="sm"
          @click="viewMode = 'table'"
        >
          Table
        </AppButton>
      </div>
    </div>

    <!-- Results Count -->
    <div class="mb-4 text-sm text-[--color-text-secondary]">
      Showing {{ filteredAgents.length }} of {{ agentsStore.agents.length }} agents
    </div>

    <!-- Error State -->
    <AppAlert v-if="agentsStore.error" variant="danger" class="mb-8">
      {{ agentsStore.error }}
    </AppAlert>

    <!-- Loading State -->
    <div v-if="agentsStore.loading && agentsStore.agents.length === 0" class="flex items-center justify-center py-12 gap-3">
      <AppSpinner size="lg" />
      <span class="text-[--color-text-secondary]">Loading agents...</span>
    </div>

    <!-- Empty State -->
    <AppEmptyState
      v-else-if="filteredAgents.length === 0"
      icon="🔍"
      :title="searchQuery || statusFilter !== 'all' ? 'No agents match your filters' : 'No agents available'"
      :description="searchQuery || statusFilter !== 'all' ? 'Try adjusting your search or filter criteria.' : 'There are no agents configured in the system.'"
    >
      <AppButton v-if="!searchQuery && statusFilter === 'all'" @click="agentsStore.fetchAgents">Retry</AppButton>
    </AppEmptyState>

    <!-- Grid View -->
    <div v-else-if="viewMode === 'grid'" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <AgentCard
        v-for="agent in filteredAgents"
        :key="agent.name"
        :agent="agent"
        @view="viewAgentDetail"
      />
    </div>

    <!-- Table View -->
    <AppCard v-else>
      <div class="overflow-x-auto">
        <table class="w-full">
          <thead class="border-b border-[--color-border]">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-semibold text-[--color-text-primary]">Agent</th>
              <th class="text-left py-3 px-4 text-sm font-semibold text-[--color-text-primary]">Description</th>
              <th class="text-left py-3 px-4 text-sm font-semibold text-[--color-text-primary]">Category</th>
              <th class="text-left py-3 px-4 text-sm font-semibold text-[--color-text-primary]">Status</th>
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
                <div class="font-semibold text-[--color-text-primary]">{{ agent.displayName }}</div>
              </td>
              <td class="py-4 px-4">
                <div class="text-[--color-text-secondary] text-sm max-w-xs truncate">{{ agent.description }}</div>
              </td>
              <td class="py-4 px-4">
                <AppBadge variant="info" size="sm">{{ agent.category }}</AppBadge>
              </td>
              <td class="py-4 px-4">
                <StatusBadge :status="agent.status" type="agent" />
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
      </div>
    </AppCard>
  </div>
</template>
