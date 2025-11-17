<script setup lang="ts">
import { onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { AppCard, AppButton, AppBadge, AppAlert, AppSpinner, AppEmptyState } from '@agents/design-system';
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
  <div>
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-[--color-text-primary] mb-2">Agents Dashboard</h1>
      <p class="text-[--color-text-secondary]">Overview of all available agents and their status</p>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
      <AppCard>
        <div class="text-center">
          <div class="text-5xl font-bold text-[--color-primary-600] mb-2">{{ stats.total }}</div>
          <div class="text-[--color-text-secondary]">Total Agents</div>
        </div>
      </AppCard>

      <AppCard>
        <div class="text-center">
          <div class="text-5xl font-bold text-[--color-success-600] mb-2">{{ stats.active }}</div>
          <div class="text-[--color-text-secondary]">Active</div>
        </div>
      </AppCard>

      <AppCard>
        <div class="text-center">
          <div class="text-5xl font-bold text-[--color-danger-600] mb-2">{{ stats.unhealthy }}</div>
          <div class="text-[--color-text-secondary]">Unhealthy</div>
        </div>
      </AppCard>
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

    <!-- Agents Grid -->
    <div v-else>
      <div class="flex items-center justify-between mb-6">
        <h2 class="text-2xl font-semibold text-[--color-text-primary]">Available Agents</h2>
        <AppButton @click="navigateToAgents" variant="secondary">View All</AppButton>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <AppCard v-for="agent in agents" :key="agent.name" hoverable>
          <div class="flex items-start justify-between mb-3">
            <h3 class="text-xl font-semibold text-[--color-text-primary]">{{ agent.name }}</h3>
            <AppBadge :variant="getStatusVariant(agent.status)" show-dot>
              {{ agent.status }}
            </AppBadge>
          </div>

          <p class="text-[--color-text-secondary] mb-4">{{ agent.description }}</p>

          <div class="mb-4">
            <div class="text-sm text-[--color-text-muted] mb-2">Capabilities:</div>
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

          <div class="text-sm text-[--color-text-muted]">
            Version: <span class="text-[--color-text-secondary] font-mono">{{ agent.version }}</span>
          </div>
        </AppCard>
      </div>

      <AppEmptyState
        v-if="agents.length === 0 && !loading"
        icon="🤖"
        title="No agents available"
        description="There are no agents configured in the system."
      >
        <AppButton @click="fetchAgents">Retry</AppButton>
      </AppEmptyState>
    </div>
  </div>
</template>
