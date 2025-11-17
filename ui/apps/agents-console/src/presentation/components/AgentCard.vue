<script setup lang="ts">
import { computed } from 'vue';
import { AppCard, AppButton } from '@agents/design-system';
import StatusBadge from './StatusBadge.vue';
import type { AgentSummary, AgentHealth } from '@agents/agent-domain';

interface Props {
  agent: AgentSummary;
  health?: AgentHealth;
  loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
});

const emit = defineEmits<{
  (e: 'execute', name: string): void;
  (e: 'view', name: string): void;
}>();

const categoryIcon: Record<string, string> = {
  notification: '📧',
  devops: '⚙️',
  'test-planning': '🧪',
  implementation: '💻',
  servicedesk: '🎫',
  bimclassification: '🏗️',
};

const icon = computed(() => categoryIcon[props.agent.category] || '🤖');
</script>

<template>
  <AppCard class="p-6 hover:border-[--color-border] transition-all duration-200 cursor-pointer group" @click="emit('view', agent.name)">
    <div class="flex items-start gap-4">
      <!-- Icon -->
      <div class="text-4xl flex-shrink-0 group-hover:scale-110 transition-transform duration-200">
        {{ icon }}
      </div>

      <!-- Content -->
      <div class="flex-1 min-w-0">
        <div class="flex items-start justify-between gap-2 mb-2">
          <div class="flex-1 min-w-0">
            <h3 class="text-lg font-semibold text-[--color-text-primary] truncate mb-1">
              {{ agent.displayName }}
            </h3>
            <p class="text-sm text-[--color-text-tertiary] line-clamp-2">
              {{ agent.description }}
            </p>
          </div>
          <StatusBadge :status="agent.status" type="agent" size="sm" />
        </div>

        <!-- Health Status -->
        <div v-if="health" class="flex items-center gap-2 mt-3 pt-3 border-t border-[--color-border-subtle]">
          <StatusBadge :status="health.status" type="health" size="sm" />
          <span class="text-xs text-[--color-text-tertiary]">
            Checked {{ new Date(health.lastChecked).toLocaleTimeString() }}
          </span>
        </div>

        <!-- Actions -->
        <div class="flex items-center gap-2 mt-4">
          <AppButton 
            variant="primary" 
            size="sm"
            @click.stop="emit('execute', agent.name)"
          >
            Execute
          </AppButton>
          <AppButton 
            variant="ghost" 
            size="sm"
            @click.stop="emit('view', agent.name)"
          >
            View Details
          </AppButton>
        </div>
      </div>
    </div>
  </AppCard>
</template>
