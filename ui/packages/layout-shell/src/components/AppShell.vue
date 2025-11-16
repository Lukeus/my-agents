<script setup lang="ts">
import { ref } from 'vue';
import TopNav from './TopNav.vue';
import Sidebar from './Sidebar.vue';

export interface AppShellProps {
  title?: string;
  showSidebar?: boolean;
}

const props = withDefaults(defineProps<AppShellProps>(), {
  title: 'My Agents',
  showSidebar: true,
});

const isSidebarCollapsed = ref(false);

const toggleSidebar = () => {
  isSidebarCollapsed.value = !isSidebarCollapsed.value;
};
</script>

<template>
  <div class="flex h-screen bg-[--color-surface]">
    <!-- Sidebar -->
    <Sidebar
      v-if="showSidebar"
      :collapsed="isSidebarCollapsed"
      @toggle="toggleSidebar"
    />

    <!-- Main content area -->
    <div class="flex flex-1 flex-col overflow-hidden">
      <!-- Top navigation -->
      <TopNav :title="title" />

      <!-- Page content -->
      <main class="flex-1 overflow-y-auto">
        <slot />
      </main>
    </div>
  </div>
</template>
