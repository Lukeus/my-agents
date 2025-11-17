<script setup lang="ts">
import { computed } from 'vue';

export interface AppBadgeProps {
  variant?: 'default' | 'brand' | 'success' | 'warning' | 'danger' | 'info';
  size?: 'sm' | 'md';
  dot?: boolean;
}

const props = withDefaults(defineProps<AppBadgeProps>(), {
  variant: 'default',
  size: 'md',
  dot: false,
});

const variantClasses = computed(() => {
  const variants = {
    default: 'bg-[--color-surface-elevated] text-[--color-text-secondary] border-[--color-border-subtle]',
    brand: 'bg-[--color-brand-500]/20 text-[--color-brand-100] border-[--color-brand-500]/30',
    success: 'bg-[--color-success-500]/20 text-[--color-success-100] border-[--color-success-500]/30',
    warning: 'bg-[--color-warning-500]/20 text-[--color-warning-100] border-[--color-warning-500]/30',
    danger: 'bg-[--color-danger-500]/20 text-[--color-danger-100] border-[--color-danger-500]/30',
    info: 'bg-[--color-info-500]/20 text-[--color-info-100] border-[--color-info-500]/30',
  };
  return variants[props.variant];
});

const sizeClasses = computed(() => {
  const sizes = {
    sm: 'px-2 py-0.5 text-[10px]',
    md: 'px-2.5 py-1 text-xs',
  };
  return sizes[props.size];
});

const dotColor = computed(() => {
  const colors = {
    default: 'bg-[--color-text-secondary]',
    brand: 'bg-[--color-brand-500]',
    success: 'bg-[--color-success-500]',
    warning: 'bg-[--color-warning-500]',
    danger: 'bg-[--color-danger-500]',
    info: 'bg-[--color-info-500]',
  };
  return colors[props.variant];
});
</script>

<template>
  <span
    :class="[
      'inline-flex items-center gap-1.5 rounded-[--radius-pill] border font-medium uppercase tracking-wide',
      variantClasses,
      sizeClasses,
    ]"
  >
    <span v-if="dot" :class="['h-1.5 w-1.5 rounded-full', dotColor]"></span>
    <slot />
  </span>
</template>
