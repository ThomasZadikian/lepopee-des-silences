# Playable Combat Flow

## Objectif

Décrire le flow backend complet du combat jouable du point de vue des appels API et des transitions d'état.

## Flow

```
StartRun
  → POST /api/v2/runs

ChooseNode (Combat)
  → POST /api/v2/runs/{runId}/nodes/{nodeId}/choose

ResolveCurrentEvent
  → POST /api/v2/runs/{runId}/current-event/resolve
  → Retourne CombatRuntimeDto + CombatEncounterDraftDto

GET current-combat (optionnel, vérification)
  → GET /api/v2/runs/{runId}/current-combat

Player skill action (loop jusqu'à victoire)
  → POST /api/v2/runs/{runId}/combats/{combatId}/skill-actions
  → Retourne CombatSkillActionResult avec logs + combat mis à jour + statut

Enemy auto turns (résolu automatiquement après chaque action joueur)
  → Inclus dans la réponse du skill-actions

Combat completion (victoire)
  → CombatCompleted = true
  → CanProgressRun = true
  → ActiveCombat nettoyé (404 sur GET current-combat)
  → Run.ResolveCurrentEvent() appelé (progression débloquée)

Run progression
  → La run peut continuer vers l'event suivant
```

## Backend Guarantees

- **Multi-ennemis** : combat supporte N ennemis et N alliés
- **Skill-first** : toutes les actions passent par une skill (même l'attaque de base)
- **Ciblage validé** : les règles de targeting (Self, SingleEnemy, SingleAlly, AllEnemies, AllAllies) sont appliquées avant résolution
- **Effets de base résolus** : Damage, Guard, Weaken (log), Disrupt (log)
- **Tours déterministes** : alliés d'abord (ordre d'ajout), puis ennemis (ordre d'ajout)
- **Ennemis automatiques** : après chaque action joueur, les ennemis consécutifs sont résolus automatiquement
- **Victoire/défaite** : complétion quand tous les ennemis vaincus, échec quand tous les alliés vaincus
- **Reprise de Run** : après victoire, l'event courant est résolu et la progression continue
- **Idempotence** : double-clic et appels répétés gérés (409 Conflict)

## Endpoints API

| Endpoint | Méthode | Description |
|----------|---------|-------------|
| `/api/v2/runs/{runId}/current-event/resolve` | POST | Résout l'event du nœud sélectionné |
| `/api/v2/runs/{runId}/current-combat` | GET | Récupère le combat actif |
| `/api/v2/runs/{runId}/combats/{combatId}/skill-actions` | POST | Envoie une action skill |

## DTOs Principaux

- `CombatRuntimeDto` : état complet du combat (id, status, turnNumber, activeCombatantId, allies, enemies)
- `CombatantRuntimeDto` : état d'un combattant (vitalité, guard, skills, statut)
- `CombatantSkillRuntimeDto` : skill disponible (key, displayName, targetingType, effectType, basePower, etc.)
- `CombatSkillActionResult` : résultat d'action (accepted, combat mis à jour, logs, complétion)
- `CombatLogEntryDto` : entrée de log (type, message, actorId, skillKey)
- `CombatEncounterDraftDto` : draft de rencontre (metadata, ennemis, alliés)

## Error Handling

| Situation | HTTP Code |
|-----------|-----------|
| Payload invalide | 400 BadRequest |
| Domaine invalide (combattant déjà vaincu, etc.) | 400 BadRequest |
| Ressource inconnue | 404 NotFound |
| Conflit métier (mauvais combatId, déjà terminé, pas le bon tour) | 409 Conflict |
| Erreur inattendue | 500 Internal Server Error |

## Tests

- **CombatFullFlowEndpointTests** (integration) : flow complet victoire, résolution, logs, idempotence
- **CombatMultiEnemyFlowTests** (unit) : multi-ennemis, ciblage, progression de tour, défaite
- **CombatActionEndpointTests** (integration) : actions skill, validation ciblage, completion
- **UseCombatSkillCommandHandlerTests** (unit) : handler complet avec validation, effets, progression, ennemis
- **CombatTests** (unit) : domaine Combat (advance turn, complete, fail, ensure actor can act)

## Current Limitations

- Pas d'ATB (initiative dynamique) avant 0.4.0
- Pas d'IA ennemie avancée (sélection déterministe : offensive → Damage → première skill)
- Pas de frontend combat
- Pas d'animations
- Pas d'équilibrage final (valeurs par défaut : allié 100 HP, ennemi 40 + 10/difficulté)
- Pas de rewards complexes post-combat
- Pas d'historique complet de combat persisté
- Weaken/Disrupt résolus en log uniquement (pas de statuts durables)
- Description field non présente sur CombatantSkillRuntimeDto (sera ajoutée si nécessaire)
