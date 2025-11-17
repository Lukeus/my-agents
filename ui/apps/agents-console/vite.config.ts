import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig({
  plugins: [
    vue(),
    tailwindcss({
      content: [
        './src/**/*.{vue,js,ts,jsx,tsx}',
        '../../packages/layout-shell/src/**/*.{vue,js,ts,jsx,tsx}',
        '../../packages/design-system/src/**/*.{vue,js,ts,jsx,tsx}',
      ],
    }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    strictPort: true,
  },
  build: {
    outDir: 'dist',
    sourcemap: true,
  },
});
