# Local PostgreSQL Setup (Game Engine)

## Démarrer PostgreSQL

```bash
docker compose up -d game-engine-postgres
```

## Arrêter PostgreSQL

```bash
docker compose down
```

## Reset le volume (supprime toutes les données)

```bash
docker compose down -v
```

## Connection string locale

```
Host=localhost;Port=5432;Database=leds_game_engine;Username=postgres;Password=postgres
```

## Passer en mode Postgres

Dans `appsettings.Development.json` :

```json
{
  "Persistence": {
    "Mode": "Postgres"
  }
}
```

## Lancer les migrations

```bash
cd services/game-engine
dotnet ef database update \
  --project src/Leds.GameEngine.Infrastructure \
  --startup-project src/Leds.GameEngine.Api \
  --context GameEngineDbContext
```

## Lancer l'API avec PostgreSQL

```bash
cd services/game-engine
dotnet run --project src/Leds.GameEngine.Api
```

## Lancer l'API en mode InMemory (par défaut)

```json
{
  "Persistence": {
    "Mode": "InMemory"
  }
}
```
