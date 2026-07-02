<script setup lang="ts">
import { nextTick, onBeforeUnmount, ref } from 'vue'
import ChipBadge from '@/shared/components/ChipBadge.vue'
import SigilIcon from '@/shared/components/SigilIcon.vue'
import { useClickOutside } from '../../../shared/composables/useClickOutside'
import type { DefeatedEnemySummaryDto } from '../types/rewardTypes'

defineProps<{
  enemy: DefeatedEnemySummaryDto
}>()

const POPOVER_WIDTH = 300
const GAP = 14
const VIEWPORT_MARGIN = 16

const showPopover = ref(false)
const triggerRef = ref<HTMLElement | null>(null)
const popoverRef = ref<HTMLElement | null>(null)
const popoverStyle = ref<{ top: string; left: string }>({ top: '0px', left: '0px' })

function computePosition() {
  const trigger = triggerRef.value
  if (!trigger) return

  const rect = trigger.getBoundingClientRect()
  const openRight = rect.right + GAP + POPOVER_WIDTH <= window.innerWidth - VIEWPORT_MARGIN

  const left = openRight
    ? rect.right + GAP
    : Math.max(VIEWPORT_MARGIN, rect.left - GAP - POPOVER_WIDTH)

  const maxTop = window.innerHeight - VIEWPORT_MARGIN
  const top = Math.min(rect.top, maxTop - 120)

  popoverStyle.value = { top: `${Math.max(VIEWPORT_MARGIN, top)}px`, left: `${left}px` }
}

async function toggle(event: Event) {
  event.stopPropagation()
  showPopover.value = !showPopover.value
  if (showPopover.value) {
    await nextTick()
    computePosition()
    window.addEventListener('scroll', computePosition, true)
    window.addEventListener('resize', computePosition)
  } else {
    window.removeEventListener('scroll', computePosition, true)
    window.removeEventListener('resize', computePosition)
  }
}

function close() {
  showPopover.value = false
  window.removeEventListener('scroll', computePosition, true)
  window.removeEventListener('resize', computePosition)
}

onBeforeUnmount(() => {
  window.removeEventListener('scroll', computePosition, true)
  window.removeEventListener('resize', computePosition)
})

useClickOutside(popoverRef, close, {
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
      ref="triggerRef"
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

    <Teleport to="body">
      <Transition name="del-pop-fade">
        <div
          v-if="showPopover"
          ref="popoverRef"
          class="del-pop"
          :style="{ top: popoverStyle.top, left: popoverStyle.left, width: `${POPOVER_WIDTH}px` }"
          @click.stop
        >
          <span class="del-pop__notch" aria-hidden="true" />

          <div class="del-pop__head">
            <span class="del-pop__head-icon">
              <SigilIcon kind="boss" :size="20" :stroke-width="1.2" />
            </span>
            <h4 class="del-pop__name">{{ enemy.displayName }}</h4>
            <button class="del-pop__close" type="button" @click="close" aria-label="Fermer">✕</button>
          </div>

          <p v-if="enemy.description" class="del-pop__desc">{{ enemy.description }}</p>

          <div class="del-pop__divider" />

          <p class="es-label del-pop__loot-title">Butin possible</p>

          <div v-if="enemy.lootEntries.length === 0" class="del-pop__loot-empty">
            Aucun butin connu.
          </div>
          <ul v-else class="del-pop__loot-list">
            <li v-for="entry in enemy.lootEntries" :key="entry.itemKey" class="del-pop__loot-entry">
              <span class="del-pop__loot-name">{{ entry.itemDisplayName }}</span>
              <span class="del-pop__loot-right">
                <ChipBadge :tone="rarityTone(entry.rarity)">{{ rarityLabel(entry.rarity) }}</ChipBadge>
                <span class="del-pop__loot-pct">{{ entry.dropPercent }}%</span>
              </span>
            </li>
          </ul>
        </div>
      </Transition>
    </Teleport>
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
</style>

<!-- Unscoped: teleported to <body>, outside this component's scoped-style DOM subtree. -->
<style>
.del-pop {
  position: fixed;
  z-index: 220;
  max-height: min(70vh, 460px);
  overflow-y: auto;
  padding: 18px 18px 16px;
  border-radius: 8px;
  border: 1px solid var(--frost, oklch(.70 .07 232));
  background: oklch(.19 .028 268 / .96);
  backdrop-filter: blur(20px) saturate(1.4);
  -webkit-backdrop-filter: blur(20px) saturate(1.4);
  box-shadow: 0 24px 60px -20px oklch(0.05 0 0 / 0.85), 0 0 40px -18px var(--frost-dim, var(--frost));
}

.del-pop__notch {
  position: absolute;
  top: 18px;
  left: -7px;
  width: 12px;
  height: 12px;
  background: oklch(.19 .028 268 / .96);
  border-left: 1px solid var(--frost, oklch(.70 .07 232));
  border-bottom: 1px solid var(--frost, oklch(.70 .07 232));
  transform: rotate(45deg);
}

.del-pop__head {
  display: flex;
  align-items: center;
  gap: 9px;
  margin-bottom: 10px;
}

.del-pop__head-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  border-radius: 50%;
  flex: 0 0 auto;
  color: var(--frost);
  border: 1px solid var(--line);
  background: oklch(0.26 0.03 232 / 0.3);
}

.del-pop__name {
  font-family: var(--font-display);
  font-size: 16px;
  font-weight: 600;
  color: var(--ink);
  margin: 0;
  flex: 1;
  min-width: 0;
}

.del-pop__close {
  width: 22px;
  height: 22px;
  flex: 0 0 auto;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  border: 1px solid var(--line);
  background: oklch(.26 .02 268 / .6);
  color: var(--ink-3);
  cursor: pointer;
  font-size: 10px;
}

.del-pop__close:hover {
  color: var(--ink);
  border-color: var(--line-strong, var(--line));
}

.del-pop__desc {
  font-size: 12.5px;
  line-height: 1.55;
  color: var(--ink-3);
  margin: 0;
}

.del-pop__divider {
  height: 1px;
  background: var(--line);
  margin: 14px 0 10px;
}

.del-pop__loot-title {
  color: var(--ink-4);
  margin: 0 0 8px;
}

.del-pop__loot-empty {
  font-size: 12px;
  color: var(--ink-5, oklch(.42 .018 268));
}

.del-pop__loot-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.del-pop__loot-entry {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.del-pop__loot-name {
  font-size: 12.5px;
  color: var(--ink-2);
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.del-pop__loot-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex: 0 0 auto;
}

.del-pop__loot-pct {
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--ink-4);
  min-width: 32px;
  text-align: right;
}

.del-pop-fade-enter-active,
.del-pop-fade-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}

.del-pop-fade-enter-from,
.del-pop-fade-leave-to {
  opacity: 0;
  transform: translateX(-6px);
}
</style>
