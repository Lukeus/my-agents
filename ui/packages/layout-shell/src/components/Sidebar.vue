<script setup lang="ts">
import { computed } from 'vue';

export interface SidebarProps {
  collapsed?: boolean;
}

const props = withDefaults(defineProps<SidebarProps>(), {
  collapsed: false,
});

const emit = defineEmits<{
  toggle: [];
}>();

const navItems = [
  { name: 'Dashboard', icon: '🏠', path: '/' },
  { name: 'Agents', icon: '🤖', path: '/agents' },
  { name: 'Runs', icon: '▶️', path: '/runs' },
  { name: 'Settings', icon: '⚙️', path: '/settings' },
];
</script>

<template>
  <aside
    class="flex flex-col border-r border-[--color-border-subtle] bg-[--color-surface-elevated] transition-all duration-200"
    :class="collapsed ? 'w-16' : 'w-64'"
  >
    <!-- Sidebar header with toggle -->
    <div class="flex h-16 items-center justify-between px-4 border-b border-[--color-border-subtle]">
      <span v-if="!collapsed" class="text-sm font-medium text-[--color-text-secondary]">Navigation</span>
      <button
        class="rounded-[--radius-md] p-2 hover:bg-[--color-surface-hover] transition-colors"
        @click="emit('toggle')"
      >
        <svg class="h-4 w-4 text-[--color-text-secondary]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            :d="collapsed ? 'M9 5l7 7-7 7' : 'M15 19l-7-7 7-7'"
          />
        </svg>
      </button>
    </div>

    <!-- Navigation items -->
    <nav class="flex-1 space-y-1 p-2">
      <router-link
        v-for="item in navItems"
        :key="item.path"
        :to="item.path"
        class="flex items-center gap-3 rounded-[--radius-md] px-3 py-2 text-sm transition-colors hover:bg-[--color-surface-hover]"
        active-class="bg-[--color-brand-500]/20 text-[--color-brand-500]"
      >
        <span class="text-base">{{ item.icon }}</span>
        <span v-if="!collapsed">{{ item.name }}</span>
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
