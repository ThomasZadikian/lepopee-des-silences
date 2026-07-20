<script setup lang="ts">
import RoomClimatePanel from '../room-climate/RoomClimatePanel.vue';
import type { ActivePalaceLawDto, ActiveCurseDto, RoomClimateStateDto } from '../runs/types/runTypes';

defineProps<{
  laws?: ActivePalaceLawDto[] | null;
  curses?: ActiveCurseDto[] | null;
  roomClimate?: RoomClimateStateDto | null;
  showRoomClimate?: boolean;
  /** true when the player owns "Déni permanent" — shows the revoke action on each law. */
  lawDenialEnabled?: boolean;
  /** true when "Déni permanent" is currently usable (owned, cooldown elapsed). */
  canUseLawDenial?: boolean;
}>()

const emit = defineEmits<{ close: []; revokeLaw: [lawKey: string] }>()

function domainTone(domain: string): 'blood' | 'frost' | 'gold' | '' {
  const d = domain?.toLowerCase() ?? ''
  if (d.includes('combat') || d.includes('confron')) return 'blood'
  if (d.includes('mem') || d.includes('récit') || d.includes('recit') || d.includes('narr')) return 'frost'
  if (d.includes('loi') || d.includes('édit') || d.includes('edit')) return 'gold'
  return ''
}

function primaryDomain(law: ActivePalaceLawDto): string {
  return law.domains?.[0] ?? ''
}

const DURATION_LABELS: Record<string, string> = {
  UntilRunEnds:   'run entière',
  Permanent:      'permanent',
  UntilRoomEnd:   'fin de salle',
  UntilCombatEnd: 'fin de combat',
}

function durationLabel(d: string | null | undefined): string {
  if (!d) return ''
  return DURATION_LABELS[d] ?? d
}
</script>

<template>
  <div
    class="lp-root"
    role="dialog"
    aria-modal="true"
    aria-label="Influences actives"
    tabindex="-1"
    @keydown.escape="emit('close')"
  >
    <!-- Header -->
    <header class="lp-head">
      <div class="lp-head__left">
        <svg
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
          aria-hidden="true"
          style="color: var(--gold); flex: 0 0 auto;"
        >
          <path d="M12 3L4 7v5c0 5 3.5 9.74 8 11 4.5-1.26 8-6 8-11V7l-8-4z"/>
        </svg>
        <div>
          <span class="es-kicker" style="color: oklch(.65 .09 84 / .7); display: block; margin-bottom: 3px;">
            Registre du Palais
          </span>
          <h3 style="font-size: 22px; font-family: var(--font-display); color: var(--ink); margin: 0;">
            Influences actives
          </h3>
        </div>
      </div>
      <button class="lp-close" @click="emit('close')" aria-label="Fermer">
        <svg
          width="16"
          height="16"
          viewBox="0 0 16 16"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          aria-hidden="true"
        >
          <line x1="3" y1="3" x2="13" y2="13"/>
          <line x1="13" y1="3" x2="3" y2="13"/>
        </svg>
      </button>
    </header>

    <div class="lp-divider" />

    <RoomClimatePanel v-if="showRoomClimate" class="lp-climate" :climate="roomClimate" />

    <!-- Content -->
    <div v-if="laws?.length || curses?.length" class="lp-body">

      <!-- ── Lois du Palais ── -->
      <section v-if="laws && laws.length" class="lp-section">
        <h4 class="lp-section__title">
          Lois du Palais
          <span class="lp-section__count">{{ laws.length }}</span>
        </h4>
        <div
          v-for="law in laws"
          :key="law.key"
          class="lp-law"
        >
          <svg
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="1.5"
            stroke-linecap="round"
            stroke-linejoin="round"
            aria-hidden="true"
            :style="{
              color: domainTone(primaryDomain(law)) === 'gold'  ? 'var(--gold)'
                   : domainTone(primaryDomain(law)) === 'blood' ? 'var(--blood)'
                   : domainTone(primaryDomain(law)) === 'frost' ? 'var(--frost)'
                   : 'var(--gold-dim, oklch(.65 .09 84 / .6))',
              flex: '0 0 auto',
              marginTop: '2px',
            }"
          >
            <path d="M12 3L4 7v5c0 5 3.5 9.74 8 11 4.5-1.26 8-6 8-11V7l-8-4z"/>
          </svg>
          <div class="lp-law__body">
            <div class="lp-law__head">
              <span class="lp-law__name">{{ law.displayName || law.key }}</span>
              <span class="es-mono" style="font-size: 10px; color: var(--ink-4); flex: 0 0 auto;">
                {{ law.version }}
              </span>
            </div>
            <div v-if="primaryDomain(law)" style="margin-bottom: 7px; display: flex; gap: 5px; flex-wrap: wrap;">
              <span
                class="es-chip"
                :class="{
                  'es-chip--gold':  domainTone(primaryDomain(law)) === 'gold',
                  'es-chip--frost': domainTone(primaryDomain(law)) === 'frost',
                  'es-chip--blood': domainTone(primaryDomain(law)) === 'blood',
                }"
              >{{ primaryDomain(law) }}</span>
              <span v-if="law.rarity" class="es-chip">{{ law.rarity }}</span>
              <span v-if="law.polarity" class="es-chip">{{ law.polarity }}</span>
            </div>
            <p class="lp-law__desc">{{ law.description }}</p>
            <button
              v-if="lawDenialEnabled"
              class="lp-law__revoke"
              :disabled="!canUseLawDenial"
              :title="canUseLawDenial ? 'Déni permanent' : 'Déni permanent — en recharge'"
              @click="emit('revokeLaw', law.key)"
            >
              Révoquer (Déni permanent)
            </button>
          </div>
        </div>
      </section>

      <!-- ── Malédictions ── -->
      <section v-if="curses && curses.length" class="lp-section">
        <h4 class="lp-section__title lp-section__title--blood">
          Malédictions
          <span class="lp-section__count lp-section__count--blood">{{ curses.length }}</span>
        </h4>
        <div
          v-for="curse in curses"
          :key="curse.id"
          class="lp-curse"
          :class="{ 'lp-curse--consumed': !!curse.consumedAtUtc }"
        >
          <svg
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="1.5"
            stroke-linecap="round"
            stroke-linejoin="round"
            aria-hidden="true"
            style="color: var(--blood); flex: 0 0 auto; margin-top: 2px;"
          >
            <path d="M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20z"/>
            <line x1="12" y1="8" x2="12" y2="12"/>
            <line x1="12" y1="16" x2="12.01" y2="16"/>
          </svg>
          <div class="lp-curse__body">
            <div class="lp-curse__head">
              <span class="lp-curse__name">
                {{ curse.displayName ?? curse.curseDefinitionKey }}
              </span>
              <div class="lp-curse__badges">
                <span v-if="curse.severity" class="es-chip es-chip--blood" style="font-size: 9px; padding: 1px 6px;">
                  {{ curse.severity }}
                </span>
                <span v-if="curse.consumedAtUtc" class="es-chip" style="font-size: 9px; padding: 1px 6px; opacity: .6;">
                  Consommée
                </span>
              </div>
            </div>
            <p v-if="curse.description" class="lp-curse__desc">{{ curse.description }}</p>
            <span v-if="curse.duration" class="lp-curse__duration">
              {{ durationLabel(curse.duration) }}
            </span>
          </div>
        </div>
      </section>

    </div>

    <!-- Empty state -->
    <div v-else class="lp-empty">
      <svg
        width="32"
        height="32"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="1"
        stroke-linecap="round"
        stroke-linejoin="round"
        aria-hidden="true"
        style="color: var(--ink-4);"
      >
        <path d="M12 3L4 7v5c0 5 3.5 9.74 8 11 4.5-1.26 8-6 8-11V7l-8-4z"/>
      </svg>
      <span class="es-label" style="color: var(--ink-4);">Aucune influence active.</span>
    </div>

    <!-- Footer -->
    <footer class="lp-foot">
      <button
        class="es-btn es-btn--ghost"
        style="font-size: 12px; padding: 8px 18px;"
        @click="emit('close')"
      >
        Fermer
      </button>
    </footer>
  </div>
</template>

<style scoped>
.lp-root {
  position: absolute;
  top: 0;
  right: 0;
  height: 100%;
  width: 400px;
  display: flex;
  flex-direction: column;
  z-index: 40;
  background: oklch(.20 .028 268 / .82);
  backdrop-filter: blur(18px) saturate(1.4);
  -webkit-backdrop-filter: blur(18px) saturate(1.4);
  border-left: 1px solid var(--frost, oklch(.70 .07 232));
  outline: none;
}

.lp-root::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 24px;
  height: 24px;
  border-top: 1px solid var(--frost, oklch(.70 .07 232));
  border-left: 1px solid var(--frost, oklch(.70 .07 232));
  opacity: .5;
  pointer-events: none;
}

.lp-root::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 0;
  width: 24px;
  height: 24px;
  border-bottom: 1px solid var(--frost, oklch(.70 .07 232));
  border-left: 1px solid var(--frost, oklch(.70 .07 232));
  opacity: .5;
  pointer-events: none;
}

.lp-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  padding: 22px 22px 14px;
  flex: 0 0 auto;
}

.lp-head__left {
  display: flex;
  align-items: flex-start;
  gap: 10px;
}

.lp-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  border-radius: 4px;
  border: 1px solid var(--line);
  background: oklch(.26 .02 268 / .6);
  color: var(--ink-3);
  cursor: pointer;
  flex: 0 0 auto;
  transition: border-color .15s, color .15s, background .15s;
}

.lp-close:hover {
  border-color: var(--line-strong);
  color: var(--ink);
  background: oklch(.30 .024 268 / .7);
}

.lp-close:focus-visible {
  outline: 2px solid var(--frost);
  outline-offset: 2px;
}

.lp-divider {
  flex: 0 0 auto;
  height: 1px;
  background: var(--line);
  margin: 0 22px;
}

.lp-body {
  flex: 1;
  overflow-y: auto;
  padding: 16px 22px;
  display: flex;
  flex-direction: column;
  gap: 20px;
}

/* ── Sections ── */
.lp-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.lp-section__title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-family: var(--font-caps, var(--font));
  font-size: 9.5px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
  padding-bottom: 8px;
  border-bottom: 1px solid var(--line-soft);
  margin: 0;
}

.lp-section__title--blood { color: oklch(.62 .14 20 / .7); border-bottom-color: oklch(.52 .15 20 / .3); }

.lp-section__count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: 8px;
  background: oklch(.28 .02 268 / .6);
  border: 1px solid var(--line-soft);
  font-size: 9px;
  letter-spacing: 0;
  color: var(--ink-3);
}

.lp-section__count--blood {
  background: oklch(.38 .10 20 / .15);
  border-color: oklch(.52 .15 20 / .3);
  color: var(--blood);
}

/* ── Law entries ── */
.lp-law {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 12px 0;
  border-bottom: 1px solid var(--line-soft, oklch(.32 .022 268 / .5));
}

.lp-law:last-child { border-bottom: none; }

.lp-law__body { flex: 1; min-width: 0; }

.lp-law__head {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: 8px;
  margin-bottom: 6px;
}

.lp-law__name {
  font-family: var(--font-display);
  font-size: 15.5px;
  font-weight: 600;
  color: var(--gold);
}

.lp-law__desc {
  font-size: 12.5px;
  line-height: 1.55;
  color: var(--ink-3);
  margin: 0;
}

.lp-law__revoke {
  margin-top: 8px;
  padding: 5px 12px;
  font-family: var(--font-caps, var(--font));
  font-size: 10px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gold);
  background: transparent;
  border: 1px solid var(--edge-gold, oklch(.65 .09 84 / .4));
  border-radius: 4px;
  cursor: pointer;
  transition: background .15s, color .15s;
}

.lp-law__revoke:hover:not(:disabled) {
  background: var(--wash-gold, oklch(.65 .09 84 / .12));
  color: var(--ink);
}

.lp-law__revoke:disabled {
  opacity: .4;
  cursor: not-allowed;
}

/* ── Curse entries ── */
.lp-curse {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 12px 0;
  border-bottom: 1px solid oklch(.32 .022 268 / .5);
  transition: opacity .2s;
}

.lp-curse:last-child { border-bottom: none; }

.lp-curse--consumed { opacity: .55; }

.lp-curse__body { flex: 1; min-width: 0; }

.lp-curse__head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 8px;
  margin-bottom: 5px;
}

.lp-curse__name {
  font-family: var(--font-display);
  font-size: 14.5px;
  font-weight: 600;
  color: var(--blood);
}

.lp-curse__badges {
  display: flex;
  gap: 4px;
  flex-shrink: 0;
}

.lp-curse__desc {
  font-size: 12.5px;
  line-height: 1.5;
  color: var(--ink-3);
  margin: 0 0 4px;
}

.lp-curse__duration {
  font-family: var(--font-caps, var(--font));
  font-size: 9.5px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--ink-5, oklch(.42 .018 268));
}

/* ── Empty state ── */
.lp-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  gap: 4px;
}

.lp-foot {
  flex: 0 0 auto;
  padding: 14px 22px 18px;
  border-top: 1px solid var(--line-soft, oklch(.32 .022 268 / .5));
  display: flex;
  justify-content: flex-end;
}
</style>
