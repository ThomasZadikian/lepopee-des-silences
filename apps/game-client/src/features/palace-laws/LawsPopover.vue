<script setup lang="ts">
import { ref, watchEffect } from 'vue';
import RoomClimatePanel from '../room-climate/RoomClimatePanel.vue';
import type { ActivePalaceLawDto, RoomClimateStateDto } from '../runs/types/runTypes';
import { upcomingRoomsApi } from './api/upcomingRoomsApi';
import type { UpcomingRoomDto } from './types/upcomingRoomsTypes';

const props = defineProps<{
  laws?: ActivePalaceLawDto[] | null;
  roomClimate?: RoomClimateStateDto | null;
  showRoomClimate?: boolean;
  /** true when the player owns "Déni permanent" — shows the revoke action on each law. */
  lawDenialEnabled?: boolean;
  /** true when "Déni permanent" is currently usable (owned, cooldown elapsed). */
  canUseLawDenial?: boolean;
  /** Needed to fetch the "Édit des Portes Ouvertes" upcoming-rooms preview. */
  runId?: string | null;
}>()

const emit = defineEmits<{ close: []; revokeLaw: [lawKey: string] }>()

// "Édit des Portes Ouvertes" (law.portes-ouvertes): while active, reveals the names of
// the rest of the floor's rooms — fetched on demand rather than carried on the run DTO,
// since it's a dedicated, law-gated preview endpoint.
const upcomingRooms = ref<UpcomingRoomDto[]>([])

watchEffect(async () => {
  const hasPortesOuvertes = props.laws?.some(law => law.key === 'law.portes-ouvertes') ?? false
  if (!hasPortesOuvertes || !props.runId) {
    upcomingRooms.value = []
    return
  }

  try {
    const response = await upcomingRoomsApi.getUpcomingRooms(props.runId)
    upcomingRooms.value = response.isRevealed ? response.rooms : []
  } catch {
    upcomingRooms.value = []
  }
})

function primaryDomain(law: ActivePalaceLawDto): string {
  return law.domains?.[0] ?? ''
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
    <button class="lp-close" @click="emit('close')" aria-label="Fermer">✕</button>

    <RoomClimatePanel v-if="showRoomClimate" :climate="roomClimate" />

    <div class="lp-head-row">
      <span class="lp-kicker">Lois du Palais</span>
      <span class="lp-count">{{ laws?.length ?? 0 }}</span>
    </div>

    <div v-if="laws && laws.length" class="lp-list">
      <div v-for="law in laws" :key="law.key" class="lp-law">
        <div class="lp-law__chips">
          <span v-if="primaryDomain(law)" class="lp-chip">{{ primaryDomain(law) }}</span>
          <span v-if="law.rarity" class="lp-chip">{{ law.rarity }}</span>
          <span v-if="law.polarity" class="lp-chip" :class="{ 'lp-chip--danger': law.polarity === 'Négative' }">{{ law.polarity }}</span>
        </div>
        <span class="lp-law__name">{{ law.displayName || law.key }} <span class="lp-law__version">v{{ law.version }}</span></span>
        <p class="lp-law__desc">{{ law.description }}</p>

        <ul v-if="law.key === 'law.portes-ouvertes' && upcomingRooms.length" class="lp-law__rooms">
          <li v-for="room in upcomingRooms" :key="room.roomIndex">{{ room.displayName ?? '???' }}</li>
        </ul>

        <button
          v-if="lawDenialEnabled"
          class="lp-revoke"
          :disabled="!canUseLawDenial"
          :title="canUseLawDenial ? 'Déni permanent' : 'Déni permanent — en recharge'"
          @click="emit('revokeLaw', law.key)"
        >
          Révoquer (Déni permanent)
        </button>
      </div>
    </div>

    <p v-else class="lp-empty">Aucune loi active.</p>
  </div>
</template>

<style scoped>
.lp-root {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  z-index: var(--z-drawer, 30);
  width: 380px;
  background: var(--panel);
  border-left: 1px solid var(--line);
  padding: 24px 22px;
  overflow-y: auto;
  outline: none;
  animation: lp-slide .35s cubic-bezier(0.5, 0, 0.5, 1);
}

@keyframes lp-slide {
  from { transform: translateX(100%); }
  to { transform: translateX(0); }
}

.lp-close {
  all: unset;
  position: absolute;
  top: 16px;
  right: 16px;
  cursor: pointer;
  color: var(--ink-4);
  font-size: 12px;
  padding: 4px;
  transition: color .15s;
}

.lp-close:hover { color: var(--mint-dim); }

.lp-head-row {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  margin-bottom: 10px;
}

.lp-kicker {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: .14em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.lp-count { font-family: var(--font-mono); font-size: 11px; color: var(--ink-4); }

.lp-list { display: flex; flex-direction: column; gap: 10px; }

.lp-law {
  padding: 12px 14px;
  background: var(--panel-2);
  border: 1px solid var(--line-soft);
}

.lp-law__chips { display: flex; gap: 6px; flex-wrap: wrap; margin-bottom: 6px; }

.lp-chip {
  font-size: 9px;
  letter-spacing: .06em;
  text-transform: uppercase;
  padding: 2px 6px;
  border: 1px solid var(--mint-dim);
  color: var(--mint-dim);
}

.lp-chip--danger { border-color: var(--danger-dim); color: var(--danger-dim); }

.lp-law__name {
  font-family: var(--font-display);
  font-style: italic;
  font-size: 14px;
  color: var(--ink);
}

.lp-law__version { font-family: var(--font-mono); font-size: 10px; color: var(--ink-4); }

.lp-law__desc {
  margin: 6px 0 0;
  font-size: 12px;
  color: var(--ink-3);
  line-height: 1.5;
}

.lp-law__rooms {
  list-style: none;
  margin: 8px 0 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.lp-law__rooms li {
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--ink-4);
}

.lp-revoke {
  margin-top: 10px;
  padding: 6px 12px;
  background: transparent;
  border: 1px solid var(--danger-dim);
  color: var(--danger-dim);
  font-family: var(--font);
  font-size: 10px;
  letter-spacing: .06em;
  text-transform: uppercase;
  cursor: pointer;
  transition: opacity .15s;
}

.lp-revoke:hover:not(:disabled) { opacity: .8; }
.lp-revoke:disabled { color: var(--ink-5); border-color: var(--line); cursor: not-allowed; }

.lp-empty {
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--ink-4);
  font-style: italic;
}

@media (prefers-reduced-motion: reduce) {
  .lp-root { animation: none; }
}
</style>
