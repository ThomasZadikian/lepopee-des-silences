# Game Engine — Player Service Integration

## Objectif

Connecter Game Engine au Player Service pour initialiser une run depuis un snapshot joueur.

## Règle fondamentale

Game Engine copie le snapshot dans son propre état runtime.
Game Engine ne rappelle pas Player Service pendant les actions de combat.

## Endpoint consommé

```http
GET /api/v2/players/{playerId}/run-snapshot
```

Réponse attendue :

```json
{
  "playerId": "...",
  "displayName": "Thomas",
  "characters": [
    {
      "characterId": "...",
      "definitionKey": "character.player.self",
      "displayName": "Le Porteur",
      "maxVitality": 100,
      "baseMana": 0,
      "baseCharge": 0,
      "skillKeys": ["skill.basic.strike", "skill.basic.guard"]
    }
  ]
}
```

## Mode InMemory

Par défaut, `PlayerGateway:Mode = InMemory`.

`InMemoryPlayerRunSnapshotGateway` retourne un snapshot de développement stable :
- DisplayName : "Joueur"
- Character : "Le Porteur"
- MaxVitality : 100
- Skills : skill.basic.strike + skill.basic.guard

Ce mode permet aux tests et au dev local de fonctionner sans Player Service lancé.

## Mode Http

```json
{
  "PlayerGateway": {
    "Mode": "Http",
    "BaseUrl": "http://localhost:5189",
    "Timeout": "00:00:05"
  }
}
```

`HttpPlayerRunSnapshotGateway` appelle le Player Service et mappe la réponse.

Erreurs explicites :
- 404 → `NotFoundException("Player", playerId)`
- 409 → `ConflictException` avec le message du Player Service
- Service indisponible → exception HTTP claire

## Flux StartRun

1. Frontend appelle `POST /api/v2/runs` avec `{ playerId }`.
2. `StartRunCommandHandler` appelle `IPlayerRunSnapshotGateway.GetRunSnapshotAsync(playerId)`.
3. Le gateway retourne un `PlayerRunSnapshot`.
4. Le handler extrait le personnage principal du snapshot.
5. Le handler passe `maxVitality` et les skills au domaine `Run.StartNew(...)`.
6. `Run.StartNew` crée un `PlayerRuntimeState` avec les données du snapshot.
7. La run est persistée avec le snapshot copié dans son état runtime.
8. Le Game Engine ne rappelle plus le Player Service.

## Pourquoi copier et ne pas garder la référence

- Player Service = source de vérité permanente (hors run).
- Game Engine = source de vérité runtime (pendant la run).
- La run doit être autonome pendant toute sa durée.
- Si Player Service est indisponible, la run continue de fonctionner.

## Fichiers importants

- `services/game-engine/src/Leds.GameEngine.Application/Players/Ports/IPlayerRunSnapshotGateway.cs`
- `services/game-engine/src/Leds.GameEngine.Application/Players/Ports/PlayerRunSnapshot.cs`
- `services/game-engine/src/Leds.GameEngine.Infrastructure/Players/InMemoryPlayerRunSnapshotGateway.cs`
- `services/game-engine/src/Leds.GameEngine.Infrastructure/Players/HttpPlayerRunSnapshotGateway.cs`
- `services/game-engine/src/Leds.GameEngine.Infrastructure/Players/PlayerGatewayOptions.cs`
- `services/game-engine/src/Leds.GameEngine.Application/Runs/StartRun/StartRunCommandHandler.cs`
