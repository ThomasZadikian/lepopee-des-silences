<script setup lang="ts">
import { ref } from 'vue';
import { RouterLink } from 'vue-router';

import AccountAccessShell from '../features/account/components/AccountAccessShell.vue';

const analyticsConsent = ref(false);
const showClosureConfirm = ref(false);
const closureRequested = ref(false);
const exportPrepared = ref(false);

const sessions = [
  { id: 'desktop', label: 'Navigateur actuel', detail: 'Session active · appareil autorisé', current: true },
  { id: 'mobile', label: 'Appareil mobile', detail: 'Session reconnue · aucune Run active', current: false },
];

function requestClosure() {
  showClosureConfirm.value = false;
  closureRequested.value = true;
}
</script>

<template>
  <AccountAccessShell
    kicker="Compte"
    title="Votre trace dans le Palais"
    subtitle="Identité, sécurité, appareils et droits sur vos données. Les données nécessaires au jeu restent distinctes des traitements facultatifs."
  >
    <div class="account-page">
      <section class="account-section">
        <div class="account-section__heading">
          <span class="account-section__glyph">◇</span>
          <div>
            <h2>Identité</h2>
            <p>Informations visibles et adresse de connexion.</p>
          </div>
        </div>
        <dl class="account-data-grid">
          <div><dt>Pseudonyme du compte</dt><dd>Nocturne</dd></div>
          <div><dt>Adresse e-mail</dt><dd>joueur@example.fr · vérifiée</dd></div>
          <div><dt>Âge déclaré</dt><dd>16 ans ou plus · aucune date de naissance stockée</dd></div>
        </dl>
      </section>

      <section class="account-section">
        <div class="account-section__heading">
          <span class="account-section__glyph">◈</span>
          <div>
            <h2>Sécurité</h2>
            <p>La double authentification est obligatoire pour tous les comptes.</p>
          </div>
        </div>
        <div class="security-row">
          <div><strong>Google Authenticator</strong><span>Activé</span></div>
          <button type="button" class="text-action">Regénérer les codes de récupération</button>
        </div>
        <div class="security-row">
          <div><strong>Mot de passe</strong><span>Dernière modification non affichée publiquement</span></div>
          <RouterLink class="text-action" :to="{ name: 'password-reset' }">Modifier le mot de passe</RouterLink>
        </div>
      </section>

      <section class="account-section">
        <div class="account-section__heading">
          <span class="account-section__glyph">○</span>
          <div>
            <h2>Appareils et sessions</h2>
            <p>Plusieurs connexions sont possibles, mais une seule session possède l’autorité d’écriture sur une Run.</p>
          </div>
        </div>
        <ul class="session-list">
          <li v-for="session in sessions" :key="session.id" class="session-card">
            <div>
              <strong>{{ session.label }}</strong>
              <span>{{ session.detail }}</span>
            </div>
            <span v-if="session.current" class="session-card__state">Cet appareil</span>
            <button v-else type="button" class="text-action text-action--danger">Révoquer</button>
          </li>
        </ul>
      </section>

      <section class="account-section">
        <div class="account-section__heading">
          <span class="account-section__glyph">□</span>
          <div>
            <h2>Confidentialité et consentements</h2>
            <p>Les traitements nécessaires au gameplay ne sont pas présentés comme des consentements révocables.</p>
          </div>
        </div>
        <label class="consent-row">
          <div>
            <strong>Mesures d’usage facultatives</strong>
            <span>Aide à comprendre l’utilisation de l’interface. Désactivable à tout moment.</span>
          </div>
          <input v-model="analyticsConsent" type="checkbox" aria-label="Autoriser les mesures d’usage facultatives" />
        </label>
      </section>

      <section class="account-section">
        <div class="account-section__heading">
          <span class="account-section__glyph">▤</span>
          <div>
            <h2>Vos données</h2>
            <p>Un export lisible par un humain, structuré par catégories, plutôt qu’un fichier JSON brut.</p>
          </div>
        </div>
        <div class="data-export">
          <button type="button" class="primary-action" @click="exportPrepared = true">
            <span>▤</span><span>Préparer mon export</span>
          </button>
          <div v-if="exportPrepared" class="data-export__preview">
            <strong>Export prêt</strong>
            <p>Identité · personnages · progression · historique des consentements · sessions · demandes RGPD.</p>
            <button type="button" class="text-action">Télécharger le document lisible</button>
          </div>
        </div>
      </section>

      <section class="account-section account-section--danger">
        <div class="account-section__heading">
          <span class="account-section__glyph">×</span>
          <div>
            <h2>Fermer le compte</h2>
            <p>La demande ouvre un délai de rétractation de 30 jours avant anonymisation irréversible des données personnelles.</p>
          </div>
        </div>
        <p v-if="closureRequested" class="closure-notice">
          Demande enregistrée. Tant que le délai de 30 jours n’est pas écoulé, elle peut être annulée.
        </p>
        <button v-else type="button" class="danger-action" @click="showClosureConfirm = true">Demander la fermeture du compte</button>
      </section>
    </div>

    <Teleport to="body">
      <div v-if="showClosureConfirm" class="closure-backdrop" @click.self="showClosureConfirm = false">
        <div class="closure-dialog">
          <h2>Demander la fermeture&nbsp;?</h2>
          <p>Votre compte ne sera pas anonymisé immédiatement. Un délai de 30 jours vous permettra d’annuler cette demande.</p>
          <div class="closure-dialog__actions">
            <button type="button" class="text-action" @click="showClosureConfirm = false">Annuler</button>
            <button type="button" class="danger-action" @click="requestClosure">Confirmer la demande</button>
          </div>
        </div>
      </div>
    </Teleport>
  </AccountAccessShell>
</template>

<style scoped>
.account-page { display: grid; text-align: left; }

.account-section {
  padding: 26px 28px;
  border-bottom: 1px solid var(--line-soft);
  display: grid;
  gap: 20px;
}

.account-section:last-child { border-bottom: 0; }
.account-section--danger { background: color-mix(in srgb, var(--danger) 4%, transparent); }

.account-section__heading { display: flex; align-items: flex-start; gap: 13px; }
.account-section__glyph { color: var(--mint-dim); width: 20px; padding-top: 2px; }
.account-section--danger .account-section__glyph { color: var(--danger); }

.account-section h2 {
  margin: 0 0 5px;
  font-family: var(--font-display);
  font-style: italic;
  font-size: 22px;
  font-weight: 400;
  color: var(--ink);
}

.account-section p { margin: 0; color: var(--ink-4); font-size: 12px; line-height: 1.55; }

.account-data-grid {
  margin: 0;
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
}

.account-data-grid div { padding: 14px; border: 1px solid var(--line-soft); background: var(--bg-2); }
.account-data-grid dt { color: var(--ink-4); font-size: 10px; text-transform: uppercase; letter-spacing: .12em; }
.account-data-grid dd { margin: 7px 0 0; color: var(--ink-2); font-size: 12px; line-height: 1.5; }

.security-row,
.session-card,
.consent-row {
  min-height: 54px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  padding: 12px 0;
  border-top: 1px solid var(--line-soft);
}

.security-row:first-of-type { border-top: 0; }
.security-row div,
.session-card div,
.consent-row div { display: grid; gap: 3px; }
.security-row strong,
.session-card strong,
.consent-row strong { color: var(--ink-2); font-size: 13px; font-weight: 500; }
.security-row span,
.session-card span,
.consent-row span { color: var(--ink-4); font-size: 11px; }

.session-list { margin: 0; padding: 0; list-style: none; }
.session-card__state { color: var(--mint-dim) !important; font-family: var(--font-mono); }

.consent-row input { width: 18px; height: 18px; accent-color: var(--mint-dim); }

.text-action,
.primary-action,
.danger-action {
  border: 0;
  background: transparent;
  color: var(--mint-dim);
  font: 600 10px var(--font);
  letter-spacing: .1em;
  text-transform: uppercase;
  text-decoration: none;
  cursor: pointer;
}

.text-action--danger,
.danger-action { color: var(--danger); }

.primary-action,
.danger-action {
  justify-self: start;
  padding: 10px 13px;
  border: 1px solid currentColor;
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.data-export { display: flex; align-items: center; gap: 18px; flex-wrap: wrap; }
.data-export__preview { display: grid; gap: 4px; }
.data-export__preview strong { color: var(--ink-2); font-size: 12px; }
.closure-notice { color: var(--mint-dim) !important; }

.closure-backdrop {
  position: fixed;
  inset: 0;
  z-index: var(--z-modal);
  background: rgba(5, 6, 9, .78);
  display: grid;
  place-items: center;
  padding: 20px;
}

.closure-dialog {
  width: min(440px, 100%);
  padding: 26px;
  border: 1px solid var(--line);
  background: var(--panel);
  box-shadow: var(--shadow-panel);
  color: var(--ink);
  text-align: left;
}

.closure-dialog h2 { margin: 0 0 10px; font-family: var(--font-display); font-style: italic; font-weight: 400; }
.closure-dialog p { color: var(--ink-3); font-size: 12px; line-height: 1.6; }
.closure-dialog__actions { margin-top: 22px; display: flex; justify-content: flex-end; gap: 14px; }

@media (max-width: 720px) {
  .account-section { padding: 22px 18px; }
  .account-data-grid { grid-template-columns: 1fr; }
  .security-row, .session-card, .consent-row { align-items: flex-start; flex-direction: column; gap: 10px; }
}
</style>
