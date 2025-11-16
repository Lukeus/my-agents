<script setup lang="ts">
import { ref } from 'vue';
import TopNav from './TopNav.vue';
import Sidebar from './Sidebar.vue';

export interface AppInfo {
  name: string;
  icon: string;
}

export interface AppItem {
  name: string;
  icon: string;
  href: string;
}

export interface NavItem {
  label: string;
  icon: string;
  route: string;
  isActive?: boolean;
}

export interface HealthStatus {
  status: 'healthy' | 'degraded' | 'down';
  message: string;
}

export interface AppShellProps {
  currentApp: AppInfo;
  availableApps: AppItem[];
  navigationItems: NavItem[];
  healthStatus: HealthStatus;
  showSidebar?: boolean;
}

const props = withDefaults(defineProps<AppShellProps>(), {
  showSidebar: true,
});

const isSidebarCollapsed = ref(false);

const toggleSidebar = () => {
  isSidebarCollapsed.value = !isSidebarCollapsed.value;
};
</script>

<template>
  <div class="h-screen flex overflow-hidden bg-[--color-surface]">
    <!-- Sidebar -->
    <Sidebar
      v-if="showSidebar"
      :collapsed="isSidebarCollapsed"
      :navigation-items="navigationItems"
      @toggle="toggleSidebar"
    />

    <!-- Main content area -->
    <div class="flex flex-col flex-1 overflow-hidden">
      <!-- Top navigation -->
      <TopNav
        :current-app="currentApp"
        :available-apps="availableApps"
        :health-status="healthStatus"
      />

      <!-- Page content -->
      <main class="flex-1 overflow-y-auto bg-[--color-surface]">
        <div class="py-6">
          <div class="max-w-7xl mx-auto px-4 sm:px-6 md:px-8">
            <slot />
          </div>
        </div>
      </main>
    </div>
  </div>
</template>
