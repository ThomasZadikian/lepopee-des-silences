# ADR-004 — Event Sourcing ciblé sur les runs

## Statut

Acceptée.

## Contexte

Une run est une succession de choix irréversibles, d’événements, de combats, de récompenses et de modifications du Palais.

L’état courant d’une run doit pouvoir être reconstruit, audité et projeté vers plusieurs vues : état de run, Tome, leaderboard, historique joueur, audit.

## Décision

L’Event Sourcing est appliqué prioritairement au domaine Run.

Les autres domaines peuvent rester transactionnels dans PostgreSQL tant qu’ils ne nécessitent pas une historisation complète.

## Événements initiaux

- `RunStarted`
- `RunSeedGenerated`
- `RoomGenerated`
- `NodeSelected`
- `EventResolved`
- `PalaceLawApplied`
- `CombatStarted`
- `CombatActionResolved`
- `CombatEnded`
- `RewardOffered`
- `RewardSelected`
- `CompanionJoined`
- `HimLitGenerated`
- `RunCompleted`
- `RunFailed`
- `RunAbandoned`

## Conséquences

### Positives

- Historique complet de run.
- Reproductibilité.
- Auditabilité.
- Support naturel du Tome.
- Support naturel du leaderboard.
- Débogage facilité.

### Négatives

- Complexité supérieure à un CRUD classique.
- Nécessité de gérer les projections.
- Nécessité de versionner les événements.

## Règle de conception

Les événements de run sont append-only.

Ils ne doivent jamais être modifiés après écriture.