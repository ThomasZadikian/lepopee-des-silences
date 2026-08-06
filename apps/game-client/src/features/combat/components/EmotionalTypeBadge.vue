<script setup lang="ts">
import { computed } from 'vue';

import { useEmotionalRegisterCatalog } from '../../emotional-registers/store';

const props = withDefaults(
  defineProps<{
    type: string;
    /** Compact omits the label and shows only the colored glyph. */
    compact?: boolean;
  }>(),
  { compact: false },
);

const registerStore = useEmotionalRegisterCatalog();
const definition = computed(() => registerStore.definitionOf(props.type));
const label = computed(() => definition.value?.displayName ?? props.type);
const glyph = computed(() => definition.value?.glyph ?? '!');
const color = computed(() => definition.value?.color ?? 'var(--danger-dim)');
</script>

<template>
  <span
    class="type-badge"
    :class="{ 'type-badge--compact': compact, 'type-badge--invalid': !definition }"
    :style="{ '--type-color': color }"
    :title="definition ? `Registre émotionnel : ${label}` : `Registre émotionnel inconnu : ${type}`"
  >
    <span class="type-badge__glyph">{{ glyph }}</span>
    <span v-if="!compact" class="type-badge__label">{{ label }}</span>
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
.type-badge--invalid { border-style: dashed; }
.type-badge__glyph { font-size: 0.7em; line-height: 1; }
.type-badge__label { font-weight: 600; }
</style>
