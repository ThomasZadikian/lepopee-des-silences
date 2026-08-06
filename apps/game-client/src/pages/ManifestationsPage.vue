<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

import EmotionalTypeBadge from '../features/combat/components/EmotionalTypeBadge.vue';
import { enemyCodexApi, type BossCodexEntry } from '../features/emotional-registers/enemyCodexApi';
import { useEmotionalRegisterCatalog } from '../features/emotional-registers/store';
import LivingWalls from '../shared/components/LivingWalls.vue';

const router = useRouter();
const props = defineProps<{ embedded?: boolean }>();
const emotionalRegisters = useEmotionalRegisterCatalog();

type BossLore = {
  mech: string[];
  quote: string;
};

const LORE: Record<string, BossLore> = {
  'canon.enemy.himlit': {
    mech: ['Brume', 'Prière d’aspiration', 'Flamme séraphine'],
    quote: '« Lettre après lettre, le sang pleure. »',
  },
};

const bosses = ref<BossCodexEntry[]>([]);
const loadError = ref<string | null>(null);
const selectedIndex = ref(0);
const selected = computed(() => bosses.value[selectedIndex.value]);
const selectedLore = computed<BossLore>(() => selected.value
  ? LORE[selected.value.key] ?? { mech: [], quote: '' }
  : { mech: [], quote: '' });
const selectedRegister = computed(() =>
  emotionalRegisters.definitionOf(selected.value?.emotionalRegister));
const selectedWeaknesses = computed(() => selectedRegister.value?.incomingAffinities
  .filter((affinity) => affinity.outcome === 'Weak') ?? []);
const selectedResistances = computed(() => selectedRegister.value?.incomingAffinities
  .filter((affinity) => affinity.outcome === 'Resistant') ?? []);
const selectedImmunities = computed(() => selectedRegister.value?.incomingAffinities
  .filter((affinity) => affinity.outcome === 'Immune') ?? []);

function roomLabel(boss: BossCodexEntry): string {
  return boss.compatibleRoomTypes.join(' · ');
}

function threatColor(threat: number): string {
  if (threat >= 5) return 'var(--danger-dim)';
  if (threat >= 4) return 'var(--mauve)';
  if (threat >= 3) return 'var(--mauve-dim)';
  return 'var(--ink-3)';
}

onMounted(async () => {
  try {
    bosses.value = (await enemyCodexApi.listBosses()).bosses;
  } catch (caught) {
    loadError.value = caught instanceof Error ? caught.message : 'Bestiaire indisponible.';
  }
});
</script>

<template>
  <main class="manif-page" :class="{ 'manif-page--embedded': props.embedded }">
    <LivingWalls v-if="!props.embedded" />

    <div class="manif-page__content">
      <button v-if="!props.embedded" class="manif-page__back" @click="router.back()">← sommaire</button>

      <span class="manif-page__kicker">Bestiaire · gardiens des salles</span>
      <h1 class="manif-page__title">Les Manifestations majeures</h1>

      <p v-if="loadError" class="es-body">{{ loadError }}</p>
      <div v-else-if="selected" class="manif-layout">
        <div class="manif-list">
          <button
            v-for="(boss, i) in bosses"
            :key="boss.key"
            class="manif-list__item"
            :class="{ 'manif-list__item--sel': i === selectedIndex }"
            @click="selectedIndex = i"
          >
            <div class="manif-list__badge"><EmotionalTypeBadge :type="boss.emotionalRegister" compact /></div>
            <div>
              <div class="manif-list__name">{{ boss.displayName }}</div>
              <div class="manif-list__salle">{{ roomLabel(boss) }}</div>
            </div>
          </button>
        </div>

        <div class="manif-detail">
          <div class="manif-detail__portrait">
            <span class="manif-detail__tag">silhouette — {{ selected.displayName }}</span>
          </div>
          <div class="manif-detail__body">
            <div class="manif-detail__top">
              <span class="es-kicker">{{ roomLabel(selected) }}</span>
              <EmotionalTypeBadge :type="selected.emotionalRegister" />
            </div>
            <h2 class="es-h2" style="font-size: clamp(28px, 3.4vw, 44px); margin-top: 13px">
              {{ selected.displayName }}
            </h2>
            <p class="es-body" style="max-width: 48ch; margin-top: 16px; color: var(--ink-3)">
              {{ selected.description }}
            </p>

            <div class="manif-detail__affinities">
              <div class="manif-detail__affinity">
                <span class="es-label">faible à</span>
                <EmotionalTypeBadge
                  v-for="affinity in selectedWeaknesses"
                  :key="affinity.incomingRegister"
                  :type="affinity.incomingRegister"
                  compact
                />
              </div>
              <div class="manif-detail__affinity">
                <span class="es-label">résiste</span>
                <EmotionalTypeBadge
                  v-for="affinity in selectedResistances"
                  :key="affinity.incomingRegister"
                  :type="affinity.incomingRegister"
                  compact
                />
              </div>
              <div v-if="selectedImmunities.length" class="manif-detail__affinity">
                <span class="es-label">immunisé à</span>
                <EmotionalTypeBadge
                  v-for="affinity in selectedImmunities"
                  :key="affinity.incomingRegister"
                  :type="affinity.incomingRegister"
                  compact
                />
              </div>
            </div>

            <div class="manif-detail__stats">
              <div>
                <div class="es-label">Menace</div>
                <div class="manif-detail__threat" :style="{ color: threatColor(selected.threat) }">
                  {{ selected.threat }}
                </div>
              </div>
              <div class="manif-detail__mech">
                <div class="es-label" style="margin-bottom: 7px">Mécaniques</div>
                <div v-for="(line, i) in selectedLore.mech" :key="i" class="manif-detail__mech-line">
                  <span class="manif-detail__mech-mark">—</span>{{ line }}
                </div>
              </div>
            </div>

            <p v-if="selectedLore.quote" class="manif-detail__quote">{{ selectedLore.quote }}</p>
          </div>
        </div>
      </div>
    </div>
  </main>
</template>

<style scoped>
.manif-page {
  position: relative;
  min-height: 100dvh;
  background: var(--void);
  color: var(--ink);
  font-family: var(--font);
}

.manif-page--embedded { min-height: 0; }

.manif-page__content {
  position: relative;
  z-index: 2;
  max-width: 1180px;
  margin: 0 auto;
  padding: 48px 40px 96px;
}

.manif-page--embedded .manif-page__content {
  padding: 0;
  max-width: none;
}

.manif-page__back {
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
.manif-page__back:hover { color: var(--mint-dim); }

.manif-page__kicker {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.manif-page__title {
  margin: 12px 0 0;
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  font-size: 38px;
  color: var(--ink);
}

.manif-layout {
  margin-top: 42px;
  display: grid;
  grid-template-columns: 280px 1fr;
  gap: 30px;
  align-items: start;
}

.manif-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.manif-list__item {
  all: unset;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 12px;
  border: 1px solid var(--line);
  background: transparent;
  padding: 12px 14px;
  transition: border-color 0.2s, background 0.2s;
}

.manif-list__item--sel {
  border-color: var(--mint-dim);
  background: var(--panel-2);
}

.manif-list__name {
  font-family: var(--font-display);
  font-size: 18px;
  color: var(--ink-2);
  line-height: 1;
}

.manif-list__item--sel .manif-list__name { color: var(--ink); }

.manif-list__salle {
  font-family: var(--font-mono);
  font-size: 8px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-5);
  margin-top: 4px;
}

.manif-detail {
  border: 1px solid var(--line);
  background: var(--panel);
  overflow: hidden;
  display: grid;
  grid-template-columns: 210px 1fr;
}

.manif-detail__portrait {
  position: relative;
  min-height: 260px;
  border-right: 1px solid var(--line);
  background: var(--panel-2);
  display: flex;
  align-items: flex-end;
  padding: 14px;
}

.manif-detail__tag {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4);
  border: 1px solid var(--line);
  padding: 4px 9px;
  background: var(--panel);
}

.manif-detail__body { padding: 28px 30px; }

.manif-detail__top {
  display: flex;
  align-items: center;
  gap: 11px;
  flex-wrap: wrap;
}

.manif-detail__affinities {
  display: flex;
  gap: 14px;
  flex-wrap: wrap;
  margin-top: 20px;
}

.manif-detail__affinity {
  display: flex;
  align-items: center;
  gap: 7px;
}

.manif-detail__stats {
  display: flex;
  gap: 34px;
  margin-top: 22px;
  padding-top: 20px;
  border-top: 1px solid var(--line-soft);
}

.manif-detail__threat {
  font-family: var(--font-display);
  font-size: 23px;
  margin-top: 4px;
}

.manif-detail__mech { flex: 1; }

.manif-detail__mech-line {
  display: flex;
  gap: 9px;
  align-items: baseline;
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--ink-3);
  line-height: 1.7;
}

.manif-detail__mech-mark { color: var(--mint-dim); }

.manif-detail__quote {
  font-family: var(--font-display);
  font-style: italic;
  font-size: 18px;
  line-height: 1.45;
  color: var(--mint-dim);
  margin: 24px 0 0;
  max-width: 42ch;
}

@media (max-width: 720px) {
  .manif-layout { grid-template-columns: 1fr; }
  .manif-detail { grid-template-columns: 1fr; }
}
</style>
