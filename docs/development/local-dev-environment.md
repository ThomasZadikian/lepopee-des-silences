# Local Development Environment

## Prérequis

- .NET 10 SDK
- Node.js 20+
- npm
- Docker Desktop
- PowerShell 5.1+

## Démarrer l'environnement

```powershell
.\scripts\dev\start-dev.ps1
```

Démarre :
- PostgreSQL Game Engine (localhost:5432)
- PostgreSQL Player (localhost:5433)
- Game Engine API (http://localhost:5187)
- Catalog API (http://localhost:5193)
- Player API (http://localhost:5189)
- Web Client (http://localhost:5173)

## Arrêter l'environnement

```powershell
.\scripts\dev\stop-dev.ps1
```

Arrête les containers Docker. Les fenêtres `dotnet run` / `npm run dev` doivent être fermées manuellement.

## Réinitialiser les bases locales

```powershell
.\scripts\dev\reset-dev-db.ps1
```

Supprime les volumes Docker et recrée des bases propres.

## Appliquer les migrations

```powershell
.\scripts\dev\apply-migrations.ps1
```

Applique les migrations EF Core pour Game Engine (et Player quand EF sera ajouté).

## Services

| Service | URL | Port |
|---|---|---|
| Game Engine API | http://localhost:5187 | 5187 |
| Game Engine Swagger | http://localhost:5187/swagger | 5187 |
| Catalog API | http://localhost:5193 | 5193 |
| Catalog Swagger | http://localhost:5193/swagger | 5193 |
| Player API | http://localhost:5189 | 5189 |
| Player Swagger | http://localhost:5189/swagger | 5189 |
| Web Client | http://localhost:5173 | 5173 |
| Game Engine DB | localhost:5432 | 5432 |
| Player DB | localhost:5433 | 5433 |

## Configuration

Chaque service a son propre `appsettings.Development.json` :

- `services/game-engine/src/Leds.GameEngine.Api/appsettings.Development.json`
- `services/player/src/Leds.Player.Api/appsettings.Development.json`
- `services/catalog/src/Leds.Catalog.Api/appsettings.Development.json`

## Lancement manuel

Les services peuvent encore être lancés individuellement :

```powershell
# Game Engine
cd services/game-engine
dotnet run --project src/Leds.GameEngine.Api

# Catalog
cd services/catalog
dotnet run --project src/Leds.Catalog.Api

# Player
cd services/player
dotnet run --project src/Leds.Player.Api

# Web Client
cd apps/game-client
npm run dev
```

## Notes

- Les APIs tournent localement avec `dotnet run`.
- Les bases tournent dans Docker.
- Le frontend tourne avec `npm run dev`.
- Les secrets réels ne doivent pas être commit.
