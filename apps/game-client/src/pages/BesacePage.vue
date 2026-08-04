<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

import PalaceAtmosphere from '../shared/components/PalaceAtmosphere.vue';
import RuleOrnament from '../shared/components/RuleOrnament.vue';
import SealGlyph from '../shared/components/SealGlyph.vue';
import BookReader from '../shared/components/BookReader.vue';
import { useRunStore } from '../features/runs/stores/runStore';
import { usePlayerStore } from '../features/party/stores/playerStore';
import type { RunItemDto } from '../features/runs/types/runTypes';
import { inventoryApi } from '../features/inventory/api/inventoryApi';
import { itemsApi } from '../features/party/api/itemsApi';

const props = defineProps<{ embedded?: boolean }>();

const router = useRouter();
const runStore = useRunStore();
const playerStore = usePlayerStore();

const palaceShardCount = computed(() => playerStore.profile?.progression.palaceShardCount ?? 0);
const himLitShardCount = computed(() => playerStore.profile?.progression.himLitShardCount ?? 0);

const items = computed<RunItemDto[]>(() => runStore.currentRun?.inventoryItems ?? []);
const capacity = computed(() => runStore.currentRun?.runItemCapacity ?? null);
const distinctItemCount = computed(() => items.value.length);
const isBagFull = computed(() =>
  typeof capacity.value === 'number' && distinctItemCount.value >= capacity.value,
);

const selectedItem = ref<RunItemDto | null>(null);
const isLoading = ref(false);
const error = ref<string | null>(null);
const readablePagesByKey = ref<Record<string, string[]>>({});
const costByKey = ref<Record<string, { palaceShardCost: number; himLitShardCost: number }>>({});
const isReaderOpen = ref(false);
const selectedCharacterId = ref('');

onMounted(async () => {
  // Toujours rafraîchi à l'ouverture : un profil déjà en cache peut dater d'avant qu'un
  // objet trouvé plus tôt cette run n'ait rejoint le sac permanent (voir grantPermanentItem).
  void playerStore.loadProfile();
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
    // Best-effort: a failed catalog fetch just hides the "Lire" affordance.
  }
});

const selectedItemPages = computed(() =>
  selectedItem.value ? readablePagesByKey.value[selectedItem.value.definitionKey] : undefined,
);
const selectedItemCost = computed(() =>
  selectedItem.value ? costByKey.value[selectedItem.value.definitionKey] : undefined,
);
const selectedReader = computed(() =>
  runStore.currentRun?.party?.members.find((member) => member.id === selectedCharacterId.value),
);
const selectedReaderGrimoireSkill = computed(() =>
  selectedReader.value?.skills.find((skill) => skill.temporarySlot === 'Grimoire'),
);

// Weapon/Equipment/Relic run items map to a permanent equipment slot (see
// EnemyLootRewardBuilder.MapToRunItemType on the server for the reverse mapping).
// Anything else (consumables, grimoires, fragments...) can't be equipped at all.
const equipSlotLabels = { Weapon: 'Arme', Accessory: 'Accessoire', Relic: 'Relique' } as const;
type EquipSlot = keyof typeof equipSlotLabels;
const slotLimits: Record<EquipSlot, number> = { Weapon: 1, Accessory: 1, Relic: 3 };

function equipSlotFor(item: RunItemDto): EquipSlot | null {
  if (item.type === 'Weapon') return 'Weapon';
  if (item.type === 'Equipment') return 'Accessory';
  if (item.type === 'Relic') return 'Relic';
  return null;
}

const selectedItemEquipSlot = computed(() =>
  selectedItem.value ? equipSlotFor(selectedItem.value) : null,
);

// Equipping requires the item to already be in the player's permanent backpack
// (see PlayerProfile.EquipItem, server-side) — a freshly-found run item normally only
// gets there through the end-of-run keepsake ceremony, but toggleEquip() grants it right
// away the moment the player actually tries to equip it (see grantPermanentItem), so this
// no longer gates the equip button — only informs the player what equipping will also do.
const selectedItemIsPermanentlyOwned = computed(() =>
  Boolean(
    selectedItem.value &&
      playerStore.permanentItems.some(
        (owned) => owned.itemDefinitionKey === selectedItem.value!.definitionKey,
      ),
  ),
);

const equipTargetCharacter = computed(() =>
  playerStore.profile?.characters.find((c) => c.id === selectedCharacterId.value),
);

const isEquippedOnTarget = computed(() =>
  Boolean(
    selectedItem.value &&
      equipTargetCharacter.value?.items.some(
        (i) => i.itemKey === selectedItem.value!.definitionKey && i.isEquipped,
      ),
  ),
);

const isTargetSlotFull = computed(() => {
  const slot = selectedItemEquipSlot.value;
  const character = equipTargetCharacter.value;
  if (!slot || !character) return true;
  return character.items.filter((i) => i.isEquipped && (i.slot ?? 'Relic') === slot).length >= slotLimits[slot];
});

// Drives the equip sceau's transient "en cours" ring — separate from playerStore.isLoading,
// which also flips for unrelated fetches elsewhere on the page and would make the stamp
// animation replay at the wrong moments.
const equipBusy = ref(false);

async function toggleEquip() {
  if (!selectedItem.value || !selectedCharacterId.value || playerStore.isLoading) return;
  const itemKey = selectedItem.value.definitionKey;
  equipBusy.value = true;
  try {
    if (isEquippedOnTarget.value) {
      await playerStore.unequipItem(selectedCharacterId.value, itemKey);
    } else {
      if (isTargetSlotFull.value) return;

      // Equipping has always required permanent ownership on the server (see
      // PlayerProfile.EquipItem) — a run-found item only got there through the end-of-run
      // keepsake ceremony. Granting it here, right before equipping, is what makes gear found
      // THIS run usable THIS run: same endpoint that ceremony uses, just fired for one item
      // the moment it's actually needed instead of waiting for the run to end. No-op if the
      // item is already permanently owned.
      if (!selectedItemIsPermanentlyOwned.value) {
        await runStore.grantPermanentItem(itemKey);
        await playerStore.loadProfile();
      }

      await playerStore.equipItem(selectedCharacterId.value, itemKey);
    }

    // Mid-run resync: makes the new loadout's base stats apply to the run's next
    // combat instead of only the player's next run. Silently skipped during an
    // active combat (backend rejects it there — equip still updates the
    // permanent profile, it just takes effect once the fight ends).
    if (runStore.currentRun && !runStore.shouldShowCombatScene) {
      await runStore.syncPartyStats();
    }
  } finally {
    equipBusy.value = false;
  }
}

function sealToneFor(rarity: string): 'sap' | 'frost' | 'gold' | 'ink' {
  const tone = getRarityTone(rarity);
  return tone === '' ? 'ink' : (tone as 'sap' | 'frost' | 'gold');
}

const equipSealState = computed<'idle' | 'confirming' | 'confirmed'>(() => {
  if (equipBusy.value) return 'confirming';
  return isEquippedOnTarget.value ? 'confirmed' : 'idle';
});

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
    case 'HealAndManaRestorePercent': return `+${effectAmount}% Vitalité et Mana`;
    default:                  return '';
  }
}

function getEffectTone(effectType: string): string {
  switch (effectType) {
    case 'Heal':
    case 'ManaRestore':
    case 'ChargeRestore':
    case 'HealAndManaRestorePercent':
    case 'NextCombatGuard':   return 'sap';
    case 'Guard':             return 'frost';
    case 'NarrativeFragment': return 'gold';
    case 'Damage':            return 'blood';
    default:                  return '';
  }
}

function tacticalShapeLabel(shape?: RunItemDto['tacticalAreaShape']): string {
  switch (shape) {
    case 'Cross': return 'croix (rayon 1)';
    case 'Diamond': return 'losange (rayon 2)';
    case 'Map': return 'carte entière';
    default: return 'case';
  }
}

// RevivePercent items require a defeated ally to target — meaningless outside
// a tactical combat's targeting action (see Run.ApplyItemEffectToPlayerState),
// so the button is hidden here even when the backend still reports isUsable.
const canUseSelectedItem = computed(() =>
  Boolean(selectedItem.value?.isUsable && selectedItem.value.effectType !== 'RevivePercent'));

function selectItem(item: RunItemDto) {
  selectedItem.value = selectedItem.value?.id === item.id ? null : item;
  selectedCharacterId.value ||= runStore.currentRun?.party?.members[0]?.id ?? '';
  error.value = null;
}

async function useItem() {
  if (!selectedItem.value || !runStore.currentRun) return;
  isLoading.value = true;
  error.value = null;
  try {
    await inventoryApi.useItem(runStore.currentRun.id, selectedItem.value.id);
    await runStore.loadRun(runStore.currentRun.id);
    selectedItem.value = null;
  } catch (err) {
    error.value = err instanceof Error ? err.message : "L'utilisation a échoué.";
  } finally {
    isLoading.value = false;
  }
}

async function readGrimoire() {
  if (!selectedItem.value || !selectedCharacterId.value || !runStore.currentRun) return;
  isLoading.value = true;
  error.value = null;
  try {
    await inventoryApi.readGrimoire(
      runStore.currentRun.id,
      selectedItem.value.id,
      selectedCharacterId.value,
    );
    await runStore.loadRun(runStore.currentRun.id);
    selectedItem.value = null;
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'La lecture a échoué.';
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <main class="besace-page" :class="{ 'besace-page--embedded': props.embedded }" data-mood="palais">
    <PalaceAtmosphere v-if="!props.embedded" />

    <div class="besace-page__content">
      <button v-if="!props.embedded" class="besace-page__back" @click="router.back()">← Retour</button>

      <div class="besace-page__head">
        <div>
          <span class="es-kicker">Système · objets de run</span>
          <h1 class="es-h1" style="font-size: clamp(30px, 4.4vw, 52px); margin-top: 12px">La Besace</h1>
        </div>
        <span v-if="typeof capacity === 'number'" class="besace-page__capacity" :class="{ 'besace-page__capacity--full': isBagFull }">
          {{ distinctItemCount }} / {{ capacity }}
        </span>
      </div>
      <RuleOrnament style="width: 150px; margin: 16px 0" />

      <section class="besace-currency" aria-label="Monnaie">
        <div class="besace-currency__item">
          <span class="besace-currency__label">Éclats du Palais</span>
          <span class="besace-currency__value besace-currency__value--gold">{{ palaceShardCount }}</span>
        </div>
        <div class="besace-currency__item">
          <span class="besace-currency__label">Éclats de Him'Lit</span>
          <span class="besace-currency__value besace-currency__value--frost">{{ himLitShardCount }}</span>
        </div>
      </section>

      <p v-if="!runStore.currentRun" class="besace-page__status">Aucune run active.</p>
      <p v-else-if="items.length === 0" class="besace-page__status">Ton sac est vide.</p>

      <template v-else>
        <div class="besace-page__grid">
          <button
            v-for="item in items"
            :key="item.id"
            class="besace-cell"
            :class="[
              selectedItem?.id === item.id ? 'besace-cell--sel' : '',
              getRarityTone(item.rarity) ? `besace-cell--${getRarityTone(item.rarity)}` : '',
            ]"
            @click="selectItem(item)"
          >
            <span class="besace-cell__name">{{ item.displayName }}</span>
            <div class="besace-cell__foot">
              <span v-if="item.quantity > 1" class="besace-cell__qty">×{{ item.quantity }}</span>
              <span
                class="besace-cell__rarity"
                :class="getRarityTone(item.rarity) ? `besace-cell__rarity--${getRarityTone(item.rarity)}` : ''"
              >
                {{ getRarityLabel(item.rarity) }}
              </span>
            </div>
          </button>
        </div>

        <Transition name="besace-fade">
          <div v-if="selectedItem" class="besace-sheet">
            <div class="besace-sheet__head">
              <h4 class="besace-sheet__name">{{ selectedItem.displayName }}</h4>
              <div class="besace-sheet__badges">
                <span v-if="selectedItem.quantity > 1" class="besace-badge">×{{ selectedItem.quantity }}</span>
                <span
                  v-if="getRarityTone(selectedItem.rarity)"
                  class="besace-badge"
                  :class="`besace-badge--${getRarityTone(selectedItem.rarity)}`"
                >
                  {{ getRarityLabel(selectedItem.rarity) }}
                </span>
              </div>
            </div>

            <p class="besace-sheet__type">{{ selectedItem.type }}</p>
            <p class="besace-sheet__desc">{{ selectedItem.description }}</p>

            <div v-if="selectedItem.effectAmount > 0" class="besace-sheet__effect">
              <span class="besace-effect-label">Effet</span>
              <span
                class="besace-effect-value"
                :class="getEffectTone(selectedItem.effectType) ? `besace-effect-value--${getEffectTone(selectedItem.effectType)}` : ''"
              >
                {{ getEffectLabel(selectedItem.effectType, selectedItem.effectAmount) }}
              </span>
            </div>

            <div v-if="typeof selectedItem.tacticalRange === 'number'" class="besace-sheet__contract">
              <span>Portée {{ selectedItem.tacticalRange }}</span>
              <span>{{ tacticalShapeLabel(selectedItem.tacticalAreaShape) }}</span>
              <span>{{ selectedItem.requiresLineOfSight ? 'Ligne de vue requise' : 'Ignore la ligne de vue' }}</span>
            </div>

            <div
              v-if="selectedItemCost && (selectedItemCost.palaceShardCost > 0 || selectedItemCost.himLitShardCost > 0)"
              class="besace-sheet__cost"
            >
              <span class="besace-effect-label">Valeur</span>
              <span v-if="selectedItemCost.palaceShardCost > 0" class="besace-cost-value">
                {{ selectedItemCost.palaceShardCost }} Éclats du Palais
              </span>
              <span v-if="selectedItemCost.himLitShardCost > 0" class="besace-cost-value">
                {{ selectedItemCost.himLitShardCost }} Éclats de Him'Lit
              </span>
            </div>

            <div class="besace-sheet__actions">
              <div v-if="selectedItem.type === 'Grimoire'" class="besace-sheet__seal">
                <SealGlyph
                  kind="grimoire"
                  :tone="sealToneFor(selectedItem.rarity)"
                  :size="56"
                  :sigil-size="24"
                  :state="isLoading ? 'confirming' : 'idle'"
                />
                <p class="besace-sheet__warning" style="margin: 0">
                  La lecture consume le grimoire et inscrit le sort au grimoire du lecteur.
                </p>
              </div>
              <label v-if="selectedItem.type === 'Grimoire'" class="besace-sheet__target">
                Lecteur
                <select v-model="selectedCharacterId">
                  <option
                    v-for="member in runStore.currentRun?.party?.members ?? []"
                    :key="member.id"
                    :value="member.id"
                  >
                    {{ member.displayName }}
                  </option>
                </select>
              </label>
              <p
                v-if="selectedItem.type === 'Grimoire' && selectedReaderGrimoireSkill"
                class="besace-sheet__warning"
              >
                Remplacera {{ selectedReaderGrimoireSkill.displayName }} dans l'emplacement temporaire III.
              </p>
              <button
                v-if="selectedItem.type === 'Grimoire'"
                class="besace-action-btn besace-action-btn--read"
                :disabled="isLoading || !selectedCharacterId"
                @click="readGrimoire"
              >
                {{ isLoading ? 'Lecture…' : 'Apprendre' }}
              </button>

              <template v-if="selectedItemEquipSlot">
                <div class="besace-sheet__seal">
                  <SealGlyph
                    kind="equipement"
                    :tone="sealToneFor(selectedItem.rarity)"
                    :size="56"
                    :sigil-size="24"
                    :state="equipSealState"
                  />
                  <p v-if="!selectedItemIsPermanentlyOwned" class="besace-sheet__warning" style="margin: 0">
                    L'équiper le fera rejoindre ton sac permanent — il te suivra dans tes
                    prochaines traversées aussi.
                  </p>
                </div>
                <label class="besace-sheet__target">
                  Porteur
                  <select v-model="selectedCharacterId">
                    <option
                      v-for="member in runStore.currentRun?.party?.members ?? []"
                      :key="member.id"
                      :value="member.id"
                    >
                      {{ member.displayName }}
                    </option>
                  </select>
                </label>
                <button
                  class="besace-action-btn besace-action-btn--read"
                  :class="{ 'besace-action-btn--use': isEquippedOnTarget }"
                  :disabled="playerStore.isLoading || !selectedCharacterId || (!isEquippedOnTarget && isTargetSlotFull)"
                  @click="toggleEquip"
                >
                  {{ isEquippedOnTarget ? 'Déséquiper' : `Équiper (${equipSlotLabels[selectedItemEquipSlot]})` }}
                </button>
                <p v-if="!isEquippedOnTarget && isTargetSlotFull" class="besace-sheet__unusable">
                  Emplacement {{ equipSlotLabels[selectedItemEquipSlot] }} déjà occupé sur ce personnage.
                </p>
              </template>
              <button
                v-if="selectedItemPages"
                class="besace-action-btn besace-action-btn--read"
                @click="isReaderOpen = true"
              >
                Lire
              </button>
              <button
                v-if="canUseSelectedItem"
                class="besace-action-btn besace-action-btn--use"
                :disabled="isLoading"
                @click="useItem"
              >
                {{ isLoading ? 'Utilisation…' : 'Utiliser' }}
              </button>
              <p
                v-if="!canUseSelectedItem && !selectedItemPages && !selectedItemEquipSlot && selectedItem.type !== 'Grimoire'"
                class="besace-sheet__unusable"
              >
                {{ selectedItem.effectType === 'RevivePercent'
                  ? "Cet objet ne peut être utilisé qu'en combat, sur un allié à terre."
                  : "Cet objet ne peut pas être utilisé actuellement." }}
              </p>
            </div>

            <p v-if="error" class="besace-sheet__error">{{ error }}</p>
          </div>
        </Transition>

        <BookReader
          v-if="selectedItem && selectedItemPages"
          v-model="isReaderOpen"
          :title="selectedItem.displayName"
          :pages="selectedItemPages"
        />
      </template>
    </div>
  </main>
</template>

<style scoped>
.besace-page {
  position: relative;
  height: 100dvh;
  overflow-y: auto;
  overflow-x: hidden;
  background:
    radial-gradient(70% 52% at 20% 12%, var(--wash-frost), transparent 60%),
    radial-gradient(64% 56% at 86% 80%, var(--wash-blood), transparent 58%),
    radial-gradient(58% 50% at 60% 26%, var(--wash-sap), transparent 60%),
    radial-gradient(56% 50% at 12% 92%, var(--wash-gold), transparent 60%),
    radial-gradient(150% 130% at 50% -10%, var(--bg) 0%, var(--bg-2) 48%, var(--void) 100%);
  color: var(--ink);
  font-family: var(--font);
}

.besace-page--embedded {
  height: auto;
  min-height: 100%;
  overflow: visible;
}

.besace-page__content {
  position: relative;
  z-index: 5;
  max-width: 1100px;
  margin: 0 auto;
  padding: 64px 4vw 90px;
}

.besace-page--embedded .besace-page__content {
  padding: 40px 4vw 48px;
}

.besace-page__back {
  all: unset;
  cursor: pointer;
  display: block;
  margin-bottom: 24px;
  font-family: var(--font-caps);
  font-size: 11px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
  transition: color 0.2s;
}
.besace-page__back:hover { color: var(--gold); }

.besace-page__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.besace-page__capacity {
  flex: 0 0 auto;
  font-family: var(--font-mono, monospace);
  font-size: 13px;
  letter-spacing: 0.04em;
  color: var(--ink-4);
  padding-top: 6px;
}

.besace-page__capacity--full { color: var(--blood); }

.besace-currency {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-bottom: 8px;
}

.besace-currency__item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 14px;
  border: 1px solid var(--line-soft, oklch(.32 .022 60 / .5));
  border-radius: 6px;
  background: linear-gradient(180deg, oklch(.30 .034 60 / .38), oklch(.25 .028 60 / .30));
}

.besace-currency__label {
  font-family: var(--font-caps, var(--font));
  font-size: 9px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4, oklch(.45 .015 275));
}

.besace-currency__value {
  font-family: var(--font-mono, monospace);
  font-size: 15px;
  font-variant-numeric: tabular-nums;
  color: var(--ink-2, oklch(.78 .02 275));
}

.besace-currency__value--gold  { color: var(--gold, oklch(.72 .1 85)); }
.besace-currency__value--frost { color: var(--frost, oklch(.70 .07 232)); }

.besace-page__status {
  margin-top: 40px;
  font-family: var(--font-caps);
  font-size: 12px;
  letter-spacing: 0.08em;
  color: var(--ink-4);
}

.besace-page__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 10px;
  margin-top: 12px;
}

.besace-cell {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 14px 15px;
  border: 1px solid var(--line-soft, oklch(.32 .022 60 / .5));
  border-radius: 6px;
  background: linear-gradient(180deg, oklch(.30 .034 60 / .38), oklch(.25 .028 60 / .30));
  cursor: pointer;
  text-align: left;
  transition: border-color .16s, background .16s, transform .12s;
}

.besace-cell:hover {
  border-color: var(--line-strong, oklch(.42 .03 60 / .7));
  transform: translateY(-1px);
}

.besace-cell--sel {
  border-color: var(--frost, oklch(.70 .07 232));
  background: linear-gradient(180deg, oklch(.34 .05 232 / .18), oklch(.28 .04 232 / .14));
  box-shadow: 0 0 16px -6px oklch(.70 .07 232 / .4);
}

.besace-cell--sap:not(.besace-cell--sel)   { border-left: 2px solid oklch(.70 .09 162 / .6); }
.besace-cell--frost:not(.besace-cell--sel) { border-left: 2px solid oklch(.70 .07 232 / .6); }
.besace-cell--gold:not(.besace-cell--sel)  { border-left: 2px solid oklch(.72 .1 85 / .6); }

.besace-cell__name {
  font-family: var(--font, serif);
  font-size: 13.5px;
  color: var(--ink-2, oklch(.78 .02 275));
  line-height: 1.25;
}

.besace-cell__foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 6px;
}

.besace-cell__qty {
  font-family: var(--font-mono, monospace);
  font-size: 10px;
  color: var(--ink-4, oklch(.45 .015 275));
}

.besace-cell__rarity {
  font-family: var(--font-caps, var(--font));
  font-size: 9px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4, oklch(.45 .015 275));
}

.besace-cell__rarity--sap   { color: var(--sap, oklch(.70 .09 162)); }
.besace-cell__rarity--frost { color: var(--frost, oklch(.70 .07 232)); }
.besace-cell__rarity--gold  { color: var(--gold, oklch(.72 .1 85)); }

.besace-sheet {
  margin-top: 20px;
  padding: 18px 20px 20px;
  border: 1px solid var(--line-soft, oklch(.32 .022 60 / .5));
  border-radius: 6px;
  background: linear-gradient(180deg, oklch(.24 .034 60 / .55), oklch(.20 .028 60 / .45));
  display: flex;
  flex-direction: column;
  gap: 10px;
  max-width: 420px;
}

.besace-sheet__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
}

.besace-sheet__name {
  font-family: var(--font-display, var(--font));
  font-size: 19px;
  font-weight: 600;
  color: var(--gold, oklch(.72 .1 85));
  line-height: 1.2;
  margin: 0;
}

.besace-sheet__badges { display: flex; gap: 5px; flex: 0 0 auto; }

.besace-badge {
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

.besace-badge--sap   { border-color: oklch(.70 .09 162 / .45); color: var(--sap, oklch(.70 .09 162)); background: oklch(.50 .07 162 / .12); }
.besace-badge--frost { border-color: oklch(.70 .07 232 / .45); color: var(--frost, oklch(.70 .07 232)); background: oklch(.50 .06 232 / .12); }
.besace-badge--gold  { border-color: oklch(.72 .1 85 / .45); color: var(--gold, oklch(.72 .1 85)); background: oklch(.55 .08 85 / .12); }

.besace-sheet__type {
  font-family: var(--font-caps, var(--font));
  font-size: 9px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--ink-4, oklch(.45 .015 275));
  margin: 0;
}

.besace-sheet__desc {
  font-family: var(--font, serif);
  font-size: 13px;
  line-height: 1.55;
  color: var(--ink-3, oklch(.65 .02 275));
  margin: 0;
}

.besace-sheet__effect { display: flex; flex-direction: column; gap: 3px; }

.besace-effect-label {
  font-family: var(--font-caps, var(--font));
  font-size: 9px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--ink-4, oklch(.45 .015 275));
}

.besace-effect-value {
  font-family: var(--font-mono, monospace);
  font-size: 12px;
  color: var(--ink-2, oklch(.78 .02 275));
}

.besace-effect-value--sap   { color: var(--sap, oklch(.70 .09 162)); }
.besace-effect-value--frost { color: var(--frost, oklch(.70 .07 232)); }
.besace-effect-value--gold  { color: var(--gold, oklch(.72 .1 85)); }
.besace-effect-value--blood { color: var(--blood, oklch(.52 .15 20)); }

.besace-sheet__cost { display: flex; flex-direction: column; gap: 3px; }

.besace-cost-value {
  font-family: var(--font-mono, monospace);
  font-size: 12px;
  color: var(--gold, oklch(.72 .1 85));
}

.besace-sheet__contract { display: flex; flex-wrap: wrap; gap: 5px; padding: 4px 0; }

.besace-sheet__contract span {
  padding: 3px 6px;
  border: 1px solid var(--line-soft, oklch(.32 .022 60 / .5));
  border-radius: 999px;
  color: var(--ink-3);
  font-family: var(--font-mono, monospace);
  font-size: 9px;
}

.besace-sheet__actions {
  padding-top: 2px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.besace-action-btn {
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

.besace-action-btn:disabled { opacity: .38; cursor: not-allowed; }

.besace-action-btn--use {
  border: 1px solid var(--gold, oklch(.72 .1 85));
  background: oklch(.55 .08 85 / .15);
  color: var(--gold, oklch(.72 .1 85));
  box-shadow: 0 0 18px -6px oklch(.72 .1 85 / .4);
}

.besace-action-btn--use:not(:disabled):hover {
  background: oklch(.55 .08 85 / .24);
  box-shadow: 0 0 26px -6px oklch(.72 .1 85 / .55);
}

.besace-action-btn--read {
  border: 1px solid var(--frost, oklch(.70 .07 232));
  background: oklch(.50 .06 232 / .15);
  color: var(--frost, oklch(.70 .07 232));
  box-shadow: 0 0 18px -6px oklch(.70 .07 232 / .4);
}

.besace-action-btn--read:hover {
  background: oklch(.50 .06 232 / .24);
  box-shadow: 0 0 26px -6px oklch(.70 .07 232 / .55);
}

.besace-sheet__unusable {
  font-family: var(--font-caps, var(--font));
  font-size: 10px;
  letter-spacing: 0.1em;
  color: var(--ink-4, oklch(.45 .015 275));
  margin: 0;
}

.besace-sheet__warning {
  margin: 0;
  color: var(--gold, oklch(.72 .1 85));
  font-size: 10px;
  line-height: 1.4;
}

.besace-sheet__seal {
  display: flex;
  align-items: center;
  gap: 12px;
  padding-bottom: 2px;
}

.besace-sheet__error {
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  color: var(--blood, oklch(.52 .15 20));
  margin: 0;
}

.besace-fade-enter-active,
.besace-fade-leave-active {
  transition: opacity .22s, transform .22s;
}
.besace-fade-enter-from,
.besace-fade-leave-to {
  opacity: 0;
  transform: translateY(8px);
}

@media (prefers-reduced-motion: reduce) {
  .besace-cell,
  .besace-action-btn {
    transition: none;
  }
}
</style>
