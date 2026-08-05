<script setup lang="ts">
import ChipBadge from '@/shared/components/ChipBadge.vue'
import EliseComment from '@/shared/components/EliseComment.vue'
import SealGlyph from '@/shared/components/SealGlyph.vue'
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

function sigilKind(tone: 'gold' | 'frost' | null): string {
  if (tone === 'gold') return 'rare'
  if (tone === 'frost') return 'memoire'
  return 'recompense'
}

function sealTone(tone: 'gold' | 'frost' | null): 'mint' | 'ink' {
  return tone ? 'mint' : 'ink'
}

// Choisir une carte n'est qu'une intention (déjà lisible via rop-card--sel) ; le sceau ne
// s'anime qu'au moment où ce choix devient irréversible — le clic sur « Emporter ».
function sealState(card: NormalizedCard): 'idle' | 'confirming' | 'confirmed' {
  if (selectedId.value !== card.id) return 'idle'
  if (isExpiredOrSelected.value) return 'confirmed'
  if (taken.value) return 'confirming'
  return 'idle'
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
      <div style="flex: 0 0 auto; text-align: center; padding-top: 12px">
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
        <span class="es-kicker">Une faveur, une seule</span>
        <h2 class="es-h2" style="font-size: 33px; margin-top: 12px">
          {{ offer.title ?? 'Le Palais reconnaît ta traversée' }}
        </h2>
        <p v-if="offer.description" class="es-body" style="margin-top: 8px; color: var(--ink-3); max-width: 560px; margin-left: auto; margin-right: auto">
          {{ offer.description }}
        </p>
        <p v-else class="es-body" style="margin-top: 8px; color: var(--ink-3); max-width: 560px; margin-left: auto; margin-right: auto">
          Choisis-en une. Le reste se referme et retourne au silence.
        </p>
      </div>

      <!-- Cartes -->
      <div class="rop-cards" style="flex: 1; display: flex; align-items: center; justify-content: center">
        <div
          v-if="normalizedCards.length === 0"
          style="text-align: center; color: var(--ink-4); padding: 48px"
        >
          <span class="es-body">Aucune récompense disponible.</span>
        </div>

        <div v-else class="rop-cards__grid">
          <div
            v-for="card in normalizedCards"
            :key="card.id"
            :class="[
              'rop-card',
              selectedId === card.id && 'rop-card--sel',
              selectedId === card.id && card.tone === 'gold' && 'rop-card--gold',
              isExpiredOrSelected && 'rop-card--frozen',
            ]"
            @click="selectCard(card.id)"
          >
            <div
              class="rop-card__pick"
              :style="selectedId === card.id
                ? { borderColor: 'var(--mint-dim)', color: 'var(--mint-dim)' }
                : {}"
            >{{ selectedId === card.id ? '✦' : '' }}</div>

            <div class="es-row" style="margin-bottom: 18px; flex-wrap: wrap; row-gap: 6px">
              <ChipBadge :tone="card.tone ? 'mint' : null">
                {{ rewardTypeLabel(card.rewardType) }}
              </ChipBadge>
              <span v-if="card.sourceEnemyDisplayName" class="rop-card__source">
                {{ card.sourceEnemyDisplayName }}
              </span>
            </div>

            <div style="display: flex; justify-content: center; margin: 4px 0 20px">
              <SealGlyph
                :kind="sigilKind(card.tone)"
                :tone="sealTone(card.tone)"
                :size="92"
                :sigil-size="38"
                :sigil-stroke-width="1.3"
                :state="sealState(card)"
              />
            </div>

            <h3
              class="es-h3"
              :style="{
                textAlign: 'center',
                color: selectedId === card.id ? 'var(--ink)' : 'var(--ink-2)',
              }"
            >
              {{ card.label }}
            </h3>

            <div class="es-label" style="text-align: center; margin-top: 6px; color: var(--ink-4)">
              {{ rewardTypeLabel(card.rewardType) }}
            </div>

            <div class="es-divider" style="margin: 18px 0" />

            <p class="es-body" style="font-size: 13.5px; text-align: center; flex: 1">
              {{ card.description }}
            </p>

            <p
              v-if="costLabel(card)"
              class="es-label"
              style="text-align: center; margin-top: 12px; color: var(--mint-dim)"
            >
              {{ costLabel(card) }}
            </p>
          </div>
        </div>
      </div>

      <p v-if="errorMessage" class="rop-purchase-error">{{ errorMessage }}</p>

      <!-- Footer actions -->
      <div v-if="!isExpiredOrSelected" class="es-row" style="justify-content: center; gap: 14px; flex: 0 0 auto; margin-top: 8px">
        <button
          :class="['es-btn', 'es-btn--lg', confirmBtnClass]"
          :disabled="selectedId == null || isLoading"
          :style="{
            opacity: selectedId == null ? 0.4 : 1,
            pointerEvents: selectedId == null ? 'none' : 'auto',
            minWidth: '320px',
          }"
          @click="confirmChoice"
        >
          {{ taken ? '✓ ' : '' }}Emporter « {{ chosen?.label ?? '—' }} » {{ taken ? '' : '→' }}
        </button>
      </div>
      <div v-else class="es-row" style="justify-content: center; flex: 0 0 auto; margin-top: 8px">
        <span class="rop-resolved-note">Cette récompense a déjà été résolue.</span>
      </div>

      <!-- Elise -->
      <EliseComment
        tell="à voix basse"
        style="position: absolute; bottom: 22px; right: 36px; max-width: 360px; z-index: 9"
      >
        Une relique porte toujours le nom de ce qu'on a tu.
        <em>Tu es sûr de vouloir l'emporter avec toi&nbsp;?</em>
      </EliseComment>
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
  padding: 32px 40px;
  gap: 8px;
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
  gap: 8px;
  /* Reward offers can carry more cards than fit a short viewport (merchant offers,
     combat loot with the defeated-enemies sidebar) — scroll instead of clipping
     content past the window edge. */
  overflow-y: auto;
}

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

/* ── Cards grid (wraps for 4-6 loot cards, single row for 3 or fewer) ── */
.rop-cards__grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 260px));
  gap: 20px;
  justify-content: center;
  align-items: stretch;
  max-width: 900px;
}

/* ── Reward card ── */
.rop-card {
  position: relative;
  width: 100%;
  padding: 26px 24px;
  display: flex;
  flex-direction: column;
  cursor: pointer;
  border: 1px solid var(--line);
  background: var(--panel-2);
  transition: border-color 0.24s, background 0.24s;
}

.rop-card:hover:not(.rop-card--frozen) {
  border-color: var(--mint-dim);
}

.rop-card--sel {
  border-color: var(--mint-dim);
  background: var(--panel);
}

.rop-card--frozen {
  cursor: default;
  opacity: 0.75;
}

/* Source-enemy tag: which enemy this loot dropped from (absent = generic/fallback) */
.rop-card__source {
  display: inline-block;
  font-family: var(--font-caps);
  font-size: 0.55rem;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--ink-4);
  border: 1px solid var(--line-soft);
  border-radius: 3px;
  padding: 2px 7px;
  background: oklch(0.22 0.03 60 / 0.6);
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Selection indicator dot */
.rop-card__pick {
  position: absolute;
  top: 14px;
  right: 14px;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--line);
  color: transparent;
  font-size: 12px;
  transition: all 0.2s;
}

.rop-purchase-error {
  text-align: center;
  color: var(--danger-dim);
  font-size: 0.8rem;
  margin: 4px 0 0;
}

.rop-resolved-note {
  font-family: var(--font-mono);
  font-size: 0.6rem;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-5);
}
</style>
