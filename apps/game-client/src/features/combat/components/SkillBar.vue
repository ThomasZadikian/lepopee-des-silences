<script setup lang="ts">
import type { CombatUsableItemDto, CombatantRuntimeDto, EmotionalType, TargetingType, SkillType } from '../types/combatContracts';
import EmotionalTypeBadge from './EmotionalTypeBadge.vue';
import SigilIcon from '../../../shared/components/SigilIcon.vue';

defineProps<{
  combatant: CombatantRuntimeDto | null;
  selectedSkillKey: string | null;
  isPlayerTurn: boolean;
  isLoading: boolean;
  usableBattleItems: CombatUsableItemDto[];
  selectedItemId: string | null;
}>();

defineEmits<{
  selectSkill: [skillKey: string];
  selectItem: [itemId: string];
}>();

function isSkillDisabled(combatant: CombatantRuntimeDto | null): boolean {
  if (!combatant) return true;
  return combatant.status === 'Defeated';
}

function getEffectLabel(effectType: string, effectAmount: number): string {
  switch (effectType) {
    case 'Heal': return `+${effectAmount} PV`;
    case 'Guard': return `+${effectAmount} Garde`;
    case 'ManaRestore': return `+${effectAmount} Mana`;
    case 'ChargeRestore': return `+${effectAmount} Charge`;
    default: return `+${effectAmount}`;
  }
}

const TARGETING_LABELS: Record<TargetingType, string> = {
  Self: 'Soi-même',
  SingleEnemy: 'Un ennemi',
  SingleAlly: 'Un allié',
  AllEnemies: 'Tous les ennemis',
  AllAllies: 'Toute l\'équipe',
};

const SKILL_TYPE_LABELS: Record<SkillType, string> = {
  Damage: 'Dégâts',
  Guard: 'Garde',
  Weaken: 'Affaiblissement',
  Disrupt: 'Perturbation',
  Debuff: 'Affaiblissement',
  Buff: 'Renforcement',
  Heal: 'Soin',
  Status: 'Statut',
  CopySkills: 'Copie de sorts',
  ExtendDotDuration: 'Durée de dot',
};

const EMOTIONAL_TYPE_LABELS: Record<string, string> = {
  Effroi: 'Effroi',
  Deni: 'Déni',
  Melancolie: 'Mélancolie',
  Rupture: 'Rupture',
  Memoire: 'Mémoire',
  Silence: 'Silence',
  Folie: 'Folie',
  Neutral: 'Neutre',
};

function targetingLabel(type: TargetingType): string {
  return TARGETING_LABELS[type] ?? type;
}

function skillTypeLabel(type: SkillType): string {
  return SKILL_TYPE_LABELS[type] ?? type;
}

function emotionalTypeLabel(type: EmotionalType | string): string {
  return EMOTIONAL_TYPE_LABELS[type] ?? type;
}

function costLabel(skill: CombatantRuntimeDto['skills'][number]): string | null {
  const parts: string[] = [];
  if (skill.manaCost > 0) parts.push(`${skill.manaCost} PP`);
  if (skill.chargeCost > 0) parts.push(`${skill.chargeCost} Charge`);
  return parts.length > 0 ? parts.join(' · ') : null;
}
</script>

<template>
  <section class="skill-bar">
    <template v-if="combatant">
      <div class="skill-bar__head">
        <span class="skill-bar__verb">{{ combatant.mana }} PP disponibles</span>
      </div>

      <div class="skill-bar__grid">
        <button
          v-for="skill in combatant.skills"
          :key="skill.key"
          class="skill-card"
          :class="{
            'skill-card--selected': selectedSkillKey === skill.key,
            'skill-card--disabled': !isPlayerTurn || isLoading || isSkillDisabled(combatant),
          }"
          :disabled="!isPlayerTurn || isLoading || isSkillDisabled(combatant)"
          @click="$emit('selectSkill', skill.key)"
        >
          <span class="skill-card__icon" :class="{ 'skill-card__icon--magic': skill.category === 'Magic' }">
            <SigilIcon :kind="skill.category === 'Magic' ? 'sort' : 'combat'" :size="15" :stroke-width="1.5" />
          </span>

          <span class="skill-card__body">
            <span class="skill-card__name-row">
              <span class="skill-card__name">{{ skill.displayName }}</span>
              <EmotionalTypeBadge v-if="skill.emotionalType" :type="skill.emotionalType" compact />
            </span>
            <span v-if="costLabel(skill)" class="skill-card__cost">{{ costLabel(skill) }}</span>
          </span>

          <span class="skill-card__bubble" role="tooltip">
            <span class="skill-card__bubble-title">{{ skill.displayName }}</span>
            <span class="skill-card__bubble-line">
              {{ skillTypeLabel(skill.effectType) }} · {{ targetingLabel(skill.targetingType) }}
            </span>
            <span class="skill-card__bubble-line">
              {{ skill.category === 'Magic' ? 'Magique' : 'Physique' }}<template v-if="skill.emotionalType"> · {{ emotionalTypeLabel(skill.emotionalType) }}</template>
            </span>
            <span class="skill-card__bubble-line">Puissance de base : {{ skill.basePower }}</span>
            <span v-if="costLabel(skill)" class="skill-card__bubble-line">Coût : {{ costLabel(skill) }}</span>
          </span>
        </button>
      </div>

      <template v-if="usableBattleItems.length > 0">
        <div class="skill-bar__items">
          <span class="skill-bar__items-label">Besace</span>
          <button
            v-for="item in usableBattleItems"
            :key="item.itemId"
            class="skill-card skill-card--item"
            :class="{
              'skill-card--selected': selectedItemId === item.itemId,
              'skill-card--disabled': !isPlayerTurn || isLoading,
            }"
            :disabled="!isPlayerTurn || isLoading"
            @click="$emit('selectItem', item.itemId)"
          >
            <span class="skill-card__name">{{ item.displayName }}</span>
            <span class="skill-card__meta">
              {{ getEffectLabel(item.effectType, item.effectAmount) }} · ×{{ item.quantity }}
            </span>
          </button>
        </div>
      </template>
    </template>

    <template v-else>
      <span class="es-label">Aucun combattant actif</span>
    </template>
  </section>
</template>

<style scoped>
.skill-bar {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto;
  align-items: center;
  gap: var(--space-2);
  flex: 1;
  min-width: 0;
}

.skill-bar__head {
  display: flex;
  flex-direction: column;
  gap: 1px;
  padding-right: var(--space-2);
}

.skill-bar__verb {
  font-family: var(--font-display);
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--ink-2);
}

.skill-bar__pp {
  font-family: var(--font-caps);
  font-size: 0.5rem;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4);
  white-space: nowrap;
}

.skill-bar__grid {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-1);
  min-width: 0;
}

.skill-bar__items {
  display: flex;
  align-items: center;
  gap: var(--space-1);
  padding-left: var(--space-2);
  border-left: 1px solid var(--line-soft);
}

.skill-bar__items-label {
  font-family: var(--font-caps);
  font-size: 0.5rem;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.skill-card {
  position: relative;
  display: flex;
  align-items: center;
  gap: 6px;
  text-align: left;
  border: 1px solid var(--line);
  border-radius: var(--radius-sm);
  background: var(--panel);
  padding: 4px var(--space-2);
  cursor: pointer;
  font-family: inherit;
  color: var(--ink);
  transition: border-color 0.18s ease, background 0.18s ease;
  min-width: 4.2rem;
}

.skill-card:hover:not(:disabled) {
  border-color: var(--edge-gold);
  background: var(--wash-gold);
}

.skill-card--selected {
  border-color: var(--gold) !important;
  background: oklch(0.862 0.098 86 / 0.12) !important;
}

.skill-card--disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.skill-card--item {
  border-color: color-mix(in oklch, var(--sap), transparent 60%);
}

.skill-card--item:hover:not(:disabled) {
  border-color: var(--sap);
  background: var(--wash-sap);
}

.skill-card--item.skill-card--selected {
  border-color: var(--sap) !important;
  background: oklch(0.840 0.092 162 / 0.12) !important;
}

.skill-card__icon {
  flex: 0 0 auto;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--ink-4);
}

.skill-card__icon--magic { color: var(--frost); }

.skill-card__body {
  display: flex;
  flex-direction: column;
  gap: 1px;
  min-width: 0;
}

.skill-card__name-row {
  display: flex;
  align-items: center;
  gap: 5px;
  min-width: 0;
}

.skill-card__name {
  font-family: var(--font);
  font-size: 0.76rem;
  color: var(--ink-2);
  white-space: nowrap;
}

.skill-card__cost {
  font-family: var(--font-mono);
  font-size: 0.56rem;
  letter-spacing: 0.04em;
  color: var(--ink-4);
}

.skill-card__meta {
  font-family: var(--font-caps);
  font-size: 0.48rem;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.skill-card__bubble {
  position: absolute;
  left: 50%;
  bottom: 100%;
  transform: translateX(-50%) translateY(4px);
  min-width: 190px;
  max-width: 260px;
  width: max-content;
  padding: 9px 11px;
  border-radius: 5px;
  border: 1px solid var(--line-strong, oklch(0.42 0.03 60 / 0.7));
  background: oklch(0.16 0.02 270 / 0.97);
  color: var(--ink-2, oklch(0.78 0.02 275));
  font-family: var(--font, serif);
  text-align: left;
  white-space: normal;
  box-shadow: 0 8px 24px -6px oklch(0.1 0.02 270 / 0.6);
  opacity: 0;
  visibility: hidden;
  pointer-events: none;
  transition: opacity 0.15s ease, transform 0.15s ease;
  z-index: 60;
}

.skill-card__bubble-title {
  display: block;
  font-size: 11.5px;
  font-weight: 600;
  letter-spacing: 0.02em;
  color: var(--ink-1, oklch(0.9 0.02 275));
}

.skill-card__bubble-line {
  display: block;
  margin-top: 3px;
  font-family: var(--font-mono);
  font-size: 10.5px;
  line-height: 1.4;
  color: var(--ink-3, oklch(0.68 0.02 275));
}

.skill-card:hover .skill-card__bubble,
.skill-card:focus-visible .skill-card__bubble {
  opacity: 1;
  visibility: visible;
  transform: translateX(-50%) translateY(0);
}

@media (max-width: 900px) {
  .skill-bar {
    grid-template-columns: 1fr;
  }

  .skill-bar__items {
    padding-left: 0;
    border-left: none;
  }
}

@media (prefers-reduced-motion: reduce) {
  .skill-card { transition: none; }
  .skill-card__bubble { transition: none; }
}
</style>
