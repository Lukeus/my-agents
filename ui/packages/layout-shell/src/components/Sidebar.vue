<script setup lang="ts">
import { computed } from 'vue';
import type { NavItem } from './AppShell.vue';

export interface SidebarProps {
  collapsed?: boolean;
  navigationItems: NavItem[];
}

const props = withDefaults(defineProps<SidebarProps>(), {
  collapsed: false,
});

const emit = defineEmits<{
  toggle: [];
}>();
</script>

<template>
  <aside
    class="flex flex-shrink-0 flex-col border-r border-[--color-border-subtle] bg-[--color-surface-elevated] transition-all duration-200"
    :class="collapsed ? 'w-16' : 'w-64'"
  >
    <!-- Sidebar header with toggle -->
    <div class="flex h-16 items-center justify-end px-4 border-b border-[--color-border-subtle]">
      <button
        class="rounded-[--radius-md] p-2 hover:bg-[--color-surface-hover] transition-colors"
        @click="emit('toggle')"
        title="Toggle sidebar"
      >
        <svg class="h-4 w-4 text-[--color-text-secondary]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            :d="collapsed ? 'M13 5l7 7-7 7' : 'M11 19l-7-7 7-7'"
          />
        </svg>
      </button>
    </div>

    <!-- Navigation items -->
    <nav class="flex-1 space-y-1 p-2">
      <router-link
        v-for="item in navigationItems"
        :key="item.route"
        :to="item.route"
        class="flex items-center gap-3 rounded-[--radius-md] px-3 py-2 text-sm transition-colors hover:bg-[--color-surface-hover]"
        active-class="bg-[--color-brand-500]/20 text-[--color-brand-500]"
      >
        <span class="text-base">{{ item.icon }}</span>
        <span v-if="!collapsed" class="text-[--color-text-primary]">{{ item.label }}</span>
      </router-link>
    </nav>

    <!-- Sidebar footer -->
    <div class="border-t border-[--color-border-subtle] p-4">
      <div v-if="!collapsed" class="text-xs text-[--color-text-tertiary]">
        Version 0.0.1
      </div>
    </div>
  </aside>
</template>
