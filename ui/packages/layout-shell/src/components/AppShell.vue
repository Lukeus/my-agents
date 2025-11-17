<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, computed } from 'vue';
import Sidebar from './Sidebar.vue';
import { AppSwitcher } from '@agents/design-system';
import type { AppItem } from '@agents/design-system';

export interface AppInfo {
  name: string;
  icon: string;
}


export interface NavItem {
  label: string;
  icon: string;
  route: string;
  isActive?: boolean;
}

export interface AppShellProps {
  currentApp: AppInfo;
  availableApps?: AppItem[];
  navigationItems: NavItem[];
  showSidebar?: boolean;
}

const props = withDefaults(defineProps<AppShellProps>(), {
  showSidebar: true,
  availableApps: () => [],
});

const showAppSwitcher = computed(() => props.availableApps && props.availableApps.length > 0);

const sidebarOpen = ref(false);

const openSidebar = () => {
  sidebarOpen.value = true;
};

const closeSidebar = () => {
  sidebarOpen.value = false;
};

const handleKeyDown = (event: KeyboardEvent) => {
  if (event.key === 'Escape' && sidebarOpen.value) {
    closeSidebar();
  }
};

onMounted(() => {
  document.addEventListener('keydown', handleKeyDown);
});

onBeforeUnmount(() => {
  document.removeEventListener('keydown', handleKeyDown);
});
</script>

<template>
  <div class="min-h-screen bg-[--color-surface]">
    
    <!-- Mobile sidebar overlay -->
    <div v-if="sidebarOpen && showSidebar" 
         class="lg:hidden relative z-50" 
         role="dialog" 
         aria-modal="true">
      
      <!-- Backdrop with z-index -->
      <div class="fixed inset-0 bg-gray-900/80 z-40" @click="closeSidebar"></div>
      
      <!-- Sidebar panel -->
      <div class="fixed inset-0 flex z-50">
        <div class="relative mr-16 flex w-full max-w-xs flex-1">
          
          <!-- Close button - positioned RIGHT of sidebar -->
          <div class="absolute -right-12 top-0 pt-2">
            <button 
              type="button" 
              class="ml-1 flex h-10 w-10 items-center justify-center rounded-full focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-white"
              @click="closeSidebar"
            >
              <span class="sr-only">Close sidebar</span>
              <svg class="h-6 w-6 text-white" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          
          <Sidebar :navigation-items="navigationItems" />
        </div>
      </div>
    </div>

    <!-- Desktop sidebar - PURE TAILWIND -->
    <div 
      v-if="showSidebar" 
      class="hidden lg:fixed lg:inset-y-0 lg:left-0 lg:z-50 lg:flex lg:w-[18rem] lg:flex-col lg:border-r lg:border-[--color-border-subtle] lg:bg-[--color-surface-elevated]"
    >
      <Sidebar :navigation-items="navigationItems" />
    </div>

    <!-- Content wrapper with sidebar offset -->
    <div v-if="showSidebar" class="lg:pl-[18rem]">
      
      <!-- Mobile header (menu button + app switcher/title) -->
      <div class="sticky top-0 z-40 flex h-16 shrink-0 items-center gap-x-4 border-b border-[--color-border-subtle] bg-[--color-surface-elevated] px-4 shadow-sm sm:gap-x-6 sm:px-6 lg:hidden">
        <button 
          type="button" 
          class="-m-2.5 p-2.5 text-[--color-text-secondary] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[--color-brand-500]" 
          @click="openSidebar"
        >
          <span class="sr-only">Open sidebar</span>
          <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
          </svg>
        </button>
        <div class="flex-1">
          <AppSwitcher
            v-if="showAppSwitcher"
            :current-app="currentApp.name"
            :apps="availableApps"
          />
          <h1 v-else class="text-sm font-semibold leading-6 text-[--color-text-primary]">
            {{ currentApp.name }}
          </h1>
        </div>
      </div>

      <!-- Desktop header (app switcher + title) -->
      <div class="hidden lg:flex lg:h-16 lg:items-center lg:justify-between lg:border-b lg:border-[--color-border-subtle] lg:bg-[--color-surface-elevated] lg:px-8">
        <AppSwitcher
          v-if="showAppSwitcher"
          :current-app="currentApp.name"
          :apps="availableApps"
        />
        <h1 v-else class="text-lg font-semibold text-[--color-text-primary]">
          {{ currentApp.name }}
        </h1>
      </div>

      <!-- Main content -->
      <main class="py-10">
        <div class="px-4 sm:px-6 lg:px-8">
          <slot />
        </div>
      </main>
    </div>

    <!-- No sidebar - full width -->
    <div v-if="!showSidebar">
      <main class="py-10">
        <div class="px-4 sm:px-6 lg:px-8">
          <slot />
        </div>
      </main>
    </div>
    
  </div>
</template>

