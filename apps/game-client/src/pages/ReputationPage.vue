<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import LivingWalls from '../shared/components/LivingWalls.vue';
import EmotionalTypeBadge from '../features/combat/components/EmotionalTypeBadge.vue';
import { reputationApi } from '../features/reputation/api/reputationApi';
import type { NpcReputationDto } from '../features/reputation/types/reputationTypes';

const route = useRoute();
const router = useRouter();
const props = defineProps<{ embedded?: boolean; runId?: string }>();

// Embedded mode (opened as a modal from within a run) passes the run id directly —
// there's no route navigation to carry it as a param there.
const runId = computed(() => props.runId ?? String(route.params.runId ?? ''));

const npcs = ref<NpcReputationDto[]>([]);
const isLoading = ref(false);
const error = ref<string | null>(null);

const STATE_LABELS: Record<string, string> = {
  Latent: 'Latent',
  Tendu: 'Tendu',
  Rompu: 'Rompu',
};

function stateLabel(state: string): string {
  return STATE_LABELS[state] ?? state;
}

// Latent = pas encore engagé (neutre), Tendu = lien vivant mais fragile (mauve — jamais
// une alerte), Rompu = irrévocable (danger — c'est un véritable point de non-retour).
function stateClass(state: string): string {
  if (state === 'Rompu') return 'reputation-card__state--danger';
  if (state === 'Tendu') return 'reputation-card__state--mauve';
  return '';
}

// Le score n'a pas de borne fixe côté moteur (les seuils Tendu/Rompu sont propres à chaque
// PNJ et non exposés au client) — la barre utilise donc une courbe à saturation douce plutôt
// qu'un pourcentage d'un maximum inventé : elle reste lisible quelle que soit l'ampleur du score.
function scoreFillPercent(score: number): number {
  const magnitude = Math.abs(score);
  return Math.round((magnitude / (magnitude + 10)) * 100);
}

onMounted(async () => {
  if (!runId.value) {
    error.value = 'Aucune run active — la réputation se construit au fil de vos rencontres.';
    return;
  }

  isLoading.value = true;
  error.value = null;
  try {
    const response = await reputationApi.getRunReputation(runId.value);
    npcs.value = [...response.npcs].sort((a, b) => b.relationshipScore - a.relationshipScore);
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Impossible de charger la réputation.';
  } finally {
    isLoading.value = false;
  }
});
</script>

<template>
  <main class="reputation-page" :class="{ 'reputation-page--embedded': props.embedded }">
    <LivingWalls v-if="!props.embedded" />

    <div class="reputation-page__content">
      <button v-if="!props.embedded" class="reputation-page__back" @click="router.back()">← sommaire</button>

      <h1 class="reputation-page__title">Réputation</h1>
      <p class="reputation-page__lede">
        Chaque rencontre laisse une trace — un score qui monte ou se rompt, une blessure qui se referme ou
        s'ouvre. Ce que vous avez semé avec chacun d'eux, durant cette traversée.
      </p>

      <p v-if="isLoading" class="reputation-page__status">Chargement…</p>
      <p v-else-if="error" class="reputation-page__status reputation-page__status--error">{{ error }}</p>
      <p v-else-if="npcs.length === 0" class="reputation-page__status">
        Vous n'avez encore croisé personne de mémorable dans cette traversée.
      </p>

      <section v-else class="reputation-list">
        <article v-for="npc in npcs" :key="npc.npcKey" class="reputation-card">
          <header class="reputation-card__head">
            <div class="reputation-card__title">
              <span class="reputation-card__name">{{ npc.displayName }}</span>
              <EmotionalTypeBadge :type="npc.emotionalRegister" />
            </div>
            <span class="reputation-card__state" :class="stateClass(npc.aggregateState)">
              {{ stateLabel(npc.aggregateState) }}
            </span>
          </header>

          <div class="reputation-card__bar-row">
            <div class="reputation-card__bar-track">
              <div
                class="reputation-card__bar-fill"
                :class="npc.relationshipScore < 0 ? 'reputation-card__bar-fill--neg' : 'reputation-card__bar-fill--pos'"
                :style="{ width: scoreFillPercent(npc.relationshipScore) + '%' }"
              />
            </div>
            <span class="reputation-card__score">
              {{ npc.relationshipScore > 0 ? '+' : '' }}{{ npc.relationshipScore }}
            </span>
          </div>

          <span class="reputation-card__meetings">{{ npc.timesMet }} rencontre{{ npc.timesMet > 1 ? 's' : '' }}</span>
        </article>
      </section>
    </div>
  </main>
</template>

<style scoped>
.reputation-page {
  position: relative;
  min-height: 100dvh;
  background: var(--void);
  color: var(--ink);
  font-family: var(--font);
}

.reputation-page--embedded { min-height: 0; }

.reputation-page__content {
  position: relative;
  z-index: 2;
  max-width: 900px;
  margin: 0 auto;
  padding: 48px 40px 96px;
}

.reputation-page--embedded .reputation-page__content {
  padding: 0;
  max-width: none;
}

.reputation-page__back {
  all: unset;
  cursor: pointer;
  display: block;
  margin-bottom: 24px;
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: 0.08em;
  color: var(--ink-4);
  transition: color .3s;
}
.reputation-page__back:hover { color: var(--mint-dim); }

.reputation-page__title {
  margin: 0 0 12px;
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  font-size: 38px;
  color: var(--ink);
}

.reputation-page--embedded .reputation-page__title { margin-top: 0; }

.reputation-page__lede {
  max-width: 56ch;
  margin: 0 0 32px;
  color: var(--ink-3);
  font-size: 14px;
  line-height: 1.6;
}

.reputation-page__status {
  margin-top: 24px;
  font-family: var(--font-mono);
  font-size: 12px;
  letter-spacing: .08em;
  color: var(--ink-4);
}

.reputation-page__status--error {
  color: var(--danger-dim);
}

.reputation-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 16px;
}

.reputation-card {
  border: 1px solid var(--line-soft);
  padding: 18px 20px;
  background: var(--panel);
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.reputation-card__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 10px;
}

.reputation-card__title {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.reputation-card__name {
  font-family: var(--font-display);
  font-style: italic;
  font-size: 18px;
  color: var(--ink);
  line-height: 1.2;
}

.reputation-card__state {
  flex-shrink: 0;
  font-family: var(--font-mono);
  font-size: 9px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.reputation-card__state--mauve { color: var(--mauve-dim); }
.reputation-card__state--danger { color: var(--danger-dim); }

.reputation-card__bar-row {
  display: flex;
  align-items: center;
  gap: 10px;
}

.reputation-card__bar-track {
  flex: 1;
  height: 4px;
  background: var(--line-soft);
  position: relative;
  overflow: hidden;
}

.reputation-card__bar-fill {
  height: 100%;
  transition: width .3s ease;
}

.reputation-card__bar-fill--pos { background: var(--mint-dim); }
.reputation-card__bar-fill--neg { background: var(--mauve-dim); }

.reputation-card__score {
  font-family: var(--font-mono);
  font-size: 13px;
  color: var(--ink-2);
  min-width: 3.5ch;
  text-align: right;
}

.reputation-card__meetings {
  font-family: var(--font-mono);
  font-size: 10.5px;
  color: var(--ink-5);
}
</style>
