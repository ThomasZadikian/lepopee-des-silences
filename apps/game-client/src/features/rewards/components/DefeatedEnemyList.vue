<script setup lang="ts">
import DefeatedEnemyRow from './DefeatedEnemyRow.vue'
import type { DefeatedEnemySummaryDto } from '../types/rewardTypes'

defineProps<{
  enemies: DefeatedEnemySummaryDto[]
}>()
</script>

<template>
  <aside class="del">
    <p class="es-kicker del__kicker">Ennemis vaincus</p>
    <div class="del__list">
      <DefeatedEnemyRow
        v-for="enemy in enemies"
        :key="enemy.enemyKey"
        :enemy="enemy"
      />
    </div>
  </aside>
</template>

<style scoped>
.del {
  flex: 0 0 240px;
  width: 240px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-top: 4px;
}

.del__kicker {
  color: var(--ink-4);
}

.del__list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  overflow-y: auto;
}
</style>
