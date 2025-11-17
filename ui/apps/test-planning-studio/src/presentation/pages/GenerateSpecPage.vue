<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { AppCard, AppButton, AppInput } from '@agents/design-system';
import { useGenerateSpec } from '@/application/usecases/useGenerateSpec';

const router = useRouter();
const { generatedSpec, generating, error, generateSpec } = useGenerateSpec();

const featureName = ref('');
const featureDescription = ref('');
const strategy = ref<'bdd' | 'tdd' | 'e2e' | 'unit' | 'integration'>('bdd');

const handleGenerate = async () => {
  const result = await generateSpec({
    featureName: featureName.value,
    featureDescription: featureDescription.value,
    testingStrategy: strategy.value,
  });

  if (result) {
    // Navigate to the generated spec detail
    router.push(`/specs/${result.id}`);
  }
};
</script>

<template>
  <div class="max-w-4xl mx-auto">
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-[--color-text-primary] mb-2">Generate Test Specification</h1>
      <p class="text-[--color-text-secondary]">
        Use AI to automatically generate comprehensive test specifications
      </p>
    </div>

    <!-- Error State -->
    <div
      v-if="error"
      class="mb-8 p-4 bg-[--color-danger-50] border border-[--color-danger-200] rounded-[--radius-lg] text-[--color-danger-700]"
    >
      {{ error }}
    </div>

    <AppCard>
      <form @submit.prevent="handleGenerate" class="space-y-6">
        <!-- Feature Name -->
        <div>
          <label class="block text-sm font-medium text-[--color-text-primary] mb-2">
            Feature Name *
          </label>
          <AppInput
            v-model="featureName"
            placeholder="e.g., User Authentication"
            required
          />
        </div>

        <!-- Feature Description -->
        <div>
          <label class="block text-sm font-medium text-[--color-text-primary] mb-2">
            Feature Description *
          </label>
          <textarea
            v-model="featureDescription"
            class="w-full rounded-[--radius-md] bg-[--color-surface] px-3 py-2 text-sm border border-[--color-border-subtle] text-[--color-text-primary] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[--color-brand-500] min-h-32"
            placeholder="Describe the feature and what needs to be tested..."
            required
          ></textarea>
        </div>

        <!-- Testing Strategy -->
        <div>
          <label class="block text-sm font-medium text-[--color-text-primary] mb-2">
            Testing Strategy
          </label>
          <select
            v-model="strategy"
            class="w-full rounded-[--radius-md] bg-[--color-surface] px-3 py-2 text-sm border border-[--color-border-subtle] text-[--color-text-primary] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[--color-brand-500]"
          >
            <option value="bdd">BDD (Behavior-Driven Development)</option>
            <option value="tdd">TDD (Test-Driven Development)</option>
            <option value="e2e">E2E (End-to-End)</option>
            <option value="unit">Unit Testing</option>
            <option value="integration">Integration Testing</option>
          </select>
        </div>

        <!-- Actions -->
        <div class="flex items-center gap-4">
          <AppButton
            type="submit"
            variant="primary"
            :disabled="generating || !featureName || !featureDescription"
          >
            {{ generating ? 'Generating...' : '✨ Generate Test Spec' }}
          </AppButton>
          <AppButton type="button" variant="ghost" @click="$router.push('/')">
            Cancel
          </AppButton>
        </div>
      </form>
    </AppCard>

    <!-- Generated Result Preview -->
    <AppCard v-if="generatedSpec" class="mt-8">
      <h2 class="text-2xl font-semibold text-[--color-text-primary] mb-4">✅ Generated Successfully!</h2>
      <p class="text-[--color-text-secondary] mb-4">
        Test specification for "{{ generatedSpec.feature }}" has been generated.
      </p>
      <AppButton @click="$router.push(`/specs/${generatedSpec.id}`)">
        View Specification
      </AppButton>
    </AppCard>
  </div>
</template>
