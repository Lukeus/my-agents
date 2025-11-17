<script setup lang="ts">
import { useRoute } from 'vue-router';
import { AppCard, AppButton, AppBadge } from '@agents/design-system';

const route = useRoute();
const specId = route.params.id as string;
</script>

<template>
  <div class="">
    <div class="mb-6">
      <AppButton variant="ghost" @click="$router.push('/specs')">← Back to Specs</AppButton>
    </div>

    <div class="mb-8">
      <h1 class="text-4xl font-bold text-[--color-text-primary] mb-2">Test Spec #{{ specId }}</h1>
      <AppBadge variant="warning">Coming Soon</AppBadge>
    </div>

    <AppCard>
      <p class="text-[--color-text-secondary]">
        Detailed test specification view with BDD scenarios will be displayed here
      </p>
    </AppCard>
  </div>
</template>
