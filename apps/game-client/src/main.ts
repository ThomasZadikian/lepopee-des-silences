import { createPinia } from 'pinia';
import { createApp } from 'vue';

import App from './App.vue';
import { router } from './app/router';
import { useEmotionalRegisterCatalog } from './features/emotional-registers/store';

import './shared/styles/global.css';
import './shared/styles/tokens.css';

const app = createApp(App);
const pinia = createPinia();

app.use(pinia).use(router);

// Load the Catalog-owned vocabulary before any page can render a type badge.
// An invalid or unavailable vocabulary blocks startup: rendering a second local
// interpretation would violate the Catalog-only contract.
await useEmotionalRegisterCatalog().load();
app.mount('#app');
