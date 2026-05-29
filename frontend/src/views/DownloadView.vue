<template>
  <div>
    <!-- ── HERO éditorial ─────────────────────────────────────────── -->
    <div
      style="
        margin: -32px -32px 40px -32px;
        position: relative; overflow: hidden;
        border-bottom: 1px solid var(--rpg-border);
      "
    >
      <div style="display: grid; grid-template-columns: 1fr 1fr 1fr 1fr; min-height: 160px;">
        <div style="padding: 40px 32px; border-right: 1px solid var(--rpg-border);">
          <div class="editorial-label mb-3">
            Distribution · Client lourd
          </div>
          <div style="font-family:var(--font-serif);font-size:clamp(2rem,3vw,3rem);font-weight:900;line-height:1;letter-spacing:-0.03em;margin-bottom:12px;">
            Télécharger
            <span style="font-style:italic;color:var(--rpg-ink-muted);display:block;">le jeu.</span>
          </div>
        </div>
        <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
          <div class="editorial-label mb-3">
            Version
          </div>
          <div style="font-family:var(--font-serif);font-size:2.5rem;font-weight:700;line-height:1;">
            v1.0.0
          </div>
        </div>
        <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
          <div class="editorial-label mb-3">
            Plateforme
          </div>
          <div style="font-family:var(--font-serif);font-size:1.6rem;font-weight:700;line-height:1.2;">
            Windows x64
          </div>
        </div>
        <div style="padding:40px 32px;">
          <div class="editorial-label mb-3">
            Statut
          </div>
          <div style="font-family:var(--font-serif);font-size:1.4rem;font-weight:700;line-height:1.2;color:#1E8449;">
            Disponible
          </div>
        </div>
      </div>
    </div>

    <v-row no-gutters style="align-items: flex-start; gap: 0;">

      <!-- ── COLONNE PRINCIPALE ──────────────────────────────────── -->
      <v-col cols="12" md="7" style="padding-right: 48px; border-right: 1px solid var(--rpg-border);">

        <!-- Bouton download -->
        <div style="margin-bottom: 40px;">
          <div class="editorial-label mb-4">
            Téléchargement
          </div>
          <a
            :href="downloadUrl"
            download
            style="
              display: inline-flex;
              align-items: center;
              gap: 12px;
              padding: 18px 32px;
              background: var(--rpg-ink);
              color: var(--rpg-cream);
              font-family: var(--font-serif);
              font-size: 1.1rem;
              font-weight: 700;
              letter-spacing: -0.01em;
              text-decoration: none;
              border: none;
              cursor: pointer;
              transition: opacity 0.15s ease;
            "
                @mouseenter="(e) => (e.currentTarget as HTMLAnchorElement).style.opacity = '0.85'"
                @mouseleave="(e) => (e.currentTarget as HTMLAnchorElement).style.opacity = '1'"
          >
            <span style="font-size: 1.3rem;">↓</span>
            RPG_ESI07_v1.0.0_Windows.zip
          </a>
          <div class="editorial-label mt-3" style="color: var(--rpg-ink-muted);">
            Windows 64-bit · Extraction requise
          </div>
        </div>

        <!-- Prérequis -->
        <div style="border-top: 1px solid var(--rpg-border); padding-top: 32px; margin-bottom: 40px;">
          <div class="editorial-label mb-4">
            Prérequis
          </div>
          <div
            v-for="req in requirements"
            :key="req.label"
            style="
              display: flex;
              justify-content: space-between;
              align-items: baseline;
              padding: 12px 0;
              border-bottom: 1px solid rgba(0,0,0,0.06);
            "
          >
            <span style="font-size: 13px; color: var(--rpg-ink-muted); font-weight: 600; letter-spacing: 0.06em; text-transform: uppercase;">
              {{ req.label }}
            </span>
            <span style="font-family: var(--font-serif); font-size: 1rem; font-weight: 600;">
              {{ req.value }}
            </span>
          </div>
        </div>

        <!-- Procédure d'installation -->
        <div style="border-top: 1px solid var(--rpg-border); padding-top: 32px;">
          <div class="editorial-label mb-4">
            Installation
          </div>
          <div
            v-for="(step, i) in steps"
            :key="i"
            style="display: flex; gap: 20px; margin-bottom: 28px; align-items: flex-start;"
          >
            <div style="
              font-family: var(--font-serif);
              font-size: 1.8rem;
              font-weight: 900;
              line-height: 1;
              color: rgba(0,0,0,0.12);
              min-width: 32px;
              padding-top: 2px;
            ">
              {{ String(i + 1).padStart(2, '0') }}
            </div>
            <div>
              <div style="font-family: var(--font-serif); font-size: 1rem; font-weight: 700; margin-bottom: 4px;">
                {{ step.title }}
              </div>
              <div style="font-size: 13px; color: var(--rpg-ink-muted); line-height: 1.6;">
                {{ step.description }}
              </div>
            </div>
          </div>
        </div>

      </v-col>

      <!-- ── COLONNE LATÉRALE ────────────────────────────────────── -->
      <v-col cols="12" md="5" style="padding-left: 48px;">

        <!-- MFA -->
        <div style="margin-bottom: 40px;">
          <div class="editorial-label mb-4">
            Authentification MFA
          </div>
          <div
            style="
              border: 1px solid var(--rpg-border);
              padding: 24px;
              background: var(--rpg-cream-dark, rgba(0,0,0,0.02));
            "
          >
            <div style="font-family: var(--font-serif); font-size: 1rem; font-weight: 700; margin-bottom: 12px;">
              Première connexion
            </div>
            <div style="font-size: 13px; color: var(--rpg-ink-muted); line-height: 1.7; margin-bottom: 16px;">
              Lors de votre première connexion, le jeu vous demandera de configurer l'authentification à deux facteurs (TOTP).
            </div>
            <div
              v-for="(mfaStep, i) in mfaSteps"
              :key="i"
              style="display: flex; gap: 12px; margin-bottom: 10px; align-items: flex-start;"
            >
              <div style="
                width: 20px; height: 20px;
                background: var(--rpg-ink);
                color: var(--rpg-cream, #fff);
                font-size: 10px;
                font-weight: 700;
                display: flex; align-items: center; justify-content: center;
                flex-shrink: 0;
                margin-top: 1px;
              ">
                {{ i + 1 }}
              </div>
              <div style="font-size: 12px; color: var(--rpg-ink-muted); line-height: 1.5;">
                {{ mfaStep }}
              </div>
            </div>
          </div>
        </div>

        <!-- Liens utiles -->
        <div style="border-top: 1px solid var(--rpg-border); padding-top: 32px; margin-bottom: 40px;">
          <div class="editorial-label mb-4">
            Liens
          </div>
          <div
            v-for="link in links"
            :key="link.label"
            style="
              display: flex;
              justify-content: space-between;
              align-items: center;
              padding: 14px 0;
              border-bottom: 1px solid rgba(0,0,0,0.06);
              cursor: pointer;
            "
            @click="openLink(link.url)"
          >
            <div>
              <div style="font-size: 13px; font-weight: 600;">{{ link.label }}</div>
              <div style="font-size: 11px; color: var(--rpg-ink-muted);">{{ link.description }}</div>
            </div>
            <div style="font-size: 11px; font-weight: 700; letter-spacing: 0.08em; text-transform: uppercase;">
              → 
            </div>
          </div>
        </div>

        <!-- Notes de version -->
        <div style="border-top: 1px solid var(--rpg-border); padding-top: 32px;">
          <div class="editorial-label mb-4">
            Notes v1.0.0
          </div>
          <div
            v-for="note in releaseNotes"
            :key="note"
            style="
              font-size: 12px;
              color: var(--rpg-ink-muted);
              padding: 6px 0;
              border-bottom: 1px solid rgba(0,0,0,0.04);
              line-height: 1.5;
            "
          >
            {{ note }}
          </div>
        </div>

      </v-col>
    </v-row>
  </div>
</template>

<script setup lang="ts">
const downloadUrl = 'https://github.com/ThomasZadikian/rpg_esi07/releases/download/v1.0.0/RPG_ESI07.zip'

const requirements = [
  { label: 'OS', value: 'Windows 10 / 11 (64-bit)' },
  { label: 'Espace disque', value: '500 MB minimum' },
  { label: 'RAM', value: '4 GB minimum' },
  { label: 'Réseau', value: 'Connexion Internet requise' },
  { label: 'Compte', value: 'Inscription sur le portail' },
]

const steps = [
  {
    title: 'Télécharger l\'archive',
    description: 'Cliquez sur le bouton de téléchargement ci-dessus pour obtenir RPG_ESI07_v1.0.0_Windows.zip.'
  },
  {
    title: 'Extraire l\'archive',
    description: 'Faites un clic droit sur le fichier ZIP → "Extraire tout". Choisissez un dossier de destination.'
  },
  {
    title: 'Lancer le jeu',
    description: 'Ouvrez le dossier extrait et double-cliquez sur RPG_ESI07.exe pour démarrer le jeu.'
  },
  {
    title: 'Se connecter',
    description: 'Utilisez vos identifiants créés sur ce portail. Si vous n\'avez pas encore de compte, inscrivez-vous d\'abord.'
  },
  {
    title: 'Configurer le MFA',
    description: 'À la première connexion, configurez l\'authentification à deux facteurs avec une application TOTP (Google Authenticator, Authy).'
  },
]

const mfaSteps = [
  'Entrez votre nom d\'utilisateur et mot de passe.',
  'Un QR code s\'affiche — scannez-le avec Google Authenticator ou Authy.',
  'Entrez le code à 6 chiffres généré par l\'application.',
  'Connexion validée. Le code sera demandé à chaque connexion.',
]

const links = [
  {
    label: 'Créer un compte',
    description: 'Inscription sur le portail web',
    url: '/register'
  },
  {
    label: 'GitHub Release',
    description: 'Notes de version complètes',
    url: 'https://github.com/ThomasZadikian/rpg_esi07/releases/tag/v1.0.0'
  },
]

const releaseNotes = [
  'Authentification JWT + MFA TOTP obligatoire',
  'Système de combat ATB avec boss multi-phases',
  'Bestiaire — 70 ennemis avec scaling dynamique',
  'Synchronisation temps réel avec le portail web',
  'Sauvegarde automatique en fin de combat',
  'Machine à états Markov pour l\'IA ennemie',
]

function openLink(url: string) {
  if (url.startsWith('/')) {
    window.location.href = url
  } else {
    window.open(url, '_blank')
  }
}
</script>