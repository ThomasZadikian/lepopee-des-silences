<template>
  <div>

    <!-- ── HERO ───────────────────────────────────────────────────── -->
    <div
      style="
        margin: -32px -32px 40px -32px;
        position: relative; overflow: hidden;
        border-bottom: 1px solid var(--rpg-border);
      "
    >
      <div style="display: grid; grid-template-columns: 1fr 1fr 1fr 1fr; min-height: 160px;">

        <!-- Titre -->
        <div style="padding: 40px 32px; border-right: 1px solid var(--rpg-border);">
          <div class="editorial-label mb-3">
            Compte {{ auth.username }}
          </div>
          <div style="
            font-family: var(--font-serif);
            font-size: clamp(2rem, 3vw, 3rem);
            font-weight: 900; line-height: 1;
            letter-spacing: -0.03em; margin-bottom: 12px;
          ">
            Confidentialité
            <span style="font-style:italic;color:var(--rpg-ink-muted);display:block;">
              &amp; données
            </span>
          </div>
          <div style="font-size:13px;color:var(--rpg-ink-muted);line-height:1.6;max-width:220px;">
            Tu es maître de ton archive. Consulte, exporte ou supprime
            tes données à tout moment — conformément au RGPD.
          </div>
        </div>

        <!-- Stat 1 -->
        <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
          <div class="editorial-label mb-3">Données stockées</div>
          <div style="font-family:var(--font-serif);font-size:2.5rem;font-weight:700;line-height:1;">
            —
          </div>
        </div>

        <!-- Stat 2 -->
        <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
          <div class="editorial-label mb-3">Catégories</div>
          <div style="font-family:var(--font-serif);font-size:2.5rem;font-weight:700;line-height:1;">
            3
          </div>
        </div>

        <!-- Stat 3 -->
        <div style="padding:40px 32px;">
          <div class="editorial-label mb-3">Conservation</div>
          <div style="font-family:var(--font-serif);font-size:1.6rem;font-weight:700;line-height:1.2;">
            Tant que compte actif
          </div>
        </div>
      </div>
    </div>

    <!-- ── 3 DROITS ───────────────────────────────────────────────── -->
    <div style="display:grid;grid-template-columns:1fr 1fr 1fr;gap:0;margin-bottom:40px;border:1px solid var(--rpg-border);">

      <!-- Droit 01 — Export ZIP -->
      <div style="padding:28px;border-right:1px solid var(--rpg-border);">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
          <div class="editorial-label">Droit 01</div>
          <div style="font-size:16px;">→</div>
        </div>
        <div class="editorial-label mb-2" style="color:var(--rpg-ink-muted);">■ Récupérer</div>
        <div style="font-family:var(--font-serif);font-size:1.4rem;font-weight:700;margin-bottom:12px;line-height:1.2;">
          Exporter mes données
        </div>
        <div style="font-size:13px;color:var(--rpg-ink-muted);line-height:1.6;margin-bottom:20px;">
          Téléchargement d'une archive ZIP (sauvegardes, profil,
          statistiques, bestiaire). Lisible hors-ligne.
        </div>
        <div style="font-size:11px;color:var(--rpg-ink-muted);margin-bottom:20px;">
          Format · JSON · délai 24h max.
        </div>
        <button
          style="
            width:100%;padding:12px;
            background:var(--rpg-ink);color:white;
            border:none;cursor:pointer;
            font-family:var(--font-sans);font-size:10px;
            font-weight:700;letter-spacing:.15em;text-transform:uppercase;
            transition:opacity .15s;
            display:flex;align-items:center;justify-content:space-between;
          "
          @mouseenter="e => (e.currentTarget as HTMLElement).style.opacity='.85'"
          @mouseleave="e => (e.currentTarget as HTMLElement).style.opacity='1'"
          @click="downloadJson"
        >
          <span>Demander l'export</span>
          <span>→</span>
        </button>
      </div>

      <!-- Droit 02 — Voir données -->
      <div style="padding:28px;border-right:1px solid var(--rpg-border);">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
          <div class="editorial-label">Droit 02</div>
          <div style="font-size:16px;">○</div>
        </div>
        <div class="editorial-label mb-2" style="color:var(--rpg-ink-muted);">■ Consulter</div>
        <div style="font-family:var(--font-serif);font-size:1.4rem;font-weight:700;margin-bottom:12px;line-height:1.2;">
          Voir mes données
        </div>
        <div style="font-size:13px;color:var(--rpg-ink-muted);line-height:1.6;margin-bottom:20px;">
          Vue détaillée par catégorie de tout ce que le Codex conserve
          sur toi. Aperçu en clair, ligne par ligne.
        </div>
        <div style="font-size:11px;color:var(--rpg-ink-muted);margin-bottom:20px;">
          Mise à jour temps réel.
        </div>
        <button
          style="
            width:100%;padding:12px;
            background:var(--rpg-ink);color:white;
            border:none;cursor:pointer;
            font-family:var(--font-sans);font-size:10px;
            font-weight:700;letter-spacing:.15em;text-transform:uppercase;
            transition:opacity .15s;
            display:flex;align-items:center;justify-content:space-between;
          "
          @mouseenter="e => (e.currentTarget as HTMLElement).style.opacity='.85'"
          @mouseleave="e => (e.currentTarget as HTMLElement).style.opacity='1'"
          @click="exportData"
        >
          <span>Ouvrir la vue détaillée</span>
          <span>→</span>
        </button>
      </div>

      <!-- Droit 03 — Suppression -->
      <div style="padding:28px;">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
          <div class="editorial-label">Droit 03</div>
          <div style="font-size:16px;cursor:pointer;" @click="confirmDelete = true">×</div>
        </div>
        <div class="editorial-label mb-2" style="color:#C0392B;">■ Effacer</div>
        <div style="font-family:var(--font-serif);font-size:1.4rem;font-weight:700;margin-bottom:12px;line-height:1.2;">
          Supprimer mon compte
        </div>
        <div style="font-size:13px;color:var(--rpg-ink-muted);line-height:1.6;margin-bottom:20px;">
          Effacement définitif de toutes tes données dans un délai de
          30 jours. Irréversible passé ce délai.
        </div>
        <div style="font-size:11px;color:var(--rpg-ink-muted);margin-bottom:20px;">
          Action soumise à confirmation par email.
        </div>
        <button
          style="
            width:100%;padding:12px;
            background:transparent;color:#C0392B;
            border:1px solid #C0392B;cursor:pointer;
            font-family:var(--font-sans);font-size:10px;
            font-weight:700;letter-spacing:.15em;text-transform:uppercase;
            transition:all .15s;
            display:flex;align-items:center;justify-content:space-between;
          "
          @mouseenter="e => { (e.currentTarget as HTMLElement).style.background='#C0392B'; (e.currentTarget as HTMLElement).style.color='white'; }"
          @mouseleave="e => { (e.currentTarget as HTMLElement).style.background='transparent'; (e.currentTarget as HTMLElement).style.color='#C0392B'; }"
          @click="confirmDelete = true"
        >
          <span>Demander la suppression</span>
          <span>→</span>
        </button>
      </div>
    </div>

    <!-- ── DÉTAIL PAR CATÉGORIE ───────────────────────────────────── -->
    <div style="margin-bottom:40px;">
      <div style="
        display:flex;justify-content:space-between;align-items:center;
        margin-bottom:0;padding-bottom:12px;
        border-bottom:1px solid var(--rpg-border);
      ">
        <div class="editorial-label">■ Détail par catégorie</div>
      </div>

      <!-- Header -->
      <div style="border-bottom:1px solid var(--rpg-border);padding:10px 0;">
        <div style="display:grid;grid-template-columns:2fr 3fr 2fr 2fr 2fr;gap:0;">
          <div class="editorial-label">Catégorie</div>
          <div class="editorial-label">Description</div>
          <div class="editorial-label">Volume</div>
          <div class="editorial-label">Quantité</div>
          <div class="editorial-label text-right">Actions</div>
        </div>
      </div>

      <div
        v-for="cat in categories"
        :key="cat.label"
        class="editorial-row"
        style="padding:14px 0;"
      >
        <div style="display:grid;grid-template-columns:2fr 3fr 2fr 2fr 2fr;gap:0;align-items:center;">
          <div style="font-family:var(--font-serif);font-size:1rem;font-weight:700;">
            {{ cat.label }}
          </div>
          <div style="font-size:12px;color:var(--rpg-ink-muted);font-style:italic;">
            {{ cat.desc }}
          </div>
          <div style="font-size:12px;color:var(--rpg-ink-muted);">—</div>
          <div style="font-size:12px;color:var(--rpg-ink-muted);">—</div>
          <div style="text-align:right;">
            <span
              style="font-size:10px;font-weight:600;letter-spacing:.08em;text-transform:uppercase;cursor:pointer;text-decoration:underline;margin-left:12px;"
              @click="exportData"
            >
              Exporter
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- ── Dialogue confirmation suppression ─────────────────────── -->
    <v-dialog v-model="confirmDelete" max-width="480">
      <div style="
        background:var(--rpg-cream);
        border:1px solid rgba(0,0,0,0.1);
        padding:40px;
      ">
        <div class="editorial-label mb-3" style="color:#C0392B;">■ Action irréversible</div>
        <div style="font-family:var(--font-serif);font-size:1.5rem;font-weight:700;margin-bottom:12px;">
          Supprimer mon compte ?
        </div>
        <div style="font-size:13px;color:var(--rpg-ink-muted);margin-bottom:24px;line-height:1.6;">
          Toutes tes données seront effacées définitivement dans un délai
          de 30 jours. Cette action est irréversible.
        </div>

        <div style="margin-bottom:24px;">
          <div class="editorial-label mb-2">Raison (optionnel)</div>
          <input
            v-model="deleteReason"
            type="text"
            placeholder="Pourquoi supprimes-tu ton compte ?"
            style="
              width:100%;padding:10px 12px;
              border:1px solid rgba(0,0,0,0.15);
              background:transparent;
              font-family:var(--font-sans);font-size:13px;
              outline:none;
            "
          />
        </div>

        <div class="d-flex justify-end ga-4">
          <span
            style="font-size:11px;font-weight:600;letter-spacing:.08em;text-transform:uppercase;cursor:pointer;color:var(--rpg-ink-muted);padding:10px 0;"
            @click="confirmDelete = false"
          >
            Annuler
          </span>
          <button
            style="
              padding:10px 20px;
              background:#C0392B;color:white;
              border:none;cursor:pointer;
              font-family:var(--font-sans);font-size:10px;
              font-weight:700;letter-spacing:.15em;text-transform:uppercase;
            "
            @click="deleteAccount"
          >
            Confirmer la suppression →
          </button>
        </div>
      </div>
    </v-dialog>

  </div>
</template>

<script setup lang="ts">
import { rgpdApi } from '@/api/rgpd'
import { useAuthStore } from '@/stores/auth'
import { ref } from 'vue'

const auth          = useAuthStore()
const confirmDelete = ref(false)
const deleteReason  = ref('')

const categories = [
  { label: 'Identité',     desc: 'Nom de marcheur, email, date de création.' },
  { label: 'Sauvegardes',  desc: 'Fichiers de sauvegarde, métadonnées.' },
  { label: 'Profil',       desc: 'Stats, niveau, équipement, compétences.' },
  { label: 'Bestiaire',    desc: 'Créatures rencontrées et journal de chasse.' },
  { label: 'Statistiques', desc: 'Temps de jeu, morts, kills, achievements.' },
  { label: 'Sessions',     desc: 'Historique de connexion, IP anonymisées.' },
]

async function exportData() {
  const res = await rgpdApi.exportData()
  alert(JSON.stringify(res.data, null, 2))
}

async function downloadJson() {
  const res  = await rgpdApi.exportJson()
  const url  = URL.createObjectURL(new Blob([res.data]))
  const link = document.createElement('a')
  link.href  = url
  link.download = `mes-donnees-rgpd-${auth.userId}.json`
  link.click()
  URL.revokeObjectURL(url)
}

async function deleteAccount() {
  await rgpdApi.deleteAccount(deleteReason.value || undefined)
  auth.logout()
}
</script>