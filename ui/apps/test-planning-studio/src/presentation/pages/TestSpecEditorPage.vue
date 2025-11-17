<script setup lang="ts">
import { AppCard, AppButton, AppBadge } from '@agents/design-system';
</script>

<template>
  <div class="max-w-7xl mx-auto">
    <div class="mb-6">
      <AppButton variant="ghost" @click="$router.push('/specs')">← Back to Specs</AppButton>
    </div>

    <div class="mb-8">
      <h1 class="text-4xl font-bold text-[--color-text-primary] mb-2">Edit Test Spec</h1>
      <AppBadge variant="warning">Coming Soon</AppBadge>
    </div>

    <AppCard>
      <p class="text-[--color-text-secondary]">
        Test specification editor with form fields will be displayed here
      </p>
    </AppCard>
  </div>
</template>
