<script setup lang="ts">
import { useRouter } from 'vue-router';

import LivingWalls from '../shared/components/LivingWalls.vue';
import StatusEffectToken from '../shared/components/StatusEffectToken.vue';
import type { StatusEffectKind } from '../features/combat/types/combatContracts';

const router = useRouter();
const props = defineProps<{ embedded?: boolean }>();

type StatusItem = {
  kind: StatusEffectKind;
  magnitude?: number;
  nom: string;
  engine: string;
  desc: string;
  rule: string;
};

type StatusGroup = {
  title: string;
  color: string;
  items: StatusItem[];
};

const groups: StatusGroup[] = [
  {
    title: "Altérations — par tick",
    color: 'var(--mint-dim)',
    items: [
      {
        kind: 'DamageOverTime',
        nom: 'Dégât continu',
        engine: 'DamageOverTime',
        desc: 'Ronge la vitalité à chaque tick, indifférent à la garde. Poison, brûlure, saignée de mémoire en sont des variantes.',
        rule: 'magnitude × stacks / tick',
      },
      {
        kind: 'HealOverTime',
        nom: 'Soin continu',
        engine: 'HealOverTime',
        desc: 'Restitue de la vitalité à chaque tick, tant que la flamme intérieure tient et qu’aucune brûlure ne l’annule.',
        rule: '+magnitude / tick · durée fixe',
      },
    ],
  },
  {
    title: 'Modificateurs — tant que l’état dure',
    color: 'var(--mint-dim)',
    items: [
      {
        kind: 'StatModifier',
        magnitude: 1,
        nom: 'Renforcement',
        engine: 'StatModifier +',
        desc: 'Élève une statistique (attaque, défense, vitesse, focus). Le stat visé est porté par l’effet ; les stacks se cumulent.',
        rule: '+magnitude sur le stat · cumulable',
      },
      {
        kind: 'StatModifier',
        magnitude: -1,
        nom: 'Affaiblissement',
        engine: 'StatModifier −',
        desc: 'Abaisse une statistique. Même mécanique, magnitude négative — la matière se fissure et les coups portent davantage.',
        rule: '−magnitude sur le stat · cumulable',
      },
    ],
  },
  {
    title: "Entraves — privation d’action",
    color: 'var(--mint-dim)',
    items: [
      {
        kind: 'Stun',
        nom: 'Étourdissement',
        engine: 'Stun',
        desc: 'Incapable d’agir. Son action est perdue tant que l’effet dure.',
        rule: 'bloque toute action · purge à la fin',
      },
      {
        kind: 'Silence',
        nom: 'Silence',
        engine: 'Silence',
        desc: 'Plus aucune compétence à souffle. Seule la frappe nue reste possible.',
        rule: 'bloque les compétences',
      },
    ],
  },
];
</script>

<template>
  <main class="statuts-page" :class="{ 'statuts-page--embedded': props.embedded }">
    <LivingWalls v-if="!props.embedded" />

    <div class="statuts-page__content">
      <button v-if="!props.embedded" class="statuts-page__back" @click="router.back()">← sommaire</button>

      <h1 class="statuts-page__title">Les statuts</h1>
      <p class="statuts-page__lede">
        Les effets s’accumulent en <em>stacks</em> et se résorbent au fil des tours tactiques.
      </p>

      <section v-for="group in groups" :key="group.title" class="statuts-group">
        <div class="statuts-group__title" :style="{ color: group.color }">{{ group.title }}</div>
        <div class="statuts-group__items">
          <div v-for="item in group.items" :key="item.engine" class="statuts-item">
            <StatusEffectToken :kind="item.kind" :magnitude="item.magnitude" :stacks="1" :px="54" />
            <div class="statuts-item__body">
              <div class="statuts-item__head">
                <span class="statuts-item__nom">{{ item.nom }}</span>
                <span class="statuts-item__engine">{{ item.engine }}</span>
              </div>
              <p class="statuts-item__desc">{{ item.desc }}</p>
              <div class="statuts-item__rule">{{ item.rule }}</div>
            </div>
          </div>
        </div>
      </section>
    </div>
  </main>
</template>

<style scoped>
.statuts-page {
  position: relative;
  min-height: 100dvh;
  background: var(--void);
  color: var(--ink);
  font-family: var(--font);
}

.statuts-page--embedded { min-height: 0; }

.statuts-page__content {
  position: relative;
  z-index: 2;
  max-width: 1100px;
  margin: 0 auto;
  padding: 48px 40px 96px;
}

.statuts-page--embedded .statuts-page__content {
  padding: 0;
  max-width: none;
}

.statuts-page__back {
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
.statuts-page__back:hover { color: var(--mint-dim); }

.statuts-page__title {
  margin: 0 0 12px;
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  font-size: 38px;
  color: var(--ink);
}

.statuts-page--embedded .statuts-page__title { margin-top: 0; }

.statuts-page__lede {
  max-width: 52ch;
  margin: 0;
  color: var(--ink-3);
  font-size: 14px;
  line-height: 1.6;
}

.statuts-group { margin-top: 50px; }

.statuts-group__title {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  border-bottom: 1px solid var(--line-soft);
  padding-bottom: 11px;
}

.statuts-group__items {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 0 32px;
  margin-top: 18px;
}

.statuts-item {
  display: flex;
  gap: 18px;
  align-items: flex-start;
  padding: 18px 0;
  border-bottom: 1px solid var(--line-soft);
}

.statuts-item__head {
  display: flex;
  align-items: baseline;
  gap: 8px;
}

.statuts-item__nom {
  font-family: var(--font-display);
  font-size: 21px;
  color: var(--ink);
  line-height: 1;
}

.statuts-item__engine {
  font-family: var(--font-mono);
  font-size: 9px;
  color: var(--ink-5);
}

.statuts-item__desc {
  font-family: var(--font);
  font-size: 14px;
  line-height: 1.5;
  color: var(--ink-3);
  margin: 9px 0 0;
}

.statuts-item__rule {
  font-family: var(--font-mono);
  font-size: 9px;
  letter-spacing: 0.04em;
  color: var(--ink-5);
  margin-top: 7px;
}
</style>
