<script setup lang="ts">
import { AppCard, AppBadge } from '@agents/design-system';
</script>

<template>
  <div class="">
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-[--color-text-primary] mb-2">Test Specifications</h1>
      <p class="text-[--color-text-secondary]">Browse and manage all test specs</p>
    </div>

    <AppCard>
      <div class="flex items-center gap-3">
        <AppBadge variant="warning">Coming Soon</AppBadge>
        <p class="text-[--color-text-secondary]">Test specifications list will be displayed here</p>
      </div>
    </AppCard>
  </div>
</template>
