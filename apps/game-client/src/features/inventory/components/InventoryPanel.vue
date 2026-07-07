<script setup lang="ts">
import { computed, ref } from 'vue';
import { useCombatStore } from '../../combat/stores/useCombatStore';
import type { CombatantRuntimeDto } from '../../combat/types/combatContracts';
import { useRunStore } from '../../runs/stores/runStore';
import type { RunItemDto } from '../../runs/types/runTypes';
import { inventoryApi } from '../api/inventoryApi';

const props = defineProps<{
  items: RunItemDto[];
  runId: string;
  capacity?: number | null;
}>();

const distinctItemCount = computed(() => props.items.length);
const isBagFull = computed(() =>
  typeof props.capacity === 'number' && distinctItemCount.value >= props.capacity,
);

const combatStore = useCombatStore();
const runStore = useRunStore();

const pendingItemId = ref<string | null>(null);
const isLoading = ref(false);
const error = ref<string | null>(null);

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

function getEffectTone(effectType: string): 'sap' | 'frost' | 'blood' | 'gold' | '' {
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

const availableAllies = (): CombatantRuntimeDto[] => {
  if (combatStore.allies.length) return combatStore.allies.filter(a => a.status !== 'Defeated');
  return [];
};

function startUseItem(itemId: string) {
  error.value = null;
  pendingItemId.value = itemId;
}

function cancelUseItem() {
  pendingItemId.value = null;
  error.value = null;
}

async function confirmUseItem(_targetCombatantId?: string) {
  if (!pendingItemId.value) return;
  isLoading.value = true;
  error.value = null;
  try {
    const response = await inventoryApi.useItem(props.runId, pendingItemId.value);
    if (response.usedInCombat && response.combat) {
      if (response.logEntries?.length) await combatStore.playCombatLogs(response.logEntries);
      combatStore.finishCombatResponse(response.combat);
    }
    await runStore.loadRun(props.runId);
    pendingItemId.value = null;
  } catch (err) {
    error.value = err instanceof Error ? err.message : "L'utilisation a échoué.";
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <section class="inv-root es-panel">
    <!-- Header -->
    <header class="inv-head">
      <span class="inv-kicker">
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor"
          stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"/>
          <line x1="3" y1="6" x2="21" y2="6"/>
          <path d="M16 10a4 4 0 0 1-8 0"/>
        </svg>
        Inventaire de run
      </span>
      <h2 class="inv-title">Sac à dos</h2>
      <span v-if="typeof capacity === 'number'" class="inv-capacity" :class="{ 'inv-capacity--full': isBagFull }">
        {{ distinctItemCount }} / {{ capacity }}
      </span>
    </header>

    <div class="inv-divider" />

    <!-- Empty state -->
    <div v-if="items.length === 0" class="inv-empty">
      <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="currentColor"
        stroke-width="1" stroke-linecap="round" stroke-linejoin="round"
        aria-hidden="true" style="color:var(--ink-4, oklch(.45 .015 275)); margin-bottom:10px;">
        <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"/>
        <line x1="3" y1="6" x2="21" y2="6"/>
      </svg>
      <p class="inv-empty__text">Ton sac est vide. Les objets trouvés apparaîtront ici.</p>
    </div>

    <!-- Item list -->
    <div v-else class="inv-list">
      <article
        v-for="item in items"
        :key="item.id"
        class="inv-item"
        :class="`inv-item--${getRarityTone(item.rarity) || 'common'}`"
      >
        <!-- Item header row -->
        <div class="inv-item__head">
          <div class="inv-item__left">
            <span class="inv-item__name">{{ item.displayName }}</span>
            <span v-if="item.quantity > 1" class="inv-item__qty">×{{ item.quantity }}</span>
          </div>
          <span
            class="inv-chip"
            :class="getRarityTone(item.rarity) ? `inv-chip--${getRarityTone(item.rarity)}` : ''"
          >
            {{ getRarityLabel(item.rarity) }}
          </span>
        </div>

        <!-- Type kicker -->
        <p class="inv-item__type">{{ item.type }}</p>

        <!-- Description -->
        <p class="inv-item__desc">{{ item.description }}</p>

        <!-- Effect badge -->
        <div v-if="item.effectAmount > 0" class="inv-item__effect">
          <span
            class="inv-effect-badge"
            :class="getEffectTone(item.effectType) ? `inv-effect-badge--${getEffectTone(item.effectType)}` : ''"
          >
            {{ getEffectLabel(item.effectType, item.effectAmount) }}
          </span>
        </div>

        <!-- Actions -->
        <div v-if="item.isUsable" class="inv-item__actions">
          <!-- Use button -->
          <button
            v-if="pendingItemId !== item.id"
            class="inv-btn inv-btn--use"
            :disabled="isLoading || pendingItemId !== null"
            @click="startUseItem(item.id)"
          >
            Utiliser
          </button>

          <!-- Ally picker -->
          <div v-else class="inv-picker">
            <p class="inv-picker__label">Qui bénéficie de cet objet ?</p>

            <template v-if="availableAllies().length > 0">
              <button
                v-for="ally in availableAllies()"
                :key="ally.id"
                class="inv-ally-btn"
                :disabled="isLoading"
                @click="confirmUseItem(ally.id)"
              >
                <span class="inv-ally-btn__name">{{ ally.displayName }}</span>
                <span class="inv-ally-btn__hp">{{ ally.currentVitality }} / {{ ally.maxVitality }} PV</span>
              </button>
            </template>

            <template v-else>
              <button
                class="inv-ally-btn"
                :disabled="isLoading"
                @click="confirmUseItem()"
              >
                <span class="inv-ally-btn__name">Joueur</span>
              </button>
            </template>

            <button
              class="inv-btn inv-btn--cancel"
              :disabled="isLoading"
              @click="cancelUseItem"
            >
              Annuler
            </button>

            <p v-if="error" class="inv-error">{{ error }}</p>
          </div>
        </div>
      </article>
    </div>
  </section>
</template>

<style scoped>
/* ── Root ── */
.inv-root {
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: hidden;
  border: 1px solid var(--line, oklch(.35 .025 60 / .6));
  border-radius: 6px;
  background: linear-gradient(180deg, oklch(.28 .034 60 / .55), oklch(.22 .025 60 / .45));
}

/* ── Header ── */
.inv-head {
  padding: 20px 22px 14px;
  flex: 0 0 auto;
}

.inv-kicker {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  font-family: var(--font-caps, var(--font));
  font-size: 10px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: oklch(.65 .09 84 / .7);
  margin-bottom: 8px;
}

.inv-title {
  font-family: var(--font-display, var(--font));
  font-size: 24px;
  font-weight: 500;
  color: var(--gold, oklch(.72 .1 85));
  line-height: 1.1;
  margin: 0;
}

.inv-capacity {
  display: block;
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  letter-spacing: 0.04em;
  color: var(--ink-4, oklch(.45 .015 275));
  margin-top: 4px;
}

.inv-capacity--full {
  color: var(--blood, oklch(.52 .15 20));
}

.inv-divider {
  flex: 0 0 auto;
  height: 1px;
  background: linear-gradient(90deg, transparent, var(--line, oklch(.35 .025 60 / .6)), transparent);
  margin: 0 22px 6px;
}

/* ── Empty ── */
.inv-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  gap: 4px;
}

.inv-empty__text {
  font-family: var(--font-caps, var(--font));
  font-size: 11px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--ink-4, oklch(.45 .015 275));
  text-align: center;
  max-width: 260px;
}

/* ── List ── */
.inv-list {
  flex: 1;
  overflow-y: auto;
  padding: 6px 14px 14px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

/* ── Item card ── */
.inv-item {
  border: 1px solid var(--line, oklch(.35 .025 60 / .6));
  border-radius: 5px;
  background: linear-gradient(180deg, oklch(.30 .034 60 / .38), oklch(.24 .028 60 / .30));
  padding: 13px 14px;
  display: flex;
  flex-direction: column;
  gap: 7px;
}

.inv-item--sap   { border-left: 2px solid var(--sap, oklch(.70 .09 162 / .7)); }
.inv-item--frost { border-left: 2px solid var(--frost, oklch(.70 .07 232 / .7)); }
.inv-item--gold  { border-left: 2px solid var(--gold, oklch(.72 .1 85 / .7)); }

/* ── Item head ── */
.inv-item__head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 8px;
}

.inv-item__left {
  display: flex;
  align-items: baseline;
  gap: 7px;
  min-width: 0;
}

.inv-item__name {
  font-family: var(--font-display, var(--font));
  font-size: 16px;
  font-weight: 600;
  color: var(--ink, oklch(.88 .015 70));
  line-height: 1.2;
}

.inv-item__qty {
  font-family: var(--font-mono, monospace);
  font-size: 10px;
  color: var(--ink-4, oklch(.45 .015 275));
  flex: 0 0 auto;
}

/* ── Rarity chip ── */
.inv-chip {
  flex: 0 0 auto;
  font-family: var(--font-caps, var(--font));
  font-size: 9.5px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  padding: 2px 7px;
  border-radius: 3px;
  border: 1px solid var(--line, oklch(.35 .025 60 / .6));
  color: var(--ink-4, oklch(.45 .015 275));
  background: oklch(.26 .025 60 / .5);
}

.inv-chip--sap   { border-color: oklch(.70 .09 162 / .45); color: var(--sap, oklch(.70 .09 162)); background: oklch(.50 .07 162 / .12); }
.inv-chip--frost { border-color: oklch(.70 .07 232 / .45); color: var(--frost, oklch(.70 .07 232)); background: oklch(.50 .06 232 / .12); }
.inv-chip--gold  { border-color: oklch(.72 .1 85 / .45); color: var(--gold, oklch(.72 .1 85)); background: oklch(.55 .08 85 / .12); }

/* ── Type ── */
.inv-item__type {
  font-family: var(--font-caps, var(--font));
  font-size: 9px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--ink-4, oklch(.45 .015 275));
  margin: 0;
}

/* ── Description ── */
.inv-item__desc {
  font-family: var(--font, serif);
  font-size: 13px;
  line-height: 1.55;
  color: var(--ink-3, oklch(.65 .02 275));
  margin: 0;
}

/* ── Effect ── */
.inv-item__effect {
  display: flex;
}

.inv-effect-badge {
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  letter-spacing: 0.06em;
  padding: 3px 9px;
  border-radius: 3px;
  border: 1px solid var(--line, oklch(.35 .025 60 / .6));
  color: var(--ink-3);
  background: oklch(.24 .025 60 / .5);
}

.inv-effect-badge--sap   { border-color: oklch(.70 .09 162 / .4); color: var(--sap, oklch(.70 .09 162)); background: oklch(.50 .07 162 / .1); }
.inv-effect-badge--frost { border-color: oklch(.70 .07 232 / .4); color: var(--frost, oklch(.70 .07 232)); background: oklch(.50 .06 232 / .1); }
.inv-effect-badge--gold  { border-color: oklch(.72 .1 85 / .4); color: var(--gold, oklch(.72 .1 85)); background: oklch(.55 .08 85 / .1); }
.inv-effect-badge--blood { border-color: oklch(.52 .15 20 / .4); color: var(--blood, oklch(.52 .15 20)); background: oklch(.38 .1 20 / .1); }

/* ── Actions ── */
.inv-item__actions {
  margin-top: 2px;
}

/* ── Buttons ── */
.inv-btn {
  width: 100%;
  padding: 9px 16px;
  border-radius: 4px;
  font-family: var(--font-caps, var(--font));
  font-size: 10.5px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  cursor: pointer;
  transition: opacity 0.15s, box-shadow 0.15s;
}

.inv-btn:disabled { opacity: .38; cursor: not-allowed; }

.inv-btn--use {
  border: 1px solid var(--gold, oklch(.72 .1 85));
  background: oklch(.55 .08 85 / .15);
  color: var(--gold, oklch(.72 .1 85));
  box-shadow: 0 0 18px -6px oklch(.72 .1 85 / .4);
}

.inv-btn--use:not(:disabled):hover {
  background: oklch(.55 .08 85 / .25);
  box-shadow: 0 0 26px -6px oklch(.72 .1 85 / .55);
}

.inv-btn--cancel {
  border: 1px solid var(--line, oklch(.35 .025 60 / .6));
  background: oklch(.24 .025 60 / .5);
  color: var(--ink-3, oklch(.65 .02 275));
}

.inv-btn--cancel:not(:disabled):hover {
  border-color: var(--line-strong, oklch(.42 .03 60 / .7));
  color: var(--ink-2);
}

/* ── Picker ── */
.inv-picker {
  display: flex;
  flex-direction: column;
  gap: 7px;
}

.inv-picker__label {
  font-family: var(--font-caps, var(--font));
  font-size: 9.5px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4, oklch(.45 .015 275));
  margin: 0 0 2px;
}

/* ── Ally button ── */
.inv-ally-btn {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
  padding: 9px 14px;
  border: 1px solid var(--line-soft, oklch(.32 .022 60 / .5));
  border-radius: 4px;
  background: linear-gradient(180deg, oklch(.30 .034 60 / .4), oklch(.26 .03 60 / .35));
  color: var(--ink, oklch(.88 .015 70));
  font-family: inherit;
  cursor: pointer;
  text-align: left;
  transition: border-color 0.15s, background 0.15s, transform 0.12s;
}

.inv-ally-btn:not(:disabled):hover {
  border-color: var(--frost, oklch(.70 .07 232));
  background: linear-gradient(180deg, oklch(.34 .045 60 / .45), oklch(.28 .035 60 / .4));
  transform: translateX(2px);
}

.inv-ally-btn:disabled { opacity: .38; cursor: not-allowed; }

.inv-ally-btn__name {
  font-family: var(--font, serif);
  font-size: 13.5px;
  color: var(--ink-2, oklch(.78 .02 275));
}

.inv-ally-btn__hp {
  font-family: var(--font-mono, monospace);
  font-size: 10px;
  color: var(--ink-4, oklch(.45 .015 275));
}

/* ── Error ── */
.inv-error {
  font-family: var(--font-mono, monospace);
  font-size: 11.5px;
  color: var(--blood, oklch(.52 .15 20));
  margin: 2px 0 0;
}
</style>
