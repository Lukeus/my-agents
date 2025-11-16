<script setup lang="ts">
import { AppShell } from '@agents/layout-shell';
import { computed } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();

const currentApp = computed(() => ({
  name: 'Agents Console',
  icon: '🤖',
}));

const availableApps = [
  { name: 'Agents Console', icon: '🤖', href: '/' },
  { name: 'Test Planning', icon: '🧪', href: '/test-planning' },
  { name: 'DevOps Hub', icon: '⚙️', href: '/devops' },
  { name: 'Service Desk', icon: '🎫', href: '/service-desk' },
];

const navigationItems = computed(() => [
  { label: 'Dashboard', icon: '📊', route: '/', isActive: route.path === '/' },
  { label: 'Agents', icon: '🤖', route: '/agents', isActive: route.path.startsWith('/agents') },
  { label: 'Runs', icon: '▶️', route: '/runs', isActive: route.path.startsWith('/runs') },
  { label: 'Settings', icon: '⚙️', route: '/settings', isActive: route.path === '/settings' },
]);

const healthStatus = computed(() => ({
  status: 'healthy' as const,
  message: 'All systems operational',
}));
</script>

<template>
  <AppShell
    :current-app="currentApp"
    :available-apps="availableApps"
    :navigation-items="navigationItems"
    :health-status="healthStatus"
  >
    <router-view />
  </AppShell>
</template>
