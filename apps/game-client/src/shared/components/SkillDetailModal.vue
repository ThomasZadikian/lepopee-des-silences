<script setup lang="ts">
import type { SkillDefinitionView } from '../../features/party/types/skillTypes';
import { categoryLabel, formatEffect } from '../../features/runs/components/team-management/grimoireDisplay';

defineProps<{ skill: SkillDefinitionView | null }>();
const emit = defineEmits<{ close: [] }>();
</script>

<template>
  <Teleport to="body">
    <Transition name="skill-modal-fade">
      <div v-if="skill" class="skill-modal-backdrop" @click.self="emit('close')">
        <div class="skill-modal panel" role="dialog" :aria-modal="true">
          <div class="skill-modal__head">
            <span class="skill-modal__name">{{ skill.displayName }}</span>
            <span v-if="skill.isUltimate" class="skill-modal-chip skill-modal-chip--ultimate">Ultime</span>
            <span class="skill-modal-chip">{{ categoryLabel(skill.effectType) }}</span>
            <span class="skill-modal-chip skill-modal-chip--muted">
              {{ skill.category === 'Magic' ? 'Magique' : 'Physique' }}
            </span>
          </div>

          <p class="skill-modal__desc">{{ skill.description }}</p>

          <ul v-if="skill.effects.length" class="skill-modal__effects">
            <li v-for="(effect, index) in skill.effects" :key="index">{{ formatEffect(effect) }}</li>
          </ul>

          <div class="skill-modal__stats">
            <span>Puissance : {{ skill.basePower }}{{ skill.basePowerIsPercentOfMaxVitality ? '% PV max' : '' }}</span>
            <span v-if="skill.manaCost > 0">Mana : {{ skill.manaCost }}</span>
            <span v-if="skill.chargeCost > 0">Charge : {{ skill.chargeCost }}</span>
            <span>Portée : {{ skill.tacticalRange ?? 1 }}</span>
            <span>Zone : {{ skill.tacticalAreaShape ?? 'Single' }}</span>
            <span>{{ skill.requiresLineOfSight ? 'Ligne de vue' : 'Sans ligne de vue' }}</span>
            <span v-if="(skill.cooldown ?? 0) > 0">Recharge : {{ skill.cooldown }} tours</span>
          </div>

          <button type="button" class="ghost-button skill-modal__close" @click="emit('close')">
            Fermer
          </button>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.skill-modal-backdrop {
  position: fixed;
  inset: 0;
  z-index: 220;
  display: grid;
  place-items: center;
  background: color-mix(in oklch, var(--void), transparent 30%);
  backdrop-filter: blur(2px);
}

.skill-modal {
  width: min(30rem, 90vw);
  padding: var(--space-6, 20px);
  display: grid;
  gap: 10px;
}

.skill-modal__head {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}

.skill-modal__name {
  font-size: 15px;
  font-weight: 600;
  color: var(--gold, oklch(.72 .1 85));
}

.skill-modal-chip {
  font-family: var(--font-mono, monospace);
  font-size: 10px;
  padding: 1px 6px;
  border-radius: 999px;
  border: 1px solid var(--line-soft);
  color: var(--ink-3);
}

.skill-modal-chip--muted { color: var(--ink-4); }
.skill-modal-chip--ultimate { border-color: var(--gold); color: var(--gold); }

.skill-modal__desc {
  margin: 0;
  font-size: 12.5px;
  color: var(--ink-3);
  line-height: 1.5;
}

.skill-modal__effects {
  margin: 0;
  padding-left: 16px;
  font-size: 11px;
  color: var(--frost, var(--ink-3));
  line-height: 1.5;
}

.skill-modal__stats {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  color: var(--ink-3);
}

.skill-modal__close {
  justify-self: flex-end;
  color: var(--ink-4);
}

.skill-modal__close:hover {
  color: var(--ink);
}

.skill-modal-fade-enter-active,
.skill-modal-fade-leave-active {
  transition: opacity 0.15s ease;
}

.skill-modal-fade-enter-from,
.skill-modal-fade-leave-to {
  opacity: 0;
}
</style>
