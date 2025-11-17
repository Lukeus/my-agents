<script setup lang="ts">
export interface AppCardProps {
  hoverable?: boolean;
  padding?: 'none' | 'sm' | 'md' | 'lg';
}

const props = withDefaults(defineProps<AppCardProps>(), {
  hoverable: false,
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
      hoverable ? 'card-hover' : 'card',
      paddingClasses[padding],
    ]"
  >
    <slot />
  </div>
</template>
