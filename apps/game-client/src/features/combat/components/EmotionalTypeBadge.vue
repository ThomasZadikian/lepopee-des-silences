<script setup lang="ts">
import { computed } from 'vue';

import type { EmotionalType } from '../types/combatContracts';
import { EMOTIONAL_TYPE_META } from '../../../shared/theme/typeColors';

const props = withDefaults(
  defineProps<{
    type: EmotionalType | string;
    /** Compact omits the label and shows only the colored glyph. */
    compact?: boolean;
  }>(),
  { compact: false },
);

const meta = computed(() => EMOTIONAL_TYPE_META[props.type] ?? EMOTIONAL_TYPE_META.Neutral);
const isNeutral = computed(() => (props.type ?? 'Neutral') === 'Neutral');
</script>

<template>
  <span
    v-if="!isNeutral"
    class="type-badge"
    :class="{ 'type-badge--compact': compact }"
    :style="{ '--type-color': meta.color }"
    :title="`Type émotionnel : ${meta.label}`"
  >
    <span class="type-badge__glyph">{{ meta.glyph }}</span>
    <span v-if="!compact" class="type-badge__label">{{ meta.label }}</span>
  </span>
</template>

<style scoped>
.type-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 1px 7px;
  border: 1px solid color-mix(in oklch, var(--type-color), transparent 55%);
  border-radius: 999px;
  background: color-mix(in oklch, var(--type-color), transparent 88%);
  font-family: var(--font-caps);
  font-size: 0.52rem;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--type-color);
  white-space: nowrap;
  line-height: 1.4;
}

.type-badge--compact { padding: 1px 4px; font-size: 0.62rem; }
.type-badge__glyph { font-size: 0.7em; line-height: 1; }
.type-badge__label { font-weight: 600; }
</style>