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
      path: '/specs',
      name: 'test-specs',
      component: () => import('@/presentation/pages/TestSpecsListPage.vue'),
      meta: { title: 'Test Specifications' },
    },
    {
      path: '/specs/new',
      name: 'test-spec-new',
      component: () => import('@/presentation/pages/TestSpecEditorPage.vue'),
      meta: { title: 'New Test Spec' },
    },
    {
      path: '/specs/:id',
      name: 'test-spec-detail',
      component: () => import('@/presentation/pages/TestSpecDetailPage.vue'),
      meta: { title: 'Test Spec Detail' },
    },
    {
      path: '/specs/:id/edit',
      name: 'test-spec-edit',
      component: () => import('@/presentation/pages/TestSpecEditorPage.vue'),
      meta: { title: 'Edit Test Spec' },
    },
    {
      path: '/coverage',
      name: 'coverage',
      component: () => import('@/presentation/pages/CoverageAnalysisPage.vue'),
      meta: { title: 'Coverage Analysis' },
    },
    {
      path: '/generate',
      name: 'generate',
      component: () => import('@/presentation/pages/GenerateSpecPage.vue'),
      meta: { title: 'Generate Test Spec' },
    },
  ],
});

// Update document title on route change
router.afterEach((to) => {
  document.title = to.meta.title
    ? `${to.meta.title} - Test Planning Studio`
    : 'Test Planning Studio';
});

export default router;
