<script setup lang="ts">
import { AppCard, AppBadge } from '@agents/design-system';
</script>

<template>
  <div class="max-w-7xl mx-auto">
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-text-primary mb-2">Agent Runs</h1>
      <p class="text-text-secondary">View execution history and results</p>
    </div>

    <AppCard>
      <div class="flex items-center gap-3">
        <AppBadge variant="warning">Coming Soon</AppBadge>
        <p class="text-text-secondary">
          Agent execution history and run details will be displayed here.
        </p>
      </div>
    </AppCard>
  </div>
</template>
