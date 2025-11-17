<script setup lang="ts">
import { onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { AppCard, AppButton, AppBadge, AppSpinner, AppEmptyState } from '@agents/design-system';
import { useRunsStore } from '@/stores/runsStore';
import StatusBadge from '@/presentation/components/StatusBadge.vue';

const route = useRoute();
const router = useRouter();
const runsStore = useRunsStore();

const runId = route.params.id as string;
const run = computed(() => runsStore.runs.find((r) => r.id === runId));

const formatDuration = (duration: string | undefined) => {
  if (!duration) return 'N/A';
  return duration; // Already formatted as ISO duration from TimeSpan
};

const formatDate = (date: string) => {
  return new Date(date).toLocaleString();
};

onMounted(() => {
  runsStore.fetchRuns();
});

const goBack = () => router.push('/runs');
const viewAgent = (name: string) => router.push(`/agents/${encodeURIComponent(name)}`);
</script>

<template>
  <div>
    <div class="mb-6">
      <AppButton variant="ghost" @click="goBack">← Back to Runs</AppButton>
    </div>

    <!-- Loading State -->
    <div v-if="!run && runsStore.loading" class="flex items-center justify-center py-12 gap-3">
      <AppSpinner size="lg" />
      <span class="text-[--color-text-secondary]">Loading run details...</span>
    </div>

    <!-- Run Not Found -->
    <AppEmptyState
      v-else-if="!run"
      icon="❌"
      title="Run not found"
      :description="`Run with ID '${runId}' does not exist.`"
    >
      <AppButton @click="goBack">Back to Runs</AppButton>
    </AppEmptyState>

    <!-- Run Details -->
    <div v-else>
      <!-- Header -->
      <div class="mb-8">
        <div class="flex items-start justify-between mb-4">
          <div>
            <h1 class="text-4xl font-bold text-[--color-text-primary] mb-2">Run Details</h1>
            <p class="text-[--color-text-muted] font-mono text-sm">{{ run.id }}</p>
          </div>
          <StatusBadge :status="run.status" type="run" />
        </div>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Left Column: Main Details -->
        <div class="lg:col-span-2 space-y-6">
          <!-- Run Information -->
          <AppCard>
            <h2 class="text-xl font-semibold text-[--color-text-primary] mb-4">Run Information</h2>
            <div class="space-y-3">
              <div class="flex items-center justify-between">
                <div class="text-sm text-[--color-text-muted]">Agent</div>
                <div class="flex items-center gap-2">
                  <span class="font-semibold text-[--color-text-primary]">{{ run.agentName }}</span>
                  <AppButton size="sm" variant="ghost" @click="viewAgent(run.agentName)">View</AppButton>
                </div>
              </div>
              <div class="flex items-center justify-between">
                <div class="text-sm text-[--color-text-muted]">Started</div>
                <div class="text-[--color-text-secondary]">{{ formatDate(run.startedAt) }}</div>
              </div>
              <div v-if="run.completedAt" class="flex items-center justify-between">
                <div class="text-sm text-[--color-text-muted]">Completed</div>
                <div class="text-[--color-text-secondary]">{{ formatDate(run.completedAt) }}</div>
              </div>
              <div v-if="run.duration" class="flex items-center justify-between">
                <div class="text-sm text-[--color-text-muted]">Duration</div>
                <div class="font-semibold text-[--color-text-primary]">{{ formatDuration(run.duration) }}</div>
              </div>
            </div>
          </AppCard>

          <!-- Input Data -->
          <AppCard>
            <h2 class="text-xl font-semibold text-[--color-text-primary] mb-4">Input Data</h2>
            <div class="bg-[--color-surface] rounded-lg p-4 overflow-x-auto">
              <pre class="text-sm text-[--color-text-secondary] font-mono">{{ run.input }}</pre>
            </div>
          </AppCard>

          <!-- Output/Result -->
          <AppCard v-if="run.status === 'succeeded' || run.status === 'failed'">
            <h2 class="text-xl font-semibold text-[--color-text-primary] mb-4">Result</h2>
            
            <div class="space-y-4">
              <div v-if="run.status === 'succeeded'" class="flex items-center gap-2 text-[--color-success-600]">
                <span class="text-2xl">✓</span>
                <span class="font-semibold">Execution Successful</span>
              </div>
              <div v-else class="flex items-center gap-2 text-[--color-danger-600]">
                <span class="text-2xl">✗</span>
                <span class="font-semibold">Execution Failed</span>
              </div>

              <div v-if="run.output" class="bg-[--color-surface] rounded-lg p-4 overflow-x-auto">
                <div class="text-sm text-[--color-text-muted] mb-2">Output</div>
                <pre class="text-sm text-[--color-text-secondary] font-mono">{{ run.output }}</pre>
              </div>

              <div v-if="run.errorMessage" class="bg-[--color-danger-50] dark:bg-[--color-danger-950] border border-[--color-danger-200] dark:border-[--color-danger-800] rounded-lg p-4">
                <div class="text-sm font-semibold text-[--color-danger-700] dark:text-[--color-danger-300] mb-2">Error</div>
                <pre class="text-sm text-[--color-danger-600] dark:text-[--color-danger-400] font-mono whitespace-pre-wrap">{{ run.errorMessage }}</pre>
              </div>
            </div>
          </AppCard>

          <!-- Pending/Running State -->
          <AppCard v-else-if="run.status === 'running' || run.status === 'pending'">
            <div class="flex items-center justify-center py-8 gap-3">
              <AppSpinner />
              <span class="text-[--color-text-secondary]">{{ run.status === 'running' ? 'Agent is executing...' : 'Run is pending...' }}</span>
            </div>
          </AppCard>
        </div>

        <!-- Right Column: Additional Info -->
        <div>
          <AppCard>
            <h2 class="text-xl font-semibold text-[--color-text-primary] mb-4">Additional Information</h2>
            
            <div class="space-y-3">
              <div>
                <div class="text-sm text-[--color-text-muted] mb-1">Initiated By</div>
                <div class="text-sm text-[--color-text-secondary]">{{ run.initiatedBy }}</div>
              </div>
              <div>
                <div class="text-sm text-[--color-text-muted] mb-1">Status</div>
                <StatusBadge :status="run.status" type="run" />
              </div>
            </div>
          </AppCard>
        </div>
      </div>
    </div>
  </div>
</template>
