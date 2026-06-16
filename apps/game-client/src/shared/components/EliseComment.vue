<script setup lang="ts">
import SigilIcon from './SigilIcon.vue'

defineProps<{
  tell?: string
}>()
</script>

<template>
  <div class="es-elise">
    <div class="es-elise__name">
      <SigilIcon kind="memoire" :size="13" :stroke-width="1.6" />
      ELISE
      <span
        v-if="tell"
        style="color: var(--ink-4); letter-spacing: 0.18em; margin-left: 4px"
      >· {{ tell }}</span>
    </div>
    <div class="es-elise__line">
      <slot />
    </div>
  </div>
</template>
