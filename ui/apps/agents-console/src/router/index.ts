import { createRouter, createWebHistory } from 'vue-router';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'dashboard',
      component: () => import('@/presentation/pages/DashboardPage.vue'),
      meta: { title: 'Dashboard' },
    },
    {
      path: '/agents',
      name: 'agents',
      component: () => import('@/presentation/pages/AgentsListPage.vue'),
      meta: { title: 'Agents' },
    },
    {
      path: '/agents/:name',
      name: 'agent-detail',
      component: () => import('@/presentation/pages/AgentDetailPage.vue'),
      meta: { title: 'Agent Detail' },
    },
    {
      path: '/runs',
      name: 'runs',
      component: () => import('@/presentation/pages/RunsListPage.vue'),
      meta: { title: 'Runs' },
    },
    {
      path: '/runs/:id',
      name: 'run-detail',
      component: () => import('@/presentation/pages/RunDetailPage.vue'),
      meta: { title: 'Run Detail' },
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('@/presentation/pages/SettingsPage.vue'),
      meta: { title: 'Settings' },
    },
  ],
});

// Update document title on route change
router.afterEach((to) => {
  document.title = to.meta.title ? `${to.meta.title} - Agents Console` : 'Agents Console';
});

export default router;
