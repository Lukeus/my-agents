# Tailwind 4 Sidebar Layout Fix - Final Implementation Plan

## Problem Statement

The sidebar header "Test Planning Studio" is overlaying the sidebar brand "AAgents" instead of appearing beside it. Current implementation mixes scoped CSS with Tailwind utilities, causing layout conflicts.

## Architecture Decision

✅ **Pure Tailwind 4 utility-first approach**  
❌ **NO scoped CSS** in layout components  
❌ **NO @apply directive** (not supported in Tailwind 4)  
❌ **NO custom CSS classes** for layout  
✅ **All styling via utility classes in templates**

## Critical Corrections Applied

### 1. Mobile Close Button Position ✅
- **Changed**: `absolute left-0 top-0 -mr-12` → `absolute -right-12 top-0`
- **Why**: Close button must appear RIGHT of sidebar panel, outside the container

### 2. Z-Index Layering ✅
- **Added**: `z-40` to backdrop div
- **Stack order**: Backdrop (z-40) < Sidebar panel (z-50)

### 3. .sr-only is Built-In ✅
- **Removed**: Step 3 about adding `.sr-only` to tokens.css
- **Why**: Tailwind 4 includes it by default

### 4. Focus States Enhancement ✅
- **Changed**: All `focus:` → `focus-visible:`
- **Added**: `transition-colors` to interactive elements
- **Why**: Better UX - focus ring only for keyboard navigation

### 5. Semantic HTML ✅
- **Changed**: App title from `<div>` → `<h1>`
- **Added**: `aria-hidden="true"` to decorative icons

## Files to Modify

### 1. AppShell.vue
**File**: `ui/packages/layout-shell/src/components/AppShell.vue`

**Remove**: Lines 128-207 (entire `<style scoped>` block)

**Replace template** (lines 59-125):

```vue
<template>
  <div class="min-h-screen bg-[--color-surface]">
    
    <!-- Mobile sidebar overlay -->
    <div v-if="sidebarOpen && showSidebar" 
         class="lg:hidden relative z-50" 
         role="dialog" 
         aria-modal="true">
      
      <!-- Backdrop with z-index -->
      <div class="fixed inset-0 bg-gray-900/80 z-40" @click="closeSidebar"></div>
      
      <!-- Sidebar panel -->
      <div class="fixed inset-0 flex z-50">
        <div class="relative mr-16 flex w-full max-w-xs flex-1">
          
          <!-- Close button - positioned RIGHT of sidebar -->
          <div class="absolute -right-12 top-0 pt-2">
            <button 
              type="button" 
              class="ml-1 flex h-10 w-10 items-center justify-center rounded-full focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-white"
              @click="closeSidebar"
            >
              <span class="sr-only">Close sidebar</span>
              <svg class="h-6 w-6 text-white" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          
          <Sidebar :navigation-items="navigationItems" />
        </div>
      </div>
    </div>

    <!-- Desktop sidebar - PURE TAILWIND -->
    <div 
      v-if="showSidebar" 
      class="hidden lg:fixed lg:inset-y-0 lg:left-0 lg:z-50 lg:flex lg:w-[18rem] lg:flex-col lg:border-r lg:border-[--color-border-subtle] lg:bg-[--color-surface-elevated]"
    >
      <Sidebar :navigation-items="navigationItems" />
    </div>

    <!-- Content wrapper with sidebar offset -->
    <div v-if="showSidebar" class="lg:pl-[18rem]">
      
      <!-- Mobile header (menu button + title) -->
      <div class="sticky top-0 z-40 flex h-16 shrink-0 items-center gap-x-4 border-b border-[--color-border-subtle] bg-[--color-surface-elevated] px-4 shadow-sm sm:gap-x-6 sm:px-6 lg:hidden">
        <button 
          type="button" 
          class="-m-2.5 p-2.5 text-[--color-text-secondary] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[--color-brand-500]" 
          @click="openSidebar"
        >
          <span class="sr-only">Open sidebar</span>
          <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
          </svg>
        </button>
        <h1 class="flex-1 text-sm font-semibold leading-6 text-[--color-text-primary]">
          {{ currentApp.name }}
        </h1>
      </div>

      <!-- Desktop header (app title) -->
      <div class="hidden lg:flex lg:h-16 lg:items-center lg:border-b lg:border-[--color-border-subtle] lg:bg-[--color-surface-elevated] lg:px-8">
        <h1 class="text-lg font-semibold text-[--color-text-primary]">
          {{ currentApp.name }}
        </h1>
      </div>

      <!-- Main content -->
      <main class="py-10">
        <div class="px-4 sm:px-6 lg:px-8">
          <slot />
        </div>
      </main>
    </div>

    <!-- No sidebar - full width -->
    <div v-if="!showSidebar">
      <main class="py-10">
        <div class="px-4 sm:px-6 lg:px-8">
          <slot />
        </div>
      </main>
    </div>
    
  </div>
</template>

<!-- NO <style scoped> BLOCK -->
```

### 2. Sidebar.vue
**File**: `ui/packages/layout-shell/src/components/Sidebar.vue`

**Remove**: Lines 71-90 (entire `<style scoped>` block)  
**Remove**: Outer `.sidebar-content` wrapper div (line 20)

**Replace template** (lines 18-68):

```vue
<template>
  <!-- Single root with all Tailwind utilities -->
  <div class="flex h-full w-full flex-col gap-y-5 overflow-y-auto px-6 pb-4">
    
    <!-- Brand -->
    <div class="flex h-16 shrink-0 items-center">
      <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-[--color-brand-600]">
        <span class="text-xl font-bold text-white" aria-hidden="true">A</span>
      </div>
      <span class="ml-3 text-xl font-semibold text-[--color-text-primary]">Agents</span>
    </div>

    <!-- Navigation -->
    <nav class="flex flex-1 flex-col">
      <ul role="list" class="flex flex-1 flex-col gap-y-7">
        <li>
          <ul role="list" class="-mx-2 space-y-1">
            <li v-for="item in navigationItems" :key="item.route">
              <router-link
                :to="item.route"
                :class="[
                  item.isActive
                    ? 'bg-[--color-surface] text-[--color-text-primary]'
                    : 'text-[--color-text-secondary] hover:bg-[--color-surface-hover] hover:text-[--color-text-primary]',
                  'group flex gap-x-3 rounded-md p-2 text-sm font-semibold leading-6 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[--color-brand-500]',
                ]"
              >
                <span class="text-xl" aria-hidden="true">{{ item.icon }}</span>
                <span>{{ item.label }}</span>
              </router-link>
            </li>
          </ul>
        </li>
      </ul>
    </nav>

    <!-- User profile -->
    <div class="-mx-6 mt-auto">
      <a
        href="#"
        class="flex items-center gap-x-4 px-6 py-3 text-sm font-semibold leading-6 text-[--color-text-primary] hover:bg-[--color-surface-hover] transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[--color-brand-500]"
      >
        <div class="flex h-8 w-8 items-center justify-center rounded-full bg-[--color-brand-500]">
          <span class="text-xs font-medium text-white">U</span>
        </div>
        <span class="sr-only">Your profile</span>
        <span aria-hidden="true">User</span>
      </a>
    </div>
    
  </div>
</template>

<!-- NO <style scoped> BLOCK -->
```

## Optional Enhancement: Design Tokens

**File**: `ui/packages/design-system/src/tokens.css`

**Add** (after existing theme tokens):

```css
@theme {
  /* Existing tokens... */
  
  /* Layout tokens */
  --spacing-sidebar-width: 18rem;
  --spacing-header-height: 4rem;
}
```

**Then update components** to use tokens:

```vue
<!-- Sidebar -->
lg:w-[--spacing-sidebar-width]

<!-- Content offset -->
lg:pl-[--spacing-sidebar-width]

<!-- Headers -->
h-[--spacing-header-height]
```

**Benefits**: Single source of truth, easier to adjust globally.

## Success Criteria

### Visual Requirements

**Desktop (≥1024px)**:
- [ ] Sidebar fixed at left, 288px wide
- [ ] Brand "AAgents" fully visible beside logo
- [ ] Header "Test Planning Studio" starts at 288px from left (beside sidebar)
- [ ] Navigation items show icon + label
- [ ] User profile visible at sidebar bottom
- [ ] Content has 288px left padding

**Mobile (<1024px)**:
- [ ] Sidebar hidden by default
- [ ] Menu button visible in top-left
- [ ] Tapping menu opens sidebar overlay
- [ ] Close button appears RIGHT of sidebar (outside panel)
- [ ] Tapping backdrop closes sidebar
- [ ] ESC key closes sidebar

### Technical Requirements

- [ ] **Zero scoped CSS** in AppShell.vue and Sidebar.vue
- [ ] **All layout via Tailwind utilities**
- [ ] **Zero build errors**
- [ ] **Only Tailwind 4 patterns** (no @apply, no manual @media)
- [ ] **Proper z-index stacking**: Backdrop (40) < Sidebar (50)
- [ ] **Focus-visible on all interactive elements**
- [ ] **Transition-colors for smooth hover effects**
- [ ] **Semantic HTML** (h1 for titles, nav for navigation)

### Accessibility Requirements

- [ ] Screen reader text with `.sr-only`
- [ ] `aria-hidden="true"` on decorative icons
- [ ] `role="dialog"` and `aria-modal="true"` on mobile overlay
- [ ] Focus ring only shows for keyboard navigation
- [ ] All interactive elements keyboard-accessible
- [ ] Proper heading hierarchy (h1 for app title)

## Build & Test Commands

```bash
# Build both apps
cd ui/apps/test-planning-studio
pnpm build

cd ../agents-console
pnpm build

# Run dev server for testing
cd ../test-planning-studio
pnpm dev
# Open http://localhost:5174

cd ../agents-console
pnpm dev
# Open http://localhost:5173
```

## Testing Checklist

### Desktop Testing (≥1024px)
1. Open browser at 1920x1080
2. Verify sidebar visible, 288px wide
3. Verify header starts at 288px (not overlapping sidebar)
4. Verify brand "AAgents" fully visible
5. Verify navigation shows icons + labels
6. Verify user profile at bottom

### Tablet Testing (768px - 1023px)
1. Resize browser to 800px width
2. Verify sidebar hidden
3. Verify mobile header visible with menu button
4. Verify content full-width

### Mobile Testing (<768px)
1. Resize browser to 375px width
2. Verify sidebar hidden
3. Click menu button → sidebar slides in
4. Verify close button RIGHT of sidebar
5. Click backdrop → sidebar closes
6. Press ESC → sidebar closes

### Keyboard Navigation Testing
1. Tab through all interactive elements
2. Verify focus ring only shows for keyboard (not mouse)
3. Verify Enter/Space activates links/buttons
4. Verify ESC closes mobile sidebar

## Tailwind 4 Compliance Checklist

- ✅ Uses `@import "tailwindcss"` in tokens.css
- ✅ Uses `@theme` for design tokens
- ✅ Uses arbitrary values: `w-[18rem]`, `pl-[18rem]`
- ✅ Uses CSS variables: `bg-[--color-surface]`
- ✅ Uses responsive modifiers: `lg:`, `sm:`, `md:`
- ✅ Uses `focus-visible:` instead of `focus:`
- ❌ NO @apply directives
- ❌ NO tailwind.config.js
- ❌ NO manual @media queries in components
- ❌ NO scoped CSS for layout

## Implementation Notes

### Key Tailwind 4 Patterns Used

1. **Arbitrary values**: `w-[18rem]`, `pl-[18rem]` for exact dimensions
2. **CSS variable references**: `bg-[--color-surface]` for design tokens
3. **Responsive prefixes**: `lg:fixed`, `lg:flex`, `lg:hidden`
4. **Pseudo-class prefixes**: `hover:`, `focus-visible:`, `aria-selected:`
5. **Opacity with slash**: `bg-gray-900/80` for 80% opacity
6. **Arbitrary properties**: Can use `[property:value]` if needed

### Why This Approach Works

1. **No specificity conflicts**: Tailwind utilities have consistent specificity
2. **No Vue scoping issues**: No scoped CSS to conflict with
3. **Single source of truth**: Layout defined once in template
4. **Responsive by default**: Tailwind handles all breakpoints
5. **Maintainable**: Easy to see all styles in template

---

**Status**: Ready for implementation - all corrections applied, pure Tailwind 4
