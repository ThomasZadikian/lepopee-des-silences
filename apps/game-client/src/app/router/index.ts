import { createRouter, createWebHistory } from 'vue-router';

import RunPage from '../../pages/RunPage.vue';
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
  ],
});