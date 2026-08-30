<script setup lang="ts">
import type { PalacePublicIndicatorDto } from '../runs/types/runTypes';

defineProps<{
  indicator: PalacePublicIndicatorDto;
}>();

function toneClass(tone: string | null | undefined): string {
  const normalized = tone?.toLowerCase() ?? '';
  if (normalized === 'danger') return 'pic-root--danger';
  if (normalized === 'unstable') return 'pic-root--unstable';
  if (normalized === 'mystery') return 'pic-root--mystery';
  if (normalized === 'calm') return 'pic-root--calm';
  return '';
}
</script>

<template>
  <article class="pic-root" :class="toneClass(indicator.tone)">
    <div class="pic-head">
      <strong class="pic-title">{{ indicator.label || indicator.key }}</strong>
      <span v-if="indicator.level" class="es-chip pic-chip">{{ indicator.level }}</span>
      <span v-else-if="indicator.tone" class="es-chip pic-chip">{{ indicator.tone }}</span>
    </div>

    <p v-if="indicator.description" class="pic-description">{{ indicator.description }}</p>

    <dl class="pic-meta">
      <div v-if="indicator.category">
        <dt>Catégorie</dt>
        <dd>{{ indicator.category }}</dd>
      </div>
      <div v-if="indicator.source">
        <dt>Source</dt>
        <dd>{{ indicator.source }}</dd>
      </div>
      <div>
        <dt>Clé</dt>
        <dd>{{ indicator.key }}</dd>
      </div>
    </dl>
  </article>
</template>

<style scoped>
.pic-root {
  padding: 12px 13px;
  border: 1px solid color-mix(in oklch, var(--frost), transparent 72%);
  border-radius: var(--radius-sm);
  background: oklch(0.16 0.032 268 / 0.58);
}

.pic-root--danger {
  border-color: color-mix(in oklch, var(--blood), transparent 58%);
}

.pic-root--unstable {
  border-color: color-mix(in oklch, var(--gold), transparent 62%);
}

.pic-root--mystery {
  border-color: color-mix(in oklch, var(--frost), transparent 54%);
}

.pic-root--calm {
  border-color: color-mix(in oklch, var(--frost), transparent 72%);
  background: oklch(0.17 0.028 245 / 0.52);
}

.pic-head,
.pic-meta div {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-2);
}

.pic-title {
  min-width: 0;
  color: var(--ink);
  font-family: var(--font-display);
  font-size: 0.92rem;
}

.pic-chip {
  flex: 0 0 auto;
  font-size: 0.62rem;
  padding: 1px 6px;
}

.pic-description {
  margin: 8px 0 0;
  color: var(--ink-3);
  font-size: 0.78rem;
  line-height: 1.45;
}

.pic-meta {
  display: grid;
  gap: 4px;
  margin: 10px 0 0;
  font-family: var(--font-mono);
  font-size: 0.66rem;
}

.pic-meta dt,
.pic-meta dd {
  margin: 0;
}

.pic-meta dt {
  color: var(--ink-5);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.pic-meta dd {
  color: var(--frost-dim);
  text-align: right;
}
</style>
