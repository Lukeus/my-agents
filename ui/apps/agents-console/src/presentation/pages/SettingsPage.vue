<script setup lang="ts">
import { ref, computed } from 'vue';
import { AppCard, AppButton, AppAlert } from '@agents/design-system';
import { useAgentsStore } from '@/stores/agentsStore';
import { useRunsStore } from '@/stores/runsStore';

const agentsStore = useAgentsStore();
const runsStore = useRunsStore();

const clearingCache = ref(false);
const successMessage = ref<string | null>(null);

const environment = import.meta.env.MODE;

const apiEndpoints = computed(() => [
  { name: 'Notification Agent', url: import.meta.env.VITE_NOTIFICATION_API_URL },
  { name: 'DevOps Agent', url: import.meta.env.VITE_DEVOPS_API_URL },
  { name: 'Test Planning Agent', url: import.meta.env.VITE_TESTPLANNING_API_URL },
  { name: 'Implementation Agent', url: import.meta.env.VITE_IMPLEMENTATION_API_URL },
  { name: 'Service Desk Agent', url: import.meta.env.VITE_SERVICEDESK_API_URL },
  { name: 'BIM Classification Agent', url: import.meta.env.VITE_BIMCLASSIFICATION_API_URL },
]);

const clearAllCaches = async () => {
  clearingCache.value = true;
  successMessage.value = null;

  try {
    agentsStore.clearCache();
    runsStore.clearCache();
    
    successMessage.value = 'All caches cleared successfully';
    setTimeout(() => {
      successMessage.value = null;
    }, 3000);
  } finally {
    clearingCache.value = false;
  }
};

const refreshData = async () => {
  await Promise.all([
    agentsStore.fetchAgents(),
    runsStore.fetchRuns(),
  ]);
};
</script>

<template>
  <div>
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-[--color-text-primary] mb-2">Settings</h1>
      <p class="text-[--color-text-secondary]">Configure agents and system preferences</p>
    </div>

    <div class="space-y-6">
      <!-- Success Message -->
      <AppAlert v-if="successMessage" variant="success">
        {{ successMessage }}
      </AppAlert>

      <!-- API Endpoints -->
      <AppCard>
        <h2 class="text-xl font-semibold text-[--color-text-primary] mb-4">API Endpoints</h2>
        <p class="text-sm text-[--color-text-secondary] mb-4">
          Configured agent API endpoints from environment variables.
        </p>
        
        <div class="space-y-3">
          <div v-for="endpoint in apiEndpoints" :key="endpoint.name" class="flex items-center justify-between py-2 border-b border-[--color-border] last:border-0">
            <div class="text-sm font-medium text-[--color-text-primary]">{{ endpoint.name }}</div>
            <div class="text-sm text-[--color-text-muted] font-mono truncate max-w-md">{{ endpoint.url }}</div>
          </div>
        </div>
      </AppCard>

      <!-- Cache Management -->
      <AppCard>
        <h2 class="text-xl font-semibold text-[--color-text-primary] mb-4">Cache Management</h2>
        <p class="text-sm text-[--color-text-secondary] mb-4">
          Clear cached data to force fresh API calls. Agents cache expires after 5 minutes, runs cache after 2 minutes.
        </p>
        
        <div class="flex gap-3">
          <AppButton
            @click="clearAllCaches"
            :loading="clearingCache"
            variant="secondary"
          >
            Clear All Caches
          </AppButton>
          <AppButton
            @click="refreshData"
            :loading="agentsStore.loading || runsStore.loading"
            variant="ghost"
          >
            Refresh Data
          </AppButton>
        </div>
      </AppCard>

      <!-- System Information -->
      <AppCard>
        <h2 class="text-xl font-semibold text-[--color-text-primary] mb-4">System Information</h2>
        
        <div class="space-y-3">
          <div class="flex items-center justify-between py-2 border-b border-[--color-border]">
            <div class="text-sm text-[--color-text-muted]">App Version</div>
            <div class="text-sm font-mono text-[--color-text-secondary]">1.0.0</div>
          </div>
          <div class="flex items-center justify-between py-2 border-b border-[--color-border]">
            <div class="text-sm text-[--color-text-muted]">Environment</div>
            <div class="text-sm font-mono text-[--color-text-secondary]">{{ environment }}</div>
          </div>
          <div class="flex items-center justify-between py-2 border-b border-[--color-border]">
            <div class="text-sm text-[--color-text-muted]">Total Agents</div>
            <div class="text-sm font-semibold text-[--color-text-primary]">{{ agentsStore.agents.length }}</div>
          </div>
          <div class="flex items-center justify-between py-2">
            <div class="text-sm text-[--color-text-muted]">Total Runs</div>
            <div class="text-sm font-semibold text-[--color-text-primary]">{{ runsStore.runs.length }}</div>
          </div>
        </div>
      </AppCard>

      <!-- About -->
      <AppCard>
        <h2 class="text-xl font-semibold text-[--color-text-primary] mb-4">About</h2>
        <p class="text-sm text-[--color-text-secondary] leading-relaxed">
          Agents Console is a management interface for monitoring and executing multi-agent systems.
          Built with Vue 3, TypeScript, Pinia, and the Agents Design System.
        </p>
      </AppCard>
    </div>
  </div>
</template>
