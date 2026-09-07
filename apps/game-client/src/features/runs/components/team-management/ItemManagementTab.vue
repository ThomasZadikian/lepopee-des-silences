<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import type {
  EquipmentChangePlanView, EquipmentPosition, PlayerCharacterItemView,
  PlayerCharacterView, PlayerPermanentItemView,
} from '../../../party/types/playerTypes';
import type { ItemDefinitionView } from '../../../party/types/itemTypes';
import { usePlayerStore } from '../../../party/stores/playerStore';
import { itemsApi } from '../../../party/api/itemsApi';
import { useRunStore } from '../../stores/runStore';
import { itemTypeMeta } from '../../../../shared/theme/typeColors';

const props = defineProps<{ character: PlayerCharacterView }>();
const playerStore = usePlayerStore();
const runStore = useRunStore();
const allItems = ref<ItemDefinitionView[]>([]);
const pendingPlan = ref<EquipmentChangePlanView | null>(null);

const visiblePositions: Array<{ key: EquipmentPosition; label: string }> = [
  { key: 'Head', label: 'Tête' }, { key: 'Neck', label: 'Cou' },
  { key: 'Shoulders', label: 'Épaules' }, { key: 'Cape', label: 'Cape' },
  { key: 'Chest', label: 'Torse' }, { key: 'Wrist', label: 'Poignets' },
  { key: 'Hand', label: 'Mains' }, { key: 'Waist', label: 'Taille' },
  { key: 'Legs', label: 'Jambes' }, { key: 'Feet', label: 'Pieds' },
  { key: 'Ring1', label: 'Anneau I' }, { key: 'Ring2', label: 'Anneau II' },
  { key: 'Relic', label: 'Relique' }, { key: 'MainWeapon', label: 'Arme principale' },
];

onMounted(async () => {
  try { allItems.value = (await itemsApi.listActive()).items; } catch { /* raw keys remain usable */ }
});

const combatLocked = computed(() => Boolean(runStore.currentRun && runStore.shouldShowCombatScene));
const equippedItems = computed(() => props.character.items.filter((item) => item.isEquipped));
const equipmentSlots = computed(() => {
  const slots = visiblePositions.map((position) => ({ ...position, item: undefined as PlayerCharacterItemView | undefined }));
  const legacyRelicPositions: EquipmentPosition[] = ['Relic', 'Ring1', 'Ring2'];
  for (const item of equippedItems.value) {
    let position = item.position;
    if (!position) {
      if (item.slot === 'Weapon') position = 'MainWeapon';
      else if (item.slot === 'Accessory') position = 'Neck';
      else position = legacyRelicPositions.find((candidate) => !slots.find((slot) => slot.key === candidate)?.item);
    }
    const slot = slots.find((candidate) => candidate.key === position);
    if (slot) slot.item = item;
  }
  return slots;
});

function definition(itemKey: string) { return allItems.value.find((item) => item.key === itemKey); }
function itemDisplayName(itemKey: string) { return definition(itemKey)?.displayName ?? itemKey; }
function itemTypeAccent(itemKey: string) { return itemTypeMeta(definition(itemKey)?.category); }
function weaponContract(itemKey: string): string | null {
  const item = definition(itemKey);
  if (!item || item.category !== 'Weapon') return null;
  const category = item.basicAttackCategory === 'Magic' ? 'magique' : 'physique';
  const lineOfSight = item.requiresLineOfSight ? ' · ligne de vue' : '';
  return `${item.basicAttackPower ?? 10} puissance · portée ${item.tacticalRange ?? 1} · ${category}${lineOfSight}`;
}
function equipmentEffects(itemKey: string): string[] {
  return (definition(itemKey)?.equipmentEffects ?? []).map((effect) => {
    if (['StatModifier', 'StatBonus', 'StatBonusPercent'].includes(effect.kind)) {
      const amount = effect.amount ?? 0;
      return `${amount >= 0 ? '+' : ''}${amount}${effect.kind === 'StatBonusPercent' ? '%' : ''} ${statLabel(effect.statKind)}`;
    }
    if (effect.kind === 'GrantSkill') return `Compétence : ${effect.skillKey ?? 'inconnue'}`;
    return effect.kind;
  });
}
function statLabel(stat?: string | null) {
  const labels: Record<string, string> = {
    MaxVitality: 'Vitalité', AttackPower: 'Attaque', MagicAttack: 'Attaque magique',
    Defense: 'Défense', MagicDefense: 'Défense magique', StartingGuard: 'Garde',
    Speed: 'Vitesse', Initiative: 'Initiative', Focus: 'Focus', Mana: 'Mana', Movement: 'Déplacement',
  };
  return stat ? (labels[stat] ?? stat) : 'statistique';
}

function allowedPositions(itemKey: string): EquipmentPosition[] {
  const itemDefinition = definition(itemKey);
  const authoredSlots = itemDefinition?.allowedSlots?.length
    ? itemDefinition.allowedSlots
    : itemDefinition?.equipSlot ? [itemDefinition.equipSlot] : [];
  return authoredSlots.flatMap((slot) => {
    if (slot === 'Ring') return ['Ring1', 'Ring2'] as EquipmentPosition[];
    if (slot === 'Weapon') return ['MainWeapon'] as EquipmentPosition[];
    if (slot === 'Accessory') return ['Neck'] as EquipmentPosition[];
    if (slot === 'OffWeapon') return [];
    return visiblePositions.some((position) => position.key === slot) ? [slot as EquipmentPosition] : [];
  });
}
const equippablePermanentItems = computed(() => playerStore.permanentItems
  .filter((item) => definition(item.itemDefinitionKey)?.allowedSlots?.includes('OffWeapon') !== true));

function equippedAssignment(item: PlayerPermanentItemView): PlayerCharacterItemView | undefined {
  return props.character.items.find((owned) => owned.isEquipped && (
    item.itemInstanceId ? owned.itemInstanceId === item.itemInstanceId : owned.itemKey === item.itemDefinitionKey
  ));
}
function assignedToAnotherCharacter(item: PlayerPermanentItemView): boolean {
  if (!item.itemInstanceId) return false;
  return Boolean(playerStore.profile?.characters.some((character) => character.id !== props.character.id
    && character.items.some((owned) => owned.itemInstanceId === item.itemInstanceId && owned.isEquipped)));
}
function preferredPosition(itemKey: string): EquipmentPosition | null {
  const positions = allowedPositions(itemKey);
  return positions.find((position) => !equipmentSlots.value.find((slot) => slot.key === position)?.item)
    ?? positions[0] ?? null;
}

async function requestEquip(item: PlayerPermanentItemView) {
  if (playerStore.isLoading || combatLocked.value || assignedToAnotherCharacter(item)) return;
  const position = preferredPosition(item.itemDefinitionKey);
  if (!item.itemInstanceId || !position) {
    await playerStore.equipItem(props.character.id, item.itemDefinitionKey); // legacy migration fallback
    await syncRun();
    return;
  }
  pendingPlan.value = await playerStore.previewEquipmentChange(
    props.character.id, item.itemInstanceId, position,
  );
}
function legacyLoadoutFull(item: PlayerPermanentItemView): boolean {
  return !item.itemInstanceId && !equippedAssignment(item)
    && equippedItems.value.length >= props.character.maxEquippedItems;
}
async function confirmEquip() {
  const plan = pendingPlan.value;
  if (!plan?.canEquip || combatLocked.value) return;
  await playerStore.equipItemInstance(
    props.character.id, plan.candidateItem.itemInstanceId, plan.targetPosition,
  );
  pendingPlan.value = null;
  await syncRun();
}
async function unequip(item: PlayerCharacterItemView) {
  if (playerStore.isLoading || combatLocked.value) return;
  if (item.itemInstanceId) await playerStore.unequipItemInstance(props.character.id, item.itemInstanceId);
  else await playerStore.unequipItem(props.character.id, item.itemKey);
  await syncRun();
}
async function syncRun() {
  if (runStore.currentRun && !combatLocked.value) await runStore.syncPartyStats();
}
</script>

<template>
  <div class="imk-root">
    <p v-if="playerStore.error" class="imk-error">{{ playerStore.error }}</p>
    <p v-if="combatLocked" class="imk-error">L'équipement est verrouillé pendant un combat.</p>

    <section class="imk-section">
      <h4 class="imk-section__title">Objets équipés · silhouette
        <span class="imk-section__count">{{ equippedItems.length }} / {{ character.maxEquippedItems }}</span>
      </h4>
      <ul class="imk-slots" aria-label="Silhouette d'équipement">
        <li v-for="slot in equipmentSlots" :key="slot.key" class="imk-slot"
          :class="{ 'imk-slot--empty': !slot.item }"
          :style="slot.item ? { borderLeftColor: itemTypeAccent(slot.item.itemKey).color } : undefined">
          <span class="imk-slot__label">{{ slot.label }}</span>
          <template v-if="slot.item">
            <div class="imk-row__info">
              <span class="imk-row__name">{{ itemDisplayName(slot.item.itemKey) }}</span>
              <small v-if="weaponContract(slot.item.itemKey)" class="imk-row__contract">{{ weaponContract(slot.item.itemKey) }}</small>
              <small v-for="effect in equipmentEffects(slot.item.itemKey)" :key="effect" class="imk-row__effect">{{ effect }}</small>
            </div>
            <button type="button" class="imk-toggle imk-toggle--active"
              :disabled="playerStore.isLoading || combatLocked" @click="unequip(slot.item)">Déséquiper</button>
          </template>
          <span v-else class="imk-slot__empty">Emplacement libre</span>
        </li>
      </ul>
    </section>

    <section class="imk-section">
      <h4 class="imk-section__title">Inventaire partagé · sac permanent</h4>
      <ul v-if="equippablePermanentItems.length" class="imk-list">
        <li v-for="item in equippablePermanentItems" :key="item.itemInstanceId ?? item.itemDefinitionKey"
          class="imk-row" :style="{ borderLeftColor: itemTypeAccent(item.itemDefinitionKey).color }">
          <div class="imk-row__info">
            <span class="imk-row__name">{{ itemDisplayName(item.itemDefinitionKey) }}</span>
            <small class="imk-row__effect">{{ allowedPositions(item.itemDefinitionKey).map((p) => visiblePositions.find((v) => v.key === p)?.label).join(' · ') }}</small>
            <small v-if="assignedToAnotherCharacter(item)" class="imk-row__cost">Assigné à un autre personnage</small>
          </div>
          <button v-if="!equippedAssignment(item)" type="button" class="imk-toggle"
            :disabled="playerStore.isLoading || combatLocked || assignedToAnotherCharacter(item) || legacyLoadoutFull(item)"
            @click="requestEquip(item)">Aperçu</button>
          <span v-else class="imk-row__slot">Équipé</span>
        </li>
      </ul>
      <p v-else class="imk-empty">Le sac permanent est vide pour l'instant.</p>
    </section>

    <div v-if="pendingPlan" class="imk-preview" role="dialog" aria-modal="true" aria-label="Aperçu d'équipement">
      <div class="imk-preview__card">
        <h4>{{ pendingPlan.candidateItem.displayName }}</h4>
        <p>Emplacement : {{ visiblePositions.find((p) => p.key === pendingPlan?.targetPosition)?.label }}</p>
        <p v-if="pendingPlan.currentlyEquippedItem">Remplace : {{ pendingPlan.currentlyEquippedItem.displayName }}</p>
        <ul v-if="pendingPlan.statDeltas.some((delta) => delta.delta !== 0)">
          <li v-for="delta in pendingPlan.statDeltas.filter((entry) => entry.delta !== 0)" :key="delta.stat">
            {{ statLabel(delta.stat) }} : {{ delta.current }} → {{ delta.projected }}
            ({{ delta.delta > 0 ? '+' : '' }}{{ delta.delta }})
          </li>
        </ul>
        <p v-for="skill in pendingPlan.gainedTemporarySkills" :key="`gain-${skill}`">Compétence gagnée : {{ skill }}</p>
        <p v-for="skill in pendingPlan.lostTemporarySkills" :key="`loss-${skill}`">Compétence perdue : {{ skill }}</p>
        <p v-if="!pendingPlan.canEquip" class="imk-error">{{ pendingPlan.blockingReasons.join(' · ') }}</p>
        <div class="imk-preview__actions">
          <button type="button" class="imk-toggle" @click="pendingPlan = null">Annuler</button>
          <button type="button" class="imk-toggle imk-toggle--active"
            :disabled="!pendingPlan.canEquip || playerStore.isLoading || combatLocked" @click="confirmEquip">Confirmer</button>
        </div>
      </div>
    </div>
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
  font-family: var(--font-mono);
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
  color: var(--mint-dim);
  font-size: 0.7rem;
}

.imk-row__cost {
  display: block;
  color: var(--ink-3);
  font-size: 0.7rem;
}

.imk-section__count {
  float: right;
  font-family: var(--font-mono);
  color: var(--mint-dim);
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
  border-left: 3px solid var(--line-soft);
  background: var(--panel-2);
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
  font-family: var(--font-mono);
  font-size: 9px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--mint-dim);
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
  border-left: 3px solid var(--line-soft);
  background: var(--panel-2);
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
  font-family: var(--font-mono);
  font-size: 9.5px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  padding: 4px 10px;
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
  border-color: var(--mint-dim);
  color: var(--mint-dim);
  background: var(--panel);
}

.imk-empty {
  font-size: 12px;
  color: var(--ink-4);
  font-style: italic;
  margin: 0;
}

.imk-error {
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--danger-dim);
  margin: 0;
}

.imk-preview {
  position: fixed;
  inset: 0;
  z-index: 30;
  display: grid;
  place-items: center;
  padding: 20px;
  background: rgb(0 0 0 / 70%);
}

.imk-preview__card {
  width: min(520px, 100%);
  max-height: 80vh;
  overflow: auto;
  padding: 20px;
  border: 1px solid var(--line-soft);
  background: var(--panel);
  color: var(--ink-2);
}

.imk-preview__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
