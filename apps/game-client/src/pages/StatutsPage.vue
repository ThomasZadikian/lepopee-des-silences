<script setup lang="ts">
import { useRouter } from 'vue-router';

import PalaceAtmosphere from '../shared/components/PalaceAtmosphere.vue';
import RuleOrnament from '../shared/components/RuleOrnament.vue';
import StatusEffectToken from '../shared/components/StatusEffectToken.vue';
import type { StatusEffectKind } from '../features/combat/types/combatContracts';

const router = useRouter();

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
    color: 'var(--sap)',
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
    color: 'var(--gold)',
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
    color: 'oklch(0.78 0.12 300)',
    items: [
      {
        kind: 'Stun',
        nom: 'Étourdissement',
        engine: 'Stun',
        desc: 'Incapable d’agir. La jauge ATB se fige et le tour est sauté tant que l’effet dure.',
        rule: 'bloque toute action · purge à la fin',
      },
      {
        kind: 'Silence',
        nom: 'Silence',
        engine: 'Silence',
        desc: 'Plus aucune compétence à souffle. Seule la frappe nue reste possible — la jauge, elle, continue de monter.',
        rule: 'bloque les skills · ATB continue',
      },
      {
        kind: 'AtbLock',
        nom: 'Jauge bloquée',
        engine: 'AtbLock',
        desc: 'La readiness gèle : prêt mais retenu, hors du temps. La surcharge ⚡ ne s’accumule plus.',
        rule: 'fige l’ATB sans la vider',
      },
    ],
  },
];
</script>

<template>
  <main class="statuts-page" data-mood="palais">
    <PalaceAtmosphere />

    <div class="statuts-page__content">
      <button class="statuts-page__back" @click="router.back()">← Retour</button>

      <span class="es-kicker">Système · altérations de l’esprit</span>
      <h1 class="es-h1" style="font-size: clamp(30px, 4.4vw, 52px); margin-top: 12px">Les statuts</h1>
      <RuleOrnament style="width: 150px; margin: 16px 0" />
      <p class="es-lede es-dim" style="max-width: 52ch">
        Six familles d’effets, accumulées en <em>stacks</em> et résorbées en <em>ticks</em>. Le temps ne
        s’arrête pas pour les compter — il continue de remplir les jauges.
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
  height: 100dvh;
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

.statuts-page__content {
  position: relative;
  z-index: 5;
  max-width: 1100px;
  margin: 0 auto;
  padding: 64px 4vw 90px;
}

.statuts-page__back {
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
.statuts-page__back:hover { color: var(--gold); }

.statuts-group { margin-top: 50px; }

.statuts-group__title {
  font-family: var(--font-caps);
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
