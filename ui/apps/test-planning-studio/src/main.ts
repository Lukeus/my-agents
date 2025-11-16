import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';
import { designSystemPlugin } from '@agents/design-system';
import './assets/tailwind.css';

const app = createApp(App);

app.use(createPinia());
app.use(router);
app.use(designSystemPlugin);

app.mount('#app');
