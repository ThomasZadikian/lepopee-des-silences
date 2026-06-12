# Catalog PostgreSQL Persistence and Versioned Seeding

## Objectif

Persister les définitions Catalog en PostgreSQL avec un mécanisme de seed versionné.

## Tables

| Table | Description |
|---|---|
| `catalog_skill_definitions` | Définitions de skills |
| `catalog_enemy_definitions` | Définitions d'ennemis |
| `catalog_item_definitions` | Définitions d'items |
| `catalog_palace_law_definitions` | Définitions de lois du palais |
| `catalog_seed_versions` | Versions de seed appliquées |

## Mode InMemory (défaut)

```json
{
  "Persistence": {
    "Mode": "InMemory"
  }
}
```

Les définitions viennent des InMemory read stores (seed en mémoire).

## Mode PostgreSQL

```json
{
  "Persistence": {
    "Mode": "Postgres"
  },
  "ConnectionStrings": {
    "CatalogDb": "Host=localhost;Port=5434;Database=leds_catalog;Username=postgres;Password=postgres"
  }
}
```

Les définitions viennent des EF read stores (PostgreSQL).

## Seed versionné

Le `CatalogSeedRunner` applique un contenu minimal de façon idempotente :

- Version : `alpha-0.5.5`
- Seed key : `base-catalog`
- Enregistre la version dans `catalog_seed_versions`
- Ne crée pas de doublons

Contenu du seed :

| Type | Clés |
|---|---|
| Skills | `skill.basic.strike`, `skill.basic.guard` |
| Enemies | `enemy.threshold.echo`, `enemy.threshold.fracture` |
| Items | `item.consumable.minor-heal` |
| Palace Laws | `law.threshold.silence-weight` |

## Commandes EF Core

```bash
dotnet ef migrations add <Name> \
  --project src/Leds.Catalog.Infrastructure \
  --startup-project src/Leds.Catalog.Api \
  --context CatalogDbContext \
  --output-dir Persistence/Migrations
```

## Fichiers importants

| Fichier | Rôle |
|---|---|
| `Infrastructure/Persistence/CatalogDbContext.cs` | DbContext |
| `Infrastructure/Persistence/CatalogDbContextFactory.cs` | Design-time factory |
| `Infrastructure/Persistence/CatalogSeedRunner.cs` | Seed idempotent |
| `Infrastructure/Persistence/Entities/` | Entities EF |
| `Infrastructure/Persistence/Configurations/` | Configurations EF |
| `Infrastructure/ReadStores/Ef/` | EF read stores |

## Non-objectifs

- pas de CRUD admin
- pas d'authentification
- pas de contenu massif
- pas de nouvelles mécaniques gameplay
