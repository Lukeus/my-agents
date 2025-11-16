/**
 * @agents/layout-shell
 * Shared layout components for my-agents applications
 */

// Layout Shells
export { default as AppShell } from './components/AppShell.vue';
export { default as StackedLayout } from './components/StackedLayout.vue';
export { default as MultiColumnLayout } from './components/MultiColumnLayout.vue';

// Individual Components
export { default as TopNav } from './components/TopNav.vue';
export { default as Sidebar } from './components/Sidebar.vue';

// Types
export type {
  AppShellProps,
  AppInfo,
  AppItem,
  NavItem,
  HealthStatus,
} from './components/AppShell.vue';
export type { TopNavProps } from './components/TopNav.vue';
export type { SidebarProps } from './components/Sidebar.vue';
export type { StackedLayoutProps } from './components/StackedLayout.vue';
export type { MultiColumnLayoutProps } from './components/MultiColumnLayout.vue';
