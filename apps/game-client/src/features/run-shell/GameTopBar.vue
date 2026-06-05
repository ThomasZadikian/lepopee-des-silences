<template>
  <header class="topbar panel">
    <section class="topbar__brand">
      <span class="system-label">PALACE</span>
      <strong>NAME_OF_GAME</strong>
    </section>

    <section class="topbar__item">
      <span class="system-label">SEED_NUMBER</span>
      <span class="system-value">PLACEHOLDER_SEED_NUMBER</span>
    </section>

    <section class="topbar__item">
      <span class="system-label">DEEPTH</span>
      <span class="system-value">PLACEHOLDER_DEEPTH</span>
    </section>

    <section class="topbar__item">
      <span class="system-label">ACTIVE_LAWS</span>
      <span class="system-value">PLACEHOLDER_ACTIVE_LAWS</span>
    </section>

    <section class="topbar__item topbar__score">
      <span class="system-label">PROJECTED_SCORE</span>
      <span class="system-value">PLACEHOLDER_PROJECTED_SCORE</span>
    </section>

    <section class="topbar__state">
      <span class="system-label">SEED_STATE</span>
      <span class="topbar__active">PLACEHOLDER_SEED_STATE</span>
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