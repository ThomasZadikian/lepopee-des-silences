# ADR-002 — Backend serveur-autoritaire

## Statut

Acceptée.

## Contexte

Le modèle full web expose naturellement le client à la manipulation : modification de requêtes, falsification d’état local, rejeu d’actions, double clics, appels API manuels ou tentative de triche.

Pour préserver l’intégrité du gameplay, le client ne doit jamais décider des résultats critiques.

## Décision

Le backend est source de vérité pour toutes les données critiques.

Le frontend envoie uniquement des intentions utilisateur.

## Intentions autorisées

- `StartRun`
- `ChooseNode`
- `ResolveEventChoice`
- `ChooseCombatAction`
- `SelectReward`
- `AbandonRun`

## Données décidées par le backend

- Seed de run.
- Génération des pièces.
- Génération des nœuds.
- Événements rencontrés.
- Résultats de combat.
- Récompenses.
- Inventaire.
- Progression temporaire.
- Progression permanente.
- Score.
- Leaderboard.
- Écriture du Tome.
- Statut de run.

## Conséquences

### Positives

- Réduction du risque de triche.
- Cohérence forte du gameplay.
- Reproductibilité des runs.
- Auditabilité des actions.
- Base solide pour Event Sourcing.

### Négatives

- Backend plus complexe.
- Nécessité de gérer la concurrence d’actions.
- Besoin de projections rapides pour l’interface.

## Règle de conception

Aucune réponse frontend ne doit être considérée comme un résultat métier définitif.