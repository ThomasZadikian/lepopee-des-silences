<script setup lang="ts">
import type { NodeDto } from '../runs/types/runTypes';

defineProps<{
  node: NodeDto | null;
  isLoading: boolean;
  hasActiveCombat: boolean;
  hasPendingReward: boolean;
}>();

defineEmits<{
  resolveCurrentEvent: [];
  generateNextNodes: [];
}>();

function getRiskLabel(node: NodeDto): string {
  if (node.isBoss) return 'Boss';
  if (node.riskLevel >= 75) return 'Critique';
  if (node.riskLevel >= 50) return 'Élevé';
  if (node.riskLevel >= 25) return 'Modéré';
  return 'Faible';
}

function getRiskClass(node: NodeDto): string {
  if (node.isBoss) return 'risk-label--boss';
  if (node.riskLevel >= 75) return 'risk-label--critical';
  if (node.riskLevel >= 50) return 'risk-label--high';
  if (node.riskLevel >= 25) return 'risk-label--moderate';
  return 'risk-label--low';
}

function getRiskDescription(node: NodeDto): string {
  if (node.isBoss || node.riskLevel >= 75) return 'Une présence anormale rend ce passage extrêmement risqué.';
  if (node.riskLevel >= 50) return 'Cette voie semble dangereuse.';
  if (node.riskLevel >= 25) return 'Le Palais oppose une résistance mesurée.';
  return 'Ce passage semble relativement stable.';
}

function getRewardLabel(profile: string): string {
  const labels: Record<string, string> = {
    'combat-common':    'Butin standard',
    'combat-uncommon':  'Butin amélioré',
    'elite-low':        "Récompense d'élite (mineure)",
    'elite':            "Récompense d'élite",
    'elite-risk':       "Récompense d'élite (risquée)",
    'rest-safe':        'Repos sûr',
    'rest-enhanced':    'Repos enrichi',
    'rest-minimal':     'Repos partiel',
    'item-common':      'Objet commun',
    'item-uncommon':    'Objet peu commun',
    'item-rare':        'Objet rare',
    'memory-fragment':  'Fragment de mémoire',
    'npc-basic':        'Interaction basique',
    'narrative':        'Récit du Palais',
    'npc-tome':         'Savoir ancien',
    'merchant':         'Commerce standard',
    'merchant-special': 'Commerce spécialisé',
    'law-minor':        'Loi mineure',
    'law-risk':         'Loi risquée',
    'law-major':        'Loi majeure',
    'law-choice':       'Loi à choix',
    'curse-minor':      'Malédiction mineure',
    'curse-risk':       'Malédiction risquée',
    'curse-modifier':   'Malédiction modificatrice',
    'rare-low':         'Rencontre rare (mineure)',
    'rare':             'Rencontre rare',
    'rare-high':        'Rencontre rare (intense)',
    'room-boss':        'Gardien de Salle',
  };
  return labels[profile] ?? profile;
}

function getNodeDescription(node: NodeDto): string {
  const descriptions: Record<string, string> = {
    'Combat':    'Un affrontement direct vous attend dans les profondeurs du Palais.',
    'Elite':     "Un adversaire d'élite barre le passage. La victoire sera coûteuse.",
    'Rare':      "Une présence rare s'est manifestée — imprévisible et potentiellement précieuse.",
    'RoomBoss':  'Le Gardien de cette Salle attend. Aucun passage sans combat.',
    'FinalBoss': 'La présence finale du Palais. Tout converge ici.',
    'Rest':      'Un refuge temporaire. Reprendre souffle avant de continuer.',
    'Item':      "Un objet a été laissé ici. Son origine reste obscure.",
    'Npc':       "Quelqu'un — ou quelque chose — souhaite vous parler.",
    'Merchant':  "Un marchand propose ses services dans l'ombre du Palais.",
    'Law':       'Une règle du Palais inscrite dans ses murs. La lire vous changera.',
    'Curse':     'Une malédiction latente. Y toucher a un coût.',
    'Memory':    "Un écho du passé. Ce souvenir n'est pas le vôtre.",
  };
  return descriptions[node.type] ?? 'Un nœud inconnu du Palais.';
}

function getActionLabel(node: NodeDto): string {
  if (node.isBoss) return 'Défier le boss';
  const labels: Record<string, string> = {
    'Combat':    'Affronter',
    'Elite':     'Affronter',
    'Rare':      'Affronter',
    'RoomBoss':  'Défier le boss',
    'FinalBoss': 'Défier le boss',
    'Rest':      'Se reposer',
    'Item':      'Récupérer',
    'Law':       'Examiner la Loi',
    'Merchant':  'Approcher',
    'Npc':       'Parler',
    'Curse':     'Accepter le pacte',
    'Memory':    'Se souvenir',
  };
  return labels[node.type] ?? 'Résoudre';
}
</script>

<template>
  <section class="panel node-detail">
    <template v-if="node">
      <p class="system-label">
        {{ node.type }} · NODE_DEEP {{ node.row }} · {{ node.state }}
      </p>

      <h2>{{ node.type }}</h2>

      <div class="node-detail__image">
        {{ node.type }} · Placeholder
      </div>

      <p>{{ getNodeDescription(node) }}</p>

      <p class="node-detail__risk-description">{{ getRiskDescription(node) }}</p>

      <div class="node-detail__meta">
        <div>
          <span class="system-label">RISQUE</span>
          <strong
            :class="getRiskClass(node)"
            :title="`Niveau brut : ${node.riskLevel}`"
          >{{ getRiskLabel(node) }}</strong>
        </div>

        <div>
          <span class="system-label">RÉCOMPENSE</span>
          <strong>{{ getRewardLabel(node.rewardProfile) }}</strong>
        </div>
      </div>

      <button
        v-if="node.state === 'Available' || node.state === 'Selected'"
        class="ghost-button node-detail__confirm"
        :disabled="isLoading || hasActiveCombat || hasPendingReward"
        @click="$emit('resolveCurrentEvent')"
      >
        <span v-if="hasActiveCombat">Combat en cours…</span>
        <span v-else-if="hasPendingReward">Récompense en attente…</span>
        <span v-else>{{ getActionLabel(node) }}</span>
      </button>

      <button
        v-if="node.state === 'Resolved'"
        class="ghost-button node-detail__confirm"
        :disabled="isLoading || hasActiveCombat || hasPendingReward"
        @click="$emit('generateNextNodes')"
      >
        <span v-if="hasActiveCombat">Combat en cours…</span>
        <span v-else-if="hasPendingReward">Récompense en attente…</span>
        <span v-else>Continuer</span>
      </button>
    </template>

    <template v-else>
      <p class="system-label">CHOISIR UN NŒUD</p>

      <h2>Le Palais vous attend</h2>

      <p>
        Sélectionnez un nœud sur la carte pour en voir les détails.
      </p>
    </template>
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

.node-detail__risk-description {
  font-size: 0.8rem;
  font-style: italic;
  margin-top: calc(var(--space-2) * -1);
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

.risk-label--low      { color: var(--color-frost); }
.risk-label--moderate { color: color-mix(in oklch, var(--color-frost), var(--color-gold) 35%); }
.risk-label--high     { color: var(--color-gold); }
.risk-label--critical { color: color-mix(in oklch, var(--color-blood), var(--color-gold) 20%); }
.risk-label--boss     { color: var(--color-blood); }
</style>
