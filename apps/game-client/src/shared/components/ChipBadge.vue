<script setup lang="ts">
defineProps<{
  tone?: 'gold' | 'frost' | 'blood' | 'sap' | 'mint' | null
}>()
</script>

<template>
  <span :class="['es-chip', tone && `es-chip--${tone}`]">
    <slot />
  </span>
</template>
