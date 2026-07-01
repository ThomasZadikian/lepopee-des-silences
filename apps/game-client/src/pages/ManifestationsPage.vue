<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';

import EmotionalTypeBadge from '../features/combat/components/EmotionalTypeBadge.vue';
import type { EmotionalType } from '../features/combat/types/combatContracts';
import PalaceAtmosphere from '../shared/components/PalaceAtmosphere.vue';

const router = useRouter();

type BossEntry = {
  name: string;
  salle: string;
  type: EmotionalType;
  weak: EmotionalType;
  res: EmotionalType;
  threat: 'modérée' | 'élevée' | 'instable' | 'le saigneur';
  desc: string;
  mech: string[];
  quote: string;
};

const bosses: BossEntry[] = [
  {
    name: 'Gardien des Racines',
    salle: 'La Forêt — III',
    type: 'Deni',
    weak: 'Rupture',
    res: 'Memoire',
    threat: 'élevée',
    desc: "Une masse de bois et de tendons, poussée là où trop d'instinct a été enfoui. Il ne poursuit pas : il enracine, et attend que vous renonciez à partir.",
    mech: [
      'Enracine la cuve — annule la fuite',
      'Soin continu tant qu’une racine respire',
      'Couper les racines (Rupture) coupe son soin',
    ],
    quote: '« Reste. Tout pousse mieux dans ce qui ne bouge plus. »',
  },
  {
    name: 'Voix Éteinte',
    salle: 'La Rupture — IV',
    type: 'Rupture',
    weak: 'Memoire',
    res: 'Silence',
    threat: 'instable',
    desc: "Ce qui reste d'un cri trop longtemps retenu. Elle parle avec votre propre voix, et chaque phrase éteint la combustion d'un cran.",
    mech: [
      'Inflige Silence à intervalle',
      'Affaiblissement de défense sur toute l’équipe',
      'Frappe plus fort contre une cible muette',
    ],
    quote: '« Tu l’as déjà pensé. Je ne fais que le dire à voix haute. »',
  },
  {
    name: 'Archiviste des Échos',
    salle: 'La Mémoire — II',
    type: 'Memoire',
    weak: 'Effroi',
    res: 'Melancolie',
    threat: 'modérée',
    desc: "Gardien des runs passées. Il conserve chaque version de vous qui a échoué, et vous les oppose, l'une après l'autre.",
    mech: [
      'Convoque l’écho d’une run perdue',
      'Copie votre dernière compétence',
      'Vulnérable quand ses archives sont vides',
    ],
    quote: '« Je vous ai déjà vu mourir ici. Plusieurs fois. »',
  },
  {
    name: "Gardien de l'Antichambre",
    salle: "L'Antichambre — VI",
    type: 'Neutral',
    weak: 'Effroi',
    res: 'Memoire',
    threat: 'élevée',
    desc: "Ni hostile ni clément. Il pèse, simplement. Le dernier seuil avant le cœur, et la dernière chance de faire demi-tour.",
    mech: [
      'Jauge bloquée des deux camps par phases',
      'Dégâts proportionnels à votre charge accumulée',
      'Ne s’enrage jamais',
    ],
    quote: '« Montrez-moi ce que vous valez de porter, jusqu’au centre. »',
  },
  {
    name: "Him'Lit",
    salle: 'Le Cœur — VII',
    type: 'Silence',
    weak: 'Silence',
    res: 'Effroi',
    threat: 'le saigneur',
    desc: "Le Saigneur des Silences. Il n'attaque pas le corps mais la langue : il écrit en vous, lettre après lettre, jusqu'à ce que le cœur se mette à pleurer.",
    mech: [
      'Inscrit une lettre par tick — au mot complet, dégât massif',
      'Convertit votre Soin continu en saignée',
      'Faiblesse : le silence assumé, non subi',
    ],
    quote: '« oth-oth, ha-dam yivkeh. Lettre après lettre, le sang pleure. »',
  },
];

const THREAT_COLOR: Record<BossEntry['threat'], string> = {
  'modérée': 'var(--frost)',
  'élevée': 'var(--gold)',
  'instable': 'var(--ember)',
  'le saigneur': 'var(--blood)',
};

const selectedIndex = ref(0);
const selected = computed(() => bosses[selectedIndex.value]);
</script>

<template>
  <main class="manif-page" data-mood="palais">
    <PalaceAtmosphere />

    <div class="manif-page__content">
      <button class="manif-page__back" @click="router.back()">← Retour</button>

      <span class="es-kicker">Bestiaire · gardiens des salles</span>
      <h1 class="es-h1" style="font-size: clamp(30px, 4.4vw, 52px); margin-top: 12px">
        Les Manifestations majeures
      </h1>

      <div class="manif-layout">
        <div class="manif-list">
          <button
            v-for="(boss, i) in bosses"
            :key="boss.name"
            class="manif-list__item"
            :class="{ 'manif-list__item--sel': i === selectedIndex }"
            @click="selectedIndex = i"
          >
            <div class="manif-list__badge"><EmotionalTypeBadge :type="boss.type" compact /></div>
            <div>
              <div class="manif-list__name">{{ boss.name }}</div>
              <div class="manif-list__salle">{{ boss.salle }}</div>
            </div>
          </button>
        </div>

        <div class="manif-detail">
          <div class="manif-detail__portrait">
            <span class="manif-detail__tag">silhouette — {{ selected.name }}</span>
          </div>
          <div class="manif-detail__body">
            <div class="manif-detail__top">
              <span class="es-kicker">{{ selected.salle }}</span>
              <EmotionalTypeBadge :type="selected.type" />
            </div>
            <h2 class="es-h2" style="font-size: clamp(28px, 3.4vw, 44px); margin-top: 13px">
              {{ selected.name }}
            </h2>
            <p class="es-body" style="max-width: 48ch; margin-top: 16px; color: var(--ink-3)">
              {{ selected.desc }}
            </p>

            <div class="manif-detail__affinities">
              <div class="manif-detail__affinity">
                <span class="es-label">faible à</span>
                <EmotionalTypeBadge :type="selected.weak" compact />
              </div>
              <div class="manif-detail__affinity">
                <span class="es-label">résiste</span>
                <EmotionalTypeBadge :type="selected.res" compact />
              </div>
            </div>

            <div class="manif-detail__stats">
              <div>
                <div class="es-label">Menace</div>
                <div class="manif-detail__threat" :style="{ color: THREAT_COLOR[selected.threat] }">
                  {{ selected.threat }}
                </div>
              </div>
              <div class="manif-detail__mech">
                <div class="es-label" style="margin-bottom: 7px">Mécaniques</div>
                <div v-for="(line, i) in selected.mech" :key="i" class="manif-detail__mech-line">
                  <span :style="{ color: 'var(--gold)' }">—</span>{{ line }}
                </div>
              </div>
            </div>

            <p class="manif-detail__quote">{{ selected.quote }}</p>
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

.manif-page__content {
  position: relative;
  z-index: 5;
  max-width: 1180px;
  margin: 0 auto;
  padding: 64px 4vw 90px;
}

.manif-page__back {
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
.manif-page__back:hover { color: var(--gold); }

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
  border-radius: 4px;
  background: transparent;
  padding: 12px 14px;
  transition: border-color 0.2s, background 0.2s;
}

.manif-list__item--sel {
  border-color: var(--edge-frost);
  background: linear-gradient(150deg, oklch(0.30 0.04 62 / 0.88), oklch(0.20 0.024 56 / 0.9));
}

.manif-list__name {
  font-family: var(--font-display);
  font-size: 18px;
  color: var(--ink-2);
  line-height: 1;
}

.manif-list__item--sel .manif-list__name { color: var(--ink); }

.manif-list__salle {
  font-family: var(--font-caps);
  font-size: 8px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-5);
  margin-top: 4px;
}

.manif-detail {
  border: 1px solid var(--line);
  border-radius: 6px;
  background: linear-gradient(160deg, var(--panel), var(--void));
  overflow: hidden;
  display: grid;
  grid-template-columns: 210px 1fr;
}

.manif-detail__portrait {
  position: relative;
  min-height: 260px;
  border-right: 1px solid var(--line);
  background: radial-gradient(120% 84% at 50% 24%, oklch(0.38 0.05 60) 0%, oklch(0.28 0.032 58) 52%, oklch(0.22 0.026 56) 100%);
  display: flex;
  align-items: flex-end;
  padding: 14px;
}

.manif-detail__tag {
  font-family: var(--font-caps);
  font-size: 10px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4);
  border: 1px solid var(--line);
  border-radius: 3px;
  padding: 4px 9px;
  background: oklch(0.20 0.022 56 / 0.85);
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

.manif-detail__quote {
  font-family: var(--font-display);
  font-style: italic;
  font-size: 18px;
  line-height: 1.45;
  color: var(--gold);
  margin: 24px 0 0;
  max-width: 42ch;
}

@media (max-width: 720px) {
  .manif-layout { grid-template-columns: 1fr; }
  .manif-detail { grid-template-columns: 1fr; }
}
</style>
