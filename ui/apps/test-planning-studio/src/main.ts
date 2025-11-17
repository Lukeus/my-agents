import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';
import { designSystemPlugin } from '@agents/design-system';

// CRITICAL: Import design tokens and packages for Tailwind scanning
import './assets/tailwind.css';
import '@agents/layout-shell';  // Adds layout-shell to Vite build graph

const app = createApp(App);

app.use(createPinia());
app.use(router);
app.use(designSystemPlugin);

app.mount('#app');
