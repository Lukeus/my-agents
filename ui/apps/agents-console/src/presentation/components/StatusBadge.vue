<script setup lang="ts">
import { computed } from 'vue';
import { AppBadge } from '@agents/design-system';
import type { AgentStatus, AgentRunStatus, AgentHealthStatus } from '@agents/agent-domain';

interface Props {
  status: AgentStatus | AgentRunStatus | AgentHealthStatus;
  size?: 'sm' | 'md';
}

const props = withDefaults(defineProps<Props>(), {
  size: 'md',
});

const statusConfig: Record<string, { label: string; variant: 'default' | 'brand' | 'success' | 'warning' | 'danger' | 'info' }> = {
  // Agent statuses
  enabled: { label: 'Active', variant: 'success' },
  disabled: { label: 'Disabled', variant: 'default' },
  maintenance: { label: 'Maintenance', variant: 'warning' },
  
  // Run statuses
  pending: { label: 'Pending', variant: 'default' },
  running: { label: 'Running', variant: 'info' },
  succeeded: { label: 'Succeeded', variant: 'success' },
  failed: { label: 'Failed', variant: 'danger' },
  
  // Health statuses
  healthy: { label: 'Healthy', variant: 'success' },
  degraded: { label: 'Degraded', variant: 'warning' },
  unhealthy: { label: 'Unhealthy', variant: 'danger' },
};

const config = computed(() => 
  statusConfig[props.status] || { label: props.status, variant: 'default' as const }
);
</script>

<template>
  <AppBadge :variant="config.variant" :size="size" dot>
    {{ config.label }}
  </AppBadge>
</template>
