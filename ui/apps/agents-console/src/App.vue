<script setup lang="ts">
import { AppShell } from '@agents/layout-shell';
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { appConfig } from '@agents/shared';

const route = useRoute();

const currentApp = computed(() => ({
  name: 'Agents Console',
  icon: '🤖',
}));

const availableApps = appConfig.apps;

const navigationItems = computed(() => [
  { label: 'Dashboard', icon: '📊', route: '/', isActive: route.path === '/' },
  { label: 'Agents', icon: '🤖', route: '/agents', isActive: route.path.startsWith('/agents') },
  { label: 'Runs', icon: '▶️', route: '/runs', isActive: route.path.startsWith('/runs') },
  { label: 'Settings', icon: '⚙️', route: '/settings', isActive: route.path === '/settings' },
]);
</script>

<template>
  <AppShell
    :current-app="currentApp"
    :available-apps="availableApps"
    :navigation-items="navigationItems"
  >
    <router-view />
  </AppShell>
</template>
