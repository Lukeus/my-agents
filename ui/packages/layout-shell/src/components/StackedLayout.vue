<script setup lang="ts">
import { computed } from 'vue';
import type { AppInfo, AppItem, HealthStatus } from './AppShell.vue';

export interface StackedLayoutProps {
  currentApp: AppInfo;
  availableApps: AppItem[];
  healthStatus: HealthStatus;
}

const props = defineProps<StackedLayoutProps>();
</script>

<template>
  <!-- Stacked Layout: Full-width with top navigation -->
  <div class="min-h-screen bg-[--color-surface]">
    <!-- Top Navigation -->
    <nav class="bg-[--color-surface-elevated] border-b border-[--color-border-subtle]">
      <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div class="flex h-16 justify-between">
          <div class="flex">
            <!-- Logo/Brand -->
            <div class="flex flex-shrink-0 items-center">
              <span class="text-2xl">{{ currentApp.icon }}</span>
              <span class="ml-2 text-xl font-semibold text-[--color-text-primary]">
                {{ currentApp.name }}
              </span>
            </div>
          </div>

          <!-- Right side -->
          <div class="flex items-center gap-4">
            <!-- Health Status -->
            <div class="flex items-center gap-2">
              <div
                class="h-2 w-2 rounded-full"
                :class="{
                  'bg-[--color-success-500]': healthStatus.status === 'healthy',
                  'bg-[--color-warning-500]': healthStatus.status === 'degraded',
                  'bg-[--color-danger-500]': healthStatus.status === 'down',
                }"
              ></div>
              <span class="hidden sm:inline text-sm text-[--color-text-secondary]">
                {{ healthStatus.message }}
              </span>
            </div>

            <!-- User Avatar -->
            <div class="h-8 w-8 rounded-full bg-[--color-brand-500] flex items-center justify-center">
              <span class="text-sm font-medium text-white">U</span>
            </div>
          </div>
        </div>
      </div>
    </nav>

    <!-- Page Header (optional slot) -->
    <header v-if="$slots.header" class="bg-[--color-surface-elevated] shadow">
      <div class="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
        <slot name="header" />
      </div>
    </header>

    <!-- Main Content -->
    <main>
      <div class="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
        <slot />
      </div>
    </main>
  </div>
</template>
