<script setup lang="ts">
/**
 * Fiche de détail d'un combattant — ouverte au clic sur son portrait dans la barre de combat.
 * Pur composant de présentation : le parent calcule les valeurs qui dépendent de la mise en
 * scène en cours (vitalité affichée, statuts déjà révélés) et les passe toutes faites, comme
 * pour StatusEffectToken et SkillDetailModal.
 */
import { watch } from 'vue';

import StatusEffectToken from '../../../shared/components/StatusEffectToken.vue';
import EmotionalTypeBadge from './EmotionalTypeBadge.vue';
import type { CombatantStatusEffectDto, TacticalCombatantRuntimeDto } from '../types/combatContracts';

export type PortraitDetail = {
  unit: TacticalCombatantRuntimeDto;
  displayedVitality: number;
  vitalityPercent: number;
  guardPercent: number;
  statusEffects: CombatantStatusEffectDto[];
};

const props = defineProps<{ detail: PortraitDetail | null }>();
const emit = defineEmits<{ close: [] }>();

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') emit('close');
}

watch(() => props.detail, (detail) => {
  if (detail) document.addEventListener('keydown', onKeydown);
  else document.removeEventListener('keydown', onKeydown);
});

function manaPercent(combatant: { mana: number; maxMana: number }): number {
  return Math.max(0, Math.min(100, (combatant.mana / Math.max(1, combatant.maxMana)) * 100));
}

function facingLabel(facing: 'North' | 'East' | 'South' | 'West' | undefined): string {
  switch (facing) {
    case 'North': return 'nord';
    case 'East': return 'est';
    case 'South': return 'sud';
    case 'West': return 'ouest';
    default: return '—';
  }
}

const STAT_ROWS: { key: 'attackPower' | 'defense' | 'speed' | 'magicAttack' | 'magicDefense' | 'evasion' | 'hitChanceBonusPercent'; label: string; suffix?: string }[] = [
  { key: 'attackPower', label: 'Attaque' },
  { key: 'defense', label: 'Défense' },
  { key: 'speed', label: 'Vitesse' },
  { key: 'magicAttack', label: 'Attaque magique' },
  { key: 'magicDefense', label: 'Défense magique' },
  { key: 'evasion', label: 'Évasion' },
  { key: 'hitChanceBonusPercent', label: 'Précision', suffix: '%' },
];
</script>

<template>
  <Teleport to="body">
    <Transition name="portrait-card-fade">
      <div v-if="detail" class="portrait-card-backdrop" @click.self="emit('close')">
        <div class="portrait-card" role="dialog" :aria-modal="true" :aria-label="`Détail de ${detail.unit.combatant.displayName}`">
          <header class="portrait-card__head">
            <div class="portrait-card__avatar" :class="{ 'portrait-card__avatar--enemy': detail.unit.combatant.side === 'Enemy' }">
              {{ detail.unit.combatant.displayName.charAt(0) }}
            </div>
            <div class="portrait-card__identity">
              <strong class="portrait-card__name">{{ detail.unit.combatant.displayName }}</strong>
              <span class="portrait-card__sub">
                {{ detail.unit.combatant.side === 'Enemy' ? 'Ennemi' : 'Allié' }} · {{ detail.unit.combatant.archetype }}
              </span>
            </div>
            <button type="button" class="portrait-card__close" title="Fermer" @click="emit('close')">✕</button>
          </header>

          <div class="portrait-card__resources">
            <div class="portrait-card__resource">
              <span class="portrait-card__resource-label">Vitalité</span>
              <div class="portrait-card__bar">
                <div
                  class="portrait-card__bar-fill portrait-card__bar-fill--hp"
                  :class="{ 'portrait-card__bar-fill--low': detail.displayedVitality / Math.max(1, detail.unit.combatant.maxVitality) < 0.3 }"
                  :style="{ width: `${detail.vitalityPercent}%` }"
                />
                <div
                  v-if="detail.unit.combatant.guard > 0"
                  class="portrait-card__bar-fill portrait-card__bar-fill--guard"
                  :style="{ width: `${detail.guardPercent}%` }"
                />
              </div>
              <span class="portrait-card__resource-text">
                {{ detail.displayedVitality }}/{{ detail.unit.combatant.maxVitality }}
                <template v-if="detail.unit.combatant.guard > 0"> · Garde {{ detail.unit.combatant.guard }}</template>
              </span>
            </div>

            <div v-if="detail.unit.combatant.maxMana > 0" class="portrait-card__resource">
              <span class="portrait-card__resource-label">Mana</span>
              <div class="portrait-card__bar">
                <div
                  class="portrait-card__bar-fill portrait-card__bar-fill--mana"
                  :style="{ width: `${manaPercent(detail.unit.combatant)}%` }"
                />
              </div>
              <span class="portrait-card__resource-text">
                {{ detail.unit.combatant.mana }}/{{ detail.unit.combatant.maxMana }}
              </span>
            </div>

            <div v-if="detail.unit.combatant.focus !== undefined" class="portrait-card__resource portrait-card__resource--plain">
              <span class="portrait-card__resource-label">Focus</span>
              <span class="portrait-card__resource-text">{{ detail.unit.combatant.focus }}</span>
            </div>
          </div>

          <div class="portrait-card__stats">
            <div v-for="row in STAT_ROWS" v-show="detail.unit.combatant[row.key] !== undefined" :key="row.key" class="portrait-card__stat">
              <span class="portrait-card__stat-label">{{ row.label }}</span>
              <span class="portrait-card__stat-value">{{ detail.unit.combatant[row.key] }}{{ row.suffix ?? '' }}</span>
            </div>
          </div>

          <div
            v-if="detail.unit.combatant.attackType || detail.unit.combatant.weakTo?.length || detail.unit.combatant.resistantTo?.length || detail.unit.combatant.immuneTo?.length"
            class="portrait-card__section"
          >
            <span class="portrait-card__section-title">Registre émotionnel</span>
            <div v-if="detail.unit.combatant.attackType" class="portrait-card__type-row">
              <span class="portrait-card__type-label">Attaques en</span>
              <EmotionalTypeBadge :type="detail.unit.combatant.attackType" />
            </div>
            <div v-if="detail.unit.combatant.weakTo?.length" class="portrait-card__type-row">
              <span class="portrait-card__type-label">Faible à</span>
              <EmotionalTypeBadge v-for="type in detail.unit.combatant.weakTo ?? []" :key="type" :type="type" compact />
            </div>
            <div v-if="detail.unit.combatant.resistantTo?.length" class="portrait-card__type-row">
              <span class="portrait-card__type-label">Résiste à</span>
              <EmotionalTypeBadge v-for="type in detail.unit.combatant.resistantTo ?? []" :key="type" :type="type" compact />
            </div>
            <div v-if="detail.unit.combatant.immuneTo?.length" class="portrait-card__type-row">
              <span class="portrait-card__type-label">Immunisé à</span>
              <EmotionalTypeBadge v-for="type in detail.unit.combatant.immuneTo ?? []" :key="type" :type="type" compact />
            </div>
          </div>

          <div class="portrait-card__section">
            <span class="portrait-card__section-title">États actifs</span>
            <div v-if="detail.statusEffects.length" class="portrait-card__statuses">
              <StatusEffectToken
                v-for="status in detail.statusEffects"
                :key="status.key"
                :kind="status.kind"
                :magnitude="status.magnitude"
                :stat="status.stat"
                :is-magnitude-percent-of-base-stat="status.isMagnitudePercentOfBaseStat"
                :stacks="status.stacks"
                :px="32"
                meta
                :per-tick-amount="status.perTickAmount"
                :ticks-remaining="status.ticksRemaining"
                :is-permanent="status.isPermanent"
              />
            </div>
            <p v-else class="portrait-card__empty">Aucun état actif.</p>
          </div>

          <footer class="portrait-card__foot">
            <span>Position {{ detail.unit.x }}, {{ detail.unit.y }}</span>
            <span>Face {{ facingLabel(detail.unit.facing) }}</span>
            <span>{{ detail.unit.hasMoved ? 'Déplacé' : `Déplacement ${detail.unit.movementBudget}` }}</span>
            <span>{{ detail.unit.hasActed ? 'A agi' : 'Action disponible' }}</span>
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.portrait-card-backdrop {
  position: fixed;
  inset: 0;
  z-index: 220;
  display: grid;
  place-items: center;
  background: color-mix(in oklch, var(--void), transparent 30%);
  backdrop-filter: blur(2px);
  padding: 24px;
}

.portrait-card {
  width: min(24rem, 100%);
  max-height: min(80vh, 640px);
  overflow-y: auto;
  padding: 18px 20px;
  display: grid;
  gap: 14px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: var(--panel);
  box-shadow: var(--shadow-panel, 0 24px 80px rgba(0, 0, 0, .4));
}

.portrait-card__head {
  display: flex;
  align-items: center;
  gap: 12px;
}

.portrait-card__avatar {
  flex: none;
  width: 44px;
  height: 44px;
  border-radius: 50%;
  border: 1.5px solid var(--line-strong);
  background: var(--panel-2);
  display: grid;
  place-items: center;
  font-size: 1rem;
  font-weight: 600;
  color: var(--ink-2);
}

.portrait-card__avatar--enemy {
  border-color: color-mix(in oklch, #cf6a5c, transparent 45%);
  background: color-mix(in oklch, #cf6a5c, var(--panel-2) 82%);
}

.portrait-card__identity {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.portrait-card__name {
  font-size: 15px;
  color: var(--ink);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.portrait-card__sub {
  font-size: 11.5px;
  color: var(--ink-3);
}

.portrait-card__close {
  all: unset;
  margin-left: auto;
  cursor: pointer;
  color: var(--ink-4);
  padding: 4px;
}

.portrait-card__close:hover { color: var(--ink); }

.portrait-card__resources {
  display: grid;
  gap: 8px;
}

.portrait-card__resource {
  display: grid;
  grid-template-columns: 52px 1fr auto;
  align-items: center;
  gap: 8px;
}

.portrait-card__resource--plain { grid-template-columns: 52px 1fr; }

.portrait-card__resource-label {
  font-size: 11.5px;
  color: var(--ink-3);
}

.portrait-card__resource-text {
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--ink-2);
  white-space: nowrap;
  font-variant-numeric: tabular-nums;
}

.portrait-card__bar {
  display: flex;
  height: 6px;
  border-radius: 3px;
  background: var(--line-soft);
  overflow: hidden;
}

.portrait-card__bar-fill { height: 100%; transition: width 200ms ease; }
.portrait-card__bar-fill--hp { background: #86dcb4; }
.portrait-card__bar-fill--hp.portrait-card__bar-fill--low { background: var(--danger); }
.portrait-card__bar-fill--guard { background: #e8c65c; }
.portrait-card__bar-fill--mana { background: #7ec4e8; }

.portrait-card__stats {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 6px 16px;
  padding-top: 4px;
  border-top: 1px solid var(--line-soft);
}

.portrait-card__stat {
  display: flex;
  justify-content: space-between;
  gap: 8px;
  font-size: 12px;
}

.portrait-card__stat-label { color: var(--ink-3); }
.portrait-card__stat-value { font-family: var(--font-mono); color: var(--ink); font-variant-numeric: tabular-nums; }

.portrait-card__section {
  display: grid;
  gap: 6px;
  padding-top: 8px;
  border-top: 1px solid var(--line-soft);
}

.portrait-card__section-title {
  font-family: var(--font-mono);
  font-size: 10.5px;
  letter-spacing: .06em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.portrait-card__type-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
}

.portrait-card__type-label {
  font-size: 11.5px;
  color: var(--ink-3);
  min-width: 68px;
}

.portrait-card__statuses {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.portrait-card__empty {
  margin: 0;
  font-size: 12px;
  color: var(--ink-4);
  font-style: italic;
}

.portrait-card__foot {
  display: flex;
  flex-wrap: wrap;
  gap: 4px 12px;
  padding-top: 8px;
  border-top: 1px solid var(--line-soft);
  font-size: 11px;
  color: var(--ink-4);
  font-variant-numeric: tabular-nums;
}

.portrait-card-fade-enter-active,
.portrait-card-fade-leave-active { transition: opacity 0.15s ease; }
.portrait-card-fade-enter-from,
.portrait-card-fade-leave-to { opacity: 0; }

@media (prefers-reduced-motion: reduce) {
  .portrait-card-fade-enter-active,
  .portrait-card-fade-leave-active { transition: none; }
}
</style>
