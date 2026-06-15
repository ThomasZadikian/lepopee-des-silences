<script setup lang="ts">
import { ref } from 'vue';
import { useCombatStore } from '../../combat/stores/useCombatStore';
import type { CombatantRuntimeDto } from '../../combat/types/combatContracts';
import { useRunStore } from '../../runs/stores/runStore';
import type { RunItemDto } from '../../runs/types/runTypes';
import { inventoryApi } from '../api/inventoryApi';

const props = defineProps<{
  items: RunItemDto[];
  runId: string;
}>();

const combatStore = useCombatStore();
const runStore = useRunStore();

// État local : item en cours d'utilisation + sélection d'allié
const pendingItemId = ref<string | null>(null);
const isLoading = ref(false);
const error = ref<string | null>(null);

function getRarityClass(rarity: string): string {
  switch (rarity) {
    case 'Uncommon': return 'item--uncommon';
    case 'Rare':     return 'item--rare';
    case 'Epic':     return 'item--epic';
    default:         return 'item--common';
  }
}

function getEffectLabel(effectType: string, effectAmount: number): string {
  switch (effectType) {
    case 'Heal':          return `+${effectAmount} PV`;
    case 'Guard':         return `+${effectAmount} Garde`;
    case 'ManaRestore':   return `+${effectAmount} Mana`;
    case 'ChargeRestore': return `+${effectAmount} Charge`;
    case 'NextCombatGuard':   return `+${effectAmount} Garde (prochain combat)`;
    case 'NarrativeFragment': return 'Fragment narratif';
    default: return '';
  }
}

// Les alliés disponibles viennent du store de combat (en combat)
// ou se réduisent au seul joueur hors combat.
const availableAllies = (): CombatantRuntimeDto[] => {
  const inCombat = Boolean(combatStore.allies.length);
  if (inCombat) return combatStore.allies.filter(a => a.status !== 'Defeated');
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

async function confirmUseItem(targetCombatantId?: string) {
  if (!pendingItemId.value) return;
  isLoading.value = true;
  error.value = null;

  try {
    const response = await inventoryApi.useItem(props.runId, pendingItemId.value);

    // Si en combat : mettre à jour le store de combat avec le résultat
    if (response.usedInCombat && response.combat) {
      if (response.logEntries?.length) {
        await combatStore.playCombatLogs(response.logEntries);
      }
      combatStore.finishCombatResponse(response.combat);
    }

    // Toujours rafraîchir les données de run (PV, inventaire, etc.)
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
  <section class="inventory-panel">
    <header>
      <p class="system-label">Inventaire de run</p>
      <h3>Sac à dos</h3>
    </header>

    <div v-if="items.length === 0" class="inventory-panel__empty">
      <p class="system-label">INVENTAIRE_VIDE</p>
      <p>Ton sac est vide. Les objets que tu trouveras apparaîtront ici.</p>
    </div>

    <div v-else class="inventory-panel__items">
      <div
        v-for="item in items"
        :key="item.id"
        class="inventory-item"
        :class="getRarityClass(item.rarity)"
      >
        <div class="inventory-item__header">
          <strong>{{ item.displayName }}</strong>
          <span v-if="item.quantity > 1" class="inventory-item__qty">
            ×{{ item.quantity }}
          </span>
        </div>

        <small class="inventory-item__type">{{ item.type }}</small>
        <p class="inventory-item__desc">{{ item.description }}</p>

        <span v-if="item.effectAmount > 0" class="inventory-item__effect">
          {{ getEffectLabel(item.effectType, item.effectAmount) }}
        </span>

        <!-- Bouton Utiliser (items consommables uniquement) -->
        <div v-if="item.isUsable" class="inventory-item__actions">
          <button
            v-if="pendingItemId !== item.id"
            class="btn btn--use"
            :disabled="isLoading || pendingItemId !== null"
            @click="startUseItem(item.id)"
          >
            Utiliser
          </button>

          <!-- Sélection d'allié -->
          <div v-else class="ally-picker">
            <p class="ally-picker__label">Qui bénéficie de cet objet ?</p>

            <!-- En combat : liste des alliés vivants -->
            <template v-if="availableAllies().length > 0">
              <button
                v-for="ally in availableAllies()"
                :key="ally.id"
                class="btn btn--ally"
                :disabled="isLoading"
                @click="confirmUseItem(ally.id)"
              >
                <span class="ally-name">{{ ally.displayName }}</span>
                <span class="ally-hp">{{ ally.currentVitality }} / {{ ally.maxVitality }} PV</span>
              </button>
            </template>

            <!-- Hors combat : confirmation simple sur le joueur -->
            <template v-else>
              <button
                class="btn btn--ally"
                :disabled="isLoading"
                @click="confirmUseItem()"
              >
                <span class="ally-name">Joueur</span>
              </button>
            </template>

            <button
              class="btn btn--cancel"
              :disabled="isLoading"
              @click="cancelUseItem"
            >
              Annuler
            </button>

            <p v-if="error" class="ally-picker__error">{{ error }}</p>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
/* —— Panel ————————————————————————————————————————— */
.inventory-panel {
  display: grid;
  gap: var(--space-4);
}

.inventory-panel h3 {
  color: var(--color-gold);
  font-size: 1.4rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.inventory-panel__empty p {
  color: var(--color-muted);
  font-size: 0.9rem;
}

.inventory-panel__items {
  display: grid;
  gap: var(--space-3);
}

/* —— Items ————————————————————————————————————————— */
.inventory-item {
  border: 1px solid var(--color-border);
  border-radius: 8px;
  padding: var(--space-3);
  background: var(--color-surface);
}

.inventory-item--common   { border-color: var(--color-border); }
.inventory-item--uncommon { border-color: var(--color-green, #4ade80); }
.inventory-item--rare     { border-color: var(--color-blue, #60a5fa); }
.inventory-item--epic     { border-color: var(--color-purple, #c084fc); }

.inventory-item__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--space-1);
}

.inventory-item__qty  { color: var(--color-muted); font-size: 0.85rem; }
.inventory-item__type { color: var(--color-muted); font-size: 0.75rem; text-transform: uppercase; }

.inventory-item__desc {
  color: var(--color-text-secondary);
  font-size: 0.85rem;
  margin: var(--space-1) 0;
}

.inventory-item__effect {
  color: var(--color-green, #4ade80);
  font-size: 0.8rem;
  font-weight: 600;
}

.inventory-item__actions {
  margin-top: var(--space-3);
}

/* —— Boutons ——————————————————————————————————————— */
.btn {
  border: none;
  border-radius: 6px;
  padding: var(--space-1) var(--space-3);
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.15s;
}

.btn:disabled { opacity: 0.45; cursor: not-allowed; }

.btn--use {
  background: var(--color-gold, #c9a84c);
  color: var(--color-bg, #0a0a0a);
  width: 100%;
}

.btn--use:hover:not(:disabled) { opacity: 0.85; }

.btn--ally {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
  background: var(--color-surface-raised, #1c1c1c);
  color: var(--color-text, #e8e0d0);
  border: 1px solid var(--color-border);
  margin-bottom: var(--space-2);
  text-align: left;
  padding: var(--space-2) var(--space-3);
}

.btn--ally:hover:not(:disabled) {
  border-color: var(--color-gold, #c9a84c);
  background: var(--color-surface-hover, #242424);
}

.ally-name { font-weight: 600; }
.ally-hp   { color: var(--color-muted); font-size: 0.75rem; }

.btn--cancel {
  background: transparent;
  color: var(--color-muted);
  border: 1px solid var(--color-border);
  width: 100%;
}

.btn--cancel:hover:not(:disabled) { color: var(--color-text); }

/* —— Ally picker ——————————————————————————————————— */
.ally-picker {
  display: grid;
  gap: var(--space-2);
}

.ally-picker__label {
  font-size: 0.8rem;
  color: var(--color-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: var(--space-1);
}

.ally-picker__error {
  color: var(--color-red, #f87171);
  font-size: 0.8rem;
}
</style>