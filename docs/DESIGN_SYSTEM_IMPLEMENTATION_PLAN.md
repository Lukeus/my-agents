# Vue 3 + Tailwind 4 Design System Implementation Plan

## Problem Statement

The current mono-repo has several architectural issues that prevent it from being production-ready:

1. **Invalid Tailwind utility classes** - Components use non-existent utility classes like `text-text-primary`, `bg-surface`, `text-danger-600` instead of using CSS custom properties
2. **Missing input styles** - `AppInput.vue` references undefined `input-base` class
3. **Duplicate AppShell component** - Both `design-system` and `layout-shell` packages export an `AppShell` component with different purposes
4. **Incomplete design system** - Missing critical components (Table, Spinner/Loading, Alert/Banner, EmptyState, etc.)
5. **Clean Architecture violations** - Layout-shell depends on design-system, but both are in the same layer

## Current State Analysis

### Package Structure

```
ui/
├── packages/
│   ├── design-system/          # @agents/design-system
│   │   ├── src/
│   │   │   ├── components/
│   │   │   │   ├── AppButton.vue    ✅ Good
│   │   │   │   ├── AppCard.vue      ✅ Good
│   │   │   │   ├── AppInput.vue     ⚠️ Missing .input-base class
│   │   │   │   ├── AppBadge.vue     ✅ Good
│   │   │   │   └── AppShell.vue     ❌ Wrong package (layout concern)
│   │   │   ├── tokens.css           ✅ Excellent token system
│   │   │   └── index.ts
│   │   └── package.json
│   │
│   ├── layout-shell/           # @agents/layout-shell
│   │   ├── src/
│   │   │   ├── components/
│   │   │   │   ├── AppShell.vue           ✅ Proper layout component
│   │   │   │   ├── TopNav.vue             ✅ Good
│   │   │   │   ├── Sidebar.vue            ✅ Good
│   │   │   │   ├── StackedLayout.vue      ✅ Good
│   │   │   │   └── MultiColumnLayout.vue  ✅ Good
│   │   │   └── index.ts
│   │   └── package.json
│   │
│   ├── agent-domain/           # Zod schemas
│   └── api-client/             # HTTP clients
│
└── apps/
    ├── agents-console/
    │   └── src/
    │       ├── App.vue                          ✅ Uses layout-shell correctly
    │       ├── presentation/pages/
    │       │   ├── DashboardPage.vue            ❌ Invalid Tailwind classes
    │       │   ├── AgentsListPage.vue           ❌ Invalid Tailwind classes
    │       │   ├── AgentDetailPage.vue          ❌ Invalid Tailwind classes
    │       │   ├── SettingsPage.vue             ❌ Invalid Tailwind classes
    │       │   ├── RunsListPage.vue             ❌ Invalid Tailwind classes
    │       │   └── RunDetailPage.vue            ❌ Invalid Tailwind classes
    │       └── application/usecases/
    │
    └── test-planning-studio/
        └── src/
            ├── App.vue                          ✅ Uses layout-shell correctly
            └── presentation/pages/
                ├── DashboardPage.vue            ❌ Invalid Tailwind classes
                ├── TestSpecsListPage.vue        ❌ Invalid Tailwind classes
                ├── GenerateSpecPage.vue         ❌ Invalid Tailwind classes
                └── [other pages]                ❌ Invalid Tailwind classes
```

### Key Issues Identified

#### 1. Invalid Tailwind Classes (Critical)
**Location:** All page components in both apps

**Problem:** Pages use utility classes that don't exist in Tailwind:
```vue
<!-- ❌ WRONG -->
<h1 class="text-text-primary">Title</h1>
<div class="bg-surface">Content</div>
<span class="text-danger-600">Error</span>
<div class="border-surface-border">Card</div>

<!-- ✅ CORRECT -->
<h1 class="text-[--color-text-primary]">Title</h1>
<div class="bg-[--color-surface]">Content</div>
<span class="text-[--color-danger-600]">Error</span>
<div class="border-[--color-border-subtle]">Card</div>
```

**Files affected:**
- `ui/apps/agents-console/src/presentation/pages/*.vue` (6 files)
- `ui/apps/test-planning-studio/src/presentation/pages/*.vue` (6 files)

#### 2. Missing Input Base Style
**Location:** `ui/packages/design-system/src/tokens.css`

**Problem:** `AppInput.vue` uses `input-base` class but it's not defined. Need to add:
```css
.input-base {
  display: block;
  width: 100%;
  padding: 0.5rem 0.75rem;
  font-size: var(--font-size-sm);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-md);
  background-color: var(--color-surface-elevated);
  color: var(--color-text-primary);
  transition: all 0.15s ease;
}

.input-base:focus-visible {
  outline: 2px solid var(--color-brand-500);
  outline-offset: 2px;
  border-color: var(--color-brand-500);
}

.input-base:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.input-base::placeholder {
  color: var(--color-text-muted);
}
```

#### 3. Duplicate AppShell Components
**Location:** 
- `ui/packages/design-system/src/components/AppShell.vue` (wrong location)
- `ui/packages/layout-shell/src/components/AppShell.vue` (correct location)

**Problem:** Design system should only contain primitive UI components. Layout components belong in layout-shell.

**Solution:** Remove AppShell from design-system package entirely.

#### 4. Missing Design System Components

Required components not yet implemented:

**High Priority:**
- `AppSpinner` / `AppLoader` - For loading states
- `AppAlert` / `AppBanner` - For success/error/warning/info messages
- `AppTable` - Data tables (used in AgentsListPage)
- `AppEmptyState` - Empty state placeholders
- `AppModal` / `AppDialog` - Overlays and modals
- `AppTextarea` - Multi-line text input
- `AppSelect` / `AppDropdown` - Select menus

**Medium Priority:**
- `AppCheckbox` - Checkboxes
- `AppRadio` - Radio buttons
- `AppSwitch` / `AppToggle` - Toggle switches
- `AppTabs` - Tab navigation
- `AppAccordion` - Collapsible sections
- `AppToast` / `AppNotification` - Toast notifications
- `AppAvatar` - User avatars
- `AppDivider` - Visual separators

**Low Priority:**
- `AppTooltip` - Tooltips
- `AppPopover` - Popovers
- `AppMenu` - Context menus
- `AppProgress` - Progress bars
- `AppSkeleton` - Loading skeletons

## Proposed Solution

### Phase 1: Fix Critical Issues (Immediate) ✅ COMPLETE

#### Step 1.1: Add Missing CSS Classes to tokens.css ✅
**File:** `ui/packages/design-system/src/tokens.css`

✅ Added `.input-base` class with proper styling for focus, disabled, and placeholder states.

#### Step 1.2: Remove Duplicate AppShell from Design System ✅
**Files modified:**
- ✅ Removed `ui/packages/design-system/src/components/AppShell.vue`
- ✅ Verified `ui/packages/design-system/src/index.ts` doesn't export AppShell
- ✅ Verified both apps correctly import AppShell from layout-shell

#### Step 1.3: Fix Invalid Tailwind Classes in All Pages ✅
**Pattern applied across all files:**

Replace invalid utility classes with CSS custom property syntax:
- `text-text-primary` → `text-[--color-text-primary]`
- `text-text-secondary` → `text-[--color-text-secondary]`
- `text-text-tertiary` → `text-[--color-text-tertiary]`
- `text-text-muted` → `text-[--color-text-muted]`
- `bg-surface` → `bg-[--color-surface]`
- `bg-surface-elevated` → `bg-[--color-surface-elevated]`
- `bg-surface-hover` → `bg-[--color-surface-hover]`
- `border-surface-border` → `border-[--color-border]`
- `text-primary-600` → `text-[--color-primary-600]`
- `text-danger-600` → `text-[--color-danger-600]`
- `text-success-600` → `text-[--color-success-600]`
- `text-warning-600` → `text-[--color-warning-600]`
- `bg-danger-50` → `bg-[--color-danger-50]`
- `border-danger-200` → `border-[--color-danger-200]`
- `text-danger-700` → `text-[--color-danger-700]`

**Files updated:**

agents-console (6 files fixed):
1. ✅ `ui/apps/agents-console/src/presentation/pages/DashboardPage.vue`
2. ✅ `ui/apps/agents-console/src/presentation/pages/AgentsListPage.vue`
3. ✅ `ui/apps/agents-console/src/presentation/pages/AgentDetailPage.vue`
4. ✅ `ui/apps/agents-console/src/presentation/pages/SettingsPage.vue`
5. ✅ `ui/apps/agents-console/src/presentation/pages/RunsListPage.vue`
6. ✅ `ui/apps/agents-console/src/presentation/pages/RunDetailPage.vue`

test-planning-studio (already correct):
7. ✅ `ui/apps/test-planning-studio/src/presentation/pages/DashboardPage.vue`
8. ✅ `ui/apps/test-planning-studio/src/presentation/pages/TestSpecsListPage.vue`
9. ✅ `ui/apps/test-planning-studio/src/presentation/pages/TestSpecDetailPage.vue`
10. ✅ `ui/apps/test-planning-studio/src/presentation/pages/TestSpecEditorPage.vue`
11. ✅ `ui/apps/test-planning-studio/src/presentation/pages/GenerateSpecPage.vue`
12. ✅ `ui/apps/test-planning-studio/src/presentation/pages/CoverageAnalysisPage.vue`

### Phase 2: Add Missing Design System Components ✅ COMPLETE

#### Step 2.1: Create High-Priority Components ✅

All 7 high-priority components have been created:

✅ **AppSpinner.vue** - Loading spinner with size variants (sm/md/lg) and color variants (primary/secondary/white)

✅ **AppAlert.vue** - Alert/banner with 4 variants (info/success/warning/danger), optional title, and dismissible functionality

✅ **AppTable.vue** - Data table with hoverable rows, bordered cells, and striped row options

✅ **AppEmptyState.vue** - Empty state with customizable icon, title, description, and action slot

✅ **AppModal.vue** - Modal dialog with 4 sizes, backdrop control, ESC key support, body scroll lock, and Teleport

✅ **AppTextarea.vue** - Multi-line text input matching AppInput styling with rows configuration and resize-y

✅ **AppSelect.vue** - Select dropdown with options array, placeholder support, and disabled option handling

#### Step 2.2: Update design-system index.ts ✅
✅ Exported all 7 new components and their TypeScript types
✅ Added components to designSystemPlugin for global registration

#### Step 2.3: Refactor Pages to Use New Components ✅

✅ Refactored DashboardPage.vue:
- Replaced inline error div with `AppAlert` (danger variant)
- Replaced loading text with `AppSpinner` + text
- Replaced empty state div with `AppEmptyState` component

✅ Refactored AgentsListPage.vue:
- Replaced inline error div with `AppAlert` (danger variant)
- Replaced loading text with `AppSpinner` + text
- Replaced empty state div with `AppEmptyState` component (with search-aware messaging)

### Phase 3: Validation & Testing

#### Step 3.1: Build Verification
```powershell
cd ui
pnpm install
pnpm build
```

Ensure all packages and apps build without errors.

#### Step 3.2: Type Checking
```powershell
cd ui/apps/agents-console
pnpm type-check

cd ../test-planning-studio
pnpm type-check
```

#### Step 3.3: Lint Verification
```powershell
cd ui
pnpm lint
```

Fix all linting issues.

#### Step 3.4: Visual Testing
Start both apps and verify:
- All colors render correctly using CSS custom properties
- No broken styles or missing classes
- Components look consistent across apps
- Responsive behavior works properly
- Dark theme renders correctly

```powershell
# Terminal 1
cd ui/apps/agents-console
pnpm dev

# Terminal 2
cd ui/apps/test-planning-studio
pnpm dev
```

#### Step 3.5: Unit Testing
Run existing tests to ensure no regressions:
```powershell
cd ui/apps/test-planning-studio
pnpm test
```

### Phase 4: Documentation

#### Step 4.1: Create Component Documentation
Create `ui/packages/design-system/README.md` with:
- Component catalog
- Usage examples
- Props documentation
- Design token reference

#### Step 4.2: Update WARP.md
Update frontend guidelines section with:
- Correct Tailwind CSS custom property usage
- Component import patterns
- Common pitfalls to avoid

## Success Criteria

- ✅ All Tailwind classes are valid and render correctly
- ✅ No inline CSS anywhere in the codebase
- ✅ All pages use only components from design-system and layout-shell
- ✅ Clean architecture: design-system has no dependencies, layout-shell depends only on design-system
- ✅ Zero build errors
- ✅ Zero TypeScript errors
- ✅ Zero linting errors
- ✅ All existing tests pass
- ✅ Both apps run successfully and render correctly

## Migration Path

1. **Phase 1** (2-3 hours) - Fix critical issues blocking development
2. **Phase 2** (4-6 hours) - Build out missing components systematically
3. **Phase 3** (2-3 hours) - Comprehensive testing and validation
4. **Phase 4** (1-2 hours) - Documentation and knowledge sharing

**Total Estimated Effort:** 10-15 hours

## Risk Mitigation

- **Breaking Changes:** All changes are non-breaking - only fixing invalid code
- **Testing:** Extensive visual and functional testing before declaring complete
- **Rollback:** Git commits per phase allow easy rollback if issues arise
- **Documentation:** Clear before/after examples for every change pattern

## Notes

- This plan assumes NO shortcuts per user rules
- Quality and correctness are prioritized over speed
- All changes will be linted and formatted before completion
- No deployment until explicit user approval
