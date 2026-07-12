<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useRunStore } from '../stores/runStore';
import type { RunJournalEntryDto } from '../types/runTypes';

defineEmits<{ close: [] }>();

const runStore = useRunStore();

type JournalPage = {
  roomIndex: number;
  roomNumber: number;
  roomDisplayName: string | null;
  entries: RunJournalEntryDto[];
};

const pages = computed<JournalPage[]>(() => {
  const entries = runStore.currentRun?.journalEntries ?? [];
  const byRoom = new Map<number, JournalPage>();

  for (const entry of entries) {
    let page = byRoom.get(entry.roomIndex);
    if (!page) {
      page = {
        roomIndex: entry.roomIndex,
        roomNumber: entry.roomNumber,
        roomDisplayName: entry.roomDisplayName,
        entries: [],
      };
      byRoom.set(entry.roomIndex, page);
    }
    page.entries.push(entry);
  }

  return [...byRoom.values()].sort((a, b) => a.roomIndex - b.roomIndex);
});

const pageIndex = ref(0);

// Jump to the latest page whenever the journal gains a new room — the player
// almost always wants to see what was just written, not page 1.
watch(
  pages,
  (newPages) => {
    pageIndex.value = newPages.length > 0 ? newPages.length - 1 : 0;
  },
  { immediate: true },
);

const currentPage = computed<JournalPage | null>(() => pages.value[pageIndex.value] ?? null);
const hasPages = computed(() => pages.value.length > 0);

function goToPreviousPage() {
  if (pageIndex.value > 0) pageIndex.value -= 1;
}

function goToNextPage() {
  if (pageIndex.value < pages.value.length - 1) pageIndex.value += 1;
}
</script>

<template>
  <div class="jm-backdrop" @click.self="$emit('close')">
    <div class="jm-panel">
      <button class="jm-close" @click="$emit('close')" aria-label="Fermer">✕</button>

      <div class="jm-scroll">
        <span class="jm-eyebrow">Journal · tenu à la main</span>
        <h2 class="jm-title">Le Carnet de bord</h2>

        <div v-if="!hasPages" class="jm-empty-quote">
          <div class="jm-empty-quote__speaker">○ Carnet</div>
          <div class="jm-empty-quote__text">
            Les pages sont encore vierges. <em>Elles ne le resteront pas longtemps.</em>
            Chaque salle que tu traverseras désormais y laissera sa trace.
          </div>
        </div>

        <template v-else-if="currentPage">
          <div class="jm-section-header">
            <span class="jm-section-header__line jm-section-header__line--left" />
            <span class="jm-section-header__diamond" />
            <span class="jm-section-header__label">
              Salle {{ currentPage.roomNumber }}<template v-if="currentPage.roomDisplayName"> — {{ currentPage.roomDisplayName }}</template>
            </span>
            <span class="jm-section-header__diamond" />
            <span class="jm-section-header__line jm-section-header__line--right" />
          </div>

          <div class="jm-timeline">
            <div class="jm-timeline__line" />
            <div v-for="(entry, i) in currentPage.entries" :key="i" class="jm-point">
              <span class="jm-point__marker" />
              <p class="jm-point__text">{{ entry.text }}</p>
            </div>
          </div>
        </template>

        <div v-if="pages.length > 1" class="jm-pager">
          <button class="es-btn es-btn--ghost" :disabled="pageIndex === 0" @click="goToPreviousPage">
            ‹ Salle précédente
          </button>
          <span class="jm-pager__status">Page {{ pageIndex + 1 }} / {{ pages.length }}</span>
          <button class="es-btn es-btn--ghost" :disabled="pageIndex === pages.length - 1" @click="goToNextPage">
            Salle suivante ›
          </button>
        </div>

        <div v-if="pages.length > 1" class="jm-overview">
          <button
            v-for="(page, i) in pages"
            :key="page.roomIndex"
            type="button"
            class="jm-overview__dot"
            :class="{ 'jm-overview__dot--active': i === pageIndex }"
            :title="`Salle ${page.roomNumber}`"
            @click="pageIndex = i"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.jm-backdrop {
  position: fixed;
  inset: 0;
  z-index: var(--z-modal);
  display: flex;
  align-items: center;
  justify-content: center;
  background: oklch(0.08 0.02 60 / 0.72);
  backdrop-filter: blur(4px);
  padding: var(--space-4);
}

.jm-panel {
  position: relative;
  width: min(880px, 92vw);
  max-height: 86vh;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  border-radius: var(--radius-md);
  border: 1px solid var(--line-strong);
  background: linear-gradient(180deg, var(--panel), var(--bg-2));
  box-shadow: var(--shadow-deep);
}

.jm-close {
  position: absolute;
  top: 14px;
  right: 14px;
  z-index: 2;
  background: none;
  border: none;
  cursor: pointer;
  color: var(--ink-4);
  font-size: 14px;
  padding: 6px;
  border-radius: 3px;
  transition: color .15s;
}
.jm-close:hover { color: var(--ink); }

.jm-scroll {
  overflow-y: auto;
  padding: 48px 56px 40px;
}

.jm-eyebrow {
  font-family: var(--font);
  font-size: 11px;
  letter-spacing: 0.22em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.jm-title {
  font-family: var(--display);
  font-weight: 500;
  font-size: clamp(26px, 3.6vw, 38px);
  line-height: 1.1;
  letter-spacing: 0.01em;
  margin: 10px 0 0;
  color: var(--ink);
}

/* ── Empty state (Elise-style quote card) ── */
.jm-empty-quote {
  margin-top: 28px;
  padding: 15px 20px;
  border-radius: 4px;
  border-left: 3px solid var(--frost);
  background: linear-gradient(90deg, oklch(0.36 0.07 252 / 0.4), oklch(0.22 0.024 58 / 0.6));
  backdrop-filter: blur(3px);
  box-shadow: 0 0 50px -20px oklch(0.66 0.1 252 / 0.5);
}

.jm-empty-quote__speaker {
  font-family: var(--font);
  font-size: 10px;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  color: var(--frost);
  margin-bottom: 6px;
}

.jm-empty-quote__text {
  font-size: 15px;
  line-height: 1.55;
  color: var(--ink);
  font-style: italic;
  max-width: 560px;
}

.jm-empty-quote__text em {
  color: var(--frost);
  font-style: normal;
  font-weight: 500;
}

/* ── Section header (diamond dividers, à la carte du Palais) ── */
.jm-section-header {
  position: relative;
  display: flex;
  align-items: center;
  gap: 14px;
  margin: 34px 0 22px;
}

.jm-section-header__line {
  height: 2px;
  flex: 1 1 0%;
  filter: blur(0.4px);
}
.jm-section-header__line--left {
  background: linear-gradient(90deg, transparent, var(--frost-dim));
}
.jm-section-header__line--right {
  background: linear-gradient(90deg, var(--frost-dim), transparent);
}

.jm-section-header__diamond {
  width: 9px;
  height: 9px;
  border-radius: 2px;
  transform: rotate(45deg);
  background: radial-gradient(circle at 40% 35%, oklch(0.88 0.08 252), var(--frost-dim));
  box-shadow: 0 0 14px oklch(0.68 0.1 252 / 0.6);
  flex-shrink: 0;
}

.jm-section-header__label {
  font-family: var(--font);
  font-size: 11px;
  letter-spacing: 0.22em;
  text-transform: uppercase;
  color: var(--ink-3);
  white-space: nowrap;
}

/* ── Timeline: point-by-point roadmap of what happened in this room ── */
.jm-timeline {
  position: relative;
  padding-left: 6px;
}

.jm-timeline__line {
  position: absolute;
  left: 14px;
  top: 6px;
  bottom: 6px;
  width: 2px;
  background: linear-gradient(var(--gold), var(--gold-deep) 55%, var(--frost));
  opacity: 0.45;
}

.jm-point {
  position: relative;
  padding-left: 40px;
  margin-bottom: 18px;
}
.jm-point:last-child { margin-bottom: 0; }

.jm-point__marker {
  position: absolute;
  left: 6px;
  top: 4px;
  width: 16px;
  height: 16px;
  border-radius: 3px;
  transform: rotate(45deg);
  background: radial-gradient(circle at 50% 32%, oklch(0.54 0.12 64), oklch(0.28 0.07 58));
  border: 1px solid var(--gold);
  box-shadow: 0 0 16px -2px oklch(0.74 0.13 64 / 0.6);
  z-index: 2;
}

.jm-point__text {
  font-size: 14.5px;
  line-height: 1.6;
  font-style: italic;
  color: var(--ink-2);
  background: oklch(0.30 0.024 268 / 0.35);
  border-left: 2px solid var(--line-strong);
  border-radius: 3px;
  padding: 10px 16px;
  margin: 0;
}

/* ── Pagination ── */
.jm-pager {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-top: 34px;
  padding-top: 18px;
  border-top: 1px solid var(--line-soft);
}

.jm-pager__status {
  font-family: var(--font);
  font-size: 11px;
  letter-spacing: 0.08em;
  color: var(--ink-4);
  white-space: nowrap;
}

/* ── Aperçu du carnet: one dot per page ── */
.jm-overview {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 6px;
  margin-top: 18px;
}

.jm-overview__dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--line-strong);
  border: none;
  cursor: pointer;
  padding: 0;
  transition: transform .15s, background .15s, box-shadow .15s;
}

.jm-overview__dot--active {
  background: var(--gold);
  box-shadow: 0 0 6px -1px var(--gold);
  transform: scale(1.3);
}
</style>
