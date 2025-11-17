# @agents/design-system

Shared design system for my-agents UI applications.

## Features

- **Tailwind CSS 4** with design tokens
- **Dark theme** optimized for developer tools
- **Vue 3 components** with TypeScript support
- **Consistent styling** across all apps
- **Accessible** components with ARIA support

## Installation

This package is part of the monorepo workspace and is automatically available to all apps:

```ts
import { AppButton, AppCard } from '@agents/design-system';
```

## Design Tokens

All design tokens are defined in `src/tokens.css` using Tailwind 4's `@theme` directive.

### Color Palette

- **Brand**: Primary blue/purple theme (`--color-brand-*`)
- **Surfaces**: Dark backgrounds (`--color-surface-*`)
- **Text**: Multiple levels of contrast (`--color-text-*`)
- **Semantic**: Success, warning, danger, info (`--color-{semantic}-*`)

### Typography

- **Font Sans**: System font stack
- **Font Mono**: JetBrains Mono with fallbacks
- **Sizes**: xs (12px) to 3xl (30px)

### Spacing

- **Base**: 4px (0.25rem)
- **Radius**: xs (2px) to pill (9999px)

## Components

### AppButton

Button component with multiple variants and sizes.

**Props:**
- `variant?: 'primary' | 'ghost' | 'danger' | 'success'` - Visual style (default: 'primary')
- `size?: 'sm' | 'md' | 'lg'` - Button size (default: 'md')
- `disabled?: boolean` - Disable the button
- `loading?: boolean` - Show loading spinner
- `type?: 'button' | 'submit' | 'reset'` - HTML button type

**Usage:**
```vue
<AppButton variant="primary" size="md">
  Click me
</AppButton>

<AppButton variant="ghost" :loading="isLoading">
  Save
</AppButton>
```

### AppCard

Container component for grouping content.

**Props:**
- `hover?: boolean` - Enable hover effect (default: false)
- `padding?: 'none' | 'sm' | 'md' | 'lg'` - Internal padding (default: 'md')

**Usage:**
```vue
<AppCard hover padding="lg">
  <h2>Card Title</h2>
  <p>Card content goes here</p>
</AppCard>
```

### AppInput

Form input component with label and error support.

**Props:**
- `modelValue?: string | number` - Input value (v-model)
- `type?: 'text' | 'email' | 'password' | 'number' | 'search' | 'tel' | 'url'` - Input type
- `placeholder?: string` - Placeholder text
- `disabled?: boolean` - Disable the input
- `error?: string` - Error message to display
- `label?: string` - Label text
- `id?: string` - Input ID

**Usage:**
```vue
<AppInput
  v-model="email"
  type="email"
  label="Email Address"
  placeholder="you@example.com"
  :error="emailError"
/>
```

### AppBadge

Status badge component with variants and optional dot.

**Props:**
- `variant?: 'default' | 'brand' | 'success' | 'warning' | 'danger' | 'info'` - Visual style
- `size?: 'sm' | 'md'` - Badge size (default: 'md')
- `dot?: boolean` - Show status dot (default: false)

**Usage:**
```vue
<AppBadge variant="success" :dot="true">
  Active
</AppBadge>

<AppBadge variant="warning" size="sm">
  Pending
</AppBadge>
```

## Utility Classes

The design system provides utility classes for common patterns:

### Layout
```css
.page-container  /* Max-width container with padding */
```

### Cards
```css
.card            /* Basic card style */
.card-hover      /* Card with hover effect */
```

### Forms
```css
.input-base      /* Base input styling */
.btn-base        /* Base button styling */
```

## Using in Your App

### 1. Import the plugin

```ts
// main.ts
import { createApp } from 'vue';
import DesignSystem from '@agents/design-system';
import '@agents/design-system/tokens.css';

const app = createApp(App);
app.use(DesignSystem);
app.mount('#app');
```

### 2. Use components globally

```vue
<template>
  <!-- Components are auto-registered -->
  <AppButton>Click me</AppButton>
</template>
```

### 3. Or import individually

```vue
<script setup>
import { AppButton, AppCard } from '@agents/design-system';
</script>

<template>
  <AppCard>
    <AppButton>Click me</AppButton>
  </AppCard>
</template>
```

## Development

```bash
# Build the package
pnpm build

# Watch mode
pnpm dev
```

## Extending the Design System

### Adding New Components

1. Create component in `src/components/`
2. Export from `src/index.ts`
3. Add to plugin registration
4. Document in this README

### Customizing Tokens

Edit `src/tokens.css` and modify the `@theme` values. All apps will inherit changes automatically.

## Browser Support

- Modern browsers with CSS custom properties support
- Chrome/Edge 88+
- Firefox 85+
- Safari 14+
