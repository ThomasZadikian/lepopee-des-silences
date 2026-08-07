<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRunStore } from '../../features/runs/stores/runStore';
import { usePlayerStore } from '../../features/party/stores/playerStore';
import type { RunItemDto } from '../../features/runs/types/runTypes';
import { inventoryApi } from '../../features/inventory/api/inventoryApi';
import { itemsApi } from '../../features/party/api/itemsApi';
import { itemEffectTypeMeta, itemRarityMeta, itemTypeMeta } from '../theme/typeColors';
import BookReader from './BookReader.vue';

/**
 * Le contenu réel de la Besace, partagé par les trois endroits où elle apparaît : le tiroir en
 * cours d'exploration, la page dédiée, et l'onglet Besace de l'écran Équipe — un seul gabarit
 * (voir docs/design/brief-tiroirs-et-popovers.md, "la Besace existe sous trois formes").
 */
const props = defineProps<{
  items: RunItemDto[];
  runId: string;
  capacity?: number | null;
}>();

const runStore = useRunStore();
const playerStore = usePlayerStore();

const selectedItemId = ref<string | null>(null);
const isLoading = ref(false);
const error = ref<string | null>(null);
const readablePagesByKey = ref<Record<string, string[]>>({});
const costByKey = ref<Record<string, { palaceShardCost: number; himLitShardCost: number }>>({});
const isReaderOpen = ref(false);
const selectedCharacterId = ref('');
const openGroups = ref<Record<string, boolean>>({});

onMounted(async () => {
  try {
    const { items: catalogItems } = await itemsApi.listActive();
    const byKey: Record<string, string[]> = {};
    const costs: Record<string, { palaceShardCost: number; himLitShardCost: number }> = {};
    for (const item of catalogItems) {
      if (item.readablePages && item.readablePages.length > 0) {
        byKey[item.key] = item.readablePages;
      }
      costs[item.key] = {
        palaceShardCost: item.palaceShardCost ?? 0,
        himLitShardCost: item.himLitShardCost ?? 0,
      };
    }
    readablePagesByKey.value = byKey;
    costByKey.value = costs;
  } catch {
    // Best-effort: a failed catalog fetch just hides the "Lire"/"Valeur" affordances.
  }
});

type CategoryKey = 'accessoire' | 'arme' | 'soin' | 'autre';
const CATEGORY_LABELS: Record<CategoryKey, string> = {
  accessoire: 'Accessoires',
  arme: 'Armes',
  soin: 'Objets de soin',
  autre: 'Autres',
};

function categoryOf(item: RunItemDto): CategoryKey {
  // Bucketed from the server-resolved equip slot rather than re-matching category/type
  // strings here — a new equippable category naturally lands in "arme"/"accessoire"
  // without this file needing to learn its name.
  if (item.equipSlot === 'Weapon') return 'arme';
  if (item.equipSlot === 'Accessory' || item.equipSlot === 'Relic') return 'accessoire';
  if (item.type === 'Consumable') return 'soin';
  return 'autre';
}

function rarityLabel(rarity: string): string {
  return itemRarityMeta(rarity).label.toLowerCase();
}

function rarityColor(rarity: string): string {
  return itemRarityMeta(rarity).color;
}

function tacticalShapeLabel(shape?: RunItemDto['tacticalAreaShape']): string {
  switch (shape) {
    case 'Cross': return 'croix (rayon 1)';
    case 'Diamond': return 'losange (rayon 2)';
    case 'Map': return 'carte entière';
    default: return 'cible unique';
  }
}

function tacticalContract(item: RunItemDto): string | undefined {
  if (typeof item.tacticalRange !== 'number') return undefined;
  const los = item.requiresLineOfSight ? 'ligne de vue requise' : 'ignore la ligne de vue';
  return `Portée ${item.tacticalRange} · ${tacticalShapeLabel(item.tacticalAreaShape)} · ${los}`;
}

function effectLabel(item: RunItemDto): string | undefined {
  if (item.effectType === 'None') return undefined;
  const meta = itemEffectTypeMeta(item.effectType);
  if (item.effectType === 'NarrativeFragment') return meta.label;
  if (item.effectAmount <= 0) return undefined;
  const isPercent = item.effectType.toLowerCase().includes('percent');
  return `+${item.effectAmount}${isPercent ? '%' : ''} ${meta.label}`;
}

function valueLabel(definitionKey: string): string | undefined {
  const cost = costByKey.value[definitionKey];
  if (!cost) return undefined;
  const parts: string[] = [];
  if (cost.palaceShardCost > 0) parts.push(`${cost.palaceShardCost} éclats`);
  if (cost.himLitShardCost > 0) parts.push(`${cost.himLitShardCost} éclats de Him'Lit`);
  return parts.length ? parts.join(' · ') : undefined;
}

const groups = computed(() => {
  const byCategory = new Map<CategoryKey, RunItemDto[]>();
  for (const item of props.items) {
    const key = categoryOf(item);
    if (!byCategory.has(key)) byCategory.set(key, []);
    byCategory.get(key)!.push(item);
  }
  return (Object.keys(CATEGORY_LABELS) as CategoryKey[])
    .filter((key) => byCategory.has(key))
    .map((key) => ({
      key,
      label: CATEGORY_LABELS[key],
      items: byCategory.get(key)!,
      open: openGroups.value[key] ?? true,
    }));
});

function toggleGroup(key: CategoryKey) {
  openGroups.value = { ...openGroups.value, [key]: !(openGroups.value[key] ?? true) };
}

const selectedItem = computed(() => props.items.find((i) => i.id === selectedItemId.value) ?? null);
const selectedItemPages = computed(() =>
  selectedItem.value ? readablePagesByKey.value[selectedItem.value.definitionKey] : undefined);
const selectedReader = computed(() =>
  runStore.currentRun?.party?.members.find((m) => m.id === selectedCharacterId.value));
const selectedReaderGrimoireSkill = computed(() =>
  selectedReader.value?.skills.find((s) => s.temporarySlot === 'Grimoire'));

const distinctItemCount = computed(() => props.items.length);
const isBagFull = computed(() =>
  typeof props.capacity === 'number' && distinctItemCount.value >= props.capacity);

function selectItem(item: RunItemDto) {
  selectedItemId.value = item.id;
  selectedCharacterId.value ||= runStore.currentRun?.party?.members[0]?.id ?? '';
  error.value = null;
}

async function useItem() {
  if (!selectedItem.value) return;
  isLoading.value = true;
  error.value = null;
  try {
    await inventoryApi.useItem(props.runId, selectedItem.value.id);
    await runStore.loadRun(props.runId);
    selectedItemId.value = null;
  } catch (err) {
    error.value = err instanceof Error ? err.message : "L'utilisation a échoué.";
  } finally {
    isLoading.value = false;
  }
}

async function readGrimoire() {
  if (!selectedItem.value || !selectedCharacterId.value) return;
  isLoading.value = true;
  error.value = null;
  try {
    await inventoryApi.readGrimoire(props.runId, selectedItem.value.id, selectedCharacterId.value);
    await runStore.loadRun(props.runId);
    selectedItemId.value = null;
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'La lecture a échoué.';
  } finally {
    isLoading.value = false;
  }
}

// Weapon/Equipment/Relic run items map to a permanent equipment slot — resolved
// server-side (CatalogRunItemMapper.MapEquipSlot) and carried on RunItemDto.equipSlot,
// never re-derived here from type/category strings.
const equipSlotLabels = { Weapon: 'Arme', Accessory: 'Accessoire', Relic: 'Relique' } as const;
type EquipSlot = keyof typeof equipSlotLabels;
const slotLimits: Record<EquipSlot, number> = { Weapon: 1, Accessory: 1, Relic: 3 };

const selectedItemEquipSlot = computed(() =>
  (selectedItem.value?.equipSlot as EquipSlot | undefined) ?? null);

// Equipping requires the item to already be in the player's permanent backpack — a
// freshly-found run item normally only gets there through the end-of-run keepsake ceremony,
// but toggleEquip() grants it right away the moment the player actually tries to equip it.
const selectedItemIsPermanentlyOwned = computed(() =>
  Boolean(
    selectedItem.value &&
      playerStore.permanentItems.some((owned) => owned.itemDefinitionKey === selectedItem.value!.definitionKey),
  ));

const equipTargetCharacter = computed(() =>
  playerStore.profile?.characters.find((c) => c.id === selectedCharacterId.value));

const isEquippedOnTarget = computed(() =>
  Boolean(
    selectedItem.value &&
      equipTargetCharacter.value?.items.some((i) => i.itemKey === selectedItem.value!.definitionKey && i.isEquipped),
  ));

const isTargetSlotFull = computed(() => {
  const slot = selectedItemEquipSlot.value;
  const character = equipTargetCharacter.value;
  if (!slot || !character) return true;
  return character.items.filter((i) => i.isEquipped && (i.slot ?? 'Relic') === slot).length >= slotLimits[slot];
});

async function toggleEquip() {
  if (!selectedItem.value || !selectedCharacterId.value || playerStore.isLoading) return;
  const itemKey = selectedItem.value.definitionKey;
  isLoading.value = true;
  try {
    if (isEquippedOnTarget.value) {
      await playerStore.unequipItem(selectedCharacterId.value, itemKey);
    } else {
      if (isTargetSlotFull.value) return;
      if (!selectedItemIsPermanentlyOwned.value) {
        await runStore.grantPermanentItem(itemKey);
        await playerStore.loadProfile();
      }
      await playerStore.equipItem(selectedCharacterId.value, itemKey);
    }
    if (runStore.currentRun && !runStore.shouldShowCombatScene) {
      await runStore.syncPartyStats();
    }
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <div class="bp-root">
    <div class="bp-head">
      <span class="bp-kicker">Besace</span>
      <span class="bp-capacity" :class="{ 'bp-capacity--full': isBagFull }">
        {{ distinctItemCount }}<template v-if="typeof capacity === 'number'"> / {{ capacity }}</template>
      </span>
    </div>

    <p v-if="items.length === 0" class="bp-empty">Ton sac est vide.</p>

    <div v-else class="bp-groups">
      <div v-for="g in groups" :key="g.key">
        <button type="button" class="bp-group-head" @click="toggleGroup(g.key)">
          <span class="bp-group-head__label">
            {{ g.label }} <span class="bp-group-head__count">({{ g.items.length }})</span>
          </span>
          <span class="bp-group-head__chevron">{{ g.open ? '▾' : '▸' }}</span>
        </button>
        <div v-if="g.open" class="bp-grid">
          <button
            v-for="item in g.items"
            :key="item.id"
            type="button"
            class="bp-cell"
            :style="{
              borderLeftColor: itemTypeMeta(item.type).color,
              background: item.id === selectedItemId
                ? 'var(--panel-2)'
                : `color-mix(in oklch, ${itemTypeMeta(item.type).color}, var(--panel) 92%)`,
            }"
            @click="selectItem(item)"
          >
            <div class="bp-cell__top">
              <span class="bp-cell__name">{{ item.displayName }}</span>
              <span v-if="item.quantity > 1" class="bp-cell__qty">×{{ item.quantity }}</span>
            </div>
            <span class="bp-cell__rarity" :style="{ color: rarityColor(item.rarity) }">{{ rarityLabel(item.rarity) }}</span>
          </button>
        </div>
      </div>
    </div>

    <div v-if="selectedItem" class="bp-sheet">
      <div class="bp-sheet__head">
        <span class="bp-sheet__name">{{ selectedItem.displayName }}</span>
        <span class="bp-sheet__rarity" :style="{ color: rarityColor(selectedItem.rarity) }">{{ rarityLabel(selectedItem.rarity) }}</span>
        <span v-if="selectedItem.quantity > 1" class="bp-sheet__qty">×{{ selectedItem.quantity }}</span>
      </div>
      <span class="bp-sheet__type" :style="{ color: itemTypeMeta(selectedItem.type).color }">
        {{ itemTypeMeta(selectedItem.type).glyph }} {{ itemTypeMeta(selectedItem.type).label }}
      </span>
      <p class="bp-sheet__desc">{{ selectedItem.description }}</p>

      <div v-if="effectLabel(selectedItem)" class="bp-sheet__effect">{{ effectLabel(selectedItem) }}</div>
      <div v-if="tacticalContract(selectedItem)" class="bp-sheet__contract">{{ tacticalContract(selectedItem) }}</div>
      <div v-if="valueLabel(selectedItem.definitionKey)" class="bp-sheet__value">Valeur : {{ valueLabel(selectedItem.definitionKey) }}</div>

      <div class="bp-sheet__actions">
        <template v-if="selectedItem.type === 'Grimoire'">
          <select v-model="selectedCharacterId" class="bp-sheet__select">
            <option
              v-for="member in runStore.currentRun?.party?.members ?? []"
              :key="member.id"
              :value="member.id"
            >
              {{ member.displayName }}
            </option>
          </select>
          <p v-if="selectedReaderGrimoireSkill" class="bp-sheet__warning">
            ⚠ Remplace {{ selectedReaderGrimoireSkill.displayName }} dans l'emplacement temporaire III de {{ selectedReader?.displayName }}.
          </p>
          <button type="button" class="bp-btn" :disabled="isLoading || !selectedCharacterId" @click="readGrimoire">
            {{ isLoading ? 'Lecture…' : 'Apprendre' }}
          </button>
        </template>
        <template v-else-if="selectedItemEquipSlot">
          <p v-if="!selectedItemIsPermanentlyOwned" class="bp-sheet__warning">
            L'équiper le fera rejoindre ton sac permanent — il te suivra dans tes prochaines traversées aussi.
          </p>
          <select v-model="selectedCharacterId" class="bp-sheet__select">
            <option
              v-for="member in runStore.currentRun?.party?.members ?? []"
              :key="member.id"
              :value="member.id"
            >
              {{ member.displayName }}
            </option>
          </select>
          <button
            type="button"
            class="bp-btn"
            :disabled="playerStore.isLoading || !selectedCharacterId || (!isEquippedOnTarget && isTargetSlotFull)"
            @click="toggleEquip"
          >
            {{ isEquippedOnTarget ? 'Déséquiper' : `Équiper (${equipSlotLabels[selectedItemEquipSlot]})` }}
          </button>
          <span v-if="!isEquippedOnTarget && isTargetSlotFull" class="bp-sheet__inert">
            Emplacement {{ equipSlotLabels[selectedItemEquipSlot] }} déjà occupé sur ce personnage.
          </span>
        </template>
        <button v-else-if="selectedItemPages" type="button" class="bp-btn" @click="isReaderOpen = true">Lire</button>
        <button v-else-if="selectedItem.isUsable" type="button" class="bp-btn" :disabled="isLoading" @click="useItem">
          {{ isLoading ? 'Utilisation…' : 'Utiliser' }}
        </button>
        <span v-else class="bp-sheet__inert">Cet objet ne peut pas être utilisé actuellement.</span>
      </div>

      <p v-if="error" class="bp-sheet__error">{{ error }}</p>
    </div>

    <BookReader
      v-if="selectedItem && selectedItemPages"
      v-model="isReaderOpen"
      :title="selectedItem.displayName"
      :pages="selectedItemPages"
    />
  </div>
</template>

<style scoped>
.bp-root { width: 100%; font-family: var(--font); color: var(--ink); }

.bp-head {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  margin-bottom: 16px;
}

.bp-kicker {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: .14em;
  text-transform: uppercase;
  color: var(--ink-3);
}

.bp-capacity {
  font-family: var(--font-mono);
  font-size: 12px;
  color: var(--ink-4);
}

.bp-capacity--full { color: var(--danger); }

.bp-empty {
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: .1em;
  text-transform: uppercase;
  color: var(--ink-4);
  font-style: italic;
}

.bp-groups { display: flex; flex-direction: column; gap: 1px; }

.bp-group-head {
  all: unset;
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 4px;
  cursor: pointer;
  border-bottom: 1px solid var(--line);
  box-sizing: border-box;
}

.bp-group-head__label {
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: .1em;
  text-transform: uppercase;
  color: var(--ink-2);
}

.bp-group-head__count { color: var(--ink-4); }
.bp-group-head__chevron { color: var(--ink-3); font-size: 11px; }

.bp-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1px;
  background: var(--line-soft);
  margin: 1px 0 12px;
}

.bp-cell {
  all: unset;
  box-sizing: border-box;
  cursor: pointer;
  padding: 12px 14px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  border-left: 3px solid transparent;
  transition: background .3s;
}

.bp-cell:hover { background: var(--panel-2) !important; }

.bp-cell__top {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: 6px;
}

.bp-cell__name {
  font-family: var(--font-display);
  font-style: italic;
  font-size: 13px;
  color: var(--ink);
}

.bp-cell__qty { font-family: var(--font-mono); font-size: 10px; color: var(--ink-3); }

.bp-cell__rarity {
  font-family: var(--font-mono);
  font-size: 9px;
  letter-spacing: .08em;
  text-transform: uppercase;
}

.bp-sheet {
  margin-top: 16px;
  padding: 18px 18px;
  background: var(--panel);
  border: 1px solid var(--line);
}

.bp-sheet__head {
  display: flex;
  align-items: baseline;
  gap: 10px;
  flex-wrap: wrap;
}

.bp-sheet__name { font-family: var(--font-display); font-style: italic; font-size: 16px; color: var(--ink); }
.bp-sheet__rarity { font-family: var(--font-mono); font-size: 9px; letter-spacing: .08em; text-transform: uppercase; }
.bp-sheet__qty { font-family: var(--font-mono); font-size: 10px; color: var(--ink-3); }

.bp-sheet__type {
  display: block;
  margin-top: 6px;
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: .1em;
  text-transform: uppercase;
  color: var(--ink-3);
}

.bp-sheet__desc {
  margin: 10px 0 0;
  font-size: 12.5px;
  line-height: 1.6;
  color: var(--ink-2);
}

.bp-sheet__effect {
  margin-top: 10px;
  font-family: var(--font-mono);
  font-size: 12px;
  color: var(--mint-dim);
}

.bp-sheet__contract,
.bp-sheet__value {
  margin-top: 8px;
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--ink-3);
}

.bp-sheet__actions {
  margin-top: 14px;
  padding-top: 14px;
  border-top: 1px solid var(--line-soft);
  display: flex;
  flex-direction: column;
  gap: 8px;
  align-items: flex-start;
}

.bp-sheet__select {
  width: 100%;
  font-family: var(--font);
  font-size: 12px;
  padding: 8px 10px;
  background: var(--panel-2);
  color: var(--ink-2);
  border: 1px solid var(--line);
}

.bp-sheet__warning { margin: 0; font-size: 11px; color: var(--danger-dim); }

.bp-btn {
  align-self: flex-start;
  padding: 8px 18px;
  background: transparent;
  border: 1px solid var(--mint-dim);
  color: var(--mint-dim);
  font-family: var(--font);
  font-size: 11px;
  letter-spacing: .08em;
  text-transform: uppercase;
  cursor: pointer;
  transition: color .3s, border-color .3s;
}

.bp-btn:hover:not(:disabled) { color: var(--mint); border-color: var(--mint); }
.bp-btn:disabled { opacity: .4; cursor: not-allowed; }

.bp-sheet__inert { font-size: 11px; color: var(--ink-4); font-style: italic; }

.bp-sheet__error {
  margin: 10px 0 0;
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--danger);
}
</style>
