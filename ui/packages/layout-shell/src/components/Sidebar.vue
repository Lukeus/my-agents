<script setup lang="ts">
import type { NavItem } from './AppShell.vue';

export interface SidebarProps {
  navigationItems: NavItem[];
  showCloseButton?: boolean;
}

const props = withDefaults(defineProps<SidebarProps>(), {
  showCloseButton: false,
});

const emit = defineEmits<{
  close: [];
}>();
</script>

<template>
  <!-- Single root with all Tailwind utilities -->
  <div class="flex h-full w-full flex-col gap-y-5 overflow-y-auto px-6 pb-4">
    
    <!-- Brand -->
    <div class="flex h-16 shrink-0 items-center">
      <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-[--color-brand-600]">
        <span class="text-xl font-bold text-white" aria-hidden="true">A</span>
      </div>
      <span class="ml-3 text-xl font-semibold text-[--color-text-primary]">Agents</span>
    </div>

    <!-- Navigation -->
    <nav class="flex flex-1 flex-col">
      <ul role="list" class="flex flex-1 flex-col gap-y-7">
        <li>
          <ul role="list" class="-mx-2 space-y-1">
            <li v-for="item in navigationItems" :key="item.route">
              <router-link
                :to="item.route"
                :class="[
                  item.isActive
                    ? 'bg-[--color-surface] text-[--color-text-primary]'
                    : 'text-[--color-text-secondary] hover:bg-[--color-surface-hover] hover:text-[--color-text-primary]',
                  'group flex gap-x-3 rounded-md p-2 text-sm font-semibold leading-6 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[--color-brand-500]',
                ]"
              >
                <span class="text-xl" aria-hidden="true">{{ item.icon }}</span>
                <span>{{ item.label }}</span>
              </router-link>
            </li>
          </ul>
        </li>
      </ul>
    </nav>

    <!-- User profile -->
    <div class="-mx-6 mt-auto">
      <a
        href="#"
        class="flex items-center gap-x-4 px-6 py-3 text-sm font-semibold leading-6 text-[--color-text-primary] hover:bg-[--color-surface-hover] transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[--color-brand-500]"
      >
        <div class="flex h-8 w-8 items-center justify-center rounded-full bg-[--color-brand-500]">
          <span class="text-xs font-medium text-white">U</span>
        </div>
        <span class="sr-only">Your profile</span>
        <span aria-hidden="true">User</span>
      </a>
    </div>
    
  </div>
</template>

