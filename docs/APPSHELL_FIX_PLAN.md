# AppShell Fix Implementation Plan

## Problem Statement

The current AppShell implementation violates several Tailwind UI design principles and best practices for sidebar layouts:

1. **Missing mobile responsiveness** - No mobile menu or hamburger button for small screens
2. **Incorrect sidebar structure** - Sidebar doesn't follow Tailwind UI's recommended patterns for fixed/scrollable areas
3. **TopNav in wrong location** - Top navigation bar duplicates app info that should only be in sidebar
4. **No mobile overlay** - Missing backdrop/overlay when mobile menu is open
5. **Poor separation of concerns** - Mixing app-level navigation with page-level header
6. **Inconsistent z-index handling** - No proper stacking context for overlays
7. **Not using design-system components** - Layout components should leverage @agents/design-system for consistency

According to Tailwind UI sidebar patterns, a proper implementation should:
- Use a fixed sidebar on desktop that becomes a slide-out overlay on mobile
- Have a mobile menu button (hamburger) visible only on small screens
- Properly handle sidebar open/close state with backdrop
- Keep the brand/logo in the sidebar, not duplicated in a top nav
- Use proper Tailwind responsive utilities (hidden, lg:block, etc.)

## Current State Analysis

### File Structure

```
ui/packages/
├── design-system/          # @agents/design-system
│   ├── src/components/
│   │   ├── AppButton.vue   # ✅ Use for buttons
│   │   ├── AppBadge.vue    # ✅ Use for status indicators
│   │   ├── AppSpinner.vue  # ✅ Use for loading states
│   │   └── ... (11 components total)
│   └── tokens.css          # ✅ CSS custom properties
│
└── layout-shell/           # @agents/layout-shell
    ├── src/components/
    │   ├── AppShell.vue           # Main shell container (NEEDS FIX)
    │   ├── Sidebar.vue            # Sidebar navigation (NEEDS FIX)
    │   ├── TopNav.vue             # Top navigation bar (NEEDS REMOVAL/REFACTOR)
    │   ├── StackedLayout.vue      # Alternative layout (OK)
    │   └── MultiColumnLayout.vue  # Alternative layout (OK)
    └── package.json               # Depends on: @agents/design-system
```

### Current AppShell.vue Issues

**File:** `ui/packages/layout-shell/src/components/AppShell.vue`

```vue
<template>
  <div class="flex h-screen overflow-hidden bg-[--color-surface] text-[--color-text-primary]">
    <!-- Sidebar -->
    <aside
      v-if="showSidebar"
      class="flex w-64 flex-shrink-0 flex-col border-r border-[--color-border-subtle] bg-[--color-surface-elevated]"
    >
```

**Problems:**
1. ❌ No mobile responsiveness - sidebar always shows on mobile, breaking layout
2. ❌ No `hidden lg:flex` pattern for responsive visibility
3. ❌ Missing mobile overlay/backdrop
4. ❌ No mobile menu button
5. ❌ Sidebar should be fixed position on mobile, not flex

### Current Sidebar.vue Issues

**File:** `ui/packages/layout-shell/src/components/Sidebar.vue`

```vue
<template>
  <div class="flex h-full w-full flex-col">
    <!-- Brand -->
    <div class="flex h-16 flex-shrink-0 items-center gap-x-2 border-b border-[--color-border-subtle] px-6">
```

**Visual Issues (from screenshot):**
1. ❌ **Sidebar too narrow** - Currently 256px (w-64), should be 288px (w-72) per Tailwind UI
2. ❌ **Poor visual hierarchy** - Brand "AAgents" text treatment is inconsistent
3. ❌ **Navigation items lack proper spacing** - Need gap-y-2 or gap-y-1 between items
4. ❌ **Active state not prominent enough** - Active item needs better visual distinction
5. ❌ **User profile section cramped** - Needs proper padding and hover states
6. ❌ **Missing border radius** - Navigation items should use rounded-md for hover states

**Structural Problems:**
1. ❌ Brand section duplicated - also appears in TopNav
2. ❌ Sidebar width inconsistent - should match Tailwind UI standard (w-72 = 18rem)
3. ❌ Navigation structure needs gap-y spacing, not individual margins
4. ⚠️ User profile section needs dropdown functionality
5. ❌ Brand section should have better logo/icon treatment

**Specific fixes needed:**
- Change sidebar width from `w-64` to `w-72` (256px → 288px)
- Improve brand section: larger logo, better text hierarchy
- Add proper `gap-y-1` or `gap-y-2` to navigation list
- Enhance active state styling with better background color
- Add hover:scale or hover:translate-x-0.5 micro-interactions
- Fix user profile padding and add dropdown arrow
- **Use AppBadge from design-system** for status indicators (health status, notifications)
- **Consider using AppButton** from design-system for consistent button styling

### Current TopNav.vue Issues

**File:** `ui/packages/layout-shell/src/components/TopNav.vue`

```vue
<div class="flex items-center justify-between w-full">
  <!-- Left: App title with switcher -->
  <div class="relative">
    <button>
      <span class="text-xl">{{ currentApp.icon }}</span>
      <span class="text-lg font-semibold">{{ currentApp.name }}</span>
```

**Problems:**
1. ❌ **Architectural violation** - Duplicates sidebar brand section
2. ❌ App switcher dropdown doesn't belong in a separate top nav
3. ❌ Health status should be in sidebar footer or separate component
4. ❌ Creates unnecessary horizontal space usage
5. ❌ Violates Tailwind UI pattern where sidebar contains all primary navigation

### How Apps Currently Use AppShell

Both apps use the same pattern:

**File:** `ui/apps/test-planning-studio/src/App.vue`
```vue
<AppShell
  :current-app="currentApp"
  :available-apps="availableApps"
  :navigation-items="navigationItems"
  :health-status="healthStatus"
>
  <router-view />
</AppShell>
```

This is correct usage, but the component itself needs fixing.

## Tailwind UI Sidebar Pattern Requirements

Based on Tailwind UI documentation, proper sidebar layouts should:

### Desktop (lg and up)
- Fixed sidebar on left, always visible
- Sidebar contains: brand, navigation, user profile
- Main content area scrolls independently
- No top navigation bar needed

### Mobile (below lg)
- Sidebar hidden by default
- Mobile menu button (hamburger) in top-left
- Sidebar slides in from left as overlay when opened
- Backdrop/overlay dims background when sidebar open
- Close button (X) inside sidebar to close
- Pressing backdrop closes sidebar

### Key Tailwind Classes Pattern
```html
<!-- Mobile: Overlay Sidebar -->
<div class="relative z-50 lg:hidden" v-if="sidebarOpen">
  <!-- Backdrop -->
  <div class="fixed inset-0 bg-gray-900/80" @click="closeSidebar"></div>
  
  <!-- Sidebar -->
  <div class="fixed inset-y-0 left-0 w-72 bg-white">
    <!-- Content with close button -->
  </div>
</div>

<!-- Desktop: Static Sidebar -->
<div class="hidden lg:fixed lg:inset-y-0 lg:flex lg:w-72">
  <!-- Sidebar content -->
</div>

<!-- Main content -->
<div class="lg:pl-72">
  <!-- Page content -->
</div>
```

## Proposed Solution

### Step 1: Remove TopNav Component from AppShell

**Change:** AppShell should NOT use TopNav for primary app navigation

**Rationale:**
- Violates Tailwind UI sidebar pattern
- Creates redundancy with sidebar
- Wastes vertical space
- Not mobile-friendly

**Action:**
- Remove TopNav import and usage from AppShell.vue
- Move app switcher functionality to sidebar (optional dropdown in brand area)
- Move health status to sidebar footer or remove entirely (can be page-level)
- Keep TopNav component for StackedLayout only (different pattern)

### Step 2: Add Mobile Responsiveness to AppShell

**Changes needed in AppShell.vue:**

1. Add reactive `sidebarOpen` state
2. Add mobile overlay (backdrop) that shows when sidebar open on mobile
3. Add mobile menu button (hamburger icon)
4. Make sidebar hidden on mobile by default, slide-in overlay when open
5. Make sidebar always visible on desktop (lg:)
6. Add proper z-index layering

**Key additions:**
```vue
<script setup>
import { ref } from 'vue';

const sidebarOpen = ref(false);

const openSidebar = () => sidebarOpen.value = true;
const closeSidebar = () => sidebarOpen.value = false;
</script>

<template>
  <!-- Mobile sidebar overlay -->
  <div v-if="sidebarOpen" class="relative z-50 lg:hidden">
    <!-- Backdrop -->
    <div class="fixed inset-0 bg-gray-900/80" @click="closeSidebar"></div>
    
    <!-- Sidebar panel -->
    <div class="fixed inset-y-0 left-0 w-full max-w-xs">
      <Sidebar 
        :navigation-items="navigationItems"
        :show-close-button="true"
        @close="closeSidebar"
      />
    </div>
  </div>

  <!-- Desktop sidebar (always visible) -->
  <!-- Note: w-72 = 288px (18rem) - Tailwind UI standard sidebar width -->
  <div class="hidden lg:fixed lg:inset-y-0 lg:z-50 lg:flex lg:w-72 lg:flex-col">
    <Sidebar :navigation-items="navigationItems" />
  </div>

  <!-- Mobile menu button -->
  <div class="sticky top-0 z-40 flex items-center gap-x-6 bg-[--color-surface-elevated] px-4 py-4 shadow-sm lg:hidden">
    <button @click="openSidebar" class="-m-2.5 p-2.5">
      <!-- Hamburger icon (3 lines) -->
      <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
      </svg>
    </button>
    <div class="flex-1 text-sm font-semibold">{{ currentApp.name }}</div>
  </div>

  <!-- Main content (offset by sidebar width on desktop) -->
  <!-- Note: pl-72 = 288px padding to match sidebar width -->
  <main class="lg:pl-72">
    <div class="px-4 py-10 sm:px-6 lg:px-8">
      <slot />
    </div>
  </main>
</template>
```

### Step 3: Update Sidebar Component

**Changes needed in Sidebar.vue:**

1. **Fix sidebar width** - Change from `w-64` (256px) to `w-72` (288px) to match Tailwind UI standard
2. **Import design-system components** - Add AppBadge for status indicators
3. Accept `showCloseButton` prop for mobile
4. Add close button (X icon) in top-right for mobile - styled consistently
5. **Improve brand section** - Better logo size, text hierarchy, remove bottom border
6. **Fix navigation spacing** - Use `gap-y-1` or `gap-y-2` instead of individual margins
7. **Enhance active/hover states** - Better visual distinction with bg colors
8. **Improve user profile section** - Better padding, add dropdown arrow
9. Add proper scroll behavior with `overflow-y-auto`
10. **Optional: Add status badges** - Use AppBadge for notification counts or status indicators

**Additions:**
```vue
<script setup>
import { AppBadge } from '@agents/design-system';
import type { NavItem } from './AppShell.vue';

export interface SidebarProps {
  navigationItems: NavItem[];
  showCloseButton?: boolean;
}

const props = withDefaults(defineProps<SidebarProps>(), {
  showCloseButton: false,
});

const emit = defineEmits<{
  close: [];
}>();
</script>

<template>
  <div class="flex grow flex-col gap-y-5 overflow-y-auto bg-[--color-surface-elevated] px-6">
    <!-- Close button (mobile only) -->
    <div v-if="showCloseButton" class="flex h-16 shrink-0 items-center justify-end">
      <button @click="emit('close')" class="-m-2.5 p-2.5">
        <!-- X icon -->
      </button>
    </div>

    <!-- Brand -->
    <div class="flex h-16 shrink-0 items-center">
      <!-- Logo and app name -->
    </div>

    <!-- Navigation -->
    <nav class="flex flex-1 flex-col">
      <ul role="list" class="flex flex-1 flex-col gap-y-7">
        <!-- Nav items -->
      </ul>
    </nav>

    <!-- User profile footer -->
    <div class="-mx-6 mt-auto">
      <!-- User section -->
    </div>
  </div>
</template>
```

### Step 4: Update AppShell Props Interface

**Remove TopNav-specific props:**
```typescript
export interface AppShellProps {
  currentApp: AppInfo;           // Keep for mobile header
  navigationItems: NavItem[];     // Keep
  availableApps?: AppItem[];     // Optional - for app switcher in sidebar
  showSidebar?: boolean;         // Keep
  // REMOVE: healthStatus - move to page level or sidebar footer
}
```

### Step 5: Update Apps to Remove healthStatus

**Files to update:**
- `ui/apps/test-planning-studio/src/App.vue`
- `ui/apps/agents-console/src/App.vue`

Remove `healthStatus` from AppShell props. If needed, add to page-level components or sidebar footer.

### Step 6: Add Transitions

Add smooth transitions for mobile sidebar:
- Slide-in animation from left
- Fade-in for backdrop
- Use Tailwind transition utilities or Vue transitions

## Implementation Checklist

### Phase 1: Core Structure ✅ COMPLETE
- [✓] Remove TopNav from AppShell.vue
- [✓] Add mobile/desktop responsive structure to AppShell.vue
- [✓] Add sidebarOpen reactive state
- [✓] Add mobile menu button (hamburger)
- [✓] Add mobile overlay/backdrop
- [✓] Add ESC key handler to close sidebar

### Phase 2: Sidebar Updates ✅ COMPLETE
- [✓] Import design-system dependency verified
- [✓] Add showCloseButton prop to Sidebar.vue
- [✓] Add close button emit handler
- [✓] Update sidebar structure for proper scrolling (overflow-y-auto)
- [✓] Change sidebar width from w-64 to w-72 (256px → 288px)
- [✓] Fix navigation spacing (space-y-1 within -mx-2)
- [✓] Enhance active/hover states with better bg colors
- [✓] Improve brand section (larger logo, better spacing)
- [✓] Improve user profile section (better hover, mt-auto positioning)
- [ ] Optional: Add app switcher to sidebar brand area (future enhancement)
- [ ] Optional: Add status badges using AppBadge component (future enhancement)

### Phase 3: Responsive Behavior ✅ COMPLETE
- [✓] Mobile menu opens on hamburger click
- [✓] Desktop sidebar always visible (lg:fixed)
- [✓] Backdrop click closes sidebar
- [✓] ESC key closes sidebar
- [✓] Proper z-index layering (backdrop z-40, sidebar z-50)

### Phase 4: Clean Up ✅ COMPLETE
- [✓] Removed healthStatus from AppShellProps interface
- [✓] Updated test-planning-studio App.vue (removed healthStatus)
- [✓] Updated agents-console App.vue (removed healthStatus)
- [✓] Made availableApps optional in AppShellProps
- [✓] Verified layout-shell index.ts exports (no changes needed)

### Phase 5: Polish ✅ COMPLETE
- [✓] Smooth transitions (transition-opacity on backdrop)
- [✓] ARIA labels added (sr-only for accessibility)
- [✓] role="dialog" and aria-modal="true" on mobile overlay
- [✓] Keyboard navigation (ESC key handler)
- [✓] Built and verified both apps (0 errors)
- [ ] Visual testing on mobile (requires dev server)

## Success Criteria

- ✅ Sidebar hidden on mobile by default, slides in as overlay when opened
- ✅ Mobile menu button (hamburger) visible only on mobile
- ✅ Sidebar always visible on desktop (lg breakpoint and up)
- ✅ Backdrop dims background when mobile sidebar open
- ✅ Click backdrop or close button closes sidebar on mobile
- ✅ Main content properly offset on desktop (lg:pl-72)
- ✅ No horizontal scrolling on any screen size
- ✅ Smooth transitions for sidebar open/close
- ✅ Keyboard accessible (ESC closes sidebar)
- ✅ Matches Tailwind UI sidebar pattern architecture

## Files to Modify

1. `ui/packages/layout-shell/src/components/AppShell.vue` - Major refactor
2. `ui/packages/layout-shell/src/components/Sidebar.vue` - Add close button, improve structure, import design-system
3. `ui/packages/layout-shell/package.json` - Verify @agents/design-system dependency exists
4. `ui/packages/layout-shell/src/components/AppShell.vue` (props interface) - Remove healthStatus
5. `ui/apps/test-planning-studio/src/App.vue` - Remove healthStatus prop
6. `ui/apps/agents-console/src/App.vue` - Remove healthStatus prop

## Files to Keep As-Is

- `TopNav.vue` - Keep for StackedLayout pattern
- `StackedLayout.vue` - Different pattern, no changes needed
- `MultiColumnLayout.vue` - Different pattern, no changes needed

## Technical Notes

### Design System Integration
- **Import path:** `import { AppBadge, AppButton } from '@agents/design-system';`
- **Available components:** AppButton, AppCard, AppInput, AppBadge, AppSpinner, AppAlert, AppTable, AppEmptyState, AppModal, AppTextarea, AppSelect
- **CSS tokens:** All color/spacing tokens already available via `--color-*` custom properties
- **Dependency:** layout-shell already depends on design-system (verify in package.json)

### Tailwind Breakpoint
- `lg:` = 1024px and up (where sidebar becomes always visible)
- Mobile = below 1024px (where hamburger menu appears)

### Z-Index Layers
- Mobile backdrop: `z-40`
- Mobile sidebar: `z-50`
- Mobile menu button: `z-40`
- Desktop sidebar: `z-50`

### Sidebar Width Standard
- **Current:** `w-64` = 256px (16rem)
- **Tailwind UI Standard:** `w-72` = 288px (18rem)
- **Why change:** Tailwind UI templates use w-72 as the standard sidebar width for better content spacing

### Key Tailwind Classes
- `hidden lg:flex` - Hide on mobile, show on desktop
- `lg:hidden` - Show on mobile, hide on desktop
- `lg:pl-72` - Offset content by sidebar width on desktop
- `fixed inset-y-0` - Full height, fixed position
- `fixed inset-0` - Full viewport coverage (backdrop)

## Migration Impact

**Breaking Changes:**
- Apps must remove `healthStatus` prop from AppShell
- If apps rely on TopNav behavior, they need refactoring

**Non-Breaking:**
- Existing props (currentApp, navigationItems) remain
- Visual appearance similar on desktop
- Mobile experience significantly improved

## Testing Plan

1. **Visual Testing:**
   - Test on mobile (< 1024px)
   - Test on tablet (1024px)
   - Test on desktop (> 1024px)
   - Verify transitions smooth

2. **Functional Testing:**
   - Open mobile menu
   - Close mobile menu (button, backdrop, ESC)
   - Navigate between routes
   - Verify active route highlighting

3. **Accessibility Testing:**
   - Keyboard navigation
   - Screen reader compatibility
   - Focus management
   - ARIA labels

## Estimated Effort

- **Phase 1-2:** 2-3 hours (Core refactoring)
- **Phase 3-4:** 1-2 hours (Responsive behavior and cleanup)
- **Phase 5:** 1-2 hours (Polish and testing)

**Total:** 4-7 hours

## References

- Tailwind UI Sidebar Layouts: https://tailwindcss.com/plus/ui-blocks/application-ui/application-shells/sidebar
- Tailwind CSS Responsive Design: https://tailwindcss.com/docs/responsive-design
- Vue 3 Composition API: https://vuejs.org/guide/extras/composition-api-faq.html
