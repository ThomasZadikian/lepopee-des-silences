<script setup lang="ts">
defineProps<{
  frost?: boolean
}>()
</script>

<template>
  <div :class="['es-rule', frost && 'es-rule--frost']">
    <span class="es-lozenge" />
  </div>
</template>
