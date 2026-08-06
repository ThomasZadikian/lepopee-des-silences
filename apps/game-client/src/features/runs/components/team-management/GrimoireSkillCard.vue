<script setup lang="ts">
import { computed } from 'vue';
import type { SkillDefinitionView } from '../../../party/types/skillTypes';
import { categoryLabel, formatEffect } from './grimoireDisplay';
import EmotionalTypeBadge from '../../../combat/components/EmotionalTypeBadge.vue';
import { useEmotionalRegisterCatalog } from '../../../emotional-registers/store';

const props = withDefaults(
  defineProps<{
    skill: SkillDefinitionView;
    isKnown: boolean;
    isEquipped: boolean;
    disabled: boolean;
    /** True when this skill's archetype restriction excludes the current character. */
    archetypeIncompatible?: boolean;
  }>(),
  { archetypeIncompatible: false },
);

const emit = defineEmits<{ toggleEquip: [key: string] }>();

const registerStore = useEmotionalRegisterCatalog();
const typeMeta = computed(() => registerStore.definitionOf(props.skill.emotionalRegister));
</script>

<template>
  <div
    class="grimoire-card"
    :class="{ 'grimoire-card--locked': !isKnown, 'grimoire-card--equipped': isEquipped }"
    :style="typeMeta ? { '--type-color': typeMeta.color } : undefined"
  >
    <div class="grimoire-card__head">
      <span class="grimoire-card__name">{{ skill.displayName }}</span>
      <EmotionalTypeBadge :type="skill.emotionalRegister" compact />
      <span v-if="skill.isUltimate" class="grimoire-chip grimoire-chip--ultimate">Ultime</span>
      <span class="grimoire-chip">{{ categoryLabel(skill.effectType) }}</span>
      <span class="grimoire-chip grimoire-chip--muted">{{ skill.category === 'Magic' ? 'Magique' : 'Physique' }}</span>
    </div>

    <p class="grimoire-card__desc">{{ skill.description }}</p>

    <ul v-if="skill.effects.length" class="grimoire-effects">
      <li v-for="(effect, index) in skill.effects" :key="index">{{ formatEffect(effect) }}</li>
    </ul>

    <div class="grimoire-card__stats">
      <span>Puissance : {{ skill.basePower }}{{ skill.basePowerIsPercentOfMaxVitality ? '% PV max' : '' }}</span>
      <span v-if="skill.manaCost > 0">Mana : {{ skill.manaCost }}</span>
      <span v-if="skill.chargeCost > 0">Charge : {{ skill.chargeCost }}</span>
      <span>Portée : {{ skill.tacticalRange ?? 1 }}</span>
      <span>Zone : {{ skill.tacticalAreaShape ?? 'Single' }}</span>
      <span>{{ skill.requiresLineOfSight ? 'Ligne de vue' : 'Sans ligne de vue' }}</span>
      <span v-if="(skill.cooldown ?? 0) > 0">Recharge : {{ skill.cooldown }} tours</span>
    </div>

    <template v-if="isKnown">
      <p v-if="archetypeIncompatible" class="grimoire-archetype-hint">
        Réservé à un autre archétype.
      </p>
      <button
        type="button"
        class="grimoire-toggle"
        :class="{ 'grimoire-toggle--active': isEquipped }"
        :disabled="!isEquipped && disabled"
        @click="emit('toggleEquip', skill.key)"
      >
        {{ isEquipped ? 'Équipé' : 'Équiper' }}
      </button>
    </template>
    <template v-else>
      <p class="grimoire-lock-hint">
        <template v-if="skill.acquisitionHints.length">
          {{ skill.acquisitionHints.join(' · ') }}
        </template>
        <template v-else>Non débloqué.</template>
      </p>
    </template>
  </div>
</template>

<style scoped>
.grimoire-card {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 12px;
  border: 1px solid var(--line-soft);
  border-left: 3px solid var(--line-soft);
  background: var(--panel-2);
}

/* Sorts avec un registre émotionnel intrinsèque (voir EmotionalTypeProfileProvider)
   portent l'accent pastel de leur type en bordure gauche ET en fond très atténué —
   même principe que la hotbar de sorts en combat (TacticalCombatScene). */
.grimoire-card[style*='--type-color'] {
  border-left-color: color-mix(in oklch, var(--type-color), transparent 35%);
  background: color-mix(in oklch, var(--type-color), var(--panel-2) 92%);
}

.grimoire-card--equipped {
  border-color: var(--mint-dim);
}

.grimoire-card--equipped[style*='--type-color'] {
  border-left-color: color-mix(in oklch, var(--type-color), transparent 35%);
}

.grimoire-card--locked {
  opacity: 0.5;
  filter: grayscale(0.6);
}

.grimoire-card__head {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}

.grimoire-card__name {
  font-size: 13px;
  font-weight: 600;
  color: var(--ink-2);
}

.grimoire-chip {
  font-family: var(--font-mono);
  font-size: 10px;
  padding: 1px 6px;
  border: 1px solid var(--line-soft);
  color: var(--ink-3);
}

.grimoire-chip--muted {
  color: var(--ink-4);
}

.grimoire-chip--ultimate {
  border-color: var(--mint-dim);
  color: var(--mint-dim);
}

.grimoire-card__desc {
  margin: 0;
  font-size: 12px;
  color: var(--ink-4);
  line-height: 1.4;
}

.grimoire-effects {
  margin: 0;
  padding-left: 16px;
  font-size: 11px;
  color: var(--ink-3);
  line-height: 1.5;
}

.grimoire-card__stats {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--ink-3);
}

.grimoire-lock-hint {
  margin: 0;
  font-size: 11px;
  font-style: italic;
  color: var(--ink-4);
}

.grimoire-archetype-hint {
  margin: 0;
  font-size: 11px;
  font-style: italic;
  color: var(--danger-dim);
}

.grimoire-toggle {
  align-self: flex-start;
  font-family: var(--font-mono);
  font-size: 9.5px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  padding: 4px 10px;
  border: 1px solid var(--line-soft);
  background: transparent;
  color: var(--ink-4);
  cursor: pointer;
  transition: opacity 0.15s, border-color 0.15s, color 0.15s;
}

.grimoire-toggle:disabled {
  opacity: 0.38;
  cursor: not-allowed;
}

.grimoire-toggle:not(:disabled):hover {
  border-color: var(--ink-3);
  color: var(--ink-2);
}

.grimoire-toggle--active {
  border-color: var(--mint-dim);
  color: var(--mint-dim);
  background: var(--panel);
}
</style>
