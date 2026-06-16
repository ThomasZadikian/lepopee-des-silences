<script setup lang="ts">
const props = withDefaults(defineProps<{
  kind: string
  size?: number
  strokeWidth?: number
}>(), {
  size: 22,
  strokeWidth: 1.4,
})
</script>

<template>
  <svg
    :width="props.size"
    :height="props.size"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    :stroke-width="props.strokeWidth"
    stroke-linejoin="round"
    stroke-linecap="round"
    style="display: block; flex-shrink: 0"
  >
    <!-- combat : triangle -->
    <template v-if="props.kind === 'combat'">
      <path d="M12 3.5 L20.5 19 L3.5 19 Z" />
    </template>

    <!-- elite : triangle + cercle -->
    <template v-else-if="props.kind === 'elite'">
      <path d="M12 5 L18 17 L6 17 Z" />
      <circle cx="12" cy="12.5" r="9.2" />
    </template>

    <!-- memoire : double cercle -->
    <template v-else-if="props.kind === 'memoire'">
      <circle cx="12" cy="12" r="8" />
      <circle cx="12" cy="12" r="3.2" />
    </template>

    <!-- repos : croissant -->
    <template v-else-if="props.kind === 'repos'">
      <path d="M15.6 4.4a8 8 0 1 0 0 15.2 6.6 6.6 0 0 1 0-15.2z" />
    </template>

    <!-- marchand : losange vide -->
    <template v-else-if="props.kind === 'marchand'">
      <path d="M12 3 L21 12 L12 21 L3 12 Z" />
    </template>

    <!-- loi : losange plein -->
    <template v-else-if="props.kind === 'loi'">
      <path d="M12 3 L21 12 L12 21 L3 12 Z" fill="currentColor" stroke="none" />
    </template>

    <!-- malediction : cercle tiret -->
    <template v-else-if="props.kind === 'malediction'">
      <circle cx="12" cy="12" r="8" stroke-dasharray="33 17" transform="rotate(-30 12 12)" />
    </template>

    <!-- pnj : cercle + point -->
    <template v-else-if="props.kind === 'pnj'">
      <circle cx="12" cy="12" r="7.5" />
      <circle cx="12" cy="12" r="1.9" fill="currentColor" stroke="none" />
    </template>

    <!-- objet : losange plein compact -->
    <template v-else-if="props.kind === 'objet'">
      <path d="M12 5 L17.5 12 L12 19 L6.5 12 Z" fill="currentColor" stroke="none" />
    </template>

    <!-- rare / relique : étoile 4 branches -->
    <template v-else-if="props.kind === 'rare'">
      <path d="M12 2.5 L13.7 10.3 L21.5 12 L13.7 13.7 L12 21.5 L10.3 13.7 L2.5 12 L10.3 10.3 Z" fill="currentColor" stroke="none" />
    </template>

    <!-- boss : triple cercle + losange -->
    <template v-else-if="props.kind === 'boss'">
      <circle cx="12" cy="12" r="9" />
      <circle cx="12" cy="12" r="5.6" />
      <path d="M12 8 L15 12 L12 16 L9 12 Z" fill="currentColor" stroke="none" />
    </template>

    <!-- seuil / portail -->
    <template v-else-if="props.kind === 'seuil'">
      <path d="M5 20.5 V11 a7 7 0 0 1 14 0 V20.5" />
    </template>

    <!-- generation / run -->
    <template v-else-if="props.kind === 'generation' || props.kind === 'run'">
      <path d="M12 4 L12 20 M4 12 L20 12" />
      <circle cx="12" cy="12" r="3" />
    </template>

    <!-- narration : lignes de texte -->
    <template v-else-if="props.kind === 'narration'">
      <path d="M5 7 H19 M5 12 H15 M5 17 H17" />
    </template>

    <!-- recompense : losange vide compact -->
    <template v-else-if="props.kind === 'recompense'">
      <path d="M12 5 L17.5 12 L12 19 L6.5 12 Z" />
    </template>

    <!-- sort : étoile 3 branches -->
    <template v-else-if="props.kind === 'sort'">
      <path d="M12 3 V21 M3.5 7.5 L20.5 16.5 M20.5 7.5 L3.5 16.5" />
    </template>

    <!-- map -->
    <template v-else-if="props.kind === 'map'">
      <path d="M3 6 L9 3 L15 6 L21 3 V18 L15 21 L9 18 L3 21 Z" />
      <path d="M9 3 V18 M15 6 V21" />
    </template>

    <!-- fallback générique -->
    <template v-else>
      <circle cx="12" cy="12" r="8" stroke-dasharray="2.5 4" opacity="0.85" />
      <circle cx="12" cy="12" r="1.4" fill="currentColor" stroke="none" opacity="0.85" />
    </template>
  </svg>
</template>
