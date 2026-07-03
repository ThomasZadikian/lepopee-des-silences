import { createRouter, createWebHistory } from 'vue-router';

import ManifestationsPage from '../../pages/ManifestationsPage.vue';
import RunPage from '../../pages/RunPage.vue';
import StatutsPage from '../../pages/StatutsPage.vue';
import ThresholdPage from '../../pages/ThresholdPage.vue';

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'threshold',
      component: ThresholdPage,
    },
    {
      path: '/run/:runId?',
      name: 'run',
      component: RunPage,
    },
    {
      path: '/statuts',
      name: 'statuts',
      component: StatutsPage,
    },
    {
      path: '/manifestations',
      name: 'manifestations',
      component: ManifestationsPage,
    },
  ],
});