<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';
import type { ItemDefinitionView } from '../../../party/types/itemTypes';
import { usePlayerStore } from '../../../party/stores/playerStore';
import { itemsApi } from '../../../party/api/itemsApi';

const props = defineProps<{ character: PlayerCharacterView }>();

const playerStore = usePlayerStore();

const allItems = ref<ItemDefinitionView[]>([]);

onMounted(async () => {
  try {
    const response = await itemsApi.listActive();
    allItems.value = response.items;
  } catch {
    // Best-effort: fall back to raw keys below if the catalog lookup fails.
  }
});

function itemDisplayName(itemKey: string): string {
  return allItems.value.find((i) => i.key === itemKey)?.displayName ?? itemKey;
}

function weaponContract(itemKey: string): string | null {
  const item = allItems.value.find((candidate) => candidate.key === itemKey);
  if (!item || item.itemType !== 'Weapon') return null;
  const category = item.basicAttackCategory === 'Magic' ? 'magique' : 'physique';
  const lineOfSight = item.requiresLineOfSight ? ' · ligne de vue' : '';
  return `${item.basicAttackPower ?? 10} puissance · portée ${item.tacticalRange ?? 1}`
    + ` · ${category}${lineOfSight} · ${shapeLabel(item.tacticalAreaShape)}`;
}

function shapeLabel(shape: string | undefined): string {
  switch (shape) {
    case 'Cross': return 'croix';
    case 'Diamond': return 'losange';
    case 'Map': return 'carte';
    default: return 'case';
  }
}

function equipmentEffects(itemKey: string): string[] {
  const item = allItems.value.find((candidate) => candidate.key === itemKey);
  return (item?.equipmentEffects ?? []).map((effect) => {
    if (['StatModifier', 'StatBonus', 'StatBonusPercent'].includes(effect.kind)) {
      const amount = effect.amount ?? 0;
      const unit = effect.kind === 'StatBonusPercent' ? '%' : '';
      return `${amount >= 0 ? '+' : ''}${amount}${unit} ${statLabel(effect.statKind)}`;
    }
    if (effect.kind === 'GrantSkill') return `Compétence : ${effect.skillKey ?? 'inconnue'}`;
    if (effect.kind === 'Affinity') return `Affinité : ${effect.affinityRegister ?? 'neutre'}`;
    return effect.kind;
  });
}

function statLabel(stat: string | null | undefined): string {
  const labels: Record<string, string> = {
    MaxVitality: 'Vitalité',
    AttackPower: 'Attaque',
    Defense: 'Défense',
    Guard: 'Garde',
    Speed: 'Vitesse',
    Initiative: 'Initiative',
    Focus: 'Focus',
    Mana: 'Mana',
    MagicAttack: 'Attaque magique',
    MagicDefense: 'Défense magique',
    Movement: 'Déplacement',
    RunItemCapacity: 'Capacité de besace',
  };
  return stat ? (labels[stat] ?? stat) : 'statistique';
}

const equippedItems = computed(() => props.character.items.filter((i) => i.isEquipped));
const slotLimits = { Weapon: 1, Accessory: 1, Relic: 3 } as const;
const equipmentSlots = computed(() => {
  const weapons = equippedItems.value.filter((item) => (item.slot ?? slotFor(item.itemKey)) === 'Weapon');
  const accessories = equippedItems.value.filter((item) => (item.slot ?? slotFor(item.itemKey)) === 'Accessory');
  const relics = equippedItems.value.filter((item) => (item.slot ?? slotFor(item.itemKey)) === 'Relic');
  return [
    { key: 'weapon', label: 'Arme', item: weapons[0] },
    { key: 'accessory', label: 'Accessoire', item: accessories[0] },
    { key: 'relic-1', label: 'Relique I', item: relics[0] },
    { key: 'relic-2', label: 'Relique II', item: relics[1] },
    { key: 'relic-3', label: 'Relique III', item: relics[2] },
  ];
});

function slotFor(itemKey: string): keyof typeof slotLimits | null {
  const definition = allItems.value.find((item) => item.key === itemKey);
  const type = definition?.itemType;
  if (type === 'Weapon') return 'Weapon';
  if (type === 'Accessory') return 'Accessory';
  if (type === 'Relic' || type === 'Heritage' || definition?.category === 'Relic') return 'Relic';
  // Permanent inventory only contains equippable run rewards. Keep legacy
  // entries usable while their catalog metadata is loading or being migrated.
  if (!definition) {
    return props.character.items.find((item) => item.itemKey === itemKey)?.slot ?? 'Relic';
  }
  return null;
}

function slotLabel(slot: keyof typeof slotLimits): string {
  return slot === 'Weapon' ? 'Arme' : slot === 'Accessory' ? 'Accessoire' : 'Relique';
}

function isSlotFull(itemKey: string): boolean {
  const slot = slotFor(itemKey);
  if (!slot) return true;
  return equippedItems.value.filter((item) => (item.slot ?? 'Relic') === slot).length >= slotLimits[slot];
}

const equippablePermanentItems = computed(() =>
  playerStore.permanentItems.filter((item) => slotFor(item.itemDefinitionKey) !== null),
);

function isEquippedOnCharacter(itemKey: string): boolean {
  return props.character.items.some((i) => i.itemKey === itemKey && i.isEquipped);
}

function toggleItem(itemKey: string, isEquipped: boolean) {
  if (playerStore.isLoading) return;
  if (isEquipped) {
    playerStore.unequipItem(props.character.id, itemKey);
  } else {
    if (isSlotFull(itemKey)) return;
    playerStore.equipItem(props.character.id, itemKey);
  }
}
</script>

<template>
  <div class="imk-root">
    <p v-if="playerStore.error" class="imk-error">{{ playerStore.error }}</p>

    <!-- ── Section 1: currently equipped ── -->
    <section class="imk-section">
      <h4 class="imk-section__title">
        Objets équipés · contrat tactique
        <span class="imk-section__count">{{ equippedItems.length }} / {{ character.maxEquippedItems }}</span>
      </h4>
      <ul class="imk-slots">
        <li
          v-for="slot in equipmentSlots"
          :key="slot.key"
          class="imk-slot"
          :class="{ 'imk-slot--empty': !slot.item }"
        >
          <span class="imk-slot__label">{{ slot.label }}</span>
          <template v-if="slot.item">
            <div class="imk-row__info">
              <span class="imk-row__name">{{ itemDisplayName(slot.item.itemKey) }}</span>
              <small v-if="weaponContract(slot.item.itemKey)" class="imk-row__contract">
                {{ weaponContract(slot.item.itemKey) }}
              </small>
              <small
                v-for="effect in equipmentEffects(slot.item.itemKey)"
                :key="effect"
                class="imk-row__effect"
              >
                {{ effect }}
              </small>
            </div>
            <button
              type="button"
              class="imk-toggle imk-toggle--active"
              :disabled="playerStore.isLoading"
              @click="toggleItem(slot.item.itemKey, true)"
            >
              Déséquiper
            </button>
          </template>
          <span v-else class="imk-slot__empty">Emplacement libre</span>
        </li>
      </ul>
    </section>

    <!-- ── Section 2: permanent backpack (all owned items) ── -->
    <section class="imk-section">
      <h4 class="imk-section__title">Sac permanent</h4>
      <ul v-if="equippablePermanentItems.length" class="imk-list">
        <li
          v-for="permanentItem in equippablePermanentItems"
          :key="permanentItem.itemDefinitionKey"
          class="imk-row"
        >
          <div class="imk-row__info">
            <span class="imk-row__name">{{ itemDisplayName(permanentItem.itemDefinitionKey) }}</span>
            <span v-if="slotFor(permanentItem.itemDefinitionKey)" class="imk-row__slot">
              {{ slotLabel(slotFor(permanentItem.itemDefinitionKey)!) }}
            </span>
            <small v-if="weaponContract(permanentItem.itemDefinitionKey)" class="imk-row__contract">
              {{ weaponContract(permanentItem.itemDefinitionKey) }}
            </small>
            <small
              v-for="effect in equipmentEffects(permanentItem.itemDefinitionKey)"
              :key="effect"
              class="imk-row__effect"
            >
              {{ effect }}
            </small>
            <small
              v-if="permanentItem.containedLiquidDefinitionKey"
              class="imk-row__effect"
            >
              Contenu : {{ itemDisplayName(permanentItem.containedLiquidDefinitionKey) }}
            </small>
          </div>
          <button
            type="button"
            class="imk-toggle"
            :class="{ 'imk-toggle--active': isEquippedOnCharacter(permanentItem.itemDefinitionKey) }"
            :disabled="
              playerStore.isLoading ||
              (!isEquippedOnCharacter(permanentItem.itemDefinitionKey) && isSlotFull(permanentItem.itemDefinitionKey))
            "
            @click="toggleItem(permanentItem.itemDefinitionKey, isEquippedOnCharacter(permanentItem.itemDefinitionKey))"
          >
            {{ isEquippedOnCharacter(permanentItem.itemDefinitionKey) ? 'Équipé' : 'Équiper' }}
          </button>
        </li>
      </ul>
      <p v-else class="imk-empty">Le sac permanent est vide pour l'instant.</p>
    </section>
  </div>
</template>

<style scoped>
.imk-root {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.imk-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.imk-section__title {
  font-family: var(--font-caps, var(--font));
  font-size: 11px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4);
  padding-bottom: 6px;
  border-bottom: 1px solid var(--line-soft);
  margin: 0;
}

.imk-row__slot {
  margin-left: 0.5rem;
  color: var(--ink-4);
  font-size: 0.68rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.imk-row__contract {
  display: block;
  margin-top: 0.2rem;
  color: var(--ink-4);
  font-size: 0.72rem;
}

.imk-row__effect {
  display: block;
  color: var(--frost, var(--ink-3));
  font-size: 0.7rem;
}

.imk-section__count {
  float: right;
  font-family: var(--font-mono, monospace);
  color: var(--gold);
}

.imk-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.imk-slots {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(185px, 1fr));
  gap: 8px;
}

.imk-slot {
  min-height: 96px;
  padding: 10px;
  border: 1px solid var(--line-soft);
  border-radius: 5px;
  background: oklch(0.24 0.015 283 / 0.35);
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 8px;
}

.imk-slot--empty {
  border-style: dashed;
  opacity: 0.7;
}

.imk-slot__label {
  font-family: var(--font-caps, var(--font));
  font-size: 9px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--gold);
}

.imk-slot__empty {
  margin: auto 0;
  font-size: 11px;
  font-style: italic;
  color: var(--ink-4);
}

.imk-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding: 8px 10px;
  border-radius: 4px;
  background: oklch(0.24 0.015 283 / 0.35);
}

.imk-row__info {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 8px;
}

.imk-slot .imk-toggle {
  margin-top: auto;
}

.imk-row__name {
  font-size: 13px;
  color: var(--ink-2);
}

.imk-toggle {
  flex-shrink: 0;
  font-family: var(--font-caps, var(--font));
  font-size: 9.5px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  padding: 4px 10px;
  border-radius: 3px;
  border: 1px solid var(--line-soft);
  background: transparent;
  color: var(--ink-4);
  cursor: pointer;
  transition: opacity 0.15s, border-color 0.15s, color 0.15s;
}

.imk-toggle:disabled {
  opacity: 0.38;
  cursor: not-allowed;
}

.imk-toggle:not(:disabled):hover {
  border-color: var(--ink-3);
  color: var(--ink-2);
}

.imk-toggle--active {
  border-color: var(--gold);
  color: var(--gold);
  background: oklch(0.55 0.08 85 / 0.12);
}

.imk-empty {
  font-size: 12px;
  color: var(--ink-4);
  font-style: italic;
  margin: 0;
}

.imk-error {
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  color: var(--blood);
  margin: 0;
}
</style>
