# Combat Runtime Foundation

## Objectif

Le Combat runtime représente une confrontation active dans le Game Engine.

## Différence entre EncounterDraft et Combat

**CombatEncounterDraft** :
- décrit une rencontre prévue ;
- contient des définitions issues du Catalog ;
- ne contient pas encore d'état runtime.

**Combat** :
- contient les participants runtime (Combatant) ;
- contient les ressources courantes (Vitality, Guard, Mana, Charge) ;
- contient le tour courant ;
- contient le statut du combat.

## Modèle multi-alliés / multi-ennemis

Le système supporte :
- plusieurs alliés ;
- plusieurs ennemis ;
- des compétences par combattant ;
- une séparation entre côté Ally (`CombatantSide.Player`) et côté Enemy (`CombatantSide.Enemy`).

## Modèles

### `Combat` (Domain)

| Champ | Type | Description |
|---|---|---|
| `Id` | `CombatId` | Identifiant unique du combat |
| `RunId` | `RunId` | Identifiant de la run associée |
| `RoomId` | `RoomId` | Identifiant de la room où le combat a lieu |
| `NodeId` | `NodeId` | Identifiant du node d'où le combat a été déclenché |
| `Status` | `CombatStatus` | `Active`, `Completed`, `Failed` |
| `Allies` | `IReadOnlyCollection<Combatant>` | Participants alliés |
| `Enemies` | `IReadOnlyCollection<Combatant>` | Participants ennemis |
| `ActiveCombatantId` | `CombatantId?` | Combattant dont c'est le tour (premier allié à la création) |
| `TurnNumber` | `int` | Numéro de tour courant (commence à 1) |
| `CreatedAtUtc` | `DateTime` | Horodatage de création |

### `Combatant` (Domain)

| Champ | Type | Description |
|---|---|---|
| `Id` | `CombatantId` | Identifiant unique |
| `SourceKey` | `string` | Clé source (ex: `player.self`, `enemy.sentinel`) |
| `DisplayName` | `string` | Nom affiché |
| `Side` | `CombatantSide` | `Player` ou `Enemy` |
| `Archetype` | `string` | Rôle/archétype |
| `MaxVitality` | `int` | Vitalité maximale |
| `CurrentVitality` | `int` | Vitalité courante |
| `Guard` | `int` | Bouclier/absorption |
| `Mana` | `int` | Ressource mana |
| `Charge` | `int` | Ressource charge |
| `Status` | `CombatantStatus` | `Active` ou `Defeated` |
| `Skills` | `IReadOnlyCollection<CombatantSkill>` | Compétences du combattant |

### `CombatantSkill` (Domain)

| Champ | Type |
|---|---|
| `Key` | `string` |
| `DisplayName` | `string` |
| `SkillType` | `string` |
| `TargetingType` | `string` |
| `EffectType` | `string` |
| `ManaCost` | `int` |
| `ChargeCost` | `int` |
| `BasePower` | `int` |
| `Tags` | `IReadOnlyCollection<string>` |

### Énums

- `CombatStatus` : `Pending`, `Active`, `Completed`, `Failed`
- `CombatantStatus` : `Active`, `Defeated`
- `CombatantSide` : `Player`, `Enemy` (existant, réutilisé)

## Factory

`ICombatFactory` / `CombatFactory` (Application layer)

- transforme les alliés du draft en `Combatant` côté `Player` ;
- transforme les ennemis du draft en `Combatant` côté `Enemy` ;
- copie les `CombatEncounterDraftSkill` en `CombatantSkill` ;
- allié : `MaxVitality = 100`, `CurrentVitality = 100`, `Guard = Mana = Charge = 0` ;
- ennemi : `MaxVitality = 40 + BaseDifficulty * 10`, `CurrentVitality = MaxVitality` ;
- `Status = CombatStatus.Active`, `TurnNumber = 1` ;
- `ActiveCombatantId` = premier allié (déterministe).

## Intégration pipeline

Dans `ResolveCurrentEventCommandHandler`, quand un `CombatEncounterDraft` est généré, un `Combat` runtime est créé via `ICombatFactory` et retourné dans `ResolveCurrentEventResponse.Combat` (champ `CombatRuntimeDto?`).

## Ce que cette PR implémente

- `Combat` (domain entity)
- `Combatant` (domain entity)
- `CombatantSkill` (domain value object)
- `CombatFactory` (application service)
- `CombatRuntimeDto`, `CombatantRuntimeDto`, `CombatantSkillRuntimeDto`
- Intégration pipeline ResolveCurrentEvent

## Ce que cette PR n'implémente pas

- résolution des actions de combat ;
- dégâts ;
- IA ennemie ;
- initiative avancée ;
- effets temporaires / status ;
- loot ;
- frontend ;
- persistance DB ;
- tours de combat avancés.

## Déterminisme

La création du combat est déterministe. Aucun `Random` non seedé n'est utilisé.
`ActiveCombatantId` = premier allié dans l'ordre du draft.

## Invariants

- `Combat.Create` refuse : `allies` vide, `enemies` vide, combatant defeated
- `Combatant.Create` refuse : `sourceKey` vide, `displayName` vide, `maxVitality <= 0`, `currentVitality` hors [0, maxVitality], `guard/mana/charge < 0`
- `CombatantSkill.Create` refuse : `key` vide, `displayName` vide, `basePower < 0`, `manaCost < 0`, `chargeCost < 0`

## Tests

- **CombatTests** (16 tests) : création, validation, invariants, status transitions
- **CombatantTests** (8 tests) : création allié/ennemi, validation, MarkDefeated
- **CombatantSkillTests** (6 tests) : création, validation des coûts/power
- **CombatFactoryTests** (12 tests) : création depuis draft, stats, déterminisme, cas d'erreur
- **Handler tests** : adaptation pour le nouveau constructeur param

## Future work

- ajouter les actions de combat ;
- ajouter les règles de ciblage ;
- ajouter la résolution des skills ;
- ajouter la gestion des tours (initiative) ;
- ajouter les intentions ennemies ;
- exposer les endpoints de combat.
