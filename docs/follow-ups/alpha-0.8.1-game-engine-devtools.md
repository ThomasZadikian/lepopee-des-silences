# Alpha 0.8.1 - Game Engine Devtools

## Objectif

Ajouter des outils backend de developpement pour accelerer les tests manuels locaux du Game Engine : progression rapide de rooms, manipulation du contexte de room, activation de laws/curses et manipulation basique du combat courant.

Ces endpoints sont des cheats internes. Ils ne doivent jamais etre exposes hors environnement local de developpement.

## Production Safety

- Endpoints mappes uniquement quand `IWebHostEnvironment.IsDevelopment()` vaut `true`.
- Feature flag `DevTools:Enabled` desactive par defaut.
- Token local obligatoire via `X-Leds-DevTools-Token`.
- Namespace route dedie : `/api/dev/v2` uniquement.
- Aucun endpoint ajoute sous `/api/v2`.
- Aucun secret retourne dans les responses.
- Aucun detail Markov interne expose : pas de matrix, weights, probabilities, scores, seed fragments ou raisons de decision internes.
- Tests couvrant l'indisponibilite hors Development et quand le flag est desactive.

## Configuration Locale

Configuration par defaut dans `appsettings.Development.json` :

```json
{
  "DevTools": {
    "Enabled": false,
    "Token": ""
  }
}
```

Variables documentees dans `.env.example` :

```env
GAME_ENGINE_DEVTOOLS_ENABLED=false
GAME_ENGINE_DEVTOOLS_TOKEN=change-me-local-only
```

Activation recommandee en local uniquement via user-secrets, variables d'environnement locales ou fichier local non committe.

## Endpoints Ajoutes

Tous les endpoints exigent :

```http
X-Leds-DevTools-Token: <token-local>
```

### Health

```http
GET /api/dev/v2/status
```

Retourne uniquement l'etat d'activation et l'environnement courant.

### Rooms

```http
POST /api/dev/v2/runs/{runId}/advance-room
POST /api/dev/v2/runs/{runId}/advance-rooms
```

Body multi-room :

```json
{ "count": 3 }
```

`count` accepte `1..10`. La generation de room passe par `IRunGenerator.GenerateNextRoomAsync`; aucune room n'est creee a la main.

### PalaceRoomState

```http
POST /api/dev/v2/runs/{runId}/current-room/palace-state
```

```json
{ "state": "Silent" }
```

Valeurs supportees : `Neutral`, `Silent`, `Painful`, `Enraged`, `Violent`.

### RoomClimate

```http
POST /api/dev/v2/runs/{runId}/current-room/climate
```

```json
{ "climate": "Heatwave" }
```

Valeurs supportees : `None`, `Grey`, `Rain`, `Heatwave`, `Hail`.

Le climat est applique via `RunModifierType.RoomClimate` cible sur la room courante. `None` consomme le climat actif de la room courante.

### Palace Laws

```http
POST /api/dev/v2/runs/{runId}/laws/activate
POST /api/dev/v2/runs/{runId}/laws/clear
```

```json
{ "lawKey": "law-silence-v1" }
```

L'activation charge la definition via `ICatalogContentGateway` et utilise `Run.ActivatePalaceLaw`, donc l'idempotence existante est conservee.

### Curses

```http
POST /api/dev/v2/runs/{runId}/curses/activate
POST /api/dev/v2/runs/{runId}/curses/clear
```

```json
{ "curseKey": "curse.old-wound" }
```

Le modele `ActiveCurse` existe et est persiste. L'activation utilise `ICatalogCurseDefinitionProvider`, applique `Run.ApplyCurse` et ajoute un modifier `NextCombatDifficultyMultiplier` source `Curse`.

### Combat

```http
POST /api/dev/v2/runs/{runId}/combats/current/kill-enemies
POST /api/dev/v2/runs/{runId}/combats/current/enemies/{combatantId}/kill
POST /api/dev/v2/runs/{runId}/combats/current/combatants/{combatantId}/set-vitals
```

Body vitals :

```json
{ "vitality": 1, "guard": 99 }
```

Les mutations utilisent le runtime domain `Combat` / `Combatant`, pas de SQL direct.

## Exemples Curl

```bash
curl -H "X-Leds-DevTools-Token: local-token" \
  http://localhost:5187/api/dev/v2/status
```

```bash
curl -X POST -H "X-Leds-DevTools-Token: local-token" \
  -H "Content-Type: application/json" \
  -d '{"state":"Silent"}' \
  http://localhost:5187/api/dev/v2/runs/<runId>/current-room/palace-state
```

```bash
curl -X POST -H "X-Leds-DevTools-Token: local-token" \
  -H "Content-Type: application/json" \
  -d '{"climate":"Heatwave"}' \
  http://localhost:5187/api/dev/v2/runs/<runId>/current-room/climate
```

```bash
curl -X POST -H "X-Leds-DevTools-Token: local-token" \
  http://localhost:5187/api/dev/v2/runs/<runId>/combats/current/kill-enemies
```

## Volontairement Non Implemente

- Aucun frontend dev panel.
- Aucun endpoint sous `/api/v2`.
- Aucun endpoint status/effect combat : le runtime status/DOT/poison/bleed n'existe pas encore proprement.
- Aucune exposition des details Markov internes.
- Aucune mutation SQL directe.

## Regles De Suppression Future

- Les endpoints restent limites a `Development` et au flag `DevTools:Enabled`.
- Avant une release publique, verifier que `DevTools:Enabled` reste `false` par defaut.
- Supprimer ou durcir ces endpoints si une authentification admin officielle remplace les outils locaux.

## Validations Executees

- `dotnet build services/game-engine/Leds.GameEngine.slnx`
- `dotnet test services/game-engine/Leds.GameEngine.slnx --no-build`
- `dotnet test services/game-engine/tests/Leds.GameEngine.IntegrationTests/Leds.GameEngine.IntegrationTests.csproj --filter FullyQualifiedName~DevToolsEndpointTests`
- `dotnet test services/game-engine/tests/Leds.GameEngine.UnitTests/Leds.GameEngine.UnitTests.csproj --filter FullyQualifiedName~DevToolsRunDebugServiceTests`
