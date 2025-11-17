# Layout Selection Guide

Choose the appropriate layout for your use case.

## Quick Reference

| Layout | Best For | Example Apps |
|--------|----------|--------------|
| **AppShell** (Sidebar) | Multi-section apps with lots of navigation | Admin panels, management consoles, multi-feature apps |
| **StackedLayout** | Dashboards, landing pages, single-focus apps | Analytics dashboards, public pages, simple tools |
| **MultiColumnLayout** | Master-detail, preview panes, settings | Email clients, settings with preview, file browsers |

---

## Current App Layouts

### ✅ Agents Console - Using **AppShell** (Sidebar)
**Why**: Multi-section app with agents, runs, settings navigation

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

### ✅ Test Planning Studio - Using **AppShell** (Sidebar)
**Why**: Multiple sections (specs, generate, coverage) requiring navigation

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

---

## When to Use Each Layout

### AppShell (Sidebar Layout) 👈 **Current Default**

**Use when:**
- App has 4+ main sections
- Users need quick access to multiple areas
- Building admin/management interface
- Navigation is primary user action

**Example:** 
```
├── Dashboard
├── Agents
├── Runs  
├── Settings
└── Documentation
```

**Pattern**: [Tailwind UI Sidebar](https://tailwindcss.com/plus/ui-blocks/application-ui/application-shells/sidebar)

---

### StackedLayout

**Use when:**
- Single focused page/dashboard
- Minimal navigation needed
- Want maximum content width
- Public-facing pages

**Example Use Cases:**
- Analytics dashboard (just viewing data)
- Landing/marketing pages
- Simple calculation tools
- Status pages

**Switch to StackedLayout:**
```vue
<StackedLayout
  :current-app="currentApp"
  :health-status="healthStatus"
>
  <template #header>
    <h1>System Dashboard</h1>
    <p>Real-time monitoring</p>
  </template>

  <DashboardStats />
  <Charts />
</StackedLayout>
```

**Pattern**: [Tailwind UI Stacked](https://tailwindcss.com/plus/ui-blocks/application-ui/application-shells/stacked)

---

### MultiColumnLayout

**Use when:**
- Master-detail interface
- Preview pane needed
- Settings with live preview
- List + detail view

**Example Use Cases:**
- Email interface (list + message preview)
- File browser with preview
- Settings with live changes
- Code editor with preview

**Switch to MultiColumnLayout:**
```vue
<MultiColumnLayout
  :current-app="currentApp"
  :navigation-items="navigationItems"
  :health-status="healthStatus"
>
  <!-- Main content: List view -->
  <EmailInbox />

  <!-- Secondary column: Preview -->
  <template #secondary>
    <EmailPreview />
  </template>
</MultiColumnLayout>
```

**Pattern**: [Tailwind UI Multi-Column](https://tailwindcss.com/plus/ui-blocks/application-ui/application-shells/multi-column)

---

## Migration Examples

### From AppShell to StackedLayout

**Before** (sidebar with navigation):
```vue
<AppShell :navigation-items="navItems">
  <DashboardPage />
</AppShell>
```

**After** (full-width, no sidebar):
```vue
<StackedLayout>
  <template #header>
    <h1>Dashboard</h1>
  </template>
  <DashboardPage />
</StackedLayout>
```

### From AppShell to MultiColumnLayout

**Before** (single content area):
```vue
<AppShell>
  <router-view />
</AppShell>
```

**After** (with preview column):
```vue
<MultiColumnLayout>
  <router-view />
  <template #secondary>
    <PreviewPanel />
  </template>
</MultiColumnLayout>
```

---

## Best Practices

1. **Start with AppShell** - It's the most flexible for apps that will grow
2. **Use StackedLayout** for single-purpose pages or dashboards
3. **Use MultiColumnLayout** only when you have clear master-detail UI
4. **Don't mix layouts** in the same app unless you have a good reason
5. **Mobile first** - All layouts are responsive, but test on mobile

---

## Component Imports

```ts
import {
  AppShell,
  StackedLayout,
  MultiColumnLayout
} from '@agents/layout-shell';
```

---

## Questions?

**"Should I use sidebar or stacked for my dashboard?"**
- If users need to navigate to other pages → Sidebar (AppShell)
- If it's just viewing data, no navigation → Stacked

**"When do I need multi-column?"**
- Only when you have a clear left-content + right-preview pattern
- Examples: Email, file browser, settings with preview

**"Can I switch layouts mid-app?"**
- Yes, but avoid it. Pick one layout pattern per app for consistency.
