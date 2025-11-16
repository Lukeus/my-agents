import type { App } from 'vue';
import './tokens.css';

// Import components
import AppButton from './components/AppButton.vue';
import AppCard from './components/AppCard.vue';
import AppInput from './components/AppInput.vue';
import AppBadge from './components/AppBadge.vue';

// Export components
export { AppButton, AppCard, AppInput, AppBadge };

// Export types
export type { AppButtonProps } from './components/AppButton.vue';
export type { AppCardProps } from './components/AppCard.vue';
export type { AppInputProps } from './components/AppInput.vue';
export type { AppBadgeProps } from './components/AppBadge.vue';

// Vue plugin for global registration
export default {
  install(app: App) {
    app.component('AppButton', AppButton);
    app.component('AppCard', AppCard);
    app.component('AppInput', AppInput);
    app.component('AppBadge', AppBadge);
  },
};
