<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { RouterLink, useRoute, useRouter } from 'vue-router';

import AccountAccessShell from '../features/account/components/AccountAccessShell.vue';
import {
  getChallengeToken,
  setAuthenticatedSession,
  setChallengeToken,
} from '../features/account/authSession';
import { createTotpQrCodeDataUrl } from '../features/account/totpQrCode';
import { playerApi, type MfaEnrollmentResponse } from '../shared/api/playerApi';

type AccountAccessMode =
  | 'login'
  | 'register'
  | 'verify-email'
  | 'mfa-setup'
  | 'mfa-challenge'
  | 'password-recovery'
  | 'password-reset';

const props = defineProps<{ mode: AccountAccessMode }>();
const router = useRouter();
const route = useRoute();

const email = ref('');
const displayName = ref('');
const password = ref('');
const passwordConfirmation = ref('');
const totpCode = ref('');
const ageConfirmed = ref(false);
const submitted = ref(false);
const busy = ref(false);
const error = ref<string | null>(null);
const mfaEnrollment = ref<MfaEnrollmentResponse | null>(null);
const mfaQrCodeDataUrl = ref<string | null>(null);
const mfaQrCodeError = ref<string | null>(null);

const content = computed(() => {
  switch (props.mode) {
    case 'register':
      return {
        kicker: 'Créer un compte',
        title: 'Entrer dans le Palais',
        subtitle: 'Votre compte porte la progression du Palais. Votre personnage sera choisi ensuite.',
        action: 'Créer mon compte',
      };
    case 'verify-email':
      return {
        kicker: 'Vérification',
        title: 'Confirmer votre adresse',
        subtitle: 'Ouvrez le lien à usage unique reçu par e-mail pour confirmer votre adresse.',
        action: 'Confirmer mon adresse',
      };
    case 'mfa-setup':
      return {
        kicker: 'Sécurité obligatoire',
        title: 'Lier votre application TOTP',
        subtitle: 'Ajoutez la clé à Google Authenticator ou à toute application TOTP compatible, puis saisissez le code à six chiffres.',
        action: 'Activer la double authentification',
      };
    case 'mfa-challenge':
      return {
        kicker: 'Double authentification',
        title: 'Prouver votre présence',
        subtitle: 'Saisissez le code temporaire de votre application d’authentification.',
        action: 'Continuer',
      };
    case 'password-recovery':
      return {
        kicker: 'Récupération',
        title: 'Retrouver votre accès',
        subtitle: 'Si cette adresse existe, un lien temporaire de réinitialisation sera envoyé.',
        action: 'Envoyer le lien',
      };
    case 'password-reset':
      return {
        kicker: 'Nouveau secret',
        title: 'Choisir un nouveau mot de passe',
        subtitle: 'Douze caractères minimum. Aucun format artificiel n’est imposé.',
        action: 'Réinitialiser le mot de passe',
      };
    default:
      return {
        kicker: 'Connexion',
        title: 'Revenir au Palais',
        subtitle: 'Votre progression vous attend là où vous l’avez laissée.',
        action: 'Se connecter',
      };
  }
});

const needsEmail = computed(() => ['login', 'register', 'password-recovery'].includes(props.mode));
const needsPassword = computed(() => ['login', 'register', 'password-reset'].includes(props.mode));
const needsTotp = computed(() => ['mfa-setup', 'mfa-challenge'].includes(props.mode));
const verifiedMessage = computed(() => props.mode === 'login' && route.query.verified === '1');

function queryToken(name: string): string | null {
  const value = route.query[name];
  return typeof value === 'string' && value.trim() ? value : null;
}

function errorMessage(cause: unknown): string {
  return cause instanceof Error
    ? cause.message
    : 'Une erreur inattendue empêche de poursuivre. Réessayez.';
}

async function loadMfaEnrollment() {
  if (props.mode !== 'mfa-setup') return;

  const challengeToken = getChallengeToken();
  if (!challengeToken) {
    error.value = 'Le défi d’authentification a expiré. Reconnectez-vous.';
    return;
  }

  try {
    busy.value = true;
    const enrollment = await playerApi.beginMfaEnrollment(challengeToken);
    mfaEnrollment.value = enrollment;

    try {
      mfaQrCodeDataUrl.value = await createTotpQrCodeDataUrl(enrollment.otpAuthUri);
    } catch {
      mfaQrCodeError.value = 'Le QR code n’a pas pu être généré. Utilisez la clé manuelle ci-dessous.';
    }
  } catch (cause) {
    error.value = errorMessage(cause);
  } finally {
    busy.value = false;
  }
}

onMounted(loadMfaEnrollment);

function validationError(): string | null {
  if (needsEmail.value && !email.value.trim()) {
    return 'Une adresse e-mail est requise.';
  }
  if (props.mode === 'register' && !displayName.value.trim()) {
    return 'Un pseudonyme de compte est requis.';
  }
  if (needsPassword.value && password.value.length < 12) {
    return 'Le mot de passe doit contenir au moins 12 caractères.';
  }
  if (['register', 'password-reset'].includes(props.mode) && password.value !== passwordConfirmation.value) {
    return 'Les mots de passe ne correspondent pas.';
  }
  if (props.mode === 'register' && !ageConfirmed.value) {
    return 'Vous devez confirmer avoir au moins 16 ans.';
  }
  if (needsTotp.value && !/^\d{6}$/.test(totpCode.value)) {
    return 'Le code doit contenir six chiffres.';
  }

  return null;
}

function requiredChallengeToken(): string | null {
  const challengeToken = getChallengeToken();
  if (!challengeToken) {
    error.value = 'Le défi d’authentification a expiré. Reconnectez-vous.';
  }
  return challengeToken;
}

async function submitRegistration() {
  await playerApi.registerAccount({
    displayName: displayName.value.trim(),
    email: email.value.trim(),
    password: password.value,
    ageConfirmed: ageConfirmed.value,
  });
  await router.push({ name: 'verify-email' });
}

async function submitEmailVerification() {
  const token = queryToken('token');
  if (!token) {
    error.value = 'Le lien de vérification est incomplet ou a expiré.';
    return;
  }
  await playerApi.verifyEmail(token);
  await router.push({ name: 'login', query: { verified: '1' } });
}

async function submitLogin() {
  const response = await playerApi.beginLogin(email.value.trim(), password.value);
  if (response.status === 'email-verification-required') {
    error.value = 'Votre adresse e-mail doit être vérifiée avant la connexion.';
    return;
  }
  if (!response.challengeToken) {
    error.value = 'Le serveur n’a pas fourni de défi d’authentification valide.';
    return;
  }

  setChallengeToken(response.challengeToken);
  if (response.status === 'mfa-setup-required') {
    await router.push({ name: 'mfa-setup' });
    return;
  }
  if (response.status === 'mfa-required') {
    await router.push({ name: 'mfa-challenge' });
    return;
  }
  error.value = 'État d’authentification inattendu. Recommencez la connexion.';
}

async function submitMfaEnrollment() {
  const challengeToken = requiredChallengeToken();
  if (!challengeToken) return;

  const session = await playerApi.confirmMfaEnrollment(challengeToken, totpCode.value);
  setAuthenticatedSession(session);
  setChallengeToken(null);
  await router.push({ name: 'character-selection' });
}

async function submitMfaChallenge() {
  const challengeToken = requiredChallengeToken();
  if (!challengeToken) return;

  const session = await playerApi.completeMfaChallenge(challengeToken, totpCode.value);
  setAuthenticatedSession(session);
  setChallengeToken(null);
  await router.push({ name: 'character-selection' });
}

async function submitPasswordReset() {
  const token = queryToken('token');
  if (!token) {
    error.value = 'Le lien de réinitialisation est incomplet ou a expiré.';
    return;
  }
  await playerApi.resetPassword(token, password.value);
  await router.push({ name: 'login' });
}

async function submitCurrentMode() {
  switch (props.mode) {
    case 'register':
      await submitRegistration();
      break;
    case 'verify-email':
      await submitEmailVerification();
      break;
    case 'login':
      await submitLogin();
      break;
    case 'mfa-setup':
      await submitMfaEnrollment();
      break;
    case 'mfa-challenge':
      await submitMfaChallenge();
      break;
    case 'password-recovery':
      await playerApi.requestPasswordReset(email.value.trim());
      submitted.value = true;
      break;
    case 'password-reset':
      await submitPasswordReset();
      break;
  }
}

async function submit() {
  error.value = validationError();
  submitted.value = false;
  if (error.value) return;

  try {
    busy.value = true;
    await submitCurrentMode();
  } catch (cause) {
    error.value = errorMessage(cause);
  } finally {
    busy.value = false;
  }
}
</script>

<template>
  <AccountAccessShell :kicker="content.kicker" :title="content.title" :subtitle="content.subtitle" narrow>
    <form class="access-form" @submit.prevent="submit">
      <div v-if="mode === 'mfa-setup'" class="mfa-enrolment">
        <div class="mfa-enrolment__qr" aria-live="polite">
          <img
            v-if="mfaQrCodeDataUrl"
            class="mfa-enrolment__qr-image"
            :src="mfaQrCodeDataUrl"
            alt="QR code de configuration de la double authentification"
            width="150"
            height="150"
          />
          <template v-else>
            <span class="mfa-enrolment__mark">◇</span>
            <span>{{ busy ? 'Génération…' : 'QR indisponible' }}</span>
          </template>
        </div>
        <p v-if="mfaEnrollment" class="mfa-enrolment__hint">
          Clé manuelle : <strong>{{ mfaEnrollment.manualEntryKey }}</strong>
        </p>
        <p v-else class="mfa-enrolment__hint">
          La clé TOTP est générée et protégée côté serveur ; elle n’est jamais journalisée dans le navigateur.
        </p>
        <p v-if="mfaQrCodeError" class="mfa-enrolment__warning" role="alert">
          {{ mfaQrCodeError }}
        </p>
      </div>

      <p v-if="verifiedMessage" class="access-success">
        Adresse confirmée. Connectez-vous pour terminer la configuration de sécurité.
      </p>

      <label v-if="needsEmail" class="access-field">
        <span class="access-field__label">Adresse e-mail</span>
        <input v-model="email" class="access-field__input" type="email" autocomplete="email" placeholder="vous@exemple.fr" />
      </label>

      <label v-if="mode === 'register'" class="access-field">
        <span class="access-field__label">Pseudonyme du compte</span>
        <input v-model="displayName" class="access-field__input" autocomplete="nickname" placeholder="Nocturne" />
      </label>

      <label v-if="needsPassword" class="access-field">
        <span class="access-field__label">Mot de passe</span>
        <input v-model="password" class="access-field__input" type="password" :autocomplete="mode === 'login' ? 'current-password' : 'new-password'" placeholder="12 caractères minimum" />
      </label>

      <label v-if="mode === 'register' || mode === 'password-reset'" class="access-field">
        <span class="access-field__label">Confirmer le mot de passe</span>
        <input v-model="passwordConfirmation" class="access-field__input" type="password" autocomplete="new-password" />
      </label>

      <label v-if="needsTotp" class="access-field">
        <span class="access-field__label">Code d’authentification TOTP</span>
        <input v-model="totpCode" class="access-field__input access-field__input--code" inputmode="numeric" autocomplete="one-time-code" maxlength="6" placeholder="000000" />
      </label>

      <label v-if="mode === 'register'" class="age-confirmation">
        <input v-model="ageConfirmed" type="checkbox" />
        <span>Je confirme avoir au moins 16 ans.</span>
      </label>

      <p v-if="mode === 'verify-email'" class="access-note">
        La validation dépend du token reçu par e-mail. Si vous avez ouvert ce lien dans un autre onglet, vous pouvez poursuivre ici.
      </p>

      <p v-if="submitted" class="access-success">
        Si cette adresse correspond à un compte, le message de récupération a été envoyé.
      </p>
      <p v-if="error" class="access-error" role="alert">{{ error }}</p>

      <button class="access-submit" type="submit" :disabled="busy">
        <span class="access-submit__glyph">◈</span>
        <span>{{ busy ? 'Traitement…' : content.action }}</span>
      </button>
    </form>

    <template #footer>
      <div class="access-footer-links">
        <RouterLink v-if="mode === 'login'" :to="{ name: 'register' }">Créer un compte</RouterLink>
        <RouterLink v-if="mode === 'login'" :to="{ name: 'password-recovery' }">Mot de passe oublié</RouterLink>
        <RouterLink v-if="mode !== 'login' && mode !== 'mfa-challenge'" :to="{ name: 'login' }">Retour à la connexion</RouterLink>
      </div>
    </template>
  </AccountAccessShell>
</template>

<style scoped>
.access-form {
  padding: 28px;
  display: grid;
  gap: 20px;
  text-align: left;
}

.access-field {
  display: grid;
  gap: 8px;
}

.access-field__label {
  font-size: 10px;
  font-weight: 600;
  letter-spacing: .16em;
  text-transform: uppercase;
  color: var(--ink-3);
}

.access-field__input {
  width: 100%;
  box-sizing: border-box;
  padding: 12px 13px;
  border: 1px solid var(--line);
  border-radius: 0;
  outline: none;
  background: var(--bg-2);
  color: var(--ink);
  font: 14px var(--font);
  transition: border-color .25s, box-shadow .25s;
}

.access-field__input:focus {
  border-color: var(--mint-dim);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--mint) 12%, transparent);
}

.access-field__input--code {
  font-family: var(--font-mono);
  font-size: 20px;
  letter-spacing: .3em;
  text-align: center;
}

.age-confirmation {
  display: flex;
  align-items: center;
  gap: 10px;
  color: var(--ink-3);
  font-size: 13px;
}

.age-confirmation input { accent-color: var(--mint-dim); }

.access-submit {
  margin-top: 4px;
  padding: 13px 16px;
  border: 1px solid var(--mint-dim);
  background: transparent;
  color: var(--mint);
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 9px;
  font: 600 11px var(--font);
  letter-spacing: .13em;
  text-transform: uppercase;
  cursor: pointer;
  transition: background .3s, color .3s;
}

.access-submit:hover:not(:disabled) {
  background: color-mix(in srgb, var(--mint) 11%, transparent);
  color: var(--ink);
}

.access-submit:disabled { opacity: .55; cursor: wait; }
.access-submit__glyph { font-size: 12px; }

.access-note,
.mfa-enrolment__hint {
  color: var(--ink-4);
  font-size: 12px;
  line-height: 1.6;
}

.mfa-enrolment__warning {
  margin: 0;
  color: var(--danger);
  font-size: 12px;
  line-height: 1.6;
  text-align: center;
}

.mfa-enrolment__hint strong {
  color: var(--ink-2);
  font-family: var(--font-mono);
  overflow-wrap: anywhere;
}

.access-error { color: var(--danger); font-size: 12px; }
.access-success { color: var(--mint-dim); font-size: 12px; }

.mfa-enrolment {
  display: grid;
  gap: 14px;
  justify-items: center;
  padding-bottom: 4px;
}

.mfa-enrolment__qr {
  width: 150px;
  aspect-ratio: 1;
  border: 1px solid var(--line-strong);
  background:
    linear-gradient(45deg, color-mix(in srgb, var(--mint) 5%, transparent) 25%, transparent 25%) 0 0 / 12px 12px,
    linear-gradient(-45deg, color-mix(in srgb, var(--mint) 5%, transparent) 25%, transparent 25%) 0 0 / 12px 12px,
    var(--bg-2);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
  color: var(--ink-4);
  font: 10px var(--font-mono);
  text-transform: uppercase;
  letter-spacing: .1em;
}

.mfa-enrolment__qr-image {
  display: block;
  width: 100%;
  height: 100%;
  background: #fff;
  object-fit: contain;
}

.mfa-enrolment__mark { font-size: 26px; color: var(--mint-dim); }

.access-footer-links {
  display: flex;
  justify-content: center;
  gap: 20px;
  flex-wrap: wrap;
}

.access-footer-links a {
  color: var(--ink-4);
  text-decoration: none;
  transition: color .25s;
}

.access-footer-links a:hover { color: var(--mint); }
</style>
