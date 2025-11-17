<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { AppInput, AppSelect, AppSpinner, AppEmptyState, AppButton } from '@agents/design-system';
import { useRunsStore } from '@/stores/runsStore';
import { useAgentsStore } from '@/stores/agentsStore';
import RunCard from '@/presentation/components/RunCard.vue';
import type { AgentRunStatus } from '@agents/agent-domain';

const router = useRouter();
const runsStore = useRunsStore();
const agentsStore = useAgentsStore();

const searchQuery = ref('');
const statusFilter = ref<AgentRunStatus | 'all'>('all');
const agentFilter = ref<string>('all');

const filteredRuns = computed(() => {
  let result = runsStore.runs;

  // Filter by search query (run ID or agent name)
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase();
    result = result.filter(
      (run) =>
        run.id.toLowerCase().includes(query) ||
        run.agentName.toLowerCase().includes(query)
    );
  }

  // Filter by status
  if (statusFilter.value !== 'all') {
    result = result.filter((run) => run.status === statusFilter.value);
  }

  // Filter by agent
  if (agentFilter.value !== 'all') {
    result = result.filter((run) => run.agentName === agentFilter.value);
  }

  return result;
});

const statusOptions = [
  { value: 'all', label: 'All Statuses' },
  { value: 'pending', label: 'Pending' },
  { value: 'running', label: 'Running' },
  { value: 'succeeded', label: 'Succeeded' },
  { value: 'failed', label: 'Failed' },
];

const agentOptions = computed(() => [
  { value: 'all', label: 'All Agents' },
  ...agentsStore.agents.map((agent) => ({
    value: agent.name,
    label: agent.name,
  })),
]);

onMounted(async () => {
  await Promise.all([
    runsStore.fetchRuns(),
    agentsStore.fetchAgents(),
  ]);
});

const viewRun = (id: string) => router.push(`/runs/${id}`);
</script>

<template>
  <div>
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-[--color-text-primary] mb-2">Agent Runs</h1>
      <p class="text-[--color-text-secondary]">View execution history and results</p>
    </div>

    <!-- Filters Bar -->
    <div class="flex flex-col md:flex-row gap-4 mb-6">
      <div class="flex-1">
        <AppInput
          v-model="searchQuery"
          placeholder="Search by run ID or agent name..."
        />
      </div>
      <div class="w-full md:w-48">
        <AppSelect v-model="agentFilter" :options="agentOptions" />
      </div>
      <div class="w-full md:w-48">
        <AppSelect v-model="statusFilter" :options="statusOptions" />
      </div>
    </div>

    <!-- Results Count -->
    <div class="mb-4 text-sm text-[--color-text-secondary]">
      Showing {{ filteredRuns.length }} of {{ runsStore.runs.length }} runs
    </div>

    <!-- Loading State -->
    <div v-if="runsStore.loading && runsStore.runs.length === 0" class="flex items-center justify-center py-12 gap-3">
      <AppSpinner size="lg" />
      <span class="text-[--color-text-secondary]">Loading runs...</span>
    </div>

    <!-- Empty State -->
    <AppEmptyState
      v-else-if="filteredRuns.length === 0"
      icon="▶️"
      :title="searchQuery || statusFilter !== 'all' || agentFilter !== 'all' ? 'No runs match your filters' : 'No runs yet'"
      :description="searchQuery || statusFilter !== 'all' || agentFilter !== 'all' ? 'Try adjusting your search or filter criteria.' : 'Execute an agent to see run history here.'"
    >
      <AppButton v-if="!searchQuery && statusFilter === 'all' && agentFilter === 'all'" @click="router.push('/agents')">
        Execute Agent
      </AppButton>
    </AppEmptyState>

    <!-- Runs List -->
    <div v-else class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <RunCard
        v-for="run in filteredRuns"
        :key="run.id"
        :run="run"
        @view="viewRun"
      />
    </div>
  </div>
</template>
