<template>
  <section class="panel party">
    <header>
      <span class="system-label">Équipe</span>
      <span class="system-value">Joueur + 1</span>
    </header>

    <article class="party__member">
      <div class="party__portrait" />
      <div>
        <strong>Joueur</strong>
        <span>Toi</span>
      </div>
      <em>Saignement</em>
      <meter min="0" max="190" value="138" />
      <small>PV 138 / 190 · PP 6 / 12</small>
    </article>

    <article class="party__member">
      <div class="party__portrait" />
      <div>
        <strong>Neige</strong>
        <span>Compagnon · Givre</span>
      </div>
      <em class="party__tag--frost">Givre +1</em>
      <meter min="0" max="190" value="172" />
      <small>PV 172 / 190 · PP 9 / 12</small>
    </article>
  </section>
</template>

<style scoped>
.party {
  padding: var(--space-4);
}

.party header {
  display: flex;
  justify-content: space-between;
  margin-bottom: var(--space-4);
}

.party__member {
  display: grid;
  grid-template-columns: 2.5rem 1fr auto;
  gap: var(--space-3);
  padding: var(--space-3) 0;
  border-top: 1px solid color-mix(in oklch, var(--color-line), transparent 45%);
}

.party__portrait {
  width: 2.5rem;
  height: 2.5rem;
  background: radial-gradient(circle, var(--color-panel-soft), var(--color-void));
  border: 1px solid var(--color-line);
}

.party__member strong,
.party__member span,
.party__member small,
.party__member meter {
  grid-column: 2 / -1;
}

.party__member span,
.party__member small {
  color: var(--color-muted);
}

.party__member em {
  color: var(--color-blood);
  font-family: var(--font-mono);
  font-size: 0.7rem;
  font-style: normal;
  text-transform: uppercase;
}

.party__tag--frost {
  color: var(--color-frost) !important;
}
</style>