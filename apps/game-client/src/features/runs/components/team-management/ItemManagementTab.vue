<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import type {
  EquipmentChangePlanView, EquipmentPosition, PlayerCharacterItemView,
  PlayerCharacterView, PlayerPermanentItemView,
} from '../../../party/types/playerTypes';
import type { ItemDefinitionView } from '../../../party/types/itemTypes';
import { usePlayerStore } from '../../../party/stores/playerStore';
import { playerApi } from '../../../party/api/playerApi';
import { itemsApi } from '../../../party/api/itemsApi';
import { getActivePlayerId, useRunStore } from '../../stores/runStore';
import { itemTypeMeta } from '../../../../shared/theme/typeColors';

const props = defineProps<{ character: PlayerCharacterView }>();
const playerStore = usePlayerStore();
const runStore = useRunStore();
const allItems = ref<ItemDefinitionView[]>([]);
const pendingPlan = ref<EquipmentChangePlanView | null>(null);
const openPosition = ref<EquipmentPosition | null>(null);
const pinnedPosition = ref<EquipmentPosition | null>(null);
const comparisonPlan = ref<EquipmentChangePlanView | null>(null);
const comparisonItemKey = ref<string | null>(null);
const comparisonLoading = ref(false);
const comparisonError = ref<string | null>(null);
const comparisonPointer = ref({ x: 0, y: 0 });
const previewCache = new Map<string, EquipmentChangePlanView>();
let previewRequestId = 0;

const visiblePositions: Array<{ key: EquipmentPosition; label: string }> = [
  { key: 'Head', label: 'Tête' }, { key: 'Neck', label: 'Cou' },
  { key: 'Shoulders', label: 'Épaules' }, { key: 'Cape', label: 'Cape' },
  { key: 'Chest', label: 'Torse' }, { key: 'Wrist', label: 'Poignets' },
  { key: 'Hand', label: 'Mains' }, { key: 'Waist', label: 'Taille' },
  { key: 'Legs', label: 'Jambes' }, { key: 'Feet', label: 'Pieds' },
  { key: 'Ring1', label: 'Anneau I' }, { key: 'Ring2', label: 'Anneau II' },
  { key: 'Relic', label: 'Relique' }, { key: 'MainWeapon', label: 'Arme principale' },
];

const leftPositions: EquipmentPosition[] = [
  'Head', 'Shoulders', 'Cape', 'Wrist', 'Ring1', 'MainWeapon', 'Relic',
];
const rightPositions: EquipmentPosition[] = [
  'Neck', 'Chest', 'Hand', 'Waist', 'Legs', 'Feet', 'Ring2',
];

const positionGlyphs: Record<EquipmentPosition, string> = {
  Head: '♜', Neck: '◇', Shoulders: '◒', Cape: '◢', Chest: '⬡', Wrist: '⌁',
  Hand: '✦', Waist: '═', Legs: '⋔', Feet: '⌄', Ring1: '○', Ring2: '○',
  Relic: '✧', MainWeapon: '†', OffWeapon: '◩',
};

const statisticGroups = computed(() => [
  {
    label: 'Ressources',
    items: [
      { key: 'maxVitality', label: 'Vitalité', glyph: '♥', value: props.character.stats.maxVitality },
      { key: 'mana', label: 'Mana', glyph: '◆', value: props.character.stats.mana },
      { key: 'charge', label: 'Charge', glyph: '◉', value: props.character.stats.charge },
      { key: 'movement', label: 'Déplacement', glyph: '➜', value: props.character.stats.movement ?? 4 },
    ],
  },
  {
    label: 'Offensive',
    items: [
      { key: 'attackPower', label: 'Attaque', glyph: '⚔', value: props.character.stats.attackPower },
      { key: 'magicAttack', label: 'Attaque magique', glyph: '✦', value: props.character.stats.magicAttack ?? 0 },
      { key: 'focus', label: 'Focus', glyph: '◎', value: props.character.stats.focus },
    ],
  },
  {
    label: 'Défensive',
    items: [
      { key: 'defense', label: 'Défense', glyph: '⬟', value: props.character.stats.defense },
      { key: 'magicDefense', label: 'Défense magique', glyph: '◈', value: props.character.stats.magicDefense ?? 0 },
      { key: 'startingGuard', label: 'Garde initiale', glyph: '◩', value: props.character.stats.startingGuard },
    ],
  },
  {
    label: 'Tempo',
    items: [
      { key: 'speed', label: 'Vitesse', glyph: '»', value: props.character.stats.speed },
      { key: 'initiative', label: 'Initiative', glyph: '⚑', value: props.character.stats.initiative },
    ],
  },
]);

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

function equipmentSlot(position: EquipmentPosition) {
  return equipmentSlots.value.find((slot) => slot.key === position)!;
}

function archetypeName() {
  const raw = props.character.archetypeKey;
  if (!raw) return props.character.characterType === 'Companion' ? 'Compagnon' : 'Archétype à définir';
  const value = raw.split('.').at(-1) ?? raw;
  return value.replace(/[-_]/g, ' ').replace(/\b\p{L}/gu, (letter) => letter.toUpperCase());
}

function itemGlyph(itemKey: string, position?: EquipmentPosition) {
  const category = definition(itemKey)?.category;
  if (category === 'Weapon') return '†';
  if (category === 'Relic') return '✧';
  if (category === 'Consumable') return '◒';
  return position ? positionGlyphs[position] : '◇';
}

function statDelta(key: string, value: number) {
  const base = props.character.baseStats?.[key as keyof typeof props.character.stats];
  return typeof base === 'number' ? value - base : 0;
}

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
function itemDetails(itemKey: string) {
  return [weaponContract(itemKey), ...equipmentEffects(itemKey)].filter(Boolean) as string[];
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
const equippablePermanentItems = computed(() => {
  const ownedInstanceIds = new Set(props.character.items
    .map((item) => item.itemInstanceId)
    .filter((id): id is string => Boolean(id)));
  const legacyOwnedKeys = new Set(props.character.items
    .filter((item) => !item.itemInstanceId)
    .map((item) => item.itemKey));

  return playerStore.permanentItems.filter((item) =>
    definition(item.itemDefinitionKey)?.allowedSlots?.includes('OffWeapon') !== true
    && (item.itemInstanceId
      ? ownedInstanceIds.has(item.itemInstanceId) || legacyOwnedKeys.has(item.itemDefinitionKey)
      : legacyOwnedKeys.has(item.itemDefinitionKey)));
});

function equippedAssignment(item: PlayerPermanentItemView): PlayerCharacterItemView | undefined {
  return props.character.items.find((owned) => owned.isEquipped && (
    item.itemInstanceId ? owned.itemInstanceId === item.itemInstanceId : owned.itemKey === item.itemDefinitionKey
  ));
}
function preferredPosition(itemKey: string): EquipmentPosition | null {
  const positions = allowedPositions(itemKey);
  return positions.find((position) => !equipmentSlots.value.find((slot) => slot.key === position)?.item)
    ?? positions[0] ?? null;
}

function slotCandidates(position: EquipmentPosition) {
  return equippablePermanentItems.value.filter((item) =>
    !equippedAssignment(item) && allowedPositions(item.itemDefinitionKey).includes(position));
}

function previewKey(item: PlayerPermanentItemView, position: EquipmentPosition) {
  return `${item.itemInstanceId ?? item.itemDefinitionKey}:${position}`;
}

const comparisonStyle = computed(() => {
  const width = typeof window === 'undefined' ? 1280 : window.innerWidth;
  const height = typeof window === 'undefined' ? 800 : window.innerHeight;
  return {
    left: `${Math.max(8, Math.min(comparisonPointer.value.x + 18, width - 318))}px`,
    top: `${Math.max(8, Math.min(comparisonPointer.value.y + 18, height - 330))}px`,
  };
});

function trackComparisonPointer(event: MouseEvent) {
  comparisonPointer.value = { x: event.clientX, y: event.clientY };
}

function clearComparison() {
  previewRequestId += 1;
  comparisonPlan.value = null;
  comparisonItemKey.value = null;
  comparisonLoading.value = false;
  comparisonError.value = null;
}

function openSlotPicker(position: EquipmentPosition) {
  if (openPosition.value !== position) clearComparison();
  openPosition.value = position;
}

function closeSlotPicker(position: EquipmentPosition) {
  if (pinnedPosition.value === position) return;
  if (openPosition.value === position) openPosition.value = null;
  clearComparison();
}

function toggleSlotPicker(position: EquipmentPosition) {
  if (pinnedPosition.value === position) {
    pinnedPosition.value = null;
    openPosition.value = null;
    clearComparison();
    return;
  }
  pinnedPosition.value = position;
  openSlotPicker(position);
}

function handleSlotFocusOut(event: FocusEvent, position: EquipmentPosition) {
  const slot = event.currentTarget as HTMLElement;
  if (event.relatedTarget instanceof Node && slot.contains(event.relatedTarget)) return;
  closeSlotPicker(position);
}

async function loadEquipmentPreview(
  item: PlayerPermanentItemView, position: EquipmentPosition,
): Promise<EquipmentChangePlanView | null> {
  if (!item.itemInstanceId) return null;
  const key = previewKey(item, position);
  const cached = previewCache.get(key);
  if (cached) return cached;
  const plan = await playerApi.previewEquipmentChange(
    getActivePlayerId(), props.character.id, item.itemInstanceId, position,
  );
  previewCache.set(key, plan);
  return plan;
}

async function previewCandidate(
  item: PlayerPermanentItemView, position: EquipmentPosition, event?: Event,
) {
  if (event && 'clientX' in event) trackComparisonPointer(event as MouseEvent);
  else if (event?.currentTarget instanceof HTMLElement) {
    const bounds = event.currentTarget.getBoundingClientRect();
    comparisonPointer.value = { x: bounds.right, y: bounds.top };
  }

  const itemKey = previewKey(item, position);
  const requestId = ++previewRequestId;
  comparisonItemKey.value = itemKey;
  comparisonPlan.value = null;
  comparisonError.value = null;

  if (!item.itemInstanceId) {
    comparisonLoading.value = false;
    comparisonError.value = 'Comparaison indisponible pour cet objet historique.';
    return;
  }

  comparisonLoading.value = true;
  try {
    const plan = await loadEquipmentPreview(item, position);
    if (requestId === previewRequestId && comparisonItemKey.value === itemKey) {
      comparisonPlan.value = plan;
    }
  } catch {
    if (requestId === previewRequestId) {
      comparisonError.value = 'Le comparatif ne peut pas être chargé pour le moment.';
    }
  } finally {
    if (requestId === previewRequestId) comparisonLoading.value = false;
  }
}

async function requestEquip(item: PlayerPermanentItemView, requestedPosition?: EquipmentPosition) {
  if (playerStore.isLoading || combatLocked.value) return;
  const position = requestedPosition ?? preferredPosition(item.itemDefinitionKey);
  if (!item.itemInstanceId || !position) {
    await playerStore.equipItem(props.character.id, item.itemDefinitionKey); // legacy migration fallback
    await syncRun();
    return;
  }
  pendingPlan.value = previewCache.get(previewKey(item, position))
    ?? await playerStore.previewEquipmentChange(props.character.id, item.itemInstanceId, position);
  pinnedPosition.value = null;
  openPosition.value = null;
  clearComparison();
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
  previewCache.clear();
  pendingPlan.value = null;
  await syncRun();
}
async function unequip(item: PlayerCharacterItemView) {
  if (playerStore.isLoading || combatLocked.value) return;
  if (item.itemInstanceId) await playerStore.unequipItemInstance(props.character.id, item.itemInstanceId);
  else await playerStore.unequipItem(props.character.id, item.itemKey);
  previewCache.clear();
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

    <div class="imk-profile-layout">
      <section class="imk-section imk-loadout-panel" aria-labelledby="equipment-title">
        <header class="imk-profile-header">
          <div>
            <span class="imk-profile-header__eyebrow">Profil d'équipement</span>
            <h3 id="equipment-title">{{ character.displayName }}</h3>
          </div>
          <div class="imk-profile-header__meta">
            <span>{{ archetypeName() }}</span>
            <strong>{{ equippedItems.length }} / {{ character.maxEquippedItems }}</strong>
          </div>
        </header>

        <div class="imk-paper-doll" aria-label="Silhouette d'équipement">
          <ul class="imk-slot-column imk-slot-column--left">
            <li v-for="position in leftPositions" :key="position">
              <article
                class="imk-slot"
                :class="{
                  'imk-slot--empty': !equipmentSlot(position).item,
                  'imk-slot--picker-open': openPosition === position,
                }"
                :data-equipment-position="position"
                :style="equipmentSlot(position).item
                  ? { '--slot-accent': itemTypeAccent(equipmentSlot(position).item!.itemKey).color }
                  : undefined"
                @mouseenter="openSlotPicker(position)"
                @mouseleave="closeSlotPicker(position)"
                @mousemove="trackComparisonPointer"
                @focusout="handleSlotFocusOut($event, position)"
              >
                <span class="imk-slot__glyph" aria-hidden="true">
                  {{ equipmentSlot(position).item ? itemGlyph(equipmentSlot(position).item!.itemKey, position) : positionGlyphs[position] }}
                </span>
                <span class="imk-slot__content">
                  <span class="imk-slot__label">{{ equipmentSlot(position).label }}</span>
                  <strong v-if="equipmentSlot(position).item" class="imk-slot__name">
                    {{ itemDisplayName(equipmentSlot(position).item!.itemKey) }}
                  </strong>
                  <small v-else>Libre</small>
                </span>
                <small v-if="equipmentSlot(position).item && itemDetails(equipmentSlot(position).item!.itemKey).length" class="imk-slot__details">
                  {{ itemDetails(equipmentSlot(position).item!.itemKey).join(' · ') }}
                </small>
                <button
                  v-if="equipmentSlot(position).item"
                  type="button"
                  class="imk-slot__remove imk-toggle"
                  :aria-label="`Déséquiper ${itemDisplayName(equipmentSlot(position).item!.itemKey)}`"
                  :title="itemDetails(equipmentSlot(position).item!.itemKey).join(' · ') || 'Déséquiper'"
                  :disabled="playerStore.isLoading || combatLocked"
                  @click="unequip(equipmentSlot(position).item!)"
                >×</button>
                <button
                  type="button"
                  class="imk-slot__browse"
                  :aria-label="`Choisir un objet pour ${equipmentSlot(position).label}`"
                  :aria-expanded="openPosition === position"
                  :disabled="combatLocked"
                  @focus="openSlotPicker(position)"
                  @click.stop="toggleSlotPicker(position)"
                >⌄</button>
                <div
                  v-if="openPosition === position"
                  class="imk-slot-picker"
                  role="listbox"
                  :aria-label="`Objets équipables : ${equipmentSlot(position).label}`"
                >
                  <header>
                    <strong>{{ equipmentSlot(position).label }}</strong>
                    <small>{{ slotCandidates(position).length }} compatible{{ slotCandidates(position).length > 1 ? 's' : '' }}</small>
                  </header>
                  <ul v-if="slotCandidates(position).length">
                    <li v-for="item in slotCandidates(position)" :key="item.itemInstanceId ?? item.itemDefinitionKey">
                      <button
                        type="button"
                        class="imk-slot-picker__item"
                        role="option"
                        :aria-selected="false"
                        :disabled="playerStore.isLoading || combatLocked"
                        @mouseenter="previewCandidate(item, position, $event)"
                        @mousemove="trackComparisonPointer"
                        @mouseleave="clearComparison"
                        @focus="previewCandidate(item, position, $event)"
                        @click="requestEquip(item, position)"
                      >
                        <span class="imk-slot-picker__glyph" aria-hidden="true">{{ itemGlyph(item.itemDefinitionKey, position) }}</span>
                        <span><strong>{{ itemDisplayName(item.itemDefinitionKey) }}</strong><small>{{ definition(item.itemDefinitionKey)?.rarity ?? 'Objet' }}</small></span>
                        <span aria-hidden="true">›</span>
                      </button>
                    </li>
                  </ul>
                  <p v-else>Aucun objet compatible dans l'inventaire.</p>
                </div>
              </article>
            </li>
          </ul>

          <div class="imk-avatar-stage" aria-hidden="true">
            <span class="imk-avatar-stage__halo"></span>
            <svg class="imk-avatar-stage__figure" viewBox="0 0 210 360" role="presentation">
              <path class="imk-avatar-stage__cloak" d="M105 72c-24 0-40 20-40 48v46L27 292c18 32 52 52 78 52s60-20 78-52l-38-126v-46c0-28-16-48-40-48Z" />
              <path class="imk-avatar-stage__body" d="M78 144h54l19 135c-27 18-65 18-92 0l19-135Z" />
              <circle class="imk-avatar-stage__head" cx="105" cy="50" r="29" />
              <path class="imk-avatar-stage__hood" d="M67 55c2-34 18-51 38-51 22 0 40 21 40 58l-22-16-18-21-16 22-22 8Z" />
              <path class="imk-avatar-stage__mark" d="M105 164v87M78 205h54" />
            </svg>
            <span class="imk-avatar-stage__name">{{ character.displayName }}</span>
            <span class="imk-avatar-stage__archetype">{{ archetypeName() }}</span>
          </div>

          <ul class="imk-slot-column imk-slot-column--right">
            <li v-for="position in rightPositions" :key="position">
              <article
                class="imk-slot"
                :class="{
                  'imk-slot--empty': !equipmentSlot(position).item,
                  'imk-slot--picker-open': openPosition === position,
                }"
                :data-equipment-position="position"
                :style="equipmentSlot(position).item
                  ? { '--slot-accent': itemTypeAccent(equipmentSlot(position).item!.itemKey).color }
                  : undefined"
                @mouseenter="openSlotPicker(position)"
                @mouseleave="closeSlotPicker(position)"
                @mousemove="trackComparisonPointer"
                @focusout="handleSlotFocusOut($event, position)"
              >
                <span class="imk-slot__glyph" aria-hidden="true">
                  {{ equipmentSlot(position).item ? itemGlyph(equipmentSlot(position).item!.itemKey, position) : positionGlyphs[position] }}
                </span>
                <span class="imk-slot__content">
                  <span class="imk-slot__label">{{ equipmentSlot(position).label }}</span>
                  <strong v-if="equipmentSlot(position).item" class="imk-slot__name">
                    {{ itemDisplayName(equipmentSlot(position).item!.itemKey) }}
                  </strong>
                  <small v-else>Libre</small>
                </span>
                <small v-if="equipmentSlot(position).item && itemDetails(equipmentSlot(position).item!.itemKey).length" class="imk-slot__details">
                  {{ itemDetails(equipmentSlot(position).item!.itemKey).join(' · ') }}
                </small>
                <button
                  v-if="equipmentSlot(position).item"
                  type="button"
                  class="imk-slot__remove imk-toggle"
                  :aria-label="`Déséquiper ${itemDisplayName(equipmentSlot(position).item!.itemKey)}`"
                  :title="itemDetails(equipmentSlot(position).item!.itemKey).join(' · ') || 'Déséquiper'"
                  :disabled="playerStore.isLoading || combatLocked"
                  @click="unequip(equipmentSlot(position).item!)"
                >×</button>
                <button
                  type="button"
                  class="imk-slot__browse"
                  :aria-label="`Choisir un objet pour ${equipmentSlot(position).label}`"
                  :aria-expanded="openPosition === position"
                  :disabled="combatLocked"
                  @focus="openSlotPicker(position)"
                  @click.stop="toggleSlotPicker(position)"
                >⌄</button>
                <div
                  v-if="openPosition === position"
                  class="imk-slot-picker"
                  role="listbox"
                  :aria-label="`Objets équipables : ${equipmentSlot(position).label}`"
                >
                  <header>
                    <strong>{{ equipmentSlot(position).label }}</strong>
                    <small>{{ slotCandidates(position).length }} compatible{{ slotCandidates(position).length > 1 ? 's' : '' }}</small>
                  </header>
                  <ul v-if="slotCandidates(position).length">
                    <li v-for="item in slotCandidates(position)" :key="item.itemInstanceId ?? item.itemDefinitionKey">
                      <button
                        type="button"
                        class="imk-slot-picker__item"
                        role="option"
                        :aria-selected="false"
                        :disabled="playerStore.isLoading || combatLocked"
                        @mouseenter="previewCandidate(item, position, $event)"
                        @mousemove="trackComparisonPointer"
                        @mouseleave="clearComparison"
                        @focus="previewCandidate(item, position, $event)"
                        @click="requestEquip(item, position)"
                      >
                        <span class="imk-slot-picker__glyph" aria-hidden="true">{{ itemGlyph(item.itemDefinitionKey, position) }}</span>
                        <span><strong>{{ itemDisplayName(item.itemDefinitionKey) }}</strong><small>{{ definition(item.itemDefinitionKey)?.rarity ?? 'Objet' }}</small></span>
                        <span aria-hidden="true">›</span>
                      </button>
                    </li>
                  </ul>
                  <p v-else>Aucun objet compatible dans l'inventaire.</p>
                </div>
              </article>
            </li>
          </ul>
        </div>
      </section>

      <aside class="imk-stat-panel" aria-label="Statistiques effectives">
        <header class="imk-stat-panel__header">
          <span>Valeurs effectives</span>
          <small>Équipement inclus</small>
        </header>
        <section v-for="group in statisticGroups" :key="group.label" class="imk-stat-group">
          <h4>{{ group.label }}</h4>
          <dl>
            <div v-for="stat in group.items" :key="stat.key" class="imk-stat-row">
              <dt><span aria-hidden="true">{{ stat.glyph }}</span>{{ stat.label }}</dt>
              <dd>
                {{ stat.value }}
                <small v-if="statDelta(stat.key, stat.value)" :class="statDelta(stat.key, stat.value) > 0 ? 'is-positive' : 'is-negative'">
                  {{ statDelta(stat.key, stat.value) > 0 ? '+' : '' }}{{ statDelta(stat.key, stat.value) }}
                </small>
              </dd>
            </div>
          </dl>
        </section>
      </aside>
    </div>

    <section class="imk-section imk-inventory-panel">
      <header class="imk-inventory-header">
        <div>
          <span class="imk-profile-header__eyebrow">Sac permanent</span>
          <h4 class="imk-section__title">Inventaire de {{ character.displayName }}</h4>
        </div>
        <span>{{ equippablePermanentItems.length }} objet{{ equippablePermanentItems.length > 1 ? 's' : '' }}</span>
      </header>
      <ul v-if="equippablePermanentItems.length" class="imk-list imk-inventory-grid" aria-label="Inventaire du personnage">
        <li v-for="item in equippablePermanentItems" :key="item.itemInstanceId ?? item.itemDefinitionKey"
          class="imk-row imk-item-card"
          :class="{ 'imk-item-card--equipped': equippedAssignment(item) }"
          :style="{ '--item-accent': itemTypeAccent(item.itemDefinitionKey).color }">
          <span class="imk-item-card__icon" aria-hidden="true">{{ itemGlyph(item.itemDefinitionKey, equippedAssignment(item)?.position ?? undefined) }}</span>
          <div class="imk-row__info">
            <span class="imk-row__name">{{ itemDisplayName(item.itemDefinitionKey) }}</span>
            <small class="imk-row__effect">{{ allowedPositions(item.itemDefinitionKey).map((p) => visiblePositions.find((v) => v.key === p)?.label).join(' · ') || 'Objet permanent' }}</small>
          </div>
          <button v-if="!equippedAssignment(item)" type="button" class="imk-toggle"
            :disabled="playerStore.isLoading || combatLocked || legacyLoadoutFull(item)"
            @click="requestEquip(item)">Aperçu</button>
          <span v-else class="imk-row__slot">Équipé</span>
        </li>
      </ul>
      <p v-else class="imk-empty">Ce personnage ne possède encore aucun objet permanent.</p>
    </section>

    <aside
      v-if="comparisonItemKey && (comparisonLoading || comparisonPlan || comparisonError)"
      class="imk-comparison-tooltip"
      :style="comparisonStyle"
      aria-live="polite"
    >
      <template v-if="comparisonPlan">
        <header>
          <span>Comparatif</span>
          <strong>{{ comparisonPlan.candidateItem.displayName }}</strong>
        </header>
        <p v-if="comparisonPlan.currentlyEquippedItem" class="imk-comparison-tooltip__replacement">
          Remplace {{ comparisonPlan.currentlyEquippedItem.displayName }}
        </p>
        <ul v-if="comparisonPlan.statDeltas.some((delta) => delta.delta !== 0)">
          <li v-for="delta in comparisonPlan.statDeltas.filter((entry) => entry.delta !== 0)" :key="delta.stat">
            <span>{{ statLabel(delta.stat) }}</span>
            <span>{{ delta.current }} → {{ delta.projected }}</span>
            <strong :class="delta.delta > 0 ? 'is-positive' : 'is-negative'">
              {{ delta.delta > 0 ? '+' : '' }}{{ delta.delta }}
            </strong>
          </li>
        </ul>
        <p v-else>Aucune variation de statistiques.</p>
        <p v-for="skill in comparisonPlan.gainedTemporarySkills" :key="`hover-gain-${skill}`" class="is-positive">
          + Compétence : {{ skill }}
        </p>
        <p v-for="skill in comparisonPlan.lostTemporarySkills" :key="`hover-loss-${skill}`" class="is-negative">
          − Compétence : {{ skill }}
        </p>
        <p v-if="!comparisonPlan.canEquip" class="imk-error">
          {{ comparisonPlan.blockingReasons.join(' · ') }}
        </p>
        <small>Cliquez pour afficher la confirmation.</small>
      </template>
      <p v-else-if="comparisonLoading" class="imk-comparison-tooltip__loading">Calcul du comparatif…</p>
      <p v-else class="imk-error">{{ comparisonError }}</p>
    </aside>

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
.imk-root { display: flex; flex-direction: column; gap: 18px; }

.imk-profile-layout {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 220px;
  gap: 12px;
  align-items: stretch;
}

.imk-section,
.imk-stat-panel {
  border: 1px solid var(--line-soft);
  background: linear-gradient(135deg, rgb(191 227 224 / 3%), transparent 34%), var(--panel);
  box-shadow: 0 18px 44px rgb(0 0 0 / 18%);
}

.imk-profile-header,
.imk-inventory-header,
.imk-stat-panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  min-height: 58px;
  padding: 10px 14px;
  border-bottom: 1px solid var(--line-soft);
  background: linear-gradient(90deg, rgb(191 227 224 / 7%), transparent 62%);
}

.imk-profile-header h3,
.imk-inventory-header h4 {
  margin: 2px 0 0;
  font-family: var(--font-display);
  font-size: 21px;
  font-weight: 500;
  color: var(--ink);
}

.imk-profile-header__eyebrow,
.imk-section__title,
.imk-stat-panel__header > span {
  margin: 0;
  font-family: var(--font-mono);
  font-size: 9px;
  letter-spacing: .12em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.imk-profile-header__meta {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 3px;
  font-family: var(--font-mono);
  font-size: 9px;
  text-transform: uppercase;
  letter-spacing: .08em;
  color: var(--ink-4);
}

.imk-profile-header__meta strong { color: var(--mint-dim); font-size: 11px; }

.imk-paper-doll {
  display: grid;
  grid-template-columns: minmax(105px, 1fr) minmax(190px, 1.55fr) minmax(105px, 1fr);
  gap: 10px;
  min-height: 520px;
  padding: 14px;
  background:
    radial-gradient(circle at 50% 42%, rgb(191 227 224 / 10%), transparent 34%),
    repeating-linear-gradient(90deg, transparent 0 49px, rgb(255 255 255 / 1.5%) 50px),
    var(--panel-2);
}

.imk-slot-column {
  position: relative;
  z-index: 2;
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  grid-template-rows: repeat(7, minmax(58px, 1fr));
  gap: 6px;
}

.imk-slot-column li { min-width: 0; }

.imk-slot {
  --slot-accent: var(--line-strong);
  position: relative;
  height: 100%;
  min-width: 0;
  display: grid;
  grid-template-columns: 32px minmax(0, 1fr) 22px;
  align-items: center;
  gap: 7px;
  padding: 6px 5px 6px 7px;
  border: 1px solid var(--line-soft);
  border-inline-start: 2px solid var(--slot-accent);
  background: rgb(12 13 18 / 76%);
  transition: border-color .15s, background .15s, transform .15s;
}

.imk-slot:not(.imk-slot--empty):hover {
  border-color: var(--slot-accent);
  background: var(--raise);
  transform: translateY(-1px);
}

.imk-slot--picker-open {
  z-index: 12;
  border-color: var(--slot-accent);
  background: var(--raise);
  opacity: 1;
}

.imk-slot-column--right .imk-slot {
  grid-template-columns: 22px minmax(0, 1fr) 32px;
  border-inline-start: 1px solid var(--line-soft);
  border-inline-end: 2px solid var(--slot-accent);
  padding-inline: 5px 7px;
}

.imk-slot-column--right .imk-slot__glyph { grid-column: 3; grid-row: 1; }
.imk-slot-column--right .imk-slot__content {
  grid-column: 2;
  grid-row: 1;
  text-align: right;
  align-items: flex-end;
}
.imk-slot-column--right .imk-slot__remove { grid-column: 1; grid-row: 1; }

.imk-slot--empty { border-style: dashed; opacity: .58; }
.imk-slot--empty.imk-slot--picker-open { opacity: 1; }

.imk-slot__glyph,
.imk-item-card__icon {
  display: grid;
  place-items: center;
  aspect-ratio: 1;
  border: 1px solid var(--line-soft);
  background: linear-gradient(145deg, var(--raise), var(--panel));
  color: var(--slot-accent, var(--item-accent, var(--ink-4)));
  font-family: var(--font-display);
  font-size: 18px;
}

.imk-slot__content {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.imk-slot__label,
.imk-row__slot {
  font-family: var(--font-mono);
  font-size: 8px;
  letter-spacing: .1em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.imk-slot__content small { font-size: 9px; color: var(--ink-4); }
.imk-slot__name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 10px;
  font-weight: 500;
  color: var(--ink-2);
}

.imk-slot__details {
  position: absolute;
  z-index: 5;
  left: 5px;
  top: calc(100% + 5px);
  width: min(230px, 72vw);
  padding: 8px 9px;
  border: 1px solid var(--line-strong);
  background: var(--raise);
  box-shadow: var(--shadow-deep);
  color: var(--ink-3);
  font-size: 9px;
  line-height: 1.45;
  pointer-events: none;
  opacity: 0;
  transform: translateY(-3px);
  transition: opacity .15s, transform .15s;
}

.imk-slot-column--right .imk-slot__details { left: auto; right: 5px; }
.imk-slot:not(.imk-slot--picker-open):hover .imk-slot__details,
.imk-slot:not(.imk-slot--picker-open):focus-within .imk-slot__details { opacity: 1; transform: translateY(0); }

.imk-slot__remove {
  width: 21px;
  height: 21px;
  padding: 0;
  display: grid;
  place-items: center;
  border-color: transparent;
  font-size: 15px;
}

.imk-slot__browse {
  position: absolute;
  right: 2px;
  bottom: 2px;
  width: 17px;
  height: 17px;
  padding: 0;
  display: grid;
  place-items: center;
  border: 0;
  background: transparent;
  color: var(--ink-5);
  font-size: 12px;
  cursor: pointer;
}
.imk-slot__browse:hover,
.imk-slot__browse:focus-visible,
.imk-slot__browse[aria-expanded='true'] { color: var(--mint-dim); outline: none; }
.imk-slot__browse:disabled { opacity: .3; cursor: not-allowed; }
.imk-slot-column--right .imk-slot__browse { right: auto; left: 2px; }

.imk-slot-picker {
  position: absolute;
  z-index: 20;
  top: -1px;
  left: calc(100% - 1px);
  width: min(270px, 72vw);
  max-height: 310px;
  overflow: auto;
  border: 1px solid var(--line-strong);
  background: var(--raise);
  box-shadow: var(--shadow-deep);
  color: var(--ink-2);
}
.imk-slot-column--right .imk-slot-picker { left: auto; right: calc(100% - 1px); }
.imk-slot-picker > header {
  position: sticky;
  z-index: 1;
  top: 0;
  display: flex;
  justify-content: space-between;
  gap: 8px;
  padding: 8px 9px;
  border-bottom: 1px solid var(--line-soft);
  background: var(--panel);
}
.imk-slot-picker > header strong {
  font-family: var(--font-mono);
  font-size: 9px;
  letter-spacing: .1em;
  text-transform: uppercase;
}
.imk-slot-picker > header small { color: var(--ink-5); font-size: 8px; }
.imk-slot-picker ul { list-style: none; margin: 0; padding: 4px; }
.imk-slot-picker li + li { border-top: 1px solid var(--line-soft); }
.imk-slot-picker > p { margin: 0; padding: 14px 10px; color: var(--ink-4); font-size: 9px; }
.imk-slot-picker__item {
  width: 100%;
  display: grid;
  grid-template-columns: 30px minmax(0, 1fr) auto;
  align-items: center;
  gap: 8px;
  padding: 7px 6px;
  border: 0;
  background: transparent;
  color: inherit;
  text-align: left;
  cursor: pointer;
}
.imk-slot-picker__item:hover,
.imk-slot-picker__item:focus-visible { background: rgb(143 196 191 / 9%); outline: 1px solid rgb(143 196 191 / 26%); }
.imk-slot-picker__item:disabled { opacity: .42; cursor: not-allowed; }
.imk-slot-picker__item > span:nth-child(2) { min-width: 0; display: flex; flex-direction: column; gap: 2px; }
.imk-slot-picker__item strong { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-size: 10px; font-weight: 500; }
.imk-slot-picker__item small { color: var(--ink-5); font-size: 8px; }
.imk-slot-picker__glyph {
  display: grid;
  place-items: center;
  width: 30px;
  aspect-ratio: 1;
  border: 1px solid var(--line-soft);
  color: var(--mint-dim);
  font-family: var(--font-display);
}

.imk-avatar-stage {
  position: relative;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: flex-end;
  min-width: 0;
  border: 1px solid var(--line-soft);
  background:
    linear-gradient(180deg, transparent 62%, rgb(12 13 18 / 92%)),
    radial-gradient(ellipse at 50% 92%, rgb(143 196 191 / 22%), transparent 42%);
}

.imk-avatar-stage::before,
.imk-avatar-stage::after {
  content: '';
  position: absolute;
  inset: 8% 18%;
  border: 1px solid rgb(191 227 224 / 10%);
  border-radius: 50% 50% 8% 8%;
}
.imk-avatar-stage::after { inset: 12% 24% 18%; border-color: rgb(191 227 224 / 6%); }

.imk-avatar-stage__halo {
  position: absolute;
  left: 50%;
  bottom: 52px;
  width: 72%;
  aspect-ratio: 4 / 1;
  translate: -50% 0;
  border: 1px solid rgb(143 196 191 / 52%);
  border-radius: 50%;
  box-shadow: 0 0 30px rgb(143 196 191 / 15%);
}

.imk-avatar-stage__figure {
  position: absolute;
  z-index: 1;
  bottom: 60px;
  width: min(90%, 225px);
  height: calc(100% - 82px);
  filter: drop-shadow(0 18px 24px rgb(0 0 0 / 45%));
}

.imk-avatar-stage__cloak { fill: #252b30; stroke: rgb(191 227 224 / 30%); stroke-width: 1.5; }
.imk-avatar-stage__body { fill: #171b20; stroke: rgb(166 163 156 / 34%); stroke-width: 1; }
.imk-avatar-stage__head { fill: #1b2025; stroke: rgb(191 227 224 / 26%); }
.imk-avatar-stage__hood { fill: #30373b; stroke: rgb(191 227 224 / 34%); }
.imk-avatar-stage__mark { fill: none; stroke: rgb(143 196 191 / 62%); stroke-width: 1.3; }

.imk-avatar-stage__name,
.imk-avatar-stage__archetype { position: relative; z-index: 2; }
.imk-avatar-stage__name {
  margin-bottom: 2px;
  font-family: var(--font-display);
  font-size: 17px;
  color: var(--ink-2);
}
.imk-avatar-stage__archetype {
  margin-bottom: 12px;
  font-family: var(--font-mono);
  font-size: 8px;
  letter-spacing: .14em;
  text-transform: uppercase;
  color: var(--mint-dim);
}

.imk-stat-panel { min-width: 0; }
.imk-stat-panel__header {
  align-items: flex-start;
  flex-direction: column;
  justify-content: center;
  gap: 2px;
}
.imk-stat-panel__header small { color: var(--mint-dim); font-size: 9px; }
.imk-stat-group { padding: 12px 13px 8px; border-bottom: 1px solid var(--line-soft); }
.imk-stat-group:last-child { border-bottom: 0; }
.imk-stat-group h4 {
  margin: 0 0 8px;
  font-family: var(--font-mono);
  font-size: 8px;
  letter-spacing: .13em;
  text-transform: uppercase;
  color: var(--ink-5);
}
.imk-stat-group dl { margin: 0; }
.imk-stat-row {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 10px;
  padding: 4px 0;
}
.imk-stat-row dt { display: flex; gap: 7px; font-size: 10px; color: var(--ink-3); }
.imk-stat-row dt span { width: 12px; color: var(--mint-dim); text-align: center; }
.imk-stat-row dd { margin: 0; font-family: var(--font-mono); font-size: 11px; color: var(--ink-2); }
.imk-stat-row dd small { margin-left: 3px; font-size: 8px; }
.imk-stat-row .is-positive { color: var(--mint-dim); }
.imk-stat-row .is-negative { color: var(--danger-dim); }

.imk-inventory-panel { display: flex; flex-direction: column; }
.imk-inventory-header > span { font-family: var(--font-mono); font-size: 9px; color: var(--ink-4); }
.imk-list { list-style: none; margin: 0; padding: 14px; }
.imk-inventory-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(132px, 1fr));
  gap: 8px;
}
.imk-row { min-width: 0; }
.imk-item-card {
  --item-accent: var(--line-strong);
  position: relative;
  display: grid;
  grid-template-columns: 42px minmax(0, 1fr);
  grid-template-rows: auto auto;
  gap: 8px 9px;
  align-items: center;
  min-height: 106px;
  padding: 9px;
  border: 1px solid var(--line-soft);
  border-top: 2px solid var(--item-accent);
  background: var(--panel-2);
}
.imk-item-card--equipped { background: linear-gradient(145deg, rgb(143 196 191 / 9%), var(--panel-2)); }
.imk-item-card__icon { --slot-accent: var(--item-accent); width: 42px; grid-row: 1; }
.imk-row__info { min-width: 0; display: flex; flex-direction: column; gap: 3px; }
.imk-row__name { overflow: hidden; text-overflow: ellipsis; font-size: 11px; color: var(--ink-2); }
.imk-row__effect,
.imk-row__contract { display: block; color: var(--ink-4); font-size: 8.5px; line-height: 1.35; }
.imk-item-card > .imk-toggle,
.imk-item-card > .imk-row__slot { grid-column: 1 / -1; justify-self: stretch; text-align: center; }

.imk-toggle {
  flex-shrink: 0;
  font-family: var(--font-mono);
  font-size: 8.5px;
  letter-spacing: .12em;
  text-transform: uppercase;
  padding: 5px 9px;
  border: 1px solid var(--line-soft);
  background: transparent;
  color: var(--ink-4);
  cursor: pointer;
  transition: opacity .15s, border-color .15s, color .15s;
}
.imk-toggle:disabled { opacity: .38; cursor: not-allowed; }
.imk-toggle:not(:disabled):hover { border-color: var(--ink-3); color: var(--ink-2); }
.imk-toggle--active { border-color: var(--mint-dim); color: var(--mint-dim); background: var(--panel); }
.imk-row__slot { padding: 5px; border: 1px solid rgb(143 196 191 / 24%); color: var(--mint-dim); }

.imk-empty { margin: 0; padding: 28px 14px; font-size: 11px; color: var(--ink-4); font-style: italic; }
.imk-error { margin: 0; font-family: var(--font-mono); font-size: 11px; color: var(--danger-dim); }

.imk-comparison-tooltip {
  position: fixed;
  z-index: 80;
  width: min(300px, calc(100vw - 16px));
  max-height: min(312px, calc(100vh - 16px));
  overflow: auto;
  padding: 11px;
  border: 1px solid var(--line-strong);
  background: rgb(18 20 25 / 98%);
  box-shadow: var(--shadow-deep);
  color: var(--ink-3);
  pointer-events: none;
}
.imk-comparison-tooltip header { display: flex; flex-direction: column; gap: 2px; margin-bottom: 8px; }
.imk-comparison-tooltip header span {
  font-family: var(--font-mono);
  font-size: 8px;
  letter-spacing: .12em;
  text-transform: uppercase;
  color: var(--mint-dim);
}
.imk-comparison-tooltip header strong { color: var(--ink-2); font-size: 12px; }
.imk-comparison-tooltip ul { list-style: none; margin: 8px 0; padding: 0; }
.imk-comparison-tooltip li {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto 34px;
  gap: 7px;
  padding: 4px 0;
  border-bottom: 1px solid var(--line-soft);
  font-family: var(--font-mono);
  font-size: 9px;
}
.imk-comparison-tooltip li strong { text-align: right; }
.imk-comparison-tooltip p { margin: 6px 0; font-size: 9px; }
.imk-comparison-tooltip > small { color: var(--ink-5); font-size: 8px; }
.imk-comparison-tooltip__replacement { color: var(--ink-5); }
.imk-comparison-tooltip .is-positive { color: var(--mint-dim); }
.imk-comparison-tooltip .is-negative { color: var(--danger-dim); }
.imk-comparison-tooltip__loading { color: var(--ink-4); font-style: italic; }

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
.imk-preview__actions { display: flex; justify-content: flex-end; gap: 8px; }

@media (max-width: 820px) {
  .imk-profile-layout { grid-template-columns: 1fr; }
  .imk-stat-panel { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .imk-stat-panel__header { grid-column: 1 / -1; }
  .imk-stat-group:nth-child(odd) { border-inline-start: 1px solid var(--line-soft); }
}

@media (max-width: 590px) {
  .imk-paper-doll { grid-template-columns: repeat(2, minmax(0, 1fr)); min-height: auto; }
  .imk-avatar-stage { grid-column: 1 / -1; grid-row: 1; min-height: 330px; }
  .imk-slot-column { grid-row: 2; }
  .imk-slot-column--right .imk-slot {
    grid-template-columns: 32px minmax(0, 1fr) 22px;
    border-inline-start: 2px solid var(--slot-accent);
    border-inline-end: 1px solid var(--line-soft);
    padding-inline: 7px 5px;
  }
  .imk-slot-column--right .imk-slot__glyph,
  .imk-slot-column--right .imk-slot__content,
  .imk-slot-column--right .imk-slot__remove { grid-row: 1; }
  .imk-slot-column--right .imk-slot__glyph { grid-column: 1; }
  .imk-slot-column--right .imk-slot__content { grid-column: 2; text-align: left; align-items: flex-start; }
  .imk-slot-column--right .imk-slot__remove { grid-column: 3; }
  .imk-stat-panel { grid-template-columns: 1fr; }
  .imk-stat-group:nth-child(odd) { border-inline-start: 0; }
  .imk-inventory-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
</style>
