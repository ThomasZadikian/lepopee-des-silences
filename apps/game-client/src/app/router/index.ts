import { createRouter, createWebHistory } from 'vue-router';

import AccountAccessPage from '../../pages/AccountAccessPage.vue';
import AccountPage from '../../pages/AccountPage.vue';
import CharacterSelectionPage from '../../pages/CharacterSelectionPage.vue';
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
      redirect: { name: 'login' },
    },
    {
      path: '/connexion',
      name: 'login',
      component: AccountAccessPage,
      props: { mode: 'login' },
    },
    {
      path: '/inscription',
      name: 'register',
      component: AccountAccessPage,
      props: { mode: 'register' },
    },
    {
      path: '/verification-email',
      name: 'verify-email',
      component: AccountAccessPage,
      props: { mode: 'verify-email' },
    },
    {
      path: '/securite/mfa/configuration',
      name: 'mfa-setup',
      component: AccountAccessPage,
      props: { mode: 'mfa-setup' },
    },
    {
      path: '/securite/mfa',
      name: 'mfa-challenge',
      component: AccountAccessPage,
      props: { mode: 'mfa-challenge' },
    },
    {
      path: '/mot-de-passe-oublie',
      name: 'password-recovery',
      component: AccountAccessPage,
      props: { mode: 'password-recovery' },
    },
    {
      path: '/reinitialisation-mot-de-passe',
      name: 'password-reset',
      component: AccountAccessPage,
      props: { mode: 'password-reset' },
    },
    {
      path: '/personnages',
      name: 'character-selection',
      component: CharacterSelectionPage,
    },
    {
      path: '/compte',
      name: 'account',
      component: AccountPage,
    },
    {
      path: '/palais',
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
