# Game Engine Active Combat Persistence

## Objectif

Persister relationnellement l'état d'un combat actif attaché à une Run.

## Tables

| Table | Description |
|---|---|
| `run_active_combats` | Combat actif (status, tour, combattant actif) |
| `run_combatants` | Combattants du combat (alliés et ennemis, même table, discriminés par `side`) |
| `run_combatant_skills` | Skills de chaque combattant |

## Choix d'architecture

- EF Core uniquement dans Infrastructure.
- Entities persistence séparées du Domain.
- Mapping explicite Domain ↔ Persistence via `CombatPersistenceMapper`.
- Méthodes `Rehydrate` publiques sur `Combat`, `Combatant`, `CombatantSkill`.
- Relation one-to-one `RunEntity` → `CombatEntity` via `RunEntity.ActiveCombatId` FK.
- Cascade delete : suppression d'un combat supprime ses combatants et skills.
- Tags des skills sérialisés en JSON.

## Ce qui est persisté

- Combat actif : ID, status, tour, combattant actif.
- Combattants : vitalité, guard, mana, charge, statut, side.
- Skills : key, type, ciblage, effet, coût, puissance.

## Non-objectifs

- Pas de RewardOffer.
- Pas de PlayerRuntimeState.
- Pas d'inventaire.
- Pas d'Event Sourcing.
- Pas de changement frontend.

## Prochaines étapes

- Persister RewardOffers.
- Ajouter PlayerRuntimeState persistant.
- Brancher les nodes Rest/Law sur un état durable.

## Commandes EF Core

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Leds.GameEngine.Infrastructure \
  --startup-project src/Leds.GameEngine.Api \
  --context GameEngineDbContext \
  --output-dir Persistence/Migrations
```
