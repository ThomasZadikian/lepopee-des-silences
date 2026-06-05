<template>
  <aside class="elise panel">
    <p class="system-label">Élise · observe</p>
    <p>
      Le Colporteur ne ment jamais — il ne dit simplement rien.
      Méfie-toi du silence qui sourit.
    </p>
  </aside>
</template>

<style scoped>
.elise {
  position: absolute;
  left: var(--space-6);
  bottom: var(--space-6);
  max-width: 34rem;
  padding: var(--space-4) var(--space-6);
  border-color: color-mix(in oklch, var(--color-frost), transparent 25%);
}

.elise p:last-child {
  margin-bottom: 0;
  color: var(--color-ink);
  line-height: 1.55;
}
</style>