import type { App } from 'vue';
import './tokens.css';

// Import components
import AppButton from './components/AppButton.vue';
import AppCard from './components/AppCard.vue';
import AppInput from './components/AppInput.vue';
import AppBadge from './components/AppBadge.vue';
import AppSpinner from './components/AppSpinner.vue';
import AppAlert from './components/AppAlert.vue';
import AppTable from './components/AppTable.vue';
import AppEmptyState from './components/AppEmptyState.vue';
import AppModal from './components/AppModal.vue';
import AppTextarea from './components/AppTextarea.vue';
import AppSelect from './components/AppSelect.vue';
import AppSwitcher from './components/AppSwitcher.vue';

// Export components
export { AppButton, AppCard, AppInput, AppBadge, AppSpinner, AppAlert, AppTable, AppEmptyState, AppModal, AppTextarea, AppSelect, AppSwitcher };

// Export types
export type { AppButtonProps } from './components/AppButton.vue';
export type { AppCardProps } from './components/AppCard.vue';
export type { AppInputProps } from './components/AppInput.vue';
export type { AppBadgeProps } from './components/AppBadge.vue';
export type { AppSpinnerProps } from './components/AppSpinner.vue';
export type { AppAlertProps } from './components/AppAlert.vue';
export type { AppTableProps } from './components/AppTable.vue';
export type { AppEmptyStateProps } from './components/AppEmptyState.vue';
export type { AppModalProps } from './components/AppModal.vue';
export type { AppTextareaProps } from './components/AppTextarea.vue';
export type { AppSelectProps, SelectOption } from './components/AppSelect.vue';
export type { AppItem } from './components/AppSwitcher.vue';

// Vue plugin for global registration
export const designSystemPlugin = {
  install(app: App) {
    app.component('AppButton', AppButton);
    app.component('AppCard', AppCard);
    app.component('AppInput', AppInput);
    app.component('AppBadge', AppBadge);
    app.component('AppSpinner', AppSpinner);
    app.component('AppAlert', AppAlert);
    app.component('AppTable', AppTable);
    app.component('AppEmptyState', AppEmptyState);
    app.component('AppTextarea', AppTextarea);
    app.component('AppSelect', AppSelect);
    app.component('AppSwitcher', AppSwitcher);
  },
};

// Also export as default for convenience
export default designSystemPlugin;
