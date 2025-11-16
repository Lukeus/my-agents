<script setup lang="ts">
import { computed } from 'vue';

export interface AppButtonProps {
  variant?: 'primary' | 'ghost' | 'danger' | 'success';
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  loading?: boolean;
  type?: 'button' | 'submit' | 'reset';
}

const props = withDefaults(defineProps<AppButtonProps>(), {
  variant: 'primary',
  size: 'md',
  disabled: false,
  loading: false,
  type: 'button',
});

const variantClasses = computed(() => {
  const variants = {
    primary: 'bg-[--color-brand-500] hover:bg-[--color-brand-600] text-white shadow-sm',
    ghost: 'bg-transparent border border-[--color-border-subtle] hover:bg-[--color-surface-elevated] text-[--color-text-primary]',
    danger: 'bg-[--color-danger-500] hover:bg-[--color-danger-600] text-white shadow-sm',
    success: 'bg-[--color-success-500] hover:bg-[--color-success-600] text-white shadow-sm',
  };
  return variants[props.variant];
});

const sizeClasses = computed(() => {
  const sizes = {
    sm: 'px-3 py-1.5 text-xs',
    md: 'px-4 py-2 text-sm',
    lg: 'px-5 py-3 text-base',
  };
  return sizes[props.size];
});
</script>

<template>
  <button
    :type="type"
    :disabled="disabled || loading"
    class="btn-base"
    :class="[variantClasses, sizeClasses]"
  >
    <span v-if="loading" class="inline-block h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent"></span>
    <slot />
  </button>
</template>
