<template>
  <section class="map">
    <header>
      <div>
        <p class="system-label">Carte du Palais — embranchements visibles</p>
        <h2>Les chemins sont irréversibles · ils peuvent se rejoindre</h2>
      </div>
      <span class="system-value">Profondeur 05 → 10</span>
    </header>

    <div class="map__canvas" aria-label="Carte roguelite placeholder">
      <div
        v-for="node in nodes"
        :key="node.id"
        class="map__node"
        :class="`map__node--${node.kind}`"
        :style="{ left: node.x, top: node.y }"
      >
        {{ node.glyph }}
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
const nodes = [
  { id: 1, x: '8%', y: '45%', glyph: '◎', kind: 'selected' },
  { id: 2, x: '20%', y: '28%', glyph: '△', kind: 'available' },
  { id: 3, x: '20%', y: '62%', glyph: '☉', kind: 'frost' },
  { id: 4, x: '35%', y: '40%', glyph: '◌', kind: 'locked' },
  { id: 5, x: '52%', y: '48%', glyph: '♢', kind: 'locked' },
  { id: 6, x: '72%', y: '34%', glyph: '◐', kind: 'locked' },
  { id: 7, x: '88%', y: '50%', glyph: '◎', kind: 'danger' },
];
</script>

<style scoped>
.map {
  height: 100%;
}

.map header {
  display: flex;
  justify-content: space-between;
  gap: var(--space-4);
}

.map h2 {
  margin: var(--space-1) 0 0;
  color: var(--color-muted);
  font-family: var(--font-mono);
  font-size: 0.8rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
}

.map__canvas {
  position: relative;
  height: calc(100% - 4rem);
  margin-top: var(--space-6);
  border: 1px solid color-mix(in oklch, var(--color-line), transparent 60%);
  background:
    radial-gradient(circle at 20% 50%, rgb(120 180 220 / 8%), transparent 18%),
    radial-gradient(circle at 88% 50%, rgb(180 40 60 / 9%), transparent 12%);
}

.map__canvas::before {
  content: '';
  position: absolute;
  inset: 12%;
  border-top: 1px dashed color-mix(in oklch, var(--color-line), transparent 35%);
  transform: skewY(-10deg);
}

.map__node {
  position: absolute;
  width: 3.2rem;
  height: 3.2rem;
  display: grid;
  place-items: center;
  translate: -50% -50%;
  border-radius: 50%;
  background: var(--color-panel);
  border: 1px solid var(--color-line);
  color: var(--color-muted);
  font-family: var(--font-mono);
  box-shadow: 0 0 40px rgb(0 0 0 / 50%);
}

.map__node--selected {
  color: var(--color-gold);
  border-color: var(--color-gold);
  box-shadow: 0 0 32px color-mix(in oklch, var(--color-gold), transparent 55%);
}

.map__node--available,
.map__node--frost {
  color: var(--color-frost);
  border-color: var(--color-frost);
}

.map__node--danger {
  color: var(--color-blood);
  border-color: var(--color-blood);
}
</style>