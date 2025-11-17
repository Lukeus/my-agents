# @agents/layout-shell

Shared layout components following Tailwind UI Application Shell patterns.

## Layouts

### 1. AppShell (Sidebar Layout)

Best for: Apps with primary navigation in a collapsible sidebar.

**Pattern**: [Tailwind UI Sidebar Layouts](https://tailwindcss.com/plus/ui-blocks/application-ui/application-shells/sidebar)

```vue
<template>
  <AppShell
    :current-app="currentApp"
    :available-apps="availableApps"
    :navigation-items="navigationItems"
    :health-status="healthStatus"
  >
    <router-view />
  </AppShell>
</template>

<script setup>
import { AppShell } from '@agents/layout-shell';

const currentApp = { name: 'My App', icon: '🚀' };
const availableApps = [
  { name: 'My App', icon: '🚀', href: '/' },
  { name: 'Other App', icon: '📊', href: '/other' },
];
const navigationItems = [
  { label: 'Dashboard', icon: '📊', route: '/', isActive: true },
  { label: 'Settings', icon: '⚙️', route: '/settings', isActive: false },
];
const healthStatus = { status: 'healthy', message: 'All systems operational' };
</script>
```

**Features:**
- Collapsible sidebar (64px collapsed, 256px expanded)
- Top navigation with app switcher
- Responsive container (max-w-7xl)
- Health status indicator
- Smooth transitions

---

### 2. StackedLayout

Best for: Dashboards and full-width interfaces with top navigation only.

**Pattern**: [Tailwind UI Stacked Layouts](https://tailwindcss.com/plus/ui-blocks/application-ui/application-shells/stacked)

```vue
<template>
  <StackedLayout
    :current-app="currentApp"
    :available-apps="availableApps"
    :health-status="healthStatus"
  >
    <!-- Optional: Page header -->
    <template #header>
      <h1 class="text-3xl font-bold text-[--color-text-primary]">Dashboard</h1>
    </template>

    <!-- Main content -->
    <DashboardStats />
    <RecentActivity />
  </StackedLayout>
</template>

<script setup>
import { StackedLayout } from '@agents/layout-shell';
</script>
```

**Features:**
- Full-width layout
- Top navigation bar
- Optional page header slot
- Responsive containers
- No sidebar - cleaner for dashboards

---

### 3. MultiColumnLayout

Best for: Complex interfaces with primary sidebar + secondary column (e.g., email, settings with preview).

**Pattern**: [Tailwind UI Multi-Column Layouts](https://tailwindcss.com/plus/ui-blocks/application-ui/application-shells/multi-column)

```vue
<template>
  <MultiColumnLayout
    :current-app="currentApp"
    :available-apps="availableApps"
    :navigation-items="navigationItems"
    :health-status="healthStatus"
  >
    <!-- Main content (left/center) -->
    <EmailList />

    <!-- Secondary column (right) -->
    <template #secondary>
      <EmailPreview />
    </template>
  </MultiColumnLayout>
</template>

<script setup>
import { MultiColumnLayout } from '@agents/layout-shell';
</script>
```

**Features:**
- Primary sidebar for main navigation
- Main content area
- Optional secondary column (320px fixed width)
- Independent scroll areas
- Perfect for master-detail interfaces

---

## Components

### Individual Components

You can also use individual components:

```vue
import { Sidebar, TopNav } from '@agents/layout-shell';
```

**Sidebar** - Collapsible navigation sidebar
**TopNav** - Top navigation with app switcher and health status

---

## Responsive Breakpoints

All layouts follow Tailwind's responsive breakpoints:

- `sm`: 640px
- `md`: 768px  
- `lg`: 1024px
- `xl`: 1280px
- `2xl`: 1536px

Containers use `max-w-7xl` (1280px) for optimal readability.

---

## Dark Theme

All layouts use CSS custom properties from `@agents/design-system`:

- `--color-surface` - Main background
- `--color-surface-elevated` - Cards, sidebars, nav bars
- `--color-text-primary` - Primary text
- `--color-brand-500` - Brand accents
- `--color-success-500` - Success states

---

## Best Practices

1. **Choose the right layout:**
   - Sidebar: Multi-section apps with lots of navigation
   - Stacked: Dashboards, single-page focused apps
   - Multi-column: Email, settings, master-detail

2. **Keep navigation reactive:**
   ```ts
   const navigationItems = computed(() => [
     { label: 'Home', icon: '🏠', route: '/', isActive: route.path === '/' }
   ]);
   ```

3. **Use slots for flexibility:**
   - Stacked: Use `#header` slot for page titles
   - Multi-column: Use `#secondary` for detail views

4. **Follow Tailwind UI patterns:**
   - Consistent spacing (px-4 sm:px-6 md:px-8)
   - Responsive design
   - Proper overflow handling
