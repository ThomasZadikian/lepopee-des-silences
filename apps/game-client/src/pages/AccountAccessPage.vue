<script setup lang="ts">
import { computed, ref } from 'vue';
import { RouterLink, useRouter } from 'vue-router';

import AccountAccessShell from '../features/account/components/AccountAccessShell.vue';

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

const email = ref('');
const displayName = ref('');
const password = ref('');
const passwordConfirmation = ref('');
const totpCode = ref('');
const ageConfirmed = ref(false);
const submitted = ref(false);
const error = ref<string | null>(null);

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
        subtitle: 'Un lien à usage unique vous a été envoyé. La traversée reste fermée tant que l’adresse n’est pas confirmée.',
        action: 'Mon adresse est confirmée',
      };
    case 'mfa-setup':
      return {
        kicker: 'Sécurité obligatoire',
        title: 'Lier Google Authenticator',
        subtitle: 'Scannez le QR code avec votre application TOTP puis saisissez le code à six chiffres.',
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

async function submit() {
  error.value = null;

  if (needsEmail.value && !email.value.trim()) {
    error.value = 'Une adresse e-mail est requise.';
    return;
  }
  if (props.mode === 'register' && !displayName.value.trim()) {
    error.value = 'Un pseudonyme de compte est requis.';
    return;
  }
  if (needsPassword.value && password.value.length < 12) {
    error.value = 'Le mot de passe doit contenir au moins 12 caractères.';
    return;
  }
  if (['register', 'password-reset'].includes(props.mode) && password.value !== passwordConfirmation.value) {
    error.value = 'Les mots de passe ne correspondent pas.';
    return;
  }
  if (props.mode === 'register' && !ageConfirmed.value) {
    error.value = 'Vous devez confirmer avoir au moins 16 ans.';
    return;
  }
  if (needsTotp.value && !/^\d{6}$/.test(totpCode.value)) {
    error.value = 'Le code doit contenir six chiffres.';
    return;
  }

  // Les appels API seront branchés sur les handlers Account/Auth du service Player.
  // Cette navigation matérialise déjà le parcours SFD et garde l'UI testable indépendamment.
  switch (props.mode) {
    case 'register':
      await router.push({ name: 'verify-email' });
      break;
    case 'verify-email':
      await router.push({ name: 'mfa-setup' });
      break;
    case 'mfa-setup':
    case 'mfa-challenge':
      await router.push({ name: 'character-selection' });
      break;
    case 'password-recovery':
      submitted.value = true;
      break;
    case 'password-reset':
      await router.push({ name: 'login' });
      break;
    default:
      await router.push({ name: 'mfa-challenge' });
      break;
  }
}
</script>

<template>
  <AccountAccessShell :kicker="content.kicker" :title="content.title" :subtitle="content.subtitle" narrow>
    <form class="access-form" @submit.prevent="submit">
      <div v-if="mode === 'mfa-setup'" class="mfa-enrolment">
        <div class="mfa-enrolment__qr" aria-label="Emplacement du QR code Google Authenticator">
          <span class="mfa-enrolment__mark">◇</span>
          <span>QR sécurisé</span>
        </div>
        <p class="mfa-enrolment__hint">
          Le secret TOTP sera fourni par le serveur et ne sera jamais journalisé côté client.
        </p>
      </div>

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
        <span class="access-field__label">Code Google Authenticator</span>
        <input v-model="totpCode" class="access-field__input access-field__input--code" inputmode="numeric" autocomplete="one-time-code" maxlength="6" placeholder="000000" />
      </label>

      <label v-if="mode === 'register'" class="age-confirmation">
        <input v-model="ageConfirmed" type="checkbox" />
        <span>Je confirme avoir au moins 16 ans.</span>
      </label>

      <p v-if="mode === 'verify-email'" class="access-note">
        Vous pouvez fermer cet écran : la validation est liée au token reçu par e-mail, pas à cette session de navigateur.
      </p>

      <p v-if="submitted" class="access-success">
        Si cette adresse correspond à un compte, le message de récupération a été envoyé.
      </p>
      <p v-if="error" class="access-error" role="alert">{{ error }}</p>

      <button class="access-submit" type="submit">
        <span class="access-submit__glyph">◈</span>
        <span>{{ content.action }}</span>
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

.access-submit:hover {
  background: color-mix(in srgb, var(--mint) 11%, transparent);
  color: var(--ink);
}

.access-submit__glyph { font-size: 12px; }

.access-note,
.mfa-enrolment__hint {
  color: var(--ink-4);
  font-size: 12px;
  line-height: 1.6;
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
