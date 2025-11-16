<script setup lang="ts">
import { onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { AppCard, AppButton, AppBadge } from '@agents/design-system';
import { useTestSpecs } from '@/application/usecases/useTestSpecs';

const router = useRouter();
const { specs, loading, error, fetchSpecs } = useTestSpecs();

const stats = computed(() => ({
  total: specs.value.length,
  bdd: specs.value.filter((s) => s.scenarios && s.scenarios.length > 0).length,
  draft: specs.value.filter((s) => s.status === 'draft').length,
}));

onMounted(() => {
  fetchSpecs();
});

const navigateToSpecs = () => {
  router.push('/specs');
};

const navigateToGenerate = () => {
  router.push('/generate');
};
</script>

<template>
  <div class="max-w-7xl mx-auto">
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-[--color-text-primary] mb-2">Test Planning Dashboard</h1>
      <p class="text-[--color-text-secondary]">
        Generate and manage test specifications with AI assistance
      </p>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
      <AppCard>
        <div class="text-center">
          <div class="text-5xl font-bold text-[--color-primary-600] mb-2">{{ stats.total }}</div>
          <div class="text-[--color-text-secondary]">Total Specs</div>
        </div>
      </AppCard>

      <AppCard>
        <div class="text-center">
          <div class="text-5xl font-bold text-[--color-success-600] mb-2">{{ stats.bdd }}</div>
          <div class="text-[--color-text-secondary]">BDD Scenarios</div>
        </div>
      </AppCard>

      <AppCard>
        <div class="text-center">
          <div class="text-5xl font-bold text-[--color-warning-600] mb-2">{{ stats.draft }}</div>
          <div class="text-[--color-text-secondary]">Drafts</div>
        </div>
      </AppCard>
    </div>

    <!-- Error State -->
    <div
      v-if="error"
      class="mb-8 p-4 bg-[--color-danger-50] border border-[--color-danger-200] rounded-[--radius-lg] text-[--color-danger-700]"
    >
      {{ error }}
    </div>

    <!-- Quick Actions -->
    <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
      <AppCard hoverable>
        <h2 class="text-2xl font-semibold text-[--color-text-primary] mb-4">Generate Test Spec</h2>
        <p class="text-[--color-text-secondary] mb-6">
          Use AI to generate comprehensive test specifications from feature descriptions
        </p>
        <AppButton @click="navigateToGenerate" variant="primary">
          ✨ Generate New Spec
        </AppButton>
      </AppCard>

      <AppCard hoverable>
        <h2 class="text-2xl font-semibold text-[--color-text-primary] mb-4">Manage Test Specs</h2>
        <p class="text-[--color-text-secondary] mb-6">
          View, edit, and organize your test specifications
        </p>
        <AppButton @click="navigateToSpecs" variant="secondary">
          📝 View All Specs
        </AppButton>
      </AppCard>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-12">
      <div class="text-[--color-text-secondary]">Loading dashboard...</div>
    </div>

    <!-- Recent Specs -->
    <div v-if="!loading && specs.length > 0">
      <div class="flex items-center justify-between mb-6">
        <h2 class="text-2xl font-semibold text-[--color-text-primary]">Recent Test Specs</h2>
        <AppButton @click="navigateToSpecs" variant="ghost">View All</AppButton>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <AppCard v-for="spec in specs.slice(0, 6)" :key="spec.id" hoverable>
          <div class="flex items-start justify-between mb-3">
            <h3 class="text-xl font-semibold text-[--color-text-primary]">{{ spec.feature }}</h3>
            <AppBadge :variant="spec.status === 'draft' ? 'warning' : 'success'">
              {{ spec.status }}
            </AppBadge>
          </div>

          <p class="text-[--color-text-secondary] text-sm mb-4 line-clamp-2">
            {{ spec.description }}
          </p>

          <div v-if="spec.scenarios && spec.scenarios.length > 0" class="text-sm text-[--color-text-muted]">
            {{ spec.scenarios.length }} BDD scenario{{ spec.scenarios.length !== 1 ? 's' : '' }}
          </div>
        </AppCard>
      </div>
    </div>

    <!-- Empty State -->
    <div v-if="!loading && specs.length === 0" class="text-center py-12">
      <div class="text-6xl mb-4">🧪</div>
      <p class="text-[--color-text-secondary] mb-4">No test specifications yet</p>
      <AppButton @click="navigateToGenerate">Generate Your First Spec</AppButton>
    </div>
  </div>
</template>
