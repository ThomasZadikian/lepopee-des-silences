<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRunStore } from '../../runs/stores/runStore';
import type { RunItemDto } from '../../runs/types/runTypes';
import { inventoryApi } from '../api/inventoryApi';
import { itemsApi } from '../../party/api/itemsApi';
import BookReader from '../../../shared/components/BookReader.vue';

const props = defineProps<{
  items: RunItemDto[];
  runId: string;
  capacity?: number | null;
}>();

const distinctItemCount = computed(() => props.items.length);
const isBagFull = computed(() =>
  typeof props.capacity === 'number' && distinctItemCount.value >= props.capacity,
);

const emit = defineEmits<{
  close: [];
}>();

const runStore = useRunStore();
const selectedItem = ref<RunItemDto | null>(null);
const isLoading = ref(false);
const error = ref<string | null>(null);
const readablePagesByKey = ref<Record<string, string[]>>({});
const isReaderOpen = ref(false);

onMounted(async () => {
  try {
    const { items: catalogItems } = await itemsApi.listActive();
    const byKey: Record<string, string[]> = {};
    for (const item of catalogItems) {
      if (item.readablePages && item.readablePages.length > 0) {
        byKey[item.key] = item.readablePages;
      }
    }
    readablePagesByKey.value = byKey;
  } catch {
    // Best-effort: a failed catalog fetch just hides the "Lire" affordance.
  }
});

const selectedItemPages = computed(() =>
  selectedItem.value ? readablePagesByKey.value[selectedItem.value.definitionKey] : undefined,
);

function getRarityTone(rarity: string): string {
  switch (rarity) {
    case 'Uncommon': return 'sap';
    case 'Rare':     return 'frost';
    case 'Epic':     return 'gold';
    default:         return '';
  }
}

function getRarityLabel(rarity: string): string {
  switch (rarity) {
    case 'Uncommon': return 'Peu commun';
    case 'Rare':     return 'Rare';
    case 'Epic':     return 'Épique';
    default:         return 'Commun';
  }
}

function getEffectLabel(effectType: string, effectAmount: number): string {
  switch (effectType) {
    case 'Heal':              return `+${effectAmount} Vitalité`;
    case 'Guard':             return `+${effectAmount} Garde`;
    case 'ManaRestore':       return `+${effectAmount} Mana`;
    case 'ChargeRestore':     return `+${effectAmount} Charge`;
    case 'NextCombatGuard':   return `+${effectAmount} Garde (prochain combat)`;
    case 'NarrativeFragment': return 'Fragment narratif';
    default:                  return '';
  }
}

function getEffectTone(effectType: string): string {
  switch (effectType) {
    case 'Heal':
    case 'ManaRestore':
    case 'ChargeRestore':
    case 'NextCombatGuard':   return 'sap';
    case 'Guard':             return 'frost';
    case 'NarrativeFragment': return 'gold';
    case 'Damage':            return 'blood';
    default:                  return '';
  }
}

function selectItem(item: RunItemDto) {
  selectedItem.value = item;
  error.value = null;
}

async function useItem() {
  if (!selectedItem.value) return;
  isLoading.value = true;
  error.value = null;
  try {
    await inventoryApi.useItem(props.runId, selectedItem.value.id);
    await runStore.loadRun(props.runId);
    selectedItem.value = null;
  } catch (err) {
    error.value = err instanceof Error ? err.message : "L'utilisation a échoué.";
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <aside class="bsd-root" aria-label="La Besace">
    <!-- Frost corner ornaments -->
    <span class="bsd-corner bsd-corner--tl" aria-hidden="true" />
    <span class="bsd-corner bsd-corner--bl" aria-hidden="true" />

    <!-- Header -->
    <header class="bsd-head">
      <div class="bsd-head__left">
        <!-- Bag sigil -->
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
          stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"
          aria-hidden="true" style="color:var(--gold, oklch(.72 .1 85)); flex:0 0 auto; margin-top:2px;">
          <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"/>
          <line x1="3" y1="6" x2="21" y2="6"/>
          <path d="M16 10a4 4 0 0 1-8 0"/>
        </svg>
        <div>
          <span class="bsd-kicker">La Besace</span>
          <h3 class="bsd-title">Objets de run</h3>
        </div>
      </div>

      <span v-if="typeof capacity === 'number'" class="bsd-capacity" :class="{ 'bsd-capacity--full': isBagFull }">
        {{ distinctItemCount }} / {{ capacity }}
      </span>

      <button class="bsd-close" @click="emit('close')" aria-label="Fermer la besace">
        <svg width="14" height="14" viewBox="0 0 14 14" fill="none"
          stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true">
          <line x1="3" y1="3" x2="11" y2="11"/>
          <line x1="11" y1="3" x2="3" y2="11"/>
        </svg>
      </button>
    </header>

    <!-- Divider -->
    <div class="bsd-divider" />

    <!-- Empty state -->
    <div v-if="items.length === 0" class="bsd-empty">
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor"
        stroke-width="1" stroke-linecap="round" stroke-linejoin="round"
        aria-hidden="true" style="color:var(--ink-4); margin-bottom:8px;">
        <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"/>
        <line x1="3" y1="6" x2="21" y2="6"/>
      </svg>
      <span class="bsd-empty__text">Ton sac est vide.</span>
    </div>

    <!-- Item grid -->
    <div v-else class="bsd-grid">
      <button
        v-for="item in items"
        :key="item.id"
        class="bsd-cell"
        :class="[
          selectedItem?.id === item.id ? 'bsd-cell--sel' : '',
          getRarityTone(item.rarity) ? `bsd-cell--${getRarityTone(item.rarity)}` : '',
        ]"
        @click="selectItem(item)"
      >
        <span class="bsd-cell__name">{{ item.displayName }}</span>
        <div class="bsd-cell__foot">
          <span v-if="item.quantity > 1" class="bsd-cell__qty">×{{ item.quantity }}</span>
          <span
            class="bsd-cell__rarity"
            :class="getRarityTone(item.rarity) ? `bsd-cell__rarity--${getRarityTone(item.rarity)}` : ''"
          >
            {{ getRarityLabel(item.rarity) }}
          </span>
        </div>
      </button>
    </div>

    <!-- Item detail sheet -->
    <Transition name="bsd-slide">
      <div v-if="selectedItem" class="bsd-sheet">
        <!-- Sheet header -->
        <div class="bsd-sheet__head">
          <h4 class="bsd-sheet__name">{{ selectedItem.displayName }}</h4>
          <div class="bsd-sheet__badges">
            <span v-if="selectedItem.quantity > 1" class="bsd-badge">×{{ selectedItem.quantity }}</span>
            <span
              v-if="getRarityTone(selectedItem.rarity)"
              class="bsd-badge"
              :class="`bsd-badge--${getRarityTone(selectedItem.rarity)}`"
            >
              {{ getRarityLabel(selectedItem.rarity) }}
            </span>
          </div>
        </div>

        <!-- Type -->
        <p class="bsd-sheet__type">{{ selectedItem.type }}</p>

        <!-- Description -->
        <p class="bsd-sheet__desc">{{ selectedItem.description }}</p>

        <!-- Effect -->
        <div v-if="selectedItem.effectAmount > 0" class="bsd-sheet__effect">
          <span class="bsd-effect-label">Effet</span>
          <span
            class="bsd-effect-value"
            :class="getEffectTone(selectedItem.effectType) ? `bsd-effect-value--${getEffectTone(selectedItem.effectType)}` : ''"
          >
            {{ getEffectLabel(selectedItem.effectType, selectedItem.effectAmount) }}
          </span>
        </div>

        <!-- Actions -->
        <div class="bsd-sheet__actions">
          <button
            v-if="selectedItemPages"
            class="bsd-action-btn bsd-action-btn--read"
            @click="isReaderOpen = true"
          >
            Lire
          </button>
          <button
            v-if="selectedItem.isUsable"
            class="bsd-action-btn bsd-action-btn--use"
            :disabled="isLoading"
            @click="useItem"
          >
            {{ isLoading ? 'Utilisation…' : 'Utiliser' }}
          </button>
          <p v-if="!selectedItem.isUsable && !selectedItemPages" class="bsd-sheet__unusable">
            Cet objet ne peut pas être utilisé actuellement.
          </p>
        </div>

        <p v-if="error" class="bsd-sheet__error">{{ error }}</p>
      </div>
    </Transition>

    <BookReader
      v-if="selectedItem && selectedItemPages"
      v-model="isReaderOpen"
      :title="selectedItem.displayName"
      :pages="selectedItemPages"
    />
  </aside>
</template>

<style scoped>
/* ── Root : side drawer ── */
.bsd-root {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 40px;
  width: 300px;
  display: flex;
  flex-direction: column;
  padding: 0;
  z-index: var(--z-drawer, 30);
  overflow: hidden;

  background: oklch(.20 .028 60 / .88);
  backdrop-filter: blur(20px) saturate(1.3);
  -webkit-backdrop-filter: blur(20px) saturate(1.3);

  border-left: 1px solid var(--line-strong, oklch(.38 .03 60 / .7));
  border-top: 1px solid oklch(.55 .08 84 / .2);
  box-shadow:
    -8px 0 60px -12px oklch(.15 .03 60 / .92),
    inset 1px 0 0 oklch(.6 .07 232 / .06);

  outline: none;
}

/* ── Frost corners ── */
.bsd-corner {
  position: absolute;
  left: 0;
  width: 22px;
  height: 22px;
  pointer-events: none;
  border-color: var(--frost, oklch(.70 .07 232));
  opacity: .45;
}
.bsd-corner--tl {
  top: 0;
  border-top: 1px solid;
  border-left: 1px solid;
}
.bsd-corner--bl {
  bottom: 0;
  border-bottom: 1px solid;
  border-left: 1px solid;
}

/* ── Header ── */
.bsd-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  padding: 18px 18px 12px;
  flex: 0 0 auto;
}

.bsd-head__left {
  display: flex;
  align-items: flex-start;
  gap: 10px;
}

.bsd-kicker {
  display: block;
  font-family: var(--font-caps, var(--font));
  font-size: 9.5px;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  color: oklch(.65 .09 84 / .7);
  margin-bottom: 3px;
}

.bsd-title {
  font-family: var(--font-display, var(--font));
  font-size: 20px;
  font-weight: 500;
  color: var(--ink, oklch(.88 .015 70));
  line-height: 1.15;
  margin: 0;
}

.bsd-capacity {
  flex: 0 0 auto;
  align-self: center;
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  letter-spacing: 0.04em;
  color: var(--ink-4, oklch(.45 .015 275));
}

.bsd-capacity--full {
  color: var(--blood, oklch(.52 .15 20));
}

.bsd-close {
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

.bsd-close:hover {
  border-color: var(--line-strong);
  color: var(--ink);
  background: oklch(.30 .025 60 / .65);
}

.bsd-close:focus-visible {
  outline: 2px solid var(--frost);
  outline-offset: 2px;
}

/* ── Divider ── */
.bsd-divider {
  flex: 0 0 auto;
  height: 1px;
  background: linear-gradient(90deg, transparent, var(--line, oklch(.35 .025 60 / .6)), transparent);
  margin: 0 18px 2px;
}

/* ── Empty ── */
.bsd-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px 24px;
  gap: 4px;
}

.bsd-empty__text {
  font-family: var(--font-caps, var(--font));
  font-size: 10px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4, oklch(.45 .015 275));
}

/* ── Grid ── */
.bsd-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  align-content: start;
  gap: 6px;
  padding: 8px 14px 0;
  overflow-y: auto;
  flex: 1;
}

/* ── Cell ── */
.bsd-cell {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 10px 11px;
  border: 1px solid var(--line-soft, oklch(.32 .022 60 / .5));
  border-radius: 5px;
  background: linear-gradient(180deg, oklch(.30 .034 60 / .38), oklch(.25 .028 60 / .30));
  cursor: pointer;
  text-align: left;
  transition: border-color .16s, background .16s, transform .12s;
}

.bsd-cell:hover {
  border-color: var(--line-strong, oklch(.42 .03 60 / .7));
  transform: translateY(-1px);
}

.bsd-cell--sel {
  border-color: var(--frost, oklch(.70 .07 232));
  background: linear-gradient(180deg, oklch(.34 .05 232 / .18), oklch(.28 .04 232 / .14));
  box-shadow: 0 0 16px -6px oklch(.70 .07 232 / .4);
}

.bsd-cell--sap:not(.bsd-cell--sel)   { border-left: 2px solid oklch(.70 .09 162 / .6); }
.bsd-cell--frost:not(.bsd-cell--sel) { border-left: 2px solid oklch(.70 .07 232 / .6); }
.bsd-cell--gold:not(.bsd-cell--sel)  { border-left: 2px solid oklch(.72 .1 85 / .6); }

.bsd-cell__name {
  font-family: var(--font, serif);
  font-size: 12.5px;
  color: var(--ink-2, oklch(.78 .02 275));
  line-height: 1.25;
}

.bsd-cell__foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 5px;
}

.bsd-cell__qty {
  font-family: var(--font-mono, monospace);
  font-size: 9.5px;
  color: var(--ink-4, oklch(.45 .015 275));
}

.bsd-cell__rarity {
  font-family: var(--font-caps, var(--font));
  font-size: 8.5px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4, oklch(.45 .015 275));
}

.bsd-cell__rarity--sap   { color: var(--sap, oklch(.70 .09 162)); }
.bsd-cell__rarity--frost { color: var(--frost, oklch(.70 .07 232)); }
.bsd-cell__rarity--gold  { color: var(--gold, oklch(.72 .1 85)); }

/* ── Item sheet ── */
.bsd-sheet {
  border-top: 1px solid var(--line-soft, oklch(.32 .022 60 / .5));
  margin-top: auto;
  flex: 0 0 auto;
  padding: 16px 18px 18px;
  background: linear-gradient(180deg, oklch(.24 .034 60 / .55), oklch(.20 .028 60 / .45));
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.bsd-sheet__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
}

.bsd-sheet__name {
  font-family: var(--font-display, var(--font));
  font-size: 18px;
  font-weight: 600;
  color: var(--gold, oklch(.72 .1 85));
  line-height: 1.2;
  margin: 0;
}

.bsd-sheet__badges {
  display: flex;
  gap: 5px;
  flex: 0 0 auto;
}

.bsd-badge {
  font-family: var(--font-caps, var(--font));
  font-size: 9px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  padding: 2px 6px;
  border-radius: 3px;
  border: 1px solid var(--line, oklch(.35 .025 60 / .6));
  color: var(--ink-4, oklch(.45 .015 275));
  background: oklch(.26 .025 60 / .6);
}

.bsd-badge--sap   { border-color: oklch(.70 .09 162 / .45); color: var(--sap, oklch(.70 .09 162)); background: oklch(.50 .07 162 / .12); }
.bsd-badge--frost { border-color: oklch(.70 .07 232 / .45); color: var(--frost, oklch(.70 .07 232)); background: oklch(.50 .06 232 / .12); }
.bsd-badge--gold  { border-color: oklch(.72 .1 85 / .45); color: var(--gold, oklch(.72 .1 85)); background: oklch(.55 .08 85 / .12); }

.bsd-sheet__type {
  font-family: var(--font-caps, var(--font));
  font-size: 9px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--ink-4, oklch(.45 .015 275));
  margin: 0;
}

.bsd-sheet__desc {
  font-family: var(--font, serif);
  font-size: 12.5px;
  line-height: 1.55;
  color: var(--ink-3, oklch(.65 .02 275));
  margin: 0;
}

.bsd-sheet__effect {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.bsd-effect-label {
  font-family: var(--font-caps, var(--font));
  font-size: 9px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--ink-4, oklch(.45 .015 275));
}

.bsd-effect-value {
  font-family: var(--font-mono, monospace);
  font-size: 12px;
  color: var(--ink-2, oklch(.78 .02 275));
}

.bsd-effect-value--sap   { color: var(--sap, oklch(.70 .09 162)); }
.bsd-effect-value--frost { color: var(--frost, oklch(.70 .07 232)); }
.bsd-effect-value--gold  { color: var(--gold, oklch(.72 .1 85)); }
.bsd-effect-value--blood { color: var(--blood, oklch(.52 .15 20)); }

.bsd-sheet__actions {
  padding-top: 2px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.bsd-action-btn {
  width: 100%;
  padding: 10px 16px;
  border-radius: 4px;
  font-family: var(--font-caps, var(--font));
  font-size: 10.5px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  cursor: pointer;
  transition: opacity .15s, box-shadow .15s;
}

.bsd-action-btn:disabled { opacity: .38; cursor: not-allowed; }

.bsd-action-btn--use {
  border: 1px solid var(--gold, oklch(.72 .1 85));
  background: oklch(.55 .08 85 / .15);
  color: var(--gold, oklch(.72 .1 85));
  box-shadow: 0 0 18px -6px oklch(.72 .1 85 / .4);
}

.bsd-action-btn--use:not(:disabled):hover {
  background: oklch(.55 .08 85 / .24);
  box-shadow: 0 0 26px -6px oklch(.72 .1 85 / .55);
}

.bsd-action-btn--read {
  border: 1px solid var(--frost, oklch(.70 .07 232));
  background: oklch(.50 .06 232 / .15);
  color: var(--frost, oklch(.70 .07 232));
  box-shadow: 0 0 18px -6px oklch(.70 .07 232 / .4);
}

.bsd-action-btn--read:hover {
  background: oklch(.50 .06 232 / .24);
  box-shadow: 0 0 26px -6px oklch(.70 .07 232 / .55);
}

.bsd-sheet__unusable {
  font-family: var(--font-caps, var(--font));
  font-size: 10px;
  letter-spacing: 0.1em;
  color: var(--ink-4, oklch(.45 .015 275));
  margin: 0;
}

.bsd-sheet__error {
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  color: var(--blood, oklch(.52 .15 20));
  margin: 0;
}

/* ── Slide transition ── */
.bsd-slide-enter-active,
.bsd-slide-leave-active {
  transition: opacity .22s, transform .22s;
}
.bsd-slide-enter-from,
.bsd-slide-leave-to {
  opacity: 0;
  transform: translateY(8px);
}

@media (prefers-reduced-motion: reduce) {
  .bsd-cell,
  .bsd-close,
  .bsd-action-btn {
    transition: none;
  }
}
</style>
