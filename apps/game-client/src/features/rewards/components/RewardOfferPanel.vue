<script setup lang="ts">
import ChipBadge from '@/shared/components/ChipBadge.vue'
import EliseComment from '@/shared/components/EliseComment.vue'
import RuleOrnament from '@/shared/components/RuleOrnament.vue'
import SigilIcon from '@/shared/components/SigilIcon.vue'
import { computed, ref } from 'vue'

// ── Types ─────────────────────────────────────────────────────────────────
export interface RewardFaveur {
  sig:     string
  name:    string
  kind:    string
  rarity:  string
  tone:    'gold' | 'frost' | null
  eff:     string
  meta:    string
}

export interface RewardRecapStat {
  k:     string
  v:     string
  gold?: boolean
}

export interface RewardLootItem {
  sig:   string
  label: string
  color: string
}

export interface RewardOfferDto {
  enemyName:      string
  enemyType:      string
  roomName:       string
  recap:          RewardRecapStat[]
  loot:           RewardLootItem[]
  faveurs:        RewardFaveur[]
  performances?:  string[]
  seed?:          string
  depth?:         number
  laws?:          number
  score?:         string
}

const props = defineProps<{
  offer: RewardOfferDto
}>()

const emit = defineEmits<{
  selectReward: [faveur: RewardFaveur | null]
}>()

// ── State UI ──────────────────────────────────────────────────────────────
const selIdx = ref<number | null>(null)
const taken  = ref(false)

const chosen = computed<RewardFaveur | null>(() =>
  selIdx.value != null ? props.offer.faveurs[selIdx.value] : null)

function selectCard(idx: number) {
  selIdx.value = idx
  taken.value  = false
}
function confirmChoice() {
  if (chosen.value == null) return
  taken.value = true
  emit('selectReward', chosen.value)
}
function refuseAll() {
  emit('selectReward', null)
}

function accentOf(f: RewardFaveur) {
  return f.tone === 'gold' ? 'var(--gold)' : f.tone === 'frost' ? 'var(--frost)' : 'var(--ink-3)'
}
function cardSlotShadow(f: RewardFaveur, isSel: boolean) {
  if (!isSel) return 'none'
  return f.tone === 'gold'
    ? '0 0 30px -6px oklch(0.7 0.09 84 / 0.6)'
    : '0 0 30px -6px oklch(0.7 0.09 268 / 0.55)'
}
const confirmBtnClass = computed(() =>
  chosen.value?.tone === 'gold' ? 'es-btn--gold' : 'es-btn--frost')
</script>

<template>
  <div class="reward-screen">
    <!-- Atmosphère -->
    <div class="es-atmos" />
    <div class="es-vignette" />
    <div class="es-grain" />

    <!-- ── Runbar ── -->
    <div class="es-runbar" style="position: relative; z-index: 8">
      <div class="es-seg" style="padding-left: 30px">
        <span class="es-seg__k">Palais</span>
        <span class="es-seg__v" style="letter-spacing: 0.04em; font-weight: 600">L'ÉPOPÉE DES SILENCES</span>
      </div>
      <div class="es-seg">
        <span class="es-seg__k">Seed</span>
        <span class="es-seg__v es-gold">{{ offer.seed ?? 'SIL-7F3A-29D' }}</span>
      </div>
      <div class="es-seg">
        <span class="es-seg__k">Profondeur</span>
        <span class="es-seg__v">{{ String(offer.depth ?? 5).padStart(2,'0') }} / 10</span>
      </div>
      <div class="es-seg">
        <span class="es-seg__k">Lois actives</span>
        <span class="es-seg__v">{{ String(offer.laws ?? 3).padStart(2,'0') }}</span>
      </div>
      <div class="es-seg es-seg--grow" />
      <div class="es-seg" style="align-items: flex-end">
        <span class="es-seg__k">Score projeté</span>
        <span class="es-seg__v">{{ offer.score ?? '13 920' }}</span>
      </div>
      <div class="es-seg" style="padding-right: 26px; border-right: none; align-items: flex-end">
        <span class="es-seg__k">Phase</span>
        <span class="es-seg__v es-gold">● RÉCOMPENSE</span>
      </div>
    </div>

    <!-- ── Contenu ── -->
    <div class="es-content reward-content">

      <!-- Bandeau récap combat -->
      <div class="es-plate" style="padding: 18px 24px; display: flex; align-items: center; gap: 24px; flex: 0 0 auto; position: relative">

        <!-- Silhouette ennemi -->
        <div style="position: relative; flex: 0 0 auto">
          <div
            class="es-slot es-silhouette"
            style="width: 90px; height: 90px; border-radius: 48% 52% 46% 54% / 12% 12% 88% 88%; border: 1px solid var(--blood-dim); opacity: 0.7"
          />
          <span style="position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; font-family: var(--font-caps); font-size: 9.5px; letter-spacing: 0.14em; color: var(--blood); text-transform: uppercase">
            dissipé
          </span>
        </div>

        <!-- Identité -->
        <div style="flex: 0 0 auto; min-width: 224px">
          <ChipBadge tone="blood" style="margin-bottom: 9px">
            <SigilIcon kind="combat" :size="11" /> Manifestation vaincue
          </ChipBadge>
          <div class="es-h3" style="font-size: 26px">{{ offer.enemyName }}</div>
          <span class="es-label" style="color: var(--ink-4)">{{ offer.enemyType }} · {{ offer.roomName }}</span>
        </div>

        <div style="width: 1px; align-self: stretch; background: var(--line)" />

        <!-- Stats -->
        <div class="es-row" style="flex: 1; justify-content: space-around">
          <div v-for="(stat, i) in offer.recap" :key="i" class="vrw-stat">
            <span class="vrw-stat__k">{{ stat.k }}</span>
            <span class="vrw-stat__v" :style="{ color: stat.gold ? 'var(--gold)' : 'var(--ink)' }">
              {{ stat.v }}
            </span>
          </div>
        </div>

        <div style="width: 1px; align-self: stretch; background: var(--line)" />

        <!-- Performances -->
        <div class="es-stack" style="gap: 7px; flex: 0 0 auto; align-items: flex-end">
          <ChipBadge
            v-for="(perf, i) in (offer.performances ?? ['◆ Sans perte', '◆ Riposte parfaite'])"
            :key="i"
            :tone="i === 0 ? 'frost' : 'gold'"
          >{{ perf }}</ChipBadge>
        </div>
      </div>

      <!-- Butin garanti -->
      <div class="es-row" style="gap: 14px; margin: 16px 0 4px; flex: 0 0 auto">
        <span class="es-label" style="flex: 0 0 auto; color: var(--gold-dim)">◇ Butin garanti</span>
        <div v-for="(item, i) in offer.loot" :key="i" class="vrw-loot">
          <SigilIcon :kind="item.sig" :size="15" :style="{ color: item.color }" />
          <span class="es-mono" :style="{ fontSize: '13px', color: item.color }">{{ item.label }}</span>
        </div>
        <div style="flex: 1" />
        <span class="es-label" style="color: var(--ink-4)">
          RewardOffered · {{ offer.depth ?? 5 }} → {{ (offer.depth ?? 5) + 1 }}
        </span>
      </div>

      <!-- Choix de faveur -->
      <div style="flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: center">
        <span class="es-kicker">Une faveur, une seule</span>
        <RuleOrnament :frost="true" style="width: 150px; margin: 12px 0 10px" />
        <h2 class="es-h2" style="text-align: center; font-size: 33px">Le Palais reconnaît ta traversée</h2>
        <p class="es-body es-dim" style="margin-top: 8px; text-align: center; max-width: 560px">
          Choisis-en une. Le reste se referme et retourne au silence.
        </p>

        <!-- Cartes faveur -->
        <div style="display: flex; gap: 22px; margin-top: 24px">
          <div
            v-for="(f, i) in offer.faveurs"
            :key="i"
            :class="[
              'vrw-card',
              selIdx === i && 'vrw-card--sel',
              selIdx === i && f.tone === 'gold' && 'vrw-card--gold'
            ]"
            @click="selectCard(i)"
          >
            <!-- Crochets d'angle sélection -->
            <template v-if="selIdx === i">
              <span :class="['es-corner', f.tone === 'gold' ? 'es-corner--frost' : '', 'tl']" />
              <span :class="['es-corner', f.tone === 'gold' ? 'es-corner--frost' : '', 'tr']" />
              <span :class="['es-corner', f.tone === 'gold' ? 'es-corner--frost' : '', 'bl']" />
              <span :class="['es-corner', f.tone === 'gold' ? 'es-corner--frost' : '', 'br']" />
            </template>

            <!-- Picto sélection -->
            <div
              class="vrw-card__pick"
              :style="selIdx === i
                ? f.tone === 'gold'
                  ? { borderColor: 'currentColor', color: 'var(--gold)', boxShadow: '0 0 14px -2px var(--gold)' }
                  : { borderColor: 'currentColor', color: 'var(--frost)', boxShadow: '0 0 14px -2px var(--frost)' }
                : {}"
            >{{ selIdx === i ? '✦' : '' }}</div>

            <!-- Rareté -->
            <div class="es-row" style="margin-bottom: 18px">
              <ChipBadge :tone="f.tone">{{ f.rarity }}</ChipBadge>
            </div>

            <!-- Icône -->
            <div style="display: flex; justify-content: center; margin: 4px 0 20px">
              <div
                class="es-slot es-silhouette"
                :style="{
                  width: '92px', height: '92px', borderRadius: '50%',
                  border: '1px solid ' + (selIdx === i ? accentOf(f) : 'var(--line)'),
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  boxShadow: cardSlotShadow(f, selIdx === i)
                }"
              >
                <SigilIcon :kind="f.sig" :size="38" :stroke-width="1.3" :style="{ color: accentOf(f) }" />
              </div>
            </div>

            <!-- Titre -->
            <h3 class="es-h3" :style="{ textAlign: 'center', color: selIdx === i ? 'var(--ink)' : 'var(--ink-2)' }">
              {{ f.name }}
            </h3>
            <div class="es-label" style="text-align: center; margin-top: 6px; color: var(--ink-4)">{{ f.kind }}</div>

            <div class="es-divider" style="margin: 18px 0" />

            <p class="es-body" style="font-size: 13.5px; text-align: center; flex: 1">{{ f.eff }}</p>
            <div class="es-mono" style="font-size: 10.5px; color: var(--ink-4); text-align: center; letter-spacing: 0.07em; margin-top: 14px">
              {{ f.meta }}
            </div>
          </div>
        </div>
      </div>

      <!-- Socle -->
      <div class="es-row" style="justify-content: center; gap: 14px; flex: 0 0 auto; margin-top: 8px">
        <button class="es-btn es-btn--ghost es-btn--lg" @click="refuseAll">
          Refuser — laisser au Palais
        </button>
        <button
          :class="['es-btn', 'es-btn--lg', confirmBtnClass]"
          :disabled="selIdx == null"
          :style="{ opacity: selIdx == null ? 0.4 : 1, pointerEvents: selIdx == null ? 'none' : 'auto', minWidth: '320px' }"
          @click="confirmChoice"
        >
          {{ taken ? '✓ ' : '' }}Emporter « {{ chosen?.name ?? '—' }} » {{ taken ? '' : '→' }}
        </button>
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
.reward-screen {
  position: relative;
  width: 100%;
  height: 100dvh;
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
.reward-content {
  height: calc(100dvh - 62px);
  display: flex;
  flex-direction: column;
  padding: 24px 60px 26px;
  z-index: 5;
}

/* ── Stat strip ── */
.vrw-stat {
  display: flex; flex-direction: column; gap: 5px;
  padding: 0 22px; border-right: 1px solid var(--line-soft);
}
.vrw-stat:last-child { border-right: none; }
.vrw-stat__k {
  font-family: var(--font-caps); font-size: 10px;
  letter-spacing: 0.2em; text-transform: uppercase; color: var(--ink-4);
}
.vrw-stat__v {
  font-family: var(--font-mono); font-size: 21px; font-weight: 500;
  color: var(--ink); letter-spacing: -0.01em;
}

/* ── Carte de faveur ── */
.vrw-card {
  position: relative; width: 312px; border-radius: 6px; padding: 26px 24px;
  display: flex; flex-direction: column; cursor: pointer;
  border: 1px solid var(--line);
  background: linear-gradient(180deg, var(--panel-2), var(--panel));
  transition: border-color .24s, transform .24s, box-shadow .24s, background .24s;
}
.vrw-card:hover {
  transform: translateY(-5px); border-color: var(--frost-dim);
  box-shadow: 0 22px 44px -26px oklch(0.1 0.03 272 / 0.85);
}
.vrw-card--sel {
  transform: translateY(-7px); border-color: var(--frost);
  background: linear-gradient(180deg, var(--raise), var(--panel));
  box-shadow: 0 0 46px -12px var(--wash-frost), var(--shadow-deep);
}
.vrw-card--sel.vrw-card--gold {
  border-color: var(--gold);
  box-shadow: 0 0 46px -12px oklch(0.74 0.12 84 / 0.6), var(--shadow-deep);
}
.vrw-card__pick {
  position: absolute; top: 14px; right: 14px; width: 24px; height: 24px;
  border-radius: 50%; display: flex; align-items: center; justify-content: center;
  border: 1px solid var(--line); color: transparent; font-size: 12px; transition: all .2s;
}

/* ── Butin loot pill ── */
.vrw-loot {
  display: flex; align-items: center; gap: 11px; padding: 11px 16px;
  border-radius: 5px; border: 1px solid var(--line);
  background: oklch(0.30 0.034 268 / 0.42);
}
</style>
