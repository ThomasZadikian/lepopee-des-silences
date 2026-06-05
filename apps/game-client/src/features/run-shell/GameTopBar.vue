<template>
  <header class="topbar panel">
    <section class="topbar__brand">
      <span class="system-label">Palais</span>
      <strong>L'Épopée des silences</strong>
    </section>

    <section class="topbar__item">
      <span class="system-label">Seed</span>
      <span class="system-value">SIL-7F3A-29D</span>
    </section>

    <section class="topbar__item">
      <span class="system-label">Profondeur</span>
      <span class="system-value">05 / 10</span>
    </section>

    <section class="topbar__item">
      <span class="system-label">Lois actives</span>
      <span class="system-value">03</span>
    </section>

    <section class="topbar__item topbar__score">
      <span class="system-label">Score projeté</span>
      <span class="system-value">12 480</span>
    </section>

    <section class="topbar__state">
      <span class="system-label">État</span>
      <span class="topbar__active">● Active</span>
    </section>
  </header>
</template>

<style scoped>
.topbar {
  display: grid;
  grid-template-columns: 1.6fr repeat(4, 1fr) auto;
  min-height: 4.5rem;
  margin: var(--space-4);
}

.topbar__brand,
.topbar__item,
.topbar__state {
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: var(--space-1);
  padding: 0 var(--space-4);
  border-right: 1px solid color-mix(in oklch, var(--color-line), transparent 45%);
}

.topbar__brand strong {
  font-size: 0.95rem;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.topbar__score {
  text-align: right;
}

.topbar__active {
  color: var(--color-frost);
  font-family: var(--font-mono);
  text-transform: uppercase;
}
</style>