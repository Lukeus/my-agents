<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router';
import { AppCard, AppButton, AppBadge } from '@agents/design-system';

const route = useRoute();
const router = useRouter();

const agentName = route.params.name as string;

const goBack = () => {
  router.push('/agents');
};
</script>

<template>
  <div class="max-w-7xl mx-auto">
    <div class="mb-6">
      <AppButton variant="ghost" @click="goBack">← Back to Agents</AppButton>
    </div>

    <div class="mb-8">
      <h1 class="text-4xl font-bold text-text-primary mb-2">{{ agentName }}</h1>
      <AppBadge variant="warning">Coming Soon</AppBadge>
    </div>

    <AppCard>
      <p class="text-text-secondary">
        Detailed agent information, health metrics, and execution history will be displayed here.
      </p>
    </AppCard>
  </div>
</template>
