<script setup lang="ts">
import { onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { AppCard, AppButton, AppBadge } from '@agents/design-system';
import { useListAgents } from '@/application/usecases/useListAgents';

const router = useRouter();
const { agents, loading, error, fetchAgents } = useListAgents();

const stats = computed(() => ({
  total: agents.value.length,
  active: agents.value.filter((a) => a.status === 'active').length,
  unhealthy: agents.value.filter((a) => a.status === 'unhealthy').length,
}));

onMounted(() => {
  fetchAgents();
});

const navigateToAgents = () => {
  router.push('/agents');
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
      <h1 class="text-4xl font-bold text-text-primary mb-2">Agents Dashboard</h1>
      <p class="text-text-secondary">Overview of all available agents and their status</p>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
      <AppCard>
        <div class="text-center">
          <div class="text-5xl font-bold text-primary-600 mb-2">{{ stats.total }}</div>
          <div class="text-text-secondary">Total Agents</div>
        </div>
      </AppCard>

      <AppCard>
        <div class="text-center">
          <div class="text-5xl font-bold text-success-600 mb-2">{{ stats.active }}</div>
          <div class="text-text-secondary">Active</div>
        </div>
      </AppCard>

      <AppCard>
        <div class="text-center">
          <div class="text-5xl font-bold text-danger-600 mb-2">{{ stats.unhealthy }}</div>
          <div class="text-text-secondary">Unhealthy</div>
        </div>
      </AppCard>
    </div>

    <!-- Error State -->
    <div v-if="error" class="mb-8 p-4 bg-danger-50 border border-danger-200 rounded-lg text-danger-700">
      {{ error }}
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-12">
      <div class="text-text-secondary">Loading agents...</div>
    </div>

    <!-- Agents Grid -->
    <div v-else>
      <div class="flex items-center justify-between mb-6">
        <h2 class="text-2xl font-semibold text-text-primary">Available Agents</h2>
        <AppButton @click="navigateToAgents" variant="secondary">View All</AppButton>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <AppCard v-for="agent in agents" :key="agent.name" hoverable>
          <div class="flex items-start justify-between mb-3">
            <h3 class="text-xl font-semibold text-text-primary">{{ agent.name }}</h3>
            <AppBadge :variant="getStatusVariant(agent.status)" show-dot>
              {{ agent.status }}
            </AppBadge>
          </div>

          <p class="text-text-secondary mb-4">{{ agent.description }}</p>

          <div class="mb-4">
            <div class="text-sm text-text-muted mb-2">Capabilities:</div>
            <div class="flex flex-wrap gap-2">
              <AppBadge
                v-for="capability in agent.capabilities"
                :key="capability"
                variant="info"
                size="sm"
              >
                {{ capability }}
              </AppBadge>
            </div>
          </div>

          <div class="text-sm text-text-muted">
            Version: <span class="text-text-secondary font-mono">{{ agent.version }}</span>
          </div>
        </AppCard>
      </div>

      <div v-if="agents.length === 0 && !loading" class="text-center py-12">
        <p class="text-text-secondary mb-4">No agents available</p>
        <AppButton @click="fetchAgents">Retry</AppButton>
      </div>
    </div>
  </div>
</template>
