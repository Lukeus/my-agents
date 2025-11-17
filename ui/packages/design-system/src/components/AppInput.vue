<script setup lang="ts">
export interface AppInputProps {
  modelValue?: string | number;
  type?: 'text' | 'email' | 'password' | 'number' | 'search' | 'tel' | 'url';
  placeholder?: string;
  disabled?: boolean;
  error?: string;
  label?: string;
  id?: string;
}

const props = withDefaults(defineProps<AppInputProps>(), {
  type: 'text',
  disabled: false,
});

const emit = defineEmits<{
  'update:modelValue': [value: string | number];
}>();

const handleInput = (event: Event) => {
  const target = event.target as HTMLInputElement;
  emit('update:modelValue', target.value);
};
</script>

<template>
  <div class="flex flex-col gap-1.5">
    <label v-if="label" :for="id" class="text-sm font-medium text-[--color-text-primary]">
      {{ label }}
    </label>
    <input
      :id="id"
      :type="type"
      :value="modelValue"
      :placeholder="placeholder"
      :disabled="disabled"
      :class="[
        'input-base',
        error ? 'border-[--color-danger-500] focus-visible:ring-[--color-danger-500]' : '',
      ]"
      @input="handleInput"
    />
    <span v-if="error" class="text-xs text-[--color-danger-500]">
      {{ error }}
    </span>
  </div>
</template>
