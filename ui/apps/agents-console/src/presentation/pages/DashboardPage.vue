<script setup lang="ts">
import { onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { AppButton, AppAlert, AppSpinner, AppEmptyState } from '@agents/design-system';
import { useAgentsStore } from '@/stores/agentsStore';
import { useRunsStore } from '@/stores/runsStore';
import StatsCard from '@/presentation/components/StatsCard.vue';
import AgentCard from '@/presentation/components/AgentCard.vue';
import RunCard from '@/presentation/components/RunCard.vue';

const router = useRouter();
const agentsStore = useAgentsStore();
const runsStore = useRunsStore();

const loading = computed(() => agentsStore.loading || runsStore.loading);
const error = computed(() => agentsStore.error || runsStore.error);

const stats = computed(() => {
  const agents = agentsStore.agents;
  const runs = runsStore.runs;
  
  // Calculate average duration from ISO duration strings
  const completedRuns = runs.filter(r => r.duration);
  const avgDurationStr = completedRuns.length > 0
    ? completedRuns[0]?.duration || '00:00:00'
    : '00:00:00';
  
  return {
    totalAgents: agents.length,
    enabledAgents: agents.filter(a => a.status === 'enabled').length,
    totalRuns: runs.length,
    successfulRuns: runs.filter(r => r.status === 'succeeded').length,
    failedRuns: runs.filter(r => r.status === 'failed').length,
    avgDuration: avgDurationStr
  };
});

const recentRuns = computed(() => runsStore.runs.slice(0, 5));

onMounted(async () => {
  await Promise.all([
    agentsStore.fetchAgents(),
    runsStore.fetchRuns()
  ]);
});

const navigateToAgents = () => router.push('/agents');
const navigateToRuns = () => router.push('/runs');
const viewAgent = (name: string) => router.push(`/agents/${name}`);
const viewRun = (id: string) => router.push(`/runs/${id}`);
</script>

<template>
  <div>
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-[--color-text-primary] mb-2">Agents Dashboard</h1>
      <p class="text-[--color-text-secondary]">Monitor and manage your multi-agent system</p>
    </div>

    <!-- Error State -->
    <AppAlert v-if="error" variant="danger" class="mb-8">
      {{ error }}
    </AppAlert>

    <!-- Loading State -->
    <div v-if="loading && agentsStore.agents.length === 0" class="flex items-center justify-center py-12 gap-3">
      <AppSpinner size="lg" />
      <span class="text-[--color-text-secondary]">Loading dashboard...</span>
    </div>

    <!-- Dashboard Content -->
    <div v-else>
      <!-- Stats Overview -->
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        <StatsCard
          title="Total Agents"
          :value="stats.totalAgents"
          :subtitle="`${stats.enabledAgents} enabled`"
          icon="🤖"
        />
        <StatsCard
          title="Total Runs"
          :value="stats.totalRuns"
          icon="▶️"
        />
        <StatsCard
          title="Success Rate"
          :value="stats.totalRuns > 0 ? Math.round((stats.successfulRuns / stats.totalRuns) * 100) : 0"
          :subtitle="`${stats.failedRuns} failed`"
          icon="✓"
        />
        <StatsCard
          title="Avg Duration"
          :value="stats.avgDuration"
          icon="⏱️"
        />
      </div>

      <!-- Agents Section -->
      <div class="mb-8">
        <div class="flex items-center justify-between mb-6">
          <h2 class="text-2xl font-semibold text-[--color-text-primary]">Available Agents</h2>
          <AppButton @click="navigateToAgents" variant="secondary" size="sm">View All</AppButton>
        </div>

        <div v-if="agentsStore.agents.length === 0" class="py-8">
          <AppEmptyState
            icon="🤖"
            title="No agents available"
            description="There are no agents configured in the system."
          >
            <AppButton @click="agentsStore.fetchAgents">Retry</AppButton>
          </AppEmptyState>
        </div>

        <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <AgentCard
            v-for="agent in agentsStore.agents"
            :key="agent.name"
            :agent="agent"
            @view="viewAgent"
          />
        </div>
      </div>

      <!-- Recent Runs Section -->
      <div>
        <div class="flex items-center justify-between mb-6">
          <h2 class="text-2xl font-semibold text-[--color-text-primary]">Recent Runs</h2>
          <AppButton @click="navigateToRuns" variant="secondary" size="sm">View All</AppButton>
        </div>

        <div v-if="recentRuns.length === 0" class="py-8">
          <AppEmptyState
            icon="▶️"
            title="No runs yet"
            description="Execute an agent to see run history here."
          >
            <AppButton @click="navigateToAgents">Execute Agent</AppButton>
          </AppEmptyState>
        </div>

        <div v-else class="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <RunCard
            v-for="run in recentRuns"
            :key="run.id"
            :run="run"
            @view="viewRun"
          />
        </div>
      </div>
    </div>
  </div>
</template>
