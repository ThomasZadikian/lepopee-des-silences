<script setup lang="ts">
/**
 * L'atmosphère "le Palais respire" — remplace PalaceAtmosphere (rouages/cœur/vapeur/or) sur
 * tous les écrans migrés vers direction-visuelle-palais-respire.md. Un fond qui vit sans
 * jamais devenir un décor : pulsation d'échelle très légère, grain permanent, vignette.
 *
 * `veins` ajoute les tracés de veines aux bords avec un segment qui voyage lentement dessus
 * (vu sur Le Seuil) — réservé aux écrans peu visités où ce niveau de mise en scène se justifie.
 * `motes` ajoute des poussières qui montent lentement (Le Seuil uniquement à ce jour).
 * Les écrans consultés en boucle (tiroirs, fiches, superposition de nœuds) n'utilisent que la
 * respiration + le grain, sans veines ni poussières, pour ne pas fatiguer l'œil.
 */
const props = withDefaults(defineProps<{
  veins?: boolean;
  motes?: boolean;
  moteCount?: number;
  seed?: number;
}>(), {
  veins: false,
  motes: false,
  moteCount: 9,
  seed: 512,
});

function rngMotes(seed: number, count: number) {
  let s = seed;
  const rnd = () => { s = (s * 9301 + 49297) % 233280; return s / 233280; };
  return Array.from({ length: count }, () => ({
    left: (6 + rnd() * 88).toFixed(1) + '%',
    delay: (rnd() * 12).toFixed(1) + 's',
  }));
}

const motesList = props.motes ? rngMotes(props.seed, props.moteCount) : [];
</script>

<template>
  <div class="living-walls" aria-hidden="true">
    <svg v-if="veins" class="living-walls__veins" viewBox="0 0 100 100" preserveAspectRatio="none">
      <path d="M0,8 C30,2 60,4 78,14 C92,22 96,40 94,62" fill="none" stroke="var(--line)" stroke-width="0.18" vector-effect="non-scaling-stroke" />
      <path d="M2,96 C16,80 14,60 28,50 C38,42 34,30 40,20" fill="none" stroke="var(--line)" stroke-width="0.18" vector-effect="non-scaling-stroke" />
      <path d="M98,30 C88,42 90,58 96,70" fill="none" stroke="var(--line)" stroke-width="0.16" vector-effect="non-scaling-stroke" />
      <path d="M4,4 C10,14 8,24 16,30" fill="none" stroke="var(--line)" stroke-width="0.16" vector-effect="non-scaling-stroke" />

      <path class="living-walls__vein-travel living-walls__vein-travel--a" d="M0,8 C30,2 60,4 78,14 C92,22 96,40 94,62" fill="none" stroke="var(--mint-dim)" stroke-width="0.22" opacity="0.5" stroke-dasharray="3 70" vector-effect="non-scaling-stroke" />
      <path class="living-walls__vein-travel living-walls__vein-travel--b" d="M2,96 C16,80 14,60 28,50 C38,42 34,30 40,20" fill="none" stroke="var(--mint-dim)" stroke-width="0.2" opacity="0.4" stroke-dasharray="2.5 60" vector-effect="non-scaling-stroke" />
    </svg>

    <span
      v-for="(m, i) in motesList"
      :key="i"
      class="living-walls__mote"
      :style="{ left: m.left, animationDelay: m.delay }"
    />

    <div class="living-walls__grain" />
    <div class="living-walls__vignette" />
  </div>
</template>

<style scoped>
.living-walls {
  position: absolute;
  inset: -2%;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
  animation: living-walls-breathe 12s cubic-bezier(0.5, 0, 0.5, 1) infinite;
}

.living-walls__veins {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
}

.living-walls__vein-travel--a { animation: living-walls-vein-travel 14s linear infinite; filter: blur(0.3px); }
.living-walls__vein-travel--b { animation: living-walls-vein-travel 17s linear infinite; animation-delay: 4s; filter: blur(0.3px); }

.living-walls__mote {
  position: absolute;
  bottom: -8px;
  width: 2px;
  height: 2px;
  border-radius: 50%;
  background: var(--mint-dim);
  box-shadow: 0 0 5px var(--mint-dim);
  animation: living-walls-mote-rise 16s ease-in infinite;
}

.living-walls__grain {
  position: absolute;
  inset: 0;
  opacity: 0.4;
  mix-blend-mode: soft-light;
  background-image:
    repeating-linear-gradient(0deg, oklch(1 0 0 / 0.018) 0px, oklch(1 0 0 / 0.018) 1px, transparent 1px, transparent 2px),
    repeating-linear-gradient(90deg, oklch(0 0 0 / 0.04) 0px, oklch(0 0 0 / 0.04) 1px, transparent 1px, transparent 3px);
}

.living-walls__vignette {
  position: absolute;
  inset: 0;
  background: radial-gradient(120% 100% at 50% 42%, transparent 44%, oklch(0.06 0.01 262 / 0.55) 82%, oklch(0.04 0.008 262 / 0.9) 100%);
}

@keyframes living-walls-breathe { 0%, 100% { transform: scale(1); } 50% { transform: scale(1.007); } }
@keyframes living-walls-vein-travel { to { stroke-dashoffset: -420; } }
@keyframes living-walls-mote-rise {
  0% { transform: translateY(0); opacity: 0; }
  14% { opacity: 0.55; }
  80% { opacity: 0.25; }
  100% { transform: translateY(-62vh); opacity: 0; }
}

@media (prefers-reduced-motion: reduce) {
  .living-walls,
  .living-walls__vein-travel--a,
  .living-walls__vein-travel--b,
  .living-walls__mote {
    animation: none;
  }
}
</style>
