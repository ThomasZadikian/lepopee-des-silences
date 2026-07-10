<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import PalaceAtmosphere from '../shared/components/PalaceAtmosphere.vue';
import RuleOrnament from '../shared/components/RuleOrnament.vue';
import EmotionalTypeBadge from '../features/combat/components/EmotionalTypeBadge.vue';
import { reputationApi } from '../features/reputation/api/reputationApi';
import type { NpcReputationDto } from '../features/reputation/types/reputationTypes';

const route = useRoute();
const router = useRouter();

const runId = computed(() => String(route.params.runId ?? ''));

const npcs = ref<NpcReputationDto[]>([]);
const isLoading = ref(false);
const error = ref<string | null>(null);

const STATE_LABELS: Record<string, string> = {
  Latent: 'Latent',
  Tendu: 'Tendu',
  Rompu: 'Rompu',
};

const STATE_TONES: Record<string, string> = {
  Latent: 'var(--sap)',
  Tendu: 'var(--gold)',
  Rompu: 'var(--blood)',
};

const OFFERING_KIND_LABELS: Record<string, string> = {
  Item: 'Objet',
  Skill: 'Sort',
  StatPoint: 'Point de compétence',
};

function stateLabel(state: string): string {
  return STATE_LABELS[state] ?? state;
}

function stateTone(state: string): string {
  return STATE_TONES[state] ?? 'var(--ink-4)';
}

function offeringKindLabel(kind: string): string {
  return OFFERING_KIND_LABELS[kind] ?? kind;
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
  <main class="reputation-page" data-mood="palais">
    <PalaceAtmosphere />

    <div class="reputation-page__content">
      <button class="reputation-page__back" @click="router.back()">← Retour</button>

      <span class="es-kicker">Système · liens avec les habitants du Palais</span>
      <h1 class="es-h1" style="font-size: clamp(30px, 4.4vw, 52px); margin-top: 12px">Réputation</h1>
      <RuleOrnament style="width: 150px; margin: 16px 0" />
      <p class="es-lede es-dim" style="max-width: 56ch">
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
            <span
              class="reputation-card__state"
              :style="{ '--state-color': stateTone(npc.aggregateState) }"
            >
              ● {{ stateLabel(npc.aggregateState) }}
            </span>
          </header>

          <div class="reputation-card__stats">
            <span class="reputation-card__stat">
              <span class="reputation-card__stat-k">Score</span>
              <span class="reputation-card__stat-v">{{ npc.relationshipScore }}</span>
            </span>
            <span class="reputation-card__stat">
              <span class="reputation-card__stat-k">Rencontres</span>
              <span class="reputation-card__stat-v">{{ npc.timesMet }}</span>
            </span>
          </div>

          <div v-if="npc.offerings.length" class="reputation-card__offerings">
            <span class="es-label">Offrandes</span>
            <ul class="reputation-offering-list">
              <li
                v-for="offering in npc.offerings"
                :key="offering.key"
                class="reputation-offering"
                :class="{ 'reputation-offering--met': offering.scoreThresholdMet }"
              >
                <span class="reputation-offering__kind">{{ offeringKindLabel(offering.kind) }}</span>
                <span class="reputation-offering__req">
                  <template v-if="offering.requiredRelationshipScore !== null">
                    Seuil {{ offering.requiredRelationshipScore }}
                  </template>
                  <template v-else>Sans seuil de score</template>
                </span>
                <span class="reputation-offering__status">
                  {{ offering.scoreThresholdMet ? 'Atteint' : 'Non atteint' }}
                </span>
              </li>
            </ul>
          </div>
        </article>
      </section>
    </div>
  </main>
</template>

<style scoped>
.reputation-page {
  position: relative;
  min-height: 100dvh;
  overflow-y: auto;
  overflow-x: hidden;
  background:
    radial-gradient(70% 52% at 20% 12%, var(--wash-frost), transparent 60%),
    radial-gradient(64% 56% at 86% 80%, var(--wash-blood), transparent 58%),
    radial-gradient(58% 50% at 60% 26%, var(--wash-sap), transparent 60%),
    radial-gradient(56% 50% at 12% 92%, var(--wash-gold), transparent 60%),
    radial-gradient(150% 130% at 50% -10%, var(--bg) 0%, var(--bg-2) 48%, var(--void) 100%);
  color: var(--ink);
  font-family: var(--font);
}

.reputation-page__content {
  position: relative;
  z-index: 5;
  max-width: 1100px;
  margin: 0 auto;
  padding: 64px 4vw 90px;
}

.reputation-page__back {
  all: unset;
  cursor: pointer;
  display: block;
  margin-bottom: 24px;
  font-family: var(--font-caps);
  font-size: 11px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
  transition: color 0.2s;
}
.reputation-page__back:hover { color: var(--gold); }

.reputation-page__status {
  margin-top: 40px;
  font-family: var(--font-caps);
  font-size: 12px;
  letter-spacing: 0.08em;
  color: var(--ink-4);
}

.reputation-page__status--error {
  color: var(--blood);
}

.reputation-list {
  margin-top: 44px;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
  gap: 20px;
}

.reputation-card {
  border: 1px solid var(--line-soft);
  border-radius: 6px;
  padding: 20px 22px;
  background: oklch(0.24 0.015 283 / 0.35);
  display: flex;
  flex-direction: column;
  gap: 14px;
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
  font-size: 20px;
  color: var(--ink);
  line-height: 1.2;
}

.reputation-card__state {
  flex-shrink: 0;
  font-family: var(--font-caps);
  font-size: 10px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--state-color);
}

.reputation-card__stats {
  display: flex;
  gap: 20px;
}

.reputation-card__stat {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.reputation-card__stat-k {
  font-family: var(--font-caps);
  font-size: 9px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-5);
}

.reputation-card__stat-v {
  font-family: var(--font-mono);
  font-size: 15px;
  color: var(--ink-2);
}

.reputation-card__offerings {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding-top: 6px;
  border-top: 1px solid var(--line-soft);
}

.reputation-offering-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.reputation-offering {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  font-size: 12px;
  color: var(--ink-4);
}

.reputation-offering__kind {
  font-family: var(--font-caps);
  font-size: 10px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--ink-3);
}

.reputation-offering__req {
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--ink-5);
}

.reputation-offering__status {
  font-family: var(--font-caps);
  font-size: 9.5px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--ink-5);
}

.reputation-offering--met .reputation-offering__status {
  color: var(--sap);
}
</style>
