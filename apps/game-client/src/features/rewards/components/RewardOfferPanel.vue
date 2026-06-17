<script setup lang="ts">
import ChipBadge from '@/shared/components/ChipBadge.vue'
import EliseComment from '@/shared/components/EliseComment.vue'
import RuleOrnament from '@/shared/components/RuleOrnament.vue'
import SigilIcon from '@/shared/components/SigilIcon.vue'
import { computed, ref } from 'vue'
import type { RewardOfferDto } from '../types/rewardTypes'

const props = defineProps<{
  offer: RewardOfferDto
  isLoading?: boolean
}>()

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
    })
  }

  return cards
})

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

function rewardTypeLabel(rewardType?: string): string {
  return rewardType ? (REWARD_TYPE_LABELS[rewardType] ?? rewardType) : 'Faveur'
}

function sigilKind(tone: 'gold' | 'frost' | null): string {
  if (tone === 'gold') return 'rare'
  if (tone === 'frost') return 'memoire'
  return 'recompense'
}

function accentOf(tone: 'gold' | 'frost' | null): string {
  if (tone === 'gold') return 'var(--gold)'
  if (tone === 'frost') return 'var(--frost)'
  return 'var(--ink-3)'
}

function cardShadow(card: NormalizedCard, selected: boolean): string {
  if (!selected) return 'none'
  return card.tone === 'gold'
    ? '0 0 46px -12px oklch(0.74 0.12 84 / 0.6), var(--shadow-deep, 0 12px 40px -18px oklch(0.05 0 0 / 0.8))'
    : '0 0 46px -12px var(--wash-frost, oklch(0.6 0.1 195 / 0.45)), var(--shadow-deep, 0 12px 40px -18px oklch(0.05 0 0 / 0.8))'
}

const confirmBtnClass = computed(() =>
  chosen.value?.tone === 'gold' ? 'es-btn--gold' : 'es-btn--frost'
)
</script>

<template>
  <div class="rop-screen">
    <div class="es-atmos" />
    <div class="es-vignette" />
    <div class="es-grain" />

    <div class="rop-content">

      <!-- Header -->
      <div style="flex: 0 0 auto; text-align: center; padding-top: 12px">
        <div class="rop-meta">
          <span v-if="sourceLabel" class="rop-source-chip">{{ sourceLabel }}</span>
          <span v-if="isExpiredOrSelected" class="rop-state-chip">
            {{ offer.state === 'Selected' ? 'Sélectionné' : 'Expiré' }}
          </span>
        </div>
        <span class="es-kicker">Une faveur, une seule</span>
        <RuleOrnament :frost="true" style="width: 150px; margin: 12px auto 10px" />
        <h2 class="es-h2" style="font-size: 33px">
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

        <div v-else style="display: flex; gap: 22px; justify-content: center; align-items: stretch">
          <div
            v-for="card in normalizedCards"
            :key="card.id"
            :class="[
              'rop-card',
              selectedId === card.id && 'rop-card--sel',
              selectedId === card.id && card.tone === 'gold' && 'rop-card--gold',
              isExpiredOrSelected && 'rop-card--frozen',
            ]"
            :style="{ boxShadow: cardShadow(card, selectedId === card.id) }"
            @click="selectCard(card.id)"
          >
            <template v-if="selectedId === card.id">
              <span class="es-corner tl" />
              <span class="es-corner tr" />
              <span class="es-corner bl" />
              <span class="es-corner br" />
            </template>

            <div
              class="rop-card__pick"
              :style="selectedId === card.id
                ? card.tone === 'gold'
                  ? { borderColor: 'var(--gold)', color: 'var(--gold)', boxShadow: '0 0 14px -2px var(--gold)' }
                  : { borderColor: 'var(--frost)', color: 'var(--frost)', boxShadow: '0 0 14px -2px var(--frost)' }
                : {}"
            >{{ selectedId === card.id ? '✦' : '' }}</div>

            <div class="es-row" style="margin-bottom: 18px">
              <ChipBadge :tone="card.tone">
                {{ rewardTypeLabel(card.rewardType) }}
              </ChipBadge>
            </div>

            <div style="display: flex; justify-content: center; margin: 4px 0 20px">
              <div
                class="rop-card__icon"
                :style="{
                  border: '1px solid ' + (selectedId === card.id ? accentOf(card.tone) : 'var(--line)'),
                  boxShadow: selectedId === card.id
                    ? '0 0 30px -6px ' + accentOf(card.tone)
                    : 'none',
                  color: accentOf(card.tone),
                }"
              >
                <SigilIcon
                  :kind="sigilKind(card.tone)"
                  :size="38"
                  :stroke-width="1.3"
                />
              </div>
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
          </div>
        </div>
      </div>

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
</template>

<style scoped>
.rop-screen {
  position: relative;
  width: 100%;
  height: 100%;
  overflow: hidden;
  background:
    radial-gradient(70% 52% at 20% 12%, var(--wash-frost), transparent 60%),
    radial-gradient(64% 56% at 86% 80%, var(--wash-blood), transparent 58%),
    radial-gradient(58% 50% at 60% 26%, var(--wash-sap),   transparent 60%),
    radial-gradient(56% 50% at 12% 92%, var(--wash-gold),  transparent 60%),
    radial-gradient(150% 130% at 50% -10%, oklch(0.310 0.058 272) 0%, var(--bg) 48%, var(--void) 100%);
  color: var(--ink);
  font-family: var(--font);
  -webkit-font-smoothing: antialiased;
}

.rop-content {
  position: relative;
  z-index: 5;
  height: 100%;
  display: flex;
  flex-direction: column;
  padding: 24px 60px 80px;
  gap: 8px;
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
  font-family: var(--font-caps);
  font-size: 0.6rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--ink-4);
  border: 1px solid var(--line-soft);
  border-radius: 3px;
  padding: 2px 8px;
  background: oklch(0.22 0.03 272 / 0.6);
}

.rop-state-chip {
  font-family: var(--font-caps);
  font-size: 0.6rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--gold);
  border: 1px solid oklch(0.72 0.09 82 / 0.4);
  border-radius: 3px;
  padding: 2px 8px;
  background: oklch(0.55 0.08 85 / 0.1);
}

/* ── Reward card ── */
.rop-card {
  position: relative;
  width: 312px;
  border-radius: 6px;
  padding: 26px 24px;
  display: flex;
  flex-direction: column;
  cursor: pointer;
  border: 1px solid var(--line);
  background: linear-gradient(180deg, var(--panel-2, oklch(0.28 0.03 268 / 0.6)), var(--panel, oklch(0.24 0.025 270)));
  transition: border-color 0.24s, transform 0.24s, box-shadow 0.24s, background 0.24s;
}

.rop-card:hover:not(.rop-card--frozen) {
  transform: translateY(-5px);
  border-color: var(--frost-dim);
  box-shadow: 0 22px 44px -26px oklch(0.1 0.03 272 / 0.85);
}

.rop-card--sel {
  transform: translateY(-7px);
  border-color: var(--frost);
  background: linear-gradient(180deg, var(--raise, oklch(0.32 0.04 268 / 0.7)), var(--panel, oklch(0.24 0.025 270)));
}

.rop-card--sel.rop-card--gold {
  border-color: var(--gold);
}

.rop-card--frozen {
  cursor: default;
  opacity: 0.75;
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

/* Icon circle */
.rop-card__icon {
  width: 92px;
  height: 92px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: border-color 0.24s, box-shadow 0.24s;
}

.rop-resolved-note {
  font-family: var(--font-caps);
  font-size: 0.6rem;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-5);
}
</style>
