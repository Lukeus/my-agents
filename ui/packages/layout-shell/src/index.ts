/**
 * @agents/layout-shell
 * Shared layout components for my-agents applications
 */

// Import CSS to add this package to Vite's build graph (for Tailwind scanning)
import './index.css';

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
  NavItem,
} from './components/AppShell.vue';
export type { AppItem } from '@agents/design-system';
export type { TopNavProps } from './components/TopNav.vue';
export type { SidebarProps } from './components/Sidebar.vue';
export type { StackedLayoutProps } from './components/StackedLayout.vue';
export type { MultiColumnLayoutProps } from './components/MultiColumnLayout.vue';
