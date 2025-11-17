<script setup lang="ts">
export interface AppCardProps {
  hover?: boolean;
  padding?: 'none' | 'sm' | 'md' | 'lg';
}

const props = withDefaults(defineProps<AppCardProps>(), {
  hover: false,
  padding: 'md',
});

const paddingClasses = {
  none: 'p-0',
  sm: 'p-3',
  md: 'p-4',
  lg: 'p-6',
};
</script>

<template>
  <div
    :class="[
      hover ? 'card-hover' : 'card',
      paddingClasses[padding],
    ]"
  >
    <slot />
  </div>
</template>
