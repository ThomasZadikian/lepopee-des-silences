<script setup lang="ts">
import { computed, ref } from 'vue';

const props = defineProps<{
  modelValue: boolean;
  title: string;
  pages: string[];
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
}>();

const currentPage = ref(0);
const direction = ref<'flip-next' | 'flip-prev'>('flip-next');

const totalPages = computed(() => props.pages.length);
const canGoNext = computed(() => currentPage.value < totalPages.value - 1);
const canGoPrev = computed(() => currentPage.value > 0);
const currentText = computed(() => props.pages[currentPage.value] ?? '');

function next() {
  if (!canGoNext.value) return;
  direction.value = 'flip-next';
  currentPage.value += 1;
}

function prev() {
  if (!canGoPrev.value) return;
  direction.value = 'flip-prev';
  currentPage.value -= 1;
}

function close() {
  currentPage.value = 0;
  emit('update:modelValue', false);
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'ArrowRight') next();
  else if (event.key === 'ArrowLeft') prev();
  else if (event.key === 'Escape') close();
}
</script>

<template>
  <Teleport to="body">
    <Transition name="book-fade">
      <div
        v-if="modelValue"
        class="book-backdrop"
        tabindex="-1"
        @click.self="close"
        @keydown="onKeydown"
      >
        <div class="book panel" role="dialog" :aria-modal="true" :aria-label="title">
          <header class="book__head">
            <span class="system-label">Carnet</span>
            <h3 class="book__title">{{ title }}</h3>
            <button class="book__close" aria-label="Fermer le carnet" @click="close">
              <svg width="14" height="14" viewBox="0 0 14 14" fill="none"
                stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true">
                <line x1="3" y1="3" x2="11" y2="11" />
                <line x1="11" y1="3" x2="3" y2="11" />
              </svg>
            </button>
          </header>

          <div class="book__stage">
            <Transition :name="direction">
              <p :key="currentPage" class="book__page">{{ currentText }}</p>
            </Transition>
          </div>

          <footer class="book__foot">
            <button class="book__nav" :disabled="!canGoPrev" @click="prev">‹ Page précédente</button>
            <span class="book__counter">Page {{ currentPage + 1 }} / {{ totalPages }}</span>
            <button class="book__nav" :disabled="!canGoNext" @click="next">Page suivante ›</button>
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.book-backdrop {
  position: fixed;
  inset: 0;
  z-index: 220;
  display: grid;
  place-items: center;
  background: color-mix(in oklch, var(--void), transparent 22%);
  backdrop-filter: blur(3px);
}

.book {
  width: min(34rem, 90vw);
  max-height: 80vh;
  padding: var(--space-6);
  display: grid;
  grid-template-rows: auto 1fr auto;
  gap: var(--space-4);
}

.book__head {
  display: flex;
  align-items: flex-start;
  gap: var(--space-3);
}

.book__title {
  flex: 1;
  margin: 2px 0 0;
  font-family: var(--font-display, var(--font));
  font-size: 1.2rem;
  color: var(--gold);
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.book__close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: 4px;
  border: 1px solid var(--line, oklch(.35 .025 60 / .6));
  background: oklch(.26 .02 60 / .55);
  color: var(--ink-3);
  cursor: pointer;
  flex: 0 0 auto;
  transition: border-color .15s, color .15s, background .15s;
}

.book__close:hover {
  border-color: var(--line-strong);
  color: var(--ink);
}

/* ── Stage: the visible "page" area, with 3D perspective for the flip ── */
.book__stage {
  position: relative;
  min-height: 14rem;
  padding: var(--space-5);
  perspective: 1600px;
  overflow: hidden;
  background: color-mix(in oklch, var(--ink), transparent 96%);
  border: 1px solid color-mix(in oklch, var(--line), transparent 40%);
  border-radius: 4px;
}

.book__page {
  position: absolute;
  inset: var(--space-5);
  margin: 0;
  overflow-y: auto;
  font-family: var(--font, serif);
  font-size: 0.95rem;
  line-height: 1.7;
  color: var(--ink-2);
  white-space: pre-wrap;
  backface-visibility: hidden;
  transform-style: preserve-3d;
}

.flip-next-enter-active,
.flip-next-leave-active,
.flip-prev-enter-active,
.flip-prev-leave-active {
  transition: transform 0.42s cubic-bezier(.4, 0, .2, 1), opacity 0.42s ease;
}

.flip-next-enter-from { transform: rotateY(90deg); opacity: 0; transform-origin: left center; }
.flip-next-leave-to   { transform: rotateY(-90deg); opacity: 0; transform-origin: right center; }
.flip-prev-enter-from { transform: rotateY(-90deg); opacity: 0; transform-origin: right center; }
.flip-prev-leave-to   { transform: rotateY(90deg); opacity: 0; transform-origin: left center; }

.book__foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  padding-top: var(--space-2);
  border-top: 1px solid color-mix(in oklch, var(--line), transparent 40%);
}

.book__nav {
  background: none;
  border: 1px solid color-mix(in oklch, var(--line), transparent 20%);
  border-radius: 4px;
  padding: 6px 12px;
  color: var(--ink-3);
  font-family: var(--font-caps, var(--font));
  font-size: 0.7rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  cursor: pointer;
  transition: border-color .15s, color .15s;
}

.book__nav:hover:not(:disabled) {
  border-color: var(--gold);
  color: var(--gold);
}

.book__nav:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.book__counter {
  font-family: var(--font-mono, monospace);
  font-size: 0.75rem;
  color: var(--ink-4);
}

.book-fade-enter-active,
.book-fade-leave-active {
  transition: opacity 0.15s ease;
}
.book-fade-enter-from,
.book-fade-leave-to {
  opacity: 0;
}

@media (prefers-reduced-motion: reduce) {
  .flip-next-enter-active,
  .flip-next-leave-active,
  .flip-prev-enter-active,
  .flip-prev-leave-active {
    transition: opacity 0.15s ease;
  }
  .flip-next-enter-from,
  .flip-next-leave-to,
  .flip-prev-enter-from,
  .flip-prev-leave-to {
    transform: none;
  }
}
</style>
