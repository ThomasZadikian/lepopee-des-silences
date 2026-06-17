# alpha-0.8.1 — Run Party Snapshot

## Problème identifié

Le frontend devait afficher l'équipe active du joueur depuis n'importe quel écran de run (carte, interlude, salle résolue…). Seul le `CombatDto` exposait les données des combattants (`CombatantRuntimeDto`), ce qui rendait l'affichage de l'équipe impossible hors combat.

Architecturalement, l'équipe active appartient au run public, pas à un combat actif. Cette dépendance était incorrecte.

## Décision

Le snapshot d'équipe est rattaché au run dès sa création (`RunPlayerSnapshot`, `RunCharacterSnapshot`). Ces données existent déjà en domaine — elles n'étaient simplement pas exposées dans `RunDto`.

La solution est DTO/application-only : aucune migration EF, aucune modification domaine, aucune logique métier déplacée.

## DTO ajouté

**`RunPartySnapshotDto`** — dans `Leds.GameEngine.Application/Runs/Dtos/RunPartySnapshotDto.cs`

```csharp
RunPartySnapshotDto
  Members: RunPartyMemberDto[]
    Id              // CharacterId (Guid)
    DefinitionKey   // ex: "character.player.self"
    DisplayName
    MaxVitality     // depuis RunCharacterStatSnapshot
    CurrentVitality // live (PlayerRuntimeState) pour l'actif, MaxVitality pour les autres
    Guard           // live pour l'actif, 0 pour les autres
    Mana            // live pour l'actif, base snapshot pour les autres
    Charge          // live pour l'actif, base snapshot pour les autres
    IsActive        // true pour index 0 (personnage principal)
    IsDefeated      // PlayerRuntimeState.IsDefeated pour l'actif
    Skills: RunPartyMemberSkillDto[]
      Key, DisplayName, SkillType, TargetingMode, EffectType
      ManaCost, ChargeCost, BasePower
```

**Mapping** : index 0 dans `RunPlayerSnapshot.Characters` → reçoit les valeurs live de `PlayerRuntimeState`. Les membres suivants (companions futurs) reçoivent leurs stats de base.

## Endpoints concernés

| Endpoint | Avant | Après |
|---|---|---|
| `POST /api/v2/runs` | `Party: null` | `Party` présent |
| `GET /api/v2/runs/{runId}` | `Party: null` | `Party` présent |
| `POST /api/v2/runs/{id}/nodes/{nodeId}/choose` | — | Réponse inchangée, GET retourne `Party` |
| `POST /api/v2/runs/{id}/current-event/resolve` | — | Réponse inchangée, GET retourne `Party` |

Les réponses des endpoints de choix de node et résolution d'événement passent par `RunDto.FromDomain()` — `Party` y est inclus automatiquement.

## Limites assumées

- Actuellement, un seul personnage actif est suivi par `PlayerRuntimeState`. Les membres d'index > 0 n'ont pas de valeurs live (CurrentVitality = MaxVitality, Guard/Mana/Charge = base snapshot). Cette limite sera levée quand le système de party multi-personnages sera implémenté.
- `Party` est `null` pour les runs créées avant data-model-0.1 (absence de `RunPlayerSnapshot`).
- Les stats internes (AttackPower, Defense, Speed, Initiative, Recovery, Focus) ne sont pas exposées dans ce DTO — elles sont utiles au moteur mais pas à l'affichage frontend.

## Validations effectuées

- Build solution : `dotnet build services/game-engine/Leds.GameEngine.slnx`
- Tests : `dotnet test services/game-engine/Leds.GameEngine.slnx`
- Nouveaux tests unitaires (GetRunByIdQueryHandlerTests) :
  - `Handle_ShouldReturnPartySnapshot_WhenRunHasPlayerSnapshot`
  - `Handle_PartySnapshot_ShouldNotDependOnActiveCombat`
  - `Handle_ShouldReturnNullParty_WhenRunHasNoPlayerSnapshot`
- Nouveaux tests d'intégration (GetRunByIdEndpointTests) :
  - `GetRunById_ShouldReturnPartySnapshot_AfterRunStart`
  - `GetRunById_ShouldReturnPartySnapshot_AfterNodeChoice`
  - `StartRun_ShouldReturnPartySnapshotInInitialResponse`

## Prochaine PR frontend prévue

`feat(game-client): display party from run state` — brancher `PartyDrawer` sur `run.party` au lieu de `combatRuntime.allies`. Le frontend n'aura plus besoin du `CombatDto` pour afficher l'équipe sur la carte.
