/**
 * @agents/layout-shell
 * Shared layout components for my-agents applications
 */

export { default as AppShell } from './components/AppShell.vue';
export { default as TopNav } from './components/TopNav.vue';
export { default as Sidebar } from './components/Sidebar.vue';

export type {
  AppShellProps,
  AppInfo,
  AppItem,
  NavItem,
  HealthStatus,
} from './components/AppShell.vue';
export type { TopNavProps } from './components/TopNav.vue';
export type { SidebarProps } from './components/Sidebar.vue';
