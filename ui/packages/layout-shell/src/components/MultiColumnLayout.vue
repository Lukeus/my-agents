<script setup lang="ts">
import { ref } from 'vue';
import Sidebar from './Sidebar.vue';
import TopNav from './TopNav.vue';
import type { AppInfo, AppItem, HealthStatus, NavItem } from './AppShell.vue';

export interface MultiColumnLayoutProps {
  currentApp: AppInfo;
  availableApps: AppItem[];
  navigationItems: NavItem[];
  healthStatus: HealthStatus;
  showSecondarySidebar?: boolean;
}

const props = withDefaults(defineProps<MultiColumnLayoutProps>(), {
  showSecondarySidebar: false,
});

const isSidebarCollapsed = ref(false);

const toggleSidebar = () => {
  isSidebarCollapsed.value = !isSidebarCollapsed.value;
};
</script>

<template>
  <!-- Multi-Column Layout: Sidebar + Main + Optional Secondary Column -->
  <div class="h-screen flex overflow-hidden bg-[--color-surface]">
    <!-- Primary Sidebar -->
    <Sidebar
      :collapsed="isSidebarCollapsed"
      :navigation-items="navigationItems"
      @toggle="toggleSidebar"
    />

    <!-- Main Content Area -->
    <div class="flex flex-col flex-1 overflow-hidden">
      <!-- Top Navigation -->
      <TopNav
        :current-app="currentApp"
        :available-apps="availableApps"
        :health-status="healthStatus"
      />

      <!-- Content Area with Optional Secondary Column -->
      <div class="flex flex-1 overflow-hidden">
        <!-- Main Content -->
        <main class="flex-1 overflow-y-auto bg-[--color-surface]">
          <div class="py-6">
            <div class="max-w-7xl mx-auto px-4 sm:px-6 md:px-8">
              <slot />
            </div>
          </div>
        </main>

        <!-- Secondary Sidebar/Column (optional) -->
        <aside
          v-if="showSecondarySidebar || $slots.secondary"
          class="flex-shrink-0 w-80 overflow-y-auto border-l border-[--color-border-subtle] bg-[--color-surface-elevated]"
        >
          <div class="p-6">
            <slot name="secondary" />
          </div>
        </aside>
      </div>
    </div>
  </div>
</template>
