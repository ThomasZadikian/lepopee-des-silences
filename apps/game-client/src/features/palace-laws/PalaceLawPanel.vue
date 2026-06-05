<template>
  <section class="panel laws">
    <header>
      <span class="system-label">Lois du Palais</span>
      <span class="system-value">03 actives</span>
    </header>

    <article>
      <strong>Loi du Silence v4</strong>
      <p>Les fragments se taisent : aucun indice avant la résolution d'un nœud.</p>
      <small>Narration · PNJ</small>
    </article>

    <article>
      <strong>Miroirs Brisés v2</strong>
      <p>+15 % esquive pour tous · +10 % de dégâts subis.</p>
      <small>Combat</small>
    </article>

    <article>
      <strong>Pesanteur v1</strong>
      <p>Profondeur écourtée, mais raretés des récompenses accrues.</p>
      <small>Génération · Récompense</small>
    </article>
  </section>
</template>

<style scoped>
.laws {
  padding: var(--space-4);
}

.laws header {
  display: flex;
  justify-content: space-between;
  margin-bottom: var(--space-4);
}

.laws article {
  padding: var(--space-3) 0;
  border-top: 1px solid color-mix(in oklch, var(--color-line), transparent 45%);
}

.laws strong {
  color: var(--color-gold);
}

.laws p {
  color: var(--color-muted);
  font-size: 0.9rem;
}

.laws small {
  color: var(--color-dim);
  font-family: var(--font-mono);
  text-transform: uppercase;
}
</style>