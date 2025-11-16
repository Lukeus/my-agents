<script setup lang="ts">
import { AppCard, AppBadge } from '@agents/design-system';
</script>

<template>
  <div class="max-w-7xl mx-auto">
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-[--color-text-primary] mb-2">Coverage Analysis</h1>
      <p class="text-[--color-text-secondary]">Analyze test coverage for your projects</p>
    </div>

    <AppCard>
      <div class="flex items-center gap-3">
        <AppBadge variant="warning">Coming Soon</AppBadge>
        <p class="text-[--color-text-secondary]">
          Test coverage analysis and visualization will be displayed here
        </p>
      </div>
    </AppCard>
  </div>
</template>
