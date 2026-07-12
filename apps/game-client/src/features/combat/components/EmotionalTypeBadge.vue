<script setup lang="ts">
import { computed } from 'vue';

import type { EmotionalType } from '../types/combatContracts';

const props = withDefaults(
  defineProps<{
    type: EmotionalType | string;
    /** Compact omits the label and shows only the colored glyph. */
    compact?: boolean;
  }>(),
  { compact: false },
);

type TypeMeta = { label: string; glyph: string; color: string };

const TYPE_META: Record<string, TypeMeta> = {
  Effroi: { label: 'Effroi', glyph: '✶', color: 'oklch(0.62 0.20 18)' },
  Deni: { label: 'Déni', glyph: '◇', color: 'oklch(0.78 0.13 78)' },
  Melancolie: { label: 'Mélancolie', glyph: '❍', color: 'oklch(0.70 0.11 248)' },
  Rupture: { label: 'Rupture', glyph: '⟡', color: 'oklch(0.66 0.19 38)' },
  Memoire: { label: 'Mémoire', glyph: '◈', color: 'oklch(0.84 0.11 86)' },
  Silence: { label: 'Silence', glyph: '○', color: 'oklch(0.80 0.02 272)' },
  Folie: { label: 'Folie', glyph: '✳', color: 'oklch(0.64 0.22 340)' },
  Neutral: { label: 'Neutre', glyph: '·', color: 'oklch(0.58 0.02 272)' },
};

const meta = computed<TypeMeta>(() => TYPE_META[props.type] ?? TYPE_META.Neutral);
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