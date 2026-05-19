<template>
  <div style="min-height: 100vh; background: var(--rpg-cream); display: flex; flex-direction: column;">
    <!-- ── Header ─────────────────────────────────────────────────── -->
    <div style="padding: 20px 48px; border-bottom: 1px solid var(--rpg-border); display: flex; align-items: center; justify-content: space-between;">
      <div style="font-family: var(--font-serif); font-size: 1.1rem; font-weight: 900; letter-spacing: -0.02em;">
        RPG_ESI07 <span style="font-weight: 300; color: var(--rpg-ink-muted);">· Codex</span>
      </div>
      <div style="font-size: 10px; font-weight: 600; letter-spacing: 0.15em; text-transform: uppercase; color: var(--rpg-ink-muted);">
        {{ new Date().toLocaleDateString('fr-FR') }}
      </div>
    </div>

    <!-- ── Corps ──────────────────────────────────────────────────── -->
    <div style="flex: 1; display: grid; grid-template-columns: 1fr 1fr; min-height: 0;">
      <!-- Colonne gauche -->
      <div style="padding: 64px 48px; border-right: 1px solid var(--rpg-border); display: flex; flex-direction: column; justify-content: space-between;">
        <div>
          <div class="editorial-label mb-4">Sécurité du Codex</div>
          <div style="font-family: var(--font-serif); font-size: clamp(2rem, 3vw, 3rem); font-weight: 900; line-height: 1; letter-spacing: -0.03em; margin-bottom: 32px;">
            Double verrou <span style="font-style: italic; color: var(--rpg-ink-muted);">activé.</span>
          </div>
          <div style="max-width: 320px; font-size: 14px; line-height: 1.7; color: var(--rpg-ink-muted); margin-bottom: 48px;">
            Le Codex exige une seconde clé. Scannez le sceau ci-contre avec
            votre application d'authentification — Google Authenticator,
            Authy ou équivalent.
          </div>
        </div>

        <div>
          <div class="editorial-label mb-4">Procédure</div>
          <div style="border-top: 1px solid var(--rpg-border);">
            <div v-for="(step, i) in steps" :key="i"
              style="padding: 14px 0; border-bottom: 1px solid var(--rpg-border); display: flex; gap: 16px; align-items: flex-start;">
              <div style="font-family: var(--font-serif); font-size: 1.2rem; font-weight: 900; color: var(--rpg-ink-muted); min-width: 24px;">
                {{ i + 1 }}.
              </div>
              <div>
                <div style="font-size: 13px; font-weight: 600; margin-bottom: 2px;">{{ step.title }}</div>
                <div style="font-size: 11px; color: var(--rpg-ink-muted);">{{ step.desc }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Colonne droite -->
      <div style="padding: 64px 48px; display: flex; flex-direction: column;">
        <!-- Étape 1 : QR Code -->
        <div v-if="currentStep === 1">
          <div class="editorial-label mb-4">■ Étape 1 — Scanner le sceau</div>

          <div v-if="loading" style="display: flex; align-items: center; gap: 12px; margin-bottom: 32px;">
            <div style="font-size: 13px; color: var(--rpg-ink-muted);">Génération du sceau en cours...</div>
          </div>

          <div v-else-if="qrCodeUri" style="margin-bottom: 32px;">
            <div style="border: 1px solid var(--rpg-border); padding: 24px; display: inline-block; margin-bottom: 16px;">
              <img :src="qrCodeUri" alt="QR Code MFA" width="180" height="180"/>
            </div>
            <div style="font-size: 11px; color: var(--rpg-ink-muted); margin-bottom: 8px;">
              Clé manuelle si le scan échoue :
            </div>
            <div style="font-family: monospace; font-size: 12px; background: rgba(0,0,0,0.04); padding: 8px 12px; letter-spacing: 0.1em; border: 1px solid var(--rpg-border);">
              {{ secret }}
            </div>
          </div>

          <div v-if="error" style="font-size: 12px; color: #C0392B; border-left: 2px solid #C0392B; padding: 8px 12px; margin-bottom: 20px; background: rgba(192,57,43,0.05);">
            {{ error }}
          </div>

          <button
            style="padding: 14px 32px; background: var(--rpg-ink); color: white; border: none; cursor: pointer; font-family: var(--font-sans); font-size: 11px; font-weight: 700; letter-spacing: 0.15em; text-transform: uppercase; transition: opacity 0.15s;"
            :disabled="!qrCodeUri || loading"
            @mouseenter="e => (e.currentTarget as HTMLElement).style.opacity = '0.85'"
            @mouseleave="e => (e.currentTarget as HTMLElement).style.opacity = '1'"
            @click="currentStep = 2"
          >
            J'ai scanné le sceau →
          </button>
        </div>

        <!-- Étape 2 : Vérification -->
        <div v-if="currentStep === 2">
          <div class="editorial-label mb-4">■ Étape 2 — Confirmer le code</div>

          <div style="max-width: 400px; font-size: 13px; color: var(--rpg-ink-muted); margin-bottom: 32px; line-height: 1.7;">
            Saisissez le code à 6 chiffres généré par votre application.
            Ce code change toutes les 30 secondes.
          </div>

          <div style="margin-bottom: 32px;">
            <div class="editorial-label mb-2">■ Code d'authentification</div>
            <input
              v-model="code"
              type="text"
              placeholder="123456"
              maxlength="6"
              style="width: 200px; padding: 12px 14px; border: 1px solid rgba(0,0,0,0.15); background: transparent; font-family: monospace; font-size: 20px; letter-spacing: 0.3em; outline: none; text-align: center;"
              @keyup.enter="handleVerify"
              @focus="e => (e.target as HTMLInputElement).style.borderColor = 'var(--rpg-ink)'"
              @blur="e => (e.target as HTMLInputElement).style.borderColor = 'rgba(0,0,0,0.15)'"
            />
          </div>

          <div v-if="error" style="font-size: 12px; color: #C0392B; border-left: 2px solid #C0392B; padding: 8px 12px; margin-bottom: 20px; background: rgba(192,57,43,0.05);">
            {{ error }}
          </div>

          <div style="display: flex; gap: 16px;">
            <button
              style="padding: 14px 32px; background: var(--rpg-ink); color: white; border: none; cursor: pointer; font-family: var(--font-sans); font-size: 11px; font-weight: 700; letter-spacing: 0.15em; text-transform: uppercase; transition: opacity 0.15s;"
              :disabled="verifying || code.length !== 6"
              @mouseenter="e => (e.currentTarget as HTMLElement).style.opacity = '0.85'"
              @mouseleave="e => (e.currentTarget as HTMLElement).style.opacity = '1'"
              @click="handleVerify"
            >
              <span v-if="verifying">Vérification...</span>
              <span v-else">Ouvrir le Codex →</span>
            </button>

            <button
              style="padding: 14px 32px; background: transparent; color: var(--rpg-ink); border: 1px solid var(--rpg-border); cursor: pointer; font-family: var(--font-sans); font-size: 11px; font-weight: 700; letter-spacing: 0.15em; text-transform: uppercase;"
              @click="currentStep = 1"
            >
              ← Retour
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ── Footer ─────────────────────────────────────────────────── -->
    <div style="padding: 16px 48px; border-top: 1px solid var(--rpg-border); display: flex; justify-content: space-between; align-items: center;">
      <div style="font-size: 10px; font-weight: 600; letter-spacing: 0.15em; text-transform: uppercase; color: var(--rpg-ink-muted);">
        RPG ESI07 · Sécurité
      </div>
      <div style="font-size: 10px; color: var(--rpg-ink-muted);">
        P. 03 / 03 — Double authentification
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

const router      = useRouter()
const auth        = useAuthStore()
const currentStep = ref(1)
const loading     = ref(false)
const verifying   = ref(false)
const error       = ref('')
const qrCodeUri   = ref('')
const secret      = ref('')
const code        = ref('')

const steps = [
  { title: 'Installer une application',  desc: 'Google Authenticator, Authy ou équivalent TOTP' },
  { title: 'Scanner le sceau',           desc: 'Ouvrez l\'application et scannez le QR code' },
  { title: 'Saisir le code',             desc: 'Entrez le code à 6 chiffres généré' },
  { title: 'Accéder au Codex',           desc: 'Votre compte est désormais sécurisé' },
]

onMounted(async () => {
  loading.value = true
  error.value   = ''
  try {
    const res     = await auth.setupMfa()
    qrCodeUri.value = res.qrCodeUri
    secret.value    = res.secret
  } catch {
    error.value = 'Impossible de générer le QR code. Retournez à la connexion.'
  } finally {
    loading.value = false
  }
})

async function handleVerify() {
  if (code.value.length !== 6) return
  error.value    = ''
  verifying.value = true
  try {
    await auth.verifyMfaSetup(code.value)
    await router.push({ name: 'Dashboard' })
  } catch {
    error.value = 'Code incorrect ou expiré. Réessayez.'
  } finally {
    verifying.value = false
  }
}
</script>