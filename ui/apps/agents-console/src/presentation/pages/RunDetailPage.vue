<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router';
import { AppCard, AppButton, AppBadge } from '@agents/design-system';

const route = useRoute();
const router = useRouter();

const runId = route.params.id as string;

const goBack = () => {
  router.push('/runs');
};
</script>

<template>
  <div class="">
    <div class="mb-6">
      <AppButton variant="ghost" @click="goBack">← Back to Runs</AppButton>
    </div>

    <div class="mb-8">
      <h1 class="text-4xl font-bold text-text-primary mb-2">Run #{{ runId }}</h1>
      <AppBadge variant="warning">Coming Soon</AppBadge>
    </div>

    <AppCard>
      <p class="text-text-secondary">
        Detailed run information, logs, and execution metrics will be displayed here.
      </p>
    </AppCard>
  </div>
</template>
