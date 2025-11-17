<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { AppCard, AppButton, AppBadge, AppTextarea, AppAlert, AppSpinner, AppEmptyState } from '@agents/design-system';
import { useAgentsStore } from '@/stores/agentsStore';
import { useRunsStore } from '@/stores/runsStore';
import { AgentRunRequestSchema, type AgentRunRequest } from '@agents/agent-domain';
import StatusBadge from '@/presentation/components/StatusBadge.vue';
import RunCard from '@/presentation/components/RunCard.vue';

const route = useRoute();
const router = useRouter();
const agentsStore = useAgentsStore();
const runsStore = useRunsStore();

const agentName = decodeURIComponent(route.params.name as string);
const agent = computed(() => agentsStore.getAgentByName(agentName));
const agentHealth = computed(() => agentsStore.getAgentHealth(agentName));
const agentRuns = computed(() => runsStore.getRunsByAgent(agentName));

const inputData = ref('');
const executing = ref(false);
const executionError = ref<string | null>(null);
const checkingHealth = ref(false);

const isValidJson = computed(() => {
  if (!inputData.value.trim()) return true;
  try {
    JSON.parse(inputData.value);
    return true;
  } catch {
    return false;
  }
});

onMounted(async () => {
  await Promise.all([
    agentsStore.fetchAgents(),
    runsStore.fetchRuns(),
  ]);
  
  if (agent.value) {
    checkHealth();
  }
});

const checkHealth = async () => {
  checkingHealth.value = true;
  await agentsStore.fetchAgentHealth(agentName);
  checkingHealth.value = false;
};

const executeAgent = async () => {
  if (!agent.value || !isValidJson.value) return;

  executing.value = true;
  executionError.value = null;

  try {
    const request: AgentRunRequest = AgentRunRequestSchema.parse({
      agentName: agent.value.name,
      input: inputData.value || '{}',
      metadata: {
        source: 'agents-console',
        timestamp: new Date().toISOString(),
      },
    });

    await runsStore.executeAgent(request);
    // Refresh runs and navigate to runs list
    await runsStore.fetchRuns(true);
    router.push('/runs');
  } catch (err) {
    executionError.value = err instanceof Error ? err.message : 'Failed to execute agent';
  } finally {
    executing.value = false;
  }
};

const goBack = () => router.push('/agents');
const viewRun = (id: string) => router.push(`/runs/${id}`);
</script>

<template>
  <div>
    <div class="mb-6">
      <AppButton variant="ghost" @click="goBack">← Back to Agents</AppButton>
    </div>

    <!-- Loading State -->
    <div v-if="!agent" class="flex items-center justify-center py-12 gap-3">
      <AppSpinner size="lg" />
      <span class="text-[--color-text-secondary]">Loading agent details...</span>
    </div>

    <!-- Agent Not Found -->
    <AppEmptyState
      v-else-if="!agent && !agentsStore.loading"
      icon="❌"
      title="Agent not found"
      :description="`Agent '${agentName}' does not exist.`"
    >
      <AppButton @click="goBack">Back to Agents</AppButton>
    </AppEmptyState>

    <!-- Agent Details -->
    <div v-else>
      <!-- Header -->
      <div class="mb-8">
        <div class="flex items-start justify-between mb-4">
          <div>
            <h1 class="text-4xl font-bold text-[--color-text-primary] mb-2">{{ agent.name }}</h1>
            <p class="text-[--color-text-secondary]">{{ agent.description }}</p>
          </div>
          <div class="flex items-center gap-3">
            <StatusBadge :status="agent.status" type="agent" />
            <AppButton size="sm" variant="secondary" :loading="checkingHealth" @click="checkHealth">
              Check Health
            </AppButton>
          </div>
        </div>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Left Column: Details -->
        <div class="lg:col-span-2 space-y-6">
          <!-- Agent Information -->
          <AppCard>
            <h2 class="text-xl font-semibold text-[--color-text-primary] mb-4">Information</h2>
            <div class="space-y-3">
              <div>
                <div class="text-sm text-[--color-text-muted] mb-1">Display Name</div>
                <div class="text-[--color-text-secondary]">{{ agent.displayName }}</div>
              </div>
              <div>
                <div class="text-sm text-[--color-text-muted] mb-1">Category</div>
                <AppBadge variant="info">{{ agent.category }}</AppBadge>
              </div>
              <div>
                <div class="text-sm text-[--color-text-muted] mb-1">Enabled</div>
                <div class="text-[--color-text-secondary]">{{ agent.isEnabled ? 'Yes' : 'No' }}</div>
              </div>
            </div>
          </AppCard>

          <!-- Execute Agent -->
          <AppCard>
            <h2 class="text-xl font-semibold text-[--color-text-primary] mb-4">Execute Agent</h2>
            
            <div class="mb-4">
              <label class="block text-sm font-medium text-[--color-text-secondary] mb-2">
                Input Data (JSON)
              </label>
              <AppTextarea
                v-model="inputData"
                placeholder='{\n  "key": "value"\n}'
                :rows="8"
              />
              <div v-if="!isValidJson" class="mt-2 text-sm text-[--color-danger-600]">
                Invalid JSON format
              </div>
            </div>

            <AppAlert v-if="executionError" variant="danger" class="mb-4">
              {{ executionError }}
            </AppAlert>

            <AppButton
              @click="executeAgent"
              :disabled="!isValidJson || agent.status !== 'enabled'"
              :loading="executing"
              size="lg"
              class="w-full"
            >
              Execute Agent
            </AppButton>

            <div v-if="agent.status !== 'enabled'" class="mt-3 text-sm text-[--color-text-muted] text-center">
              Agent must be enabled to execute
            </div>
          </AppCard>

          <!-- Recent Runs -->
          <div>
            <h2 class="text-xl font-semibold text-[--color-text-primary] mb-4">Recent Runs</h2>
            
            <AppEmptyState
              v-if="agentRuns.length === 0"
              icon="▶️"
              title="No runs yet"
              description="Execute this agent to see run history here."
            />

            <div v-else class="space-y-4">
              <RunCard
                v-for="run in agentRuns.slice(0, 5)"
                :key="run.id"
                :run="run"
                @view="viewRun"
              />
            </div>
          </div>
        </div>

        <!-- Right Column: Health -->
        <div>
          <AppCard>
            <h2 class="text-xl font-semibold text-[--color-text-primary] mb-4">Health Status</h2>
            
            <div v-if="checkingHealth" class="flex items-center justify-center py-8">
              <AppSpinner />
            </div>

            <div v-else-if="agentHealth" class="space-y-4">
              <div>
                <div class="text-sm text-[--color-text-muted] mb-2">Status</div>
                <StatusBadge :status="agentHealth.status" type="health" />
              </div>

              <div>
                <div class="text-sm text-[--color-text-muted] mb-2">Last Checked</div>
                <div class="text-sm text-[--color-text-secondary]">
                  {{ new Date(agentHealth.lastChecked).toLocaleString() }}
                </div>
              </div>

              <div v-if="agentHealth.message">
                <div class="text-sm text-[--color-text-muted] mb-2">Message</div>
                <div class="text-sm text-[--color-text-secondary]">{{ agentHealth.message }}</div>
              </div>
            </div>

            <div v-else class="text-center py-8 text-[--color-text-muted]">
              Click "Check Health" to view status
            </div>
          </AppCard>
        </div>
      </div>
    </div>
  </div>
</template>
