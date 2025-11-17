<script setup lang="ts">
import { AppShell } from '@agents/layout-shell';
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { appConfig } from '@agents/shared';

const route = useRoute();

const currentApp = {
  name: 'Test Planning Studio',
  icon: '🧪',
};

const availableApps = appConfig.apps;

const navigationItems = computed(() => [
  { label: 'Dashboard', icon: '📊', route: '/', isActive: route.path === '/' },
  { label: 'Test Specs', icon: '📝', route: '/specs', isActive: route.path.startsWith('/specs') },
  { label: 'Generate', icon: '✨', route: '/generate', isActive: route.path === '/generate' },
  { label: 'Coverage', icon: '📈', route: '/coverage', isActive: route.path === '/coverage' },
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
