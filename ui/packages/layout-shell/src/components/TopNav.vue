<script setup lang="ts">
import { ref, computed } from 'vue';
import { AppBadge } from '@agents/design-system';
import type { AppInfo, AppItem, HealthStatus } from './AppShell.vue';

export interface TopNavProps {
  currentApp: AppInfo;
  availableApps: AppItem[];
  healthStatus: HealthStatus;
}

const props = defineProps<TopNavProps>();

const showAppSwitcher = ref(false);

const healthColor = computed(() => {
  switch (props.healthStatus.status) {
    case 'healthy':
      return 'bg-[--color-success-500]';
    case 'degraded':
      return 'bg-[--color-warning-500]';
    case 'down':
      return 'bg-[--color-danger-500]';
    default:
      return 'bg-[--color-success-500]';
  }
});
</script>

<template>
  <header class="flex h-16 items-center justify-between border-b border-[--color-border-subtle] bg-[--color-surface-elevated] px-6">
    <!-- Left: App title with switcher -->
    <div class="relative">
      <button
        class="flex items-center gap-2 rounded-[--radius-md] px-3 py-2 hover:bg-[--color-surface-hover] transition-colors"
        @click="showAppSwitcher = !showAppSwitcher"
      >
        <span class="text-xl">{{ currentApp.icon }}</span>
        <span class="text-lg font-semibold text-[--color-text-primary]">{{ currentApp.name }}</span>
        <svg class="h-4 w-4 text-[--color-text-secondary]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
        </svg>
      </button>

      <!-- App switcher dropdown -->
      <div
        v-if="showAppSwitcher"
        class="absolute left-0 top-full z-50 mt-2 w-64 rounded-[--radius-lg] border border-[--color-border-subtle] bg-[--color-surface-elevated] shadow-lg"
      >
        <div class="p-2">
          <div class="mb-2 px-3 py-2 text-xs font-medium uppercase tracking-wide text-[--color-text-tertiary]">
            Switch App
          </div>
          <a
            v-for="app in availableApps"
            :key="app.name"
            :href="app.href"
            class="flex items-center gap-3 rounded-[--radius-md] px-3 py-2 text-sm hover:bg-[--color-surface-hover] transition-colors text-[--color-text-primary]"
          >
            <span class="text-lg">{{ app.icon }}</span>
            <span>{{ app.name }}</span>
          </a>
        </div>
      </div>
    </div>

    <!-- Right: Status and user menu -->
    <div class="flex items-center gap-4">
      <!-- Health status -->
      <div class="flex items-center gap-2">
        <div class="h-2 w-2 rounded-full" :class="healthColor"></div>
        <span class="text-sm text-[--color-text-secondary]">{{ healthStatus.message }}</span>
      </div>

      <!-- User menu placeholder -->
      <div class="h-8 w-8 rounded-full bg-[--color-brand-500] flex items-center justify-center">
        <span class="text-sm font-medium text-white">U</span>
      </div>
    </div>
  </header>
</template>
