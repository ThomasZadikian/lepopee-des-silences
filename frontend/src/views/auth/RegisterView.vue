<template>
  <div style="min-height: 100vh; background: var(--rpg-cream); display: flex; flex-direction: column;">
    <!-- ── Header ─────────────────────────────────────────────────── -->
    <div
      style="
      padding: 20px 48px;
      border-bottom: 1px solid var(--rpg-border);
      display: flex; align-items: center; justify-content: space-between;
    "
    >
      <div style="font-family: var(--font-serif); font-size: 1.1rem; font-weight: 900; letter-spacing: -0.02em;">
        RPG_ESI07 <span style="font-weight: 300; color: var(--rpg-ink-muted);">· Codex</span>
      </div>
      <div style="font-size: 10px; font-weight: 600; letter-spacing: 0.15em; text-transform: uppercase; color: var(--rpg-ink-muted);">
        {{ new Date().toLocaleDateString('fr-FR') }}
      </div>
    </div>

    <!-- ── Corps ──────────────────────────────────────────────────── -->
    <div style="flex: 1; display: grid; grid-template-columns: 1fr 1fr; min-height: 0;">
      <!-- Colonne gauche — éditoriale -->
      <div
        style="
        padding: 64px 48px;
        border-right: 1px solid var(--rpg-border);
        display: flex; flex-direction: column; justify-content: space-between;
      "
      >
        <div>
          <div class="editorial-label mb-4">
            Nouveau marcheur
          </div>

          <div
            style="
            font-family: var(--font-serif);
            font-size: clamp(2.5rem, 4vw, 4rem);
            font-weight: 900; line-height: 1;
            letter-spacing: -0.03em; margin-bottom: 32px;
          "
          >
            Forge ton <span style="font-style: italic; color: var(--rpg-ink-muted);">nom.</span>
          </div>

          <div style="max-width: 320px; font-size: 14px; line-height: 1.7; color: var(--rpg-ink-muted); margin-bottom: 48px;">
            Le Codex tient l'archive de tes parties. Tes sauvegardes, ton
            inventaire, tes compétences et les créatures que tu as croisées
            y trouvent leur place. Inscris-toi pour synchroniser tes mondes.
          </div>
        </div>

        <!-- Ce que conserve le Codex -->
        <div>
          <div class="editorial-label mb-4">
            Ce que conserve le codex
          </div>
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 0; border-top: 1px solid var(--rpg-border);">
            <div
              v-for="item in codexFeatures"
              :key="item.title"
              style="padding: 16px 0; border-bottom: 1px solid var(--rpg-border);"
              :style="item.right ? 'padding-left: 24px; border-left: 1px solid rgba(0,0,0,0.06);' : 'padding-right: 24px;'"
            >
              <div style="font-family: var(--font-serif); font-size: 0.95rem; font-weight: 700; margin-bottom: 2px;">
                {{ item.title }}
              </div>
              <div style="font-size: 11px; color: var(--rpg-ink-muted);">
                {{ item.desc }}
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Colonne droite — formulaire -->
      <div style="padding: 64px 48px; display: flex; flex-direction: column;">
        <!-- Onglets -->
        <div style="display: flex; gap: 24px; margin-bottom: 40px; border-bottom: 1px solid var(--rpg-border); padding-bottom: 16px;">
          <div style="font-size: 11px; font-weight: 700; letter-spacing: 0.12em; text-transform: uppercase; border-bottom: 2px solid var(--rpg-ink); padding-bottom: 16px; margin-bottom: -17px;">
            Inscription
          </div>
          <div
            style="font-size: 11px; font-weight: 600; letter-spacing: 0.12em; text-transform: uppercase; color: var(--rpg-ink-muted); cursor: pointer;"
            @click="$router.push({ name: 'Login' })"
          >
            ○ Connexion
          </div>
        </div>

        <!-- Champs -->
        <div style="max-width: 400px; width: 100%;">
          <!-- Username -->
          <div style="margin-bottom: 24px;">
            <div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
              <div class="editorial-label">
                ■ Nom de marcheur
              </div>
              <div style="font-size: 10px; color: var(--rpg-ink-muted); font-style: italic;">
                Visible publiquement · 3–24 car.
              </div>
            </div>
            <input
              v-model="form.username"
              type="text"
              placeholder="veilseeker"
              style="
                width: 100%; padding: 12px 14px;
                border: 1px solid rgba(0,0,0,0.15);
                background: transparent;
                font-family: var(--font-sans); font-size: 14px;
                outline: none; transition: border-color 0.15s;
              "
              @focus="e => (e.target as HTMLInputElement).style.borderColor = 'var(--rpg-ink)'"
              @blur="e => (e.target as HTMLInputElement).style.borderColor = 'rgba(0,0,0,0.15)'"
            >
          </div>

          <!-- Email -->
          <div style="margin-bottom: 24px;">
            <div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
              <div class="editorial-label">
                ■ Adresse email
              </div>
              <div style="font-size: 10px; color: var(--rpg-ink-muted); font-style: italic;">
                Pour récupération uniquement.
              </div>
            </div>
            <input
              v-model="form.email"
              type="email"
              placeholder="ilyana@veilstone.codex"
              style="
                width: 100%; padding: 12px 14px;
                border: 1px solid rgba(0,0,0,0.15);
                background: transparent;
                font-family: var(--font-sans); font-size: 14px;
                outline: none; transition: border-color 0.15s;
              "
              @focus="e => (e.target as HTMLInputElement).style.borderColor = 'var(--rpg-ink)'"
              @blur="e => (e.target as HTMLInputElement).style.borderColor = 'rgba(0,0,0,0.15)'"
            >
          </div>

          <!-- Mot de passe -->
          <div style="margin-bottom: 32px;">
            <div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
              <div class="editorial-label">
                ■ Mot de passe
              </div>
              <div style="font-size: 10px; color: var(--rpg-ink-muted); font-style: italic;">
                Min. 8 car. · 1 majuscule · 1 chiffre
              </div>
            </div>
            <input
              v-model="form.password"
              type="password"
              placeholder="••••••••••••"
              style="
                width: 100%; padding: 12px 14px;
                border: 1px solid rgba(0,0,0,0.15);
                background: transparent;
                font-family: var(--font-sans); font-size: 14px;
                outline: none; transition: border-color 0.15s;
              "
              @focus="e => (e.target as HTMLInputElement).style.borderColor = 'var(--rpg-ink)'"
              @blur="e => (e.target as HTMLInputElement).style.borderColor = 'rgba(0,0,0,0.15)'"
            >
          </div>

          <!-- Erreur / Succès -->
          <div
            v-if="error"
            style="
            font-size: 12px; color: #C0392B;
            border-left: 2px solid #C0392B;
            padding: 8px 12px; margin-bottom: 20px;
            background: rgba(192,57,43,0.05);
          "
          >
            {{ error }}
          </div>
          <div
            v-if="success"
            style="
            font-size: 12px; color: #1E8449;
            border-left: 2px solid #1E8449;
            padding: 8px 12px; margin-bottom: 20px;
            background: rgba(30,132,73,0.05);
          "
          >
            {{ success }}
          </div>

          <!-- Bouton -->
          <button
            style="
              width: 100%; padding: 14px;
              background: var(--rpg-ink); color: white;
              border: none; cursor: pointer;
              font-family: var(--font-sans);
              font-size: 11px; font-weight: 700;
              letter-spacing: 0.15em; text-transform: uppercase;
              transition: opacity 0.15s;
              display: flex; align-items: center; justify-content: center; gap: 8px;
            "
            :disabled="loading"
            @mouseenter="e => (e.currentTarget as HTMLElement).style.opacity = '0.85'"
            @mouseleave="e => (e.currentTarget as HTMLElement).style.opacity = '1'"
            @click="handleRegister"
          >
            <span v-if="loading">Création en cours...</span>
            <span v-else>Créer mon compte →</span>
          </button>
        </div>

        <!-- Footer note -->
        <div style="margin-top: auto; padding-top: 32px;">
          <div style="font-size: 11px; color: var(--rpg-ink-muted); line-height: 1.6;">
            En t'inscrivant, tu peux à tout moment exporter ou supprimer tes données
            depuis la page
            <span
              style="text-decoration: underline; cursor: pointer;"
              @click="$router.push({ name: 'Rgpd' })"
            >
              Confidentialité.
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- ── Footer ─────────────────────────────────────────────────── -->
    <div
      style="
      padding: 16px 48px;
      border-top: 1px solid var(--rpg-border);
      display: flex; justify-content: space-between; align-items: center;
    "
    >
      <div style="font-size: 10px; font-weight: 600; letter-spacing: 0.15em; text-transform: uppercase; color: var(--rpg-ink-muted);">
        RPG ESI07 · Édition Joueur
      </div>
      <div style="font-size: 10px; color: var(--rpg-ink-muted);">
        P. 01 / 03 — Inscription
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { reactive, ref } from 'vue'

const auth    = useAuthStore()
const form    = reactive({ username: '', email: '', password: '' })
const loading = ref(false)
const error   = ref('')
const success = ref('')

const codexFeatures = [
  { title: 'Sauvegardes', desc: '10 emplacements',   right: false },
  { title: 'Personnages', desc: 'tous niveaux',       right: true  },
  { title: 'Inventaire',  desc: 'items, runes, or',   right: false },
  { title: 'Compétences', desc: 'arbres complets',    right: true  },
  { title: 'Bestiaire',   desc: 'ennemis croisés',    right: false },
  { title: 'Statistiques',desc: 'temps, morts, quêtes', right: true },
]

async function handleRegister() {
  error.value   = ''
  success.value = ''
  loading.value = true
  try {
    await auth.register(form)
    success.value = 'Compte créé ! Redirection vers la connexion...'
  } catch (err: any) {
    error.value = err.response?.data?.message ?? "Nom d'utilisateur déjà pris"
  } finally {
    loading.value = false
  }
}
</script>