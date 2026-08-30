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
  background: rgba(0, 0, 0, 0.72);
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
  border: 1px solid var(--line);
  background: var(--panel);
}

.jm-close {
  all: unset;
  position: absolute;
  top: 16px;
  right: 16px;
  z-index: 2;
  cursor: pointer;
  color: var(--ink-4);
  font-size: 12px;
  padding: 4px;
  transition: color .15s;
}
.jm-close:hover { color: var(--mint-dim); }

.jm-scroll {
  overflow-y: auto;
  padding: 48px 56px 40px;
}

.jm-eyebrow {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.jm-title {
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  font-size: clamp(26px, 3.6vw, 38px);
  line-height: 1.1;
  margin: 10px 0 0;
  color: var(--ink);
}

/* ── Empty state ── */
.jm-empty-quote {
  margin-top: 28px;
  padding: 15px 20px;
  border-left: 2px solid var(--mint-dim);
  background: var(--panel-2);
}

.jm-empty-quote__speaker {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--mint-dim);
  margin-bottom: 6px;
}

.jm-empty-quote__text {
  font-size: 15px;
  line-height: 1.55;
  color: var(--ink-2);
  font-style: italic;
  max-width: 560px;
}

.jm-empty-quote__text em {
  color: var(--mint-dim);
  font-style: normal;
}

/* ── Section header ── */
.jm-section-header {
  position: relative;
  display: flex;
  align-items: center;
  gap: 14px;
  margin: 34px 0 22px;
}

.jm-section-header__line {
  height: 1px;
  flex: 1 1 0%;
  background: var(--line);
}

.jm-section-header__diamond {
  width: 6px;
  height: 6px;
  transform: rotate(45deg);
  background: var(--mint-dim);
  flex-shrink: 0;
}

.jm-section-header__label {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 0.14em;
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
  width: 1px;
  background: var(--line);
}

.jm-point {
  position: relative;
  padding-left: 40px;
  margin-bottom: 18px;
}
.jm-point:last-child { margin-bottom: 0; }

.jm-point__marker {
  position: absolute;
  left: 9px;
  top: 4px;
  width: 10px;
  height: 10px;
  transform: rotate(45deg);
  background: var(--void);
  border: 1px solid var(--mint-dim);
  z-index: 2;
}

.jm-point__text {
  font-size: 14.5px;
  line-height: 1.6;
  font-style: italic;
  color: var(--ink-2);
  background: var(--panel-2);
  border-left: 1px solid var(--line-soft);
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
  font-family: var(--font-mono);
  font-size: 11px;
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
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--line-strong);
  border: none;
  cursor: pointer;
  padding: 0;
  transition: transform .15s, background .15s;
}

.jm-overview__dot--active {
  background: var(--mint-dim);
  transform: scale(1.4);
}
</style>
