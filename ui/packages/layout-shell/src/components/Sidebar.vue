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
    class="flex flex-shrink-0 flex-col bg-[--color-surface-elevated] transition-all duration-200"
    :class="collapsed ? 'w-20' : 'w-64'"
  >
    <!-- Logo / Brand -->
    <div class="flex h-16 items-center px-4">
      <div v-if="!collapsed" class="flex items-center gap-2">
        <div class="h-8 w-8 rounded-lg bg-[--color-brand-600] flex items-center justify-center">
          <span class="text-white text-lg font-bold">A</span>
        </div>
        <span class="text-lg font-semibold text-[--color-text-primary]">Agents</span>
      </div>
      <div v-else class="flex items-center justify-center w-full">
        <div class="h-8 w-8 rounded-lg bg-[--color-brand-600] flex items-center justify-center">
          <span class="text-white text-lg font-bold">A</span>
        </div>
      </div>
    </div>

    <!-- Navigation items -->
    <nav class="flex-1 px-3 py-4 space-y-1">
      <router-link
        v-for="item in navigationItems"
        :key="item.route"
        :to="item.route"
        class="flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors"
        :class="[
          item.isActive 
            ? 'bg-[--color-surface] text-[--color-text-primary]' 
            : 'text-[--color-text-secondary] hover:bg-[--color-surface-hover] hover:text-[--color-text-primary]'
        ]"
      >
        <span class="text-xl">{{ item.icon }}</span>
        <span v-if="!collapsed">{{ item.label }}</span>
      </router-link>
    </nav>

    <!-- User profile footer -->
    <div class="border-t border-[--color-border-subtle] p-4">
      <div v-if="!collapsed" class="flex items-center gap-3">
        <div class="h-10 w-10 rounded-full bg-[--color-brand-500] flex items-center justify-center flex-shrink-0">
          <span class="text-sm font-medium text-white">U</span>
        </div>
        <div class="flex-1 min-w-0">
          <p class="text-sm font-medium text-[--color-text-primary] truncate">User</p>
          <p class="text-xs text-[--color-text-tertiary] truncate">user@example.com</p>
        </div>
      </div>
      <div v-else class="flex justify-center">
        <div class="h-10 w-10 rounded-full bg-[--color-brand-500] flex items-center justify-center">
          <span class="text-sm font-medium text-white">U</span>
        </div>
      </div>
    </div>
  </aside>
</template>
