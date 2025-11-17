<script setup lang="ts">
import { computed } from 'vue';
import { AppCard } from '@agents/design-system';
import StatusBadge from './StatusBadge.vue';
import type { AgentRun } from '@agents/agent-domain';

interface Props {
  run: AgentRun;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  (e: 'view', id: string): void;
}>();

const categoryIcon: Record<string, string> = {
  notification: '📧',
  devops: '⚙️',
  testplanning: '🧪',
  implementation: '💻',
  servicedesk: '🎫',
  bimclassification: '🏗️',
};

const icon = computed(() => categoryIcon[props.run.agentName] || '🤖');

const timeAgo = computed(() => {
  const now = new Date().getTime();
  const started = new Date(props.run.startedAt).getTime();
  const diffMs = now - started;
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMins / 60);
  const diffDays = Math.floor(diffHours / 24);

  if (diffDays > 0) return `${diffDays}d ago`;
  if (diffHours > 0) return `${diffHours}h ago`;
  if (diffMins > 0) return `${diffMins}m ago`;
  return 'Just now';
});
</script>

<template>
  <AppCard 
    class="p-4 hover:border-[--color-border] transition-all duration-200 cursor-pointer" 
    @click="emit('view', run.id)"
  >
    <div class="flex items-center gap-4">
      <!-- Icon -->
      <div class="text-2xl flex-shrink-0">
        {{ icon }}
      </div>

      <!-- Content -->
      <div class="flex-1 min-w-0">
        <div class="flex items-center gap-2 mb-1">
          <h4 class="font-medium text-[--color-text-primary] capitalize">
            {{ run.agentName.replace(/-/g, ' ') }}
          </h4>
          <StatusBadge :status="run.status" type="run" size="sm" />
        </div>
        <p class="text-xs text-[--color-text-tertiary] truncate mb-1">
          {{ timeAgo }} • {{ run.initiatedBy }}
        </p>
        <p v-if="run.output" class="text-sm text-[--color-text-secondary] line-clamp-1">
          {{ run.output }}
        </p>
        <p v-else-if="run.errorMessage" class="text-sm text-danger-500 line-clamp-1">
          {{ run.errorMessage }}
        </p>
      </div>

      <!-- Duration -->
      <div v-if="run.duration" class="text-right flex-shrink-0">
        <p class="text-sm font-mono text-[--color-text-secondary]">
          {{ run.duration.split('.')[0] }}
        </p>
      </div>
    </div>
  </AppCard>
</template>
