<script setup lang="ts">
import { ref } from 'vue'
import ChipBadge from '@/shared/components/ChipBadge.vue'
import SigilIcon from '@/shared/components/SigilIcon.vue'
import { useClickOutside } from '../../../shared/composables/useClickOutside'
import type { DefeatedEnemySummaryDto } from '../types/rewardTypes'

defineProps<{
  enemy: DefeatedEnemySummaryDto
}>()

const showPopover = ref(false)
const popoverRef = ref<HTMLElement | null>(null)

function toggle(event: Event) {
  event.stopPropagation()
  showPopover.value = !showPopover.value
}

useClickOutside(popoverRef, () => { showPopover.value = false }, {
  ignoreSelectors: ['.del-row__trigger'],
})

function rarityTone(rarity: string): 'sap' | 'frost' | 'gold' | null {
  switch (rarity) {
    case 'Uncommon': return 'sap'
    case 'Rare':      return 'frost'
    case 'Epic':
    case 'Legendary':
    case 'Unique':    return 'gold'
    default:          return null
  }
}

function rarityLabel(rarity: string): string {
  switch (rarity) {
    case 'Uncommon':  return 'Peu commun'
    case 'Rare':       return 'Rare'
    case 'Epic':       return 'Épique'
    case 'Legendary':  return 'Légendaire'
    case 'Unique':     return 'Unique'
    default:           return 'Commun'
  }
}
</script>

<template>
  <div class="del-row">
    <button
      class="del-row__trigger"
      type="button"
      :class="{ 'del-row__trigger--open': showPopover }"
      @click="toggle"
    >
      <span class="del-row__icon">
        <SigilIcon kind="boss" :size="18" :stroke-width="1.3" />
      </span>
      <span class="del-row__name">{{ enemy.displayName }}</span>
      <span v-if="enemy.count > 1" class="del-row__count">×{{ enemy.count }}</span>
    </button>

    <div v-if="showPopover" ref="popoverRef" class="del-row__popover" @click.stop>
      <button class="del-row__close" type="button" @click="toggle" aria-label="Fermer">✕</button>

      <h4 class="del-row__popover-name">{{ enemy.displayName }}</h4>
      <p v-if="enemy.description" class="del-row__desc">{{ enemy.description }}</p>

      <div class="del-row__divider" />

      <p class="es-label del-row__loot-title">Butin possible</p>

      <div v-if="enemy.lootEntries.length === 0" class="del-row__loot-empty">
        Aucun butin connu.
      </div>
      <ul v-else class="del-row__loot-list">
        <li v-for="entry in enemy.lootEntries" :key="entry.itemKey" class="del-row__loot-entry">
          <span class="del-row__loot-name">{{ entry.itemDisplayName }}</span>
          <span class="del-row__loot-right">
            <ChipBadge :tone="rarityTone(entry.rarity)">{{ rarityLabel(entry.rarity) }}</ChipBadge>
            <span class="del-row__loot-pct">{{ entry.dropPercent }}%</span>
          </span>
        </li>
      </ul>
    </div>
  </div>
</template>

<style scoped>
.del-row {
  position: relative;
}

.del-row__trigger {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 9px;
  padding: 9px 10px;
  border-radius: 4px;
  border: 1px solid var(--line-soft, oklch(.32 .022 268 / .5));
  background: oklch(0.24 0.025 60 / 0.4);
  color: var(--ink-2);
  cursor: pointer;
  text-align: left;
  transition: border-color 0.15s, background 0.15s;
}

.del-row__trigger:hover,
.del-row__trigger--open {
  border-color: var(--frost-dim, var(--frost));
  background: oklch(0.28 0.03 60 / 0.55);
}

.del-row__icon {
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--ink-4);
  flex: 0 0 auto;
}

.del-row__name {
  font-family: var(--font-display);
  font-size: 13.5px;
  color: var(--ink);
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.del-row__count {
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--ink-4);
  flex: 0 0 auto;
}

.del-row__popover {
  position: absolute;
  top: 0;
  left: calc(100% + 12px);
  z-index: 30;
  width: 300px;
  max-height: 420px;
  overflow-y: auto;
  padding: 18px 18px 16px;
  border-radius: 6px;
  border: 1px solid var(--frost, oklch(.70 .07 232));
  background: oklch(.20 .028 268 / .92);
  backdrop-filter: blur(18px) saturate(1.4);
  -webkit-backdrop-filter: blur(18px) saturate(1.4);
  box-shadow: 0 20px 50px -20px oklch(0.05 0 0 / 0.8);
}

.del-row__close {
  position: absolute;
  top: 10px;
  right: 10px;
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  border: 1px solid var(--line);
  background: oklch(.26 .02 268 / .6);
  color: var(--ink-3);
  cursor: pointer;
  font-size: 11px;
}

.del-row__close:hover {
  color: var(--ink);
  border-color: var(--line-strong, var(--line));
}

.del-row__popover-name {
  font-family: var(--font-display);
  font-size: 17px;
  font-weight: 600;
  color: var(--ink);
  margin: 0 22px 8px 0;
}

.del-row__desc {
  font-size: 12.5px;
  line-height: 1.55;
  color: var(--ink-3);
  margin: 0;
}

.del-row__divider {
  height: 1px;
  background: var(--line);
  margin: 14px 0 10px;
}

.del-row__loot-title {
  color: var(--ink-4);
  margin: 0 0 8px;
}

.del-row__loot-empty {
  font-size: 12px;
  color: var(--ink-5, oklch(.42 .018 268));
}

.del-row__loot-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.del-row__loot-entry {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.del-row__loot-name {
  font-size: 12.5px;
  color: var(--ink-2);
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.del-row__loot-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex: 0 0 auto;
}

.del-row__loot-pct {
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--ink-4);
  min-width: 32px;
  text-align: right;
}
</style>
