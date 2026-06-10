# Game Engine Persistence Foundation

## Objectif

Préparer la persistance PostgreSQL du Game Engine Service.

## Décision

Persistance relationnelle explicite, pas JSON.

## Mode actuel

* InMemory reste disponible pour tests et fallback local.
* Postgres devient le provider cible.

## Configuration

### appsettings.json

```json
{
  "Persistence": {
    "Mode": "InMemory"
  },
  "ConnectionStrings": {
    "GameEngineDb": "Host=localhost;Port=5432;Database=leds_game_engine;Username=postgres;Password=postgres"
  }
}
```

### Activer PostgreSQL

Pour utiliser PostgreSQL au lieu de InMemory :

```json
{
  "Persistence": {
    "Mode": "Postgres"
  }
}
```

## Tables initiales

* `runs` : entité minimale pour les runs (id, player_id, status, seed, versions, depth, timestamps)

## Structure

```
Leds.GameEngine.Infrastructure/
  Persistence/
    GameEngineDbContext.cs
    GameEngineDbContextFactory.cs
    Entities/
      RunEntity.cs
    Configurations/
      RunEntityConfiguration.cs
    Migrations/
      InitialGameEnginePersistence.cs
```

## Prochaines étapes

* persister Rooms ;
* persister Nodes ;
* persister NodeEvents ;
* persister ActiveCombat ;
* persister RewardOffers ;
* remplacer progressivement InMemoryRunRepository.

## Non-objectifs

* pas de migration complète de la Run dans cette PR ;
* pas de persistance combat ;
* pas de persistance reward ;
* pas d'Event Sourcing ;
* pas de changement frontend.

## Commandes EF Core

### Créer une migration

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Leds.GameEngine.Infrastructure \
  --startup-project src/Leds.GameEngine.Api \
  --context GameEngineDbContext \
  --output-dir Persistence/Migrations
```

### Appliquer les migrations

```bash
dotnet ef database update \
  --project src/Leds.GameEngine.Infrastructure \
  --startup-project src/Leds.GameEngine.Api \
  --context GameEngineDbContext
```
