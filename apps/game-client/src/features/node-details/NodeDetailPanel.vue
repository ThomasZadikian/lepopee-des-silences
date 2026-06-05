<template>
  <section class="panel node-detail">
    <p class="system-label">Marchand · pièce 05 · nœud sélectionné</p>
    <h2>Le Colporteur Muet</h2>

    <div class="node-detail__image">PNJ · Placeholder</div>

    <p>
      Une silhouette voûtée derrière un étal d'objets sans nom. Il ne parle pas —
      il échange. Ce que tu donnes, tu ne le retrouveras pas.
    </p>

    <div class="node-detail__meta">
      <div>
        <span class="system-label">Risque</span>
        <strong>Faible</strong>
      </div>
      <div>
        <span class="system-label">Récompense</span>
        <strong>Objet · jusqu'à rare</strong>
      </div>
    </div>

    <button class="ghost-button">Aperçu</button>
    <button class="ghost-button node-detail__confirm">Confirmer →</button>
  </section>
</template>

<style scoped>
.node-detail {
  min-height: 100%;
  padding: var(--space-4);
  border-color: color-mix(in oklch, var(--color-frost), transparent 45%);
}

.node-detail h2 {
  color: var(--color-frost);
}

.node-detail p {
  color: var(--color-muted);
  line-height: 1.55;
}

.node-detail__image {
  height: 12rem;
  display: grid;
  place-items: center;
  margin: var(--space-6) 0;
  color: var(--color-dim);
  background: radial-gradient(circle, var(--color-panel-soft), var(--color-void));
  border: 1px solid var(--color-line);
  font-family: var(--font-mono);
  font-size: 0.75rem;
  text-transform: uppercase;
}

.node-detail__meta {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-3);
  margin: var(--space-6) 0;
}

.node-detail__meta div {
  border-top: 1px solid var(--color-line);
  padding-top: var(--space-2);
}

.node-detail__meta strong {
  display: block;
  margin-top: var(--space-1);
  color: var(--color-gold);
  font-size: 0.85rem;
}

.node-detail__confirm {
  border-color: var(--color-frost);
  color: var(--color-frost);
}
</style>