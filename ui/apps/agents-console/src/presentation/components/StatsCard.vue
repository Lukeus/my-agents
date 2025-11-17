<script setup lang="ts">
import { AppCard } from '@agents/design-system';

interface Props {
  title: string;
  value: string | number;
  subtitle?: string;
  icon?: string;
  trend?: {
    value: number;
    direction: 'up' | 'down' | 'neutral';
  };
  loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
});

const trendColor = {
  up: 'text-success-500',
  down: 'text-danger-500',
  neutral: 'text-gray-400',
};

const trendIcon = {
  up: '↑',
  down: '↓',
  neutral: '→',
};
</script>

<template>
  <AppCard class="p-6">
    <div class="flex items-start justify-between">
      <div class="flex-1">
        <p class="text-sm text-[--color-text-secondary] font-medium mb-1">
          {{ title }}
        </p>
        <div v-if="loading" class="h-8 w-24 bg-[--color-surface] animate-pulse rounded-md"></div>
        <p v-else class="text-3xl font-bold text-[--color-text-primary] mb-1">
          {{ value }}
        </p>
        <p v-if="subtitle" class="text-xs text-[--color-text-tertiary]">
          {{ subtitle }}
        </p>
        <div v-if="trend" class="flex items-center gap-1 mt-2">
          <span :class="['text-sm font-medium', trendColor[trend.direction]]">
            {{ trendIcon[trend.direction] }} {{ Math.abs(trend.value) }}%
          </span>
          <span class="text-xs text-[--color-text-tertiary]">vs last period</span>
        </div>
      </div>
      <div v-if="icon" class="text-4xl opacity-40">
        {{ icon }}
      </div>
    </div>
  </AppCard>
</template>
