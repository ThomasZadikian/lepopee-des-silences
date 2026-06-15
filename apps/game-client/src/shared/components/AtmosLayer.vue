<script setup lang="ts">
import { computed } from 'vue';

/**
 * Couche atmosphérique « Aquarelle Nocturne » : lavis qui infusent (.es-atmos),
 * vignette, granulation du papier (.es-grain) et poussière de pigment flottante
 * (.es-particle). Déterministe via `seed` pour éviter les sauts au rerender.
 */
const props = withDefaults(defineProps<{ seed?: number; count?: number }>(), {
  seed: 1,
  count: 14,
});

type Particle = {
  left: string;
  top: string;
  duration: string;
  delay: string;
  scale: number;
};

const particles = computed<Particle[]>(() => {
  let s = props.seed * 9301 + 49297;
  const rnd = () => {
    s = (s * 9301 + 49297) % 233280;
    return s / 233280;
  };
  return Array.from({ length: props.count }, () => ({
    left: (rnd() * 100).toFixed(2) + '%',
    top: (45 + rnd() * 55).toFixed(2) + '%',
    duration: (14 + rnd() * 20).toFixed(1) + 's',
    delay: (-rnd() * 24).toFixed(1) + 's',
    scale: 0.6 + rnd() * 1.4,
  }));
});
</script>

<template>
  <div class="atmos-layer" aria-hidden="true">
    <div class="es-atmos" />
    <span
      v-for="(p, i) in particles"
      :key="i"
      class="es-particle"
      :style="{
        left: p.left,
        top: p.top,
        animationDuration: p.duration,
        animationDelay: p.delay,
        transform: `scale(${p.scale})`,
      }"
    />
    <div class="es-vignette" />
    <div class="es-grain" />
  </div>
</template>

<style scoped>
.atmos-layer {
  position: absolute;
  inset: 0;
  pointer-events: none;
  overflow: hidden;
  z-index: 0;
}

@media (prefers-reduced-motion: reduce) {
  .es-particle { animation: none; opacity: 0.32; }
}
</style>
