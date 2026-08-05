<script setup lang="ts">
import type { CurrentEventChoiceResultDto } from '../types/eventTypes';

const props = defineProps<{ result: CurrentEventChoiceResultDto; isLoading: boolean }>()
defineEmits<{ continue: [] }>()

function isFailure(kind: string | undefined | null): boolean {
  if (!kind) return false
  const k = kind.toLowerCase()
  return k.includes('fail') || k.includes('death') || k.includes('loss') || k.includes('bad') || k.includes('malus')
}

function outcomeLabel(kind: string | undefined | null, state: string | undefined | null): string {
  return kind ?? state ?? 'Résolu'
}
</script>

<template>
  <div class="ecr-root">
    <p class="ecr-kicker">Choix résolu</p>

    <span
      class="es-chip ecr-chip"
      :class="isFailure(result.outcomeKind) ? 'es-chip--danger' : 'es-chip--mint'"
    >
      {{ outcomeLabel(result.outcomeKind, result.state) }}
    </span>

    <h2 class="ecr-title">{{ result.title ?? 'Choix accompli' }}</h2>

    <p class="ecr-desc">
      {{ result.description ?? result.message ?? 'Ton geste a été retenu par le Palais.' }}
    </p>

    <button
      class="ecr-btn"
      :disabled="isLoading"
      @click="$emit('continue')"
    >
      <span v-if="isLoading" class="ecr-spinner" aria-hidden="true">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
          stroke="currentColor" stroke-width="2.2" stroke-linecap="round" aria-hidden="true">
          <path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"/>
        </svg>
      </span>
      <span v-else>Continuer →</span>
    </button>
  </div>
</template>

<style scoped>
.ecr-root {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 12px;
  padding: 40px 36px;
  background: var(--panel);
  border: 1px solid var(--line);
  color: var(--ink);
  font-family: var(--font);
}

.ecr-kicker {
  margin: 0;
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.ecr-chip { margin-bottom: 4px; }

.ecr-title {
  margin: 4px 0 0;
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  font-size: 28px;
  line-height: 1.1;
  color: var(--ink);
}

.ecr-desc {
  margin: 0;
  font-style: italic;
  font-size: 14px;
  line-height: 1.6;
  color: var(--ink-3);
  max-width: 42ch;
}

.ecr-btn {
  min-width: 180px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  margin-top: 8px;
  padding: 11px 26px;
  border: 1px solid var(--mint-dim);
  color: var(--mint-dim);
  background: transparent;
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  cursor: pointer;
  transition: opacity .15s;
}
.ecr-btn:hover:not(:disabled) { opacity: .8; }
.ecr-btn:disabled { color: var(--ink-5); border-color: var(--line); cursor: not-allowed; }

.ecr-spinner {
  display: flex;
  align-items: center;
  animation: ecr-spin 1s linear infinite;
}
@keyframes ecr-spin {
  to { transform: rotate(360deg); }
}

@media (prefers-reduced-motion: reduce) {
  .ecr-spinner { animation: none; }
}
</style>
