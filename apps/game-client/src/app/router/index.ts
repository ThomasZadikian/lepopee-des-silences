import { createRouter, createWebHistory } from 'vue-router';

import BesacePage from '../../pages/BesacePage.vue';
import EquipmentPage from '../../pages/EquipmentPage.vue';
import GrimoirePage from '../../pages/GrimoirePage.vue';
import ManifestationsPage from '../../pages/ManifestationsPage.vue';
import ReputationPage from '../../pages/ReputationPage.vue';
import RunPage from '../../pages/RunPage.vue';
import StatsPage from '../../pages/StatsPage.vue';
import StatutsPage from '../../pages/StatutsPage.vue';
import TeamPage from '../../pages/TeamPage.vue';
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
      component: TeamPage,
    },
    {
      path: '/statistiques',
      name: 'statistiques',
      component: StatsPage,
    },
    {
      path: '/grimoire',
      name: 'grimoire',
      component: GrimoirePage,
    },
    {
      path: '/equipement',
      name: 'equipement',
      component: EquipmentPage,
    },
    {
      path: '/besace',
      name: 'besace',
      component: BesacePage,
    },
  ],
});