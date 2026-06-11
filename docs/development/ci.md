# CI v2

## Objectif

Sécuriser les builds et tests de la branche v2 automatiquement.

## Déclencheurs

- push vers `develop`
- pull request vers `develop`

## Jobs

### Backend (matrix)

Pour chaque service, la CI vérifie :

- `dotnet restore`
- `dotnet build --configuration Release`
- `dotnet test --configuration Release`

Services vérifiés :

| Service | Solution |
|---|---|
| Game Engine | `services/game-engine/Leds.GameEngine.slnx` |
| Catalog | `services/catalog/Leds.Catalog.slnx` |
| Player | `services/player/Leds.Player.slnx` |

### Frontend

Pour `apps/web-client` :

- `npm ci`
- `npm run build`
- `npm run test`

## Fichier workflow

`.github/workflows/v2-ci.yml`

## Technologies

- .NET 10.0
- Node.js 22
- NuGet cache activé
- npm cache activé

## Non-objectifs

- pas de déploiement
- pas de staging public
- pas de secrets réels
- pas de pipeline production
- pas de migration automatique en prod

## Prochaines étapes

- ajouter CodeQL (security scanning)
- ajouter SonarCloud (quality gate)
- ajouter coverage reports
- ajouter pipeline staging
- ajouter service-to-service auth checks
