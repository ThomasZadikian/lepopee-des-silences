import { createRouter, createWebHistory } from 'vue-router';

import ManifestationsPage from '../../pages/ManifestationsPage.vue';
import ReputationPage from '../../pages/ReputationPage.vue';
import RunPage from '../../pages/RunPage.vue';
import StatutsPage from '../../pages/StatutsPage.vue';
import TeamHubPage from '../../pages/TeamHubPage.vue';
import ThresholdPage from '../../pages/ThresholdPage.vue';
import TutorialPage from '../../pages/TutorialPage.vue';

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
    {
      path: '/reputation/:runId?',
      name: 'reputation',
      component: ReputationPage,
    },
    {
      path: '/tutoriel',
      name: 'tutoriel',
      component: TutorialPage,
    },
    {
      path: '/equipe',
      name: 'equipe',
      component: TeamHubPage,
      props: { initialTab: 'equipe' },
    },
    {
      path: '/statistiques',
      name: 'statistiques',
      component: TeamHubPage,
      props: { initialTab: 'statistiques' },
    },
    {
      path: '/grimoire',
      name: 'grimoire',
      component: TeamHubPage,
      props: { initialTab: 'grimoire' },
    },
    {
      path: '/equipement',
      name: 'equipement',
      component: TeamHubPage,
      props: { initialTab: 'equipement' },
    },
    {
      path: '/besace',
      name: 'besace',
      component: TeamHubPage,
      props: { initialTab: 'besace' },
    },
  ],
});