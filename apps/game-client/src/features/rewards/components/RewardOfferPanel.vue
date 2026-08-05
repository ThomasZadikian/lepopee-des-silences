<script setup lang="ts">
import ChipBadge from '@/shared/components/ChipBadge.vue'
import { computed, ref, watch } from 'vue'
import DefeatedEnemyList from './DefeatedEnemyList.vue'
import type { RewardOfferDto } from '../types/rewardTypes'

const props = withDefaults(
  defineProps<{
    offer: RewardOfferDto
    isLoading?: boolean
    errorMessage?: string | null
    /** The player's current currency balance — shown only when at least one card has a cost (merchant offers). */
    palaceShardCount?: number
    himLitShardCount?: number
  }>(),
  { palaceShardCount: 0, himLitShardCount: 0 },
)

const defeatedEnemies = computed(() => props.offer.defeatedEnemies ?? [])

const emit = defineEmits<{
  selectReward: [choiceId: string]
}>()

// ── Source / state labels ─────────────────────────────────────────────────
const SOURCE_LABELS: Record<string, string> = {
  NodeEvent:  'Événement',
  Combat:     'Combat',
  Elite:      'Élite',
  RoomBoss:   'Boss de salle',
  Rare:       'Rencontre rare',
}

const REWARD_TYPE_LABELS: Record<string, string> = {
  Heal:             'Soin',
  TemporaryItem:    'Objet temporaire',
  StatBonus:        'Bonus de stat',
  MemoryFragment:   'Fragment mémoriel',
  Decline:          'Renoncer',
}

const sourceLabel = computed(() =>
  props.offer.source ? (SOURCE_LABELS[props.offer.source] ?? props.offer.source) : null,
)

const isExpiredOrSelected = computed(() =>
  props.offer.state === 'Selected' || props.offer.state === 'Expired',
)

// ── Normalized card type ──────────────────────────────────────────────────
type NormalizedCard = {
  id: string
  label: string
  description: string
  rarity: string | undefined
  rewardType: string | undefined
  tone: 'gold' | 'frost' | null
  sourceEnemyDisplayName: string | null | undefined
  palaceShardCost: number
  himLitShardCost: number
}

function getTone(rarity?: string, rewardType?: string): 'gold' | 'frost' | null {
  const r = (rarity ?? '').toLowerCase()
  const t = (rewardType ?? '').toLowerCase()
  if (r.includes('relique') || r.includes('epic') || r.includes('épique')) return 'gold'
  if (r.includes('rare') || r.includes('mémoire')) return 'frost'
  if (t === 'memoryfragment') return 'frost'
  if (t === 'statbonus' || t === 'temporaryitem') return 'gold'
  return null
}

const normalizedCards = computed<NormalizedCard[]>(() => {
  const cards: NormalizedCard[] = []

  for (const c of props.offer.choices ?? []) {
    cards.push({
      id: c.id,
      label: c.label,
      description: c.description,
      rarity: c.rarity,
      rewardType: c.rewardType,
      tone: getTone(c.rarity, c.rewardType),
      sourceEnemyDisplayName: c.sourceEnemyDisplayName,
      palaceShardCost: c.palaceShardCost ?? 0,
      himLitShardCost: c.himLitShardCost ?? 0,
    })
  }

  // defensive: handle legacy `options` field
  for (const o of props.offer.options ?? []) {
    const id = o.id ?? o.key ?? o.rewardId ?? ''
    if (!id) continue
    cards.push({
      id,
      label: o.displayName ?? o.name ?? o.label ?? id,
      description: o.description ?? '',
      rarity: o.rarity,
      rewardType: o.rewardType ?? o.type,
      tone: getTone(o.rarity, o.rewardType ?? o.type),
      sourceEnemyDisplayName: null,
      palaceShardCost: 0,
      himLitShardCost: 0,
    })
  }

  return cards
})

const hasAnyCost = computed(() =>
  normalizedCards.value.some(c => c.palaceShardCost > 0 || c.himLitShardCost > 0),
)

// ── UI state ──────────────────────────────────────────────────────────────
const selectedId = ref<string | null>(null)
const taken = ref(false)

const chosen = computed<NormalizedCard | null>(() =>
  selectedId.value != null
    ? normalizedCards.value.find(c => c.id === selectedId.value) ?? null
    : null
)

function selectCard(id: string) {
  if (isExpiredOrSelected.value) return
  selectedId.value = id
  taken.value = false
}

function confirmChoice() {
  if (chosen.value == null || isExpiredOrSelected.value) return
  taken.value = true
  emit('selectReward', chosen.value.id)
}

// A failed purchase (insufficient funds) never selects the offer server-side —
// re-enable the confirm button so the player can pick a cheaper item or "Renoncer".
watch(() => props.errorMessage, (message) => {
  if (message) taken.value = false
})

function costLabel(card: NormalizedCard): string | null {
  if (card.palaceShardCost <= 0 && card.himLitShardCost <= 0) return null
  const parts: string[] = []
  if (card.palaceShardCost > 0) parts.push(`${card.palaceShardCost} Éclats du Palais`)
  if (card.himLitShardCost > 0) parts.push(`${card.himLitShardCost} Éclats de Him'Lit`)
  return parts.join(' · ')
}

function rewardTypeLabel(rewardType?: string): string {
  return rewardType ? (REWARD_TYPE_LABELS[rewardType] ?? rewardType) : 'Faveur'
}

const confirmBtnClass = 'es-btn--mint'
</script>

<template>
  <div class="rop-screen">
    <div class="rop-content" :class="{ 'rop-content--with-sidebar': defeatedEnemies.length > 0 }">

      <DefeatedEnemyList
        v-if="defeatedEnemies.length > 0"
        :enemies="defeatedEnemies"
      />

      <div class="rop-main">

      <!-- Header -->
      <div class="rop-header">
        <div class="rop-meta">
          <span v-if="sourceLabel" class="rop-source-chip">{{ sourceLabel }}</span>
          <span v-if="isExpiredOrSelected" class="rop-state-chip">
            {{ offer.state === 'Selected' ? 'Sélectionné' : 'Expiré' }}
          </span>
          <span v-if="hasAnyCost" class="rop-currency">
            <span class="rop-currency__value">{{ palaceShardCount }}</span> Éclats du Palais
            <span class="rop-currency__sep">·</span>
            <span class="rop-currency__value">{{ himLitShardCount }}</span> Éclats de Him'Lit
          </span>
        </div>
        <h2 class="rop-title">{{ offer.title ?? 'Le Palais reconnaît ta traversée' }}</h2>
      </div>

      <!-- Cartes -->
      <div v-if="normalizedCards.length === 0" class="rop-empty">
        Aucune récompense disponible.
      </div>

      <div v-else class="rop-cards__grid">
        <button
          v-for="card in normalizedCards"
          :key="card.id"
          type="button"
          :class="[
            'rop-card',
            selectedId === card.id && 'rop-card--sel',
            isExpiredOrSelected && 'rop-card--frozen',
          ]"
          :disabled="isExpiredOrSelected"
          @click="selectCard(card.id)"
        >
          <div class="rop-card__top">
            <ChipBadge :tone="card.tone ? 'mint' : null">{{ rewardTypeLabel(card.rewardType) }}</ChipBadge>
            <span v-if="selectedId === card.id" class="rop-card__pick">✓</span>
          </div>
          <span v-if="card.sourceEnemyDisplayName" class="rop-card__source">{{ card.sourceEnemyDisplayName }}</span>
          <h3 class="rop-card__name">{{ card.label }}</h3>
          <p v-if="card.description" class="rop-card__desc">{{ card.description }}</p>
          <p v-if="costLabel(card)" class="rop-card__cost">{{ costLabel(card) }}</p>
        </button>
      </div>

      <p v-if="errorMessage" class="rop-purchase-error">{{ errorMessage }}</p>

      <!-- Footer actions -->
      <div v-if="!isExpiredOrSelected" class="rop-footer">
        <button
          :class="['es-btn', 'es-btn--lg', confirmBtnClass]"
          :disabled="selectedId == null || isLoading"
          :style="{
            opacity: selectedId == null ? 0.4 : 1,
            pointerEvents: selectedId == null ? 'none' : 'auto',
            minWidth: '260px',
          }"
          @click="confirmChoice"
        >
          {{ taken ? '✓ ' : '' }}Emporter « {{ chosen?.label ?? '—' }} » {{ taken ? '' : '→' }}
        </button>
      </div>
      <div v-else class="rop-footer">
        <span class="rop-resolved-note">Cette récompense a déjà été résolue.</span>
      </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.rop-screen {
  position: relative;
  width: 100%;
  background: var(--panel);
  border: 1px solid var(--line);
  color: var(--ink);
  font-family: var(--font);
  -webkit-font-smoothing: antialiased;
}

.rop-content {
  position: relative;
  display: flex;
  flex-direction: column;
  padding: 28px 32px;
  gap: 18px;
}

/* When enemies were defeated (combat rewards), lay the sidebar and the
   existing centered content side by side instead of stacking everything
   in a single centered column. */
.rop-content--with-sidebar {
  flex-direction: row;
  align-items: stretch;
  gap: 32px;
}

.rop-main {
  flex: 1;
  min-width: 0;
  min-height: 0;
  display: flex;
  flex-direction: column;
  gap: 18px;
  overflow-y: auto;
}

.rop-header { text-align: center; }

/* ── Meta row (source + state) ── */
.rop-meta {
  display: flex;
  justify-content: center;
  gap: 8px;
  margin-bottom: 8px;
  min-height: 22px;
}

.rop-source-chip {
  font-family: var(--font-mono);
  font-size: 0.6rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--ink-4);
  border: 1px solid var(--line-soft);
  padding: 2px 8px;
}

.rop-state-chip {
  font-family: var(--font-mono);
  font-size: 0.6rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--mint-dim);
  border: 1px solid var(--mint-dim);
  padding: 2px 8px;
}

.rop-currency {
  font-family: var(--font-mono);
  font-size: 0.68rem;
  letter-spacing: 0.02em;
  color: var(--ink-4);
  border: 1px solid var(--line-soft);
  padding: 2px 9px;
}

.rop-currency__sep { margin: 0 4px; opacity: 0.5; }
.rop-currency__value { color: var(--ink-2); }

.rop-title {
  margin: 0;
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  font-size: 26px;
  color: var(--ink);
}

.rop-empty {
  text-align: center;
  color: var(--ink-4);
  font-size: 13px;
  padding: 24px;
}

/* ── Cartes compactes — un popup, pas un écran ── */
.rop-cards__grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 200px));
  gap: 12px;
  justify-content: center;
  align-items: stretch;
}

.rop-card {
  all: unset;
  box-sizing: border-box;
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 14px 16px;
  cursor: pointer;
  border: 1px solid var(--line);
  background: var(--panel-2);
  transition: border-color 0.2s;
}

.rop-card:hover:not(.rop-card--frozen) { border-color: var(--mint-dim); }
.rop-card--sel { border-color: var(--mint-dim); background: var(--panel); }
.rop-card--frozen { cursor: default; opacity: 0.6; }

.rop-card__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.rop-card__pick {
  color: var(--mint-dim);
  font-size: 13px;
}

/* Source-enemy tag: which enemy this loot dropped from (absent = generic/fallback) */
.rop-card__source {
  font-family: var(--font-mono);
  font-size: 9px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--ink-5);
}

.rop-card__name {
  margin: 0;
  font-family: var(--font-display);
  font-style: italic;
  font-size: 15px;
  color: var(--ink);
}

.rop-card__desc {
  margin: 0;
  font-size: 11.5px;
  line-height: 1.4;
  color: var(--ink-3);
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.rop-card__cost {
  margin: 2px 0 0;
  font-family: var(--font-mono);
  font-size: 10px;
  color: var(--mint-dim);
}

.rop-purchase-error {
  text-align: center;
  color: var(--danger-dim);
  font-size: 0.8rem;
  margin: 0;
}

.rop-footer {
  display: flex;
  justify-content: center;
}

.rop-resolved-note {
  font-family: var(--font-mono);
  font-size: 0.6rem;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-5);
}
</style>
