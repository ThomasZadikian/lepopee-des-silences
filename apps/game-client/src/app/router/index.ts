import { createRouter, createWebHistory, type RouteLocationNormalized } from 'vue-router';

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
import { restoreAuthenticatedSession } from '../../features/account/authSession';
import { playerApi } from '../../shared/api/playerApi';

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      redirect: { name: 'character-selection' },
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
      meta: { requiresAuth: true },
    },
    {
      path: '/compte',
      name: 'account',
      component: AccountPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/palais',
      name: 'threshold',
      component: ThresholdPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/run/:runId?',
      name: 'run',
      component: RunPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/statuts',
      name: 'statuts',
      component: StatutsPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/manifestations',
      name: 'manifestations',
      component: ManifestationsPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/reputation/:runId?',
      name: 'reputation',
      component: ReputationPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/tutoriel',
      name: 'tutoriel',
      component: TutorialPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/equipe',
      name: 'equipe',
      component: TeamHubPage,
      props: { initialTab: 'equipe' },
      meta: { requiresAuth: true },
    },
    {
      path: '/statistiques',
      name: 'statistiques',
      component: TeamHubPage,
      props: { initialTab: 'statistiques' },
      meta: { requiresAuth: true },
    },
    {
      path: '/grimoire',
      name: 'grimoire',
      component: TeamHubPage,
      props: { initialTab: 'grimoire' },
      meta: { requiresAuth: true },
    },
    {
      path: '/equipement',
      name: 'equipement',
      component: TeamHubPage,
      props: { initialTab: 'equipement' },
      meta: { requiresAuth: true },
    },
    {
      path: '/besace',
      name: 'besace',
      component: TeamHubPage,
      props: { initialTab: 'besace' },
      meta: { requiresAuth: true },
    },
  ],
});

export async function requireAuthenticatedSession(
  to: Pick<RouteLocationNormalized, 'meta' | 'name' | 'fullPath'>,
) {
  if (!to.meta.requiresAuth) return true;

  const session = await restoreAuthenticatedSession(playerApi.refreshSession);
  if (session) return true;

  return {
    name: 'login',
    query: to.name === 'character-selection' ? {} : { redirect: to.fullPath },
  };
}

router.beforeEach(requireAuthenticatedSession);
