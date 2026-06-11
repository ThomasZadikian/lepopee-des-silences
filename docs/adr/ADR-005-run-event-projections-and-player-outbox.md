# ADR-005 — Projections de résultats de run et outbox Game Engine vers Player

## Statut

Acceptée.

## Date

2026-06-11

## Contexte

Le Game Engine Service possède l'état runtime des runs : rooms, nodes, combats, rewards, progression. Le Player Service possède la progression permanente du joueur : profil, personnages, statistiques, historique. Le Catalog Service possède les définitions stables.

Règle fondamentale :

```
Permanent = Player Service
Runtime de run = Game Engine Service
Définitions = Catalog Service
```

Le Player Service ne doit pas être appelé à chaque action de combat. Le snapshot joueur est copié une seule fois au démarrage de la run, et le Game Engine reste autonome pendant toute la durée de la run.

Cependant, à la fin d'une run, le Game Engine doit informer le Player Service que quelque chose s'est produit :

- run démarrée ;
- run terminée ;
- run échouée ;
- run abandonnée ;
- boss rencontré ;
- rewards sélectionnées ;
- statistiques de run disponibles.

Un appel synchrone direct depuis le handler Game Engine créerait un risque de dual-write :

```text
Game Engine sauvegarde la Run
PUIS appelle directement Player Service
PUIS Player Service échoue
→ état Game Engine sauvegardé mais Player non mis à jour
```

Ou inversement :

```text
Player mis à jour
mais sauvegarde Game Engine échoue
→ statistiques permanentes incohérentes
```

L'architecture doit rester robuste avant exposition publique.

## Décision

Utiliser un pattern Outbox côté Game Engine.

Principe :

1. Quand le Game Engine modifie son état transactionnel, il écrit aussi un événement d'intégration dans une table outbox locale.
2. La transaction PostgreSQL est commitée.
3. Un dispatcher lit les messages non publiés.
4. Le dispatcher transmet les événements vers les services intéressés, notamment Player Service.
5. En cas d'échec, le message reste retryable.
6. Une fois traité, le message est marqué comme processed.

L'outbox appartient au Game Engine. Player ne lit pas directement la base Game Engine. Game Engine ne modifie pas directement la base Player.

## Événements d'intégration MVP

### RunStartedIntegrationEvent

- **Producteur** : Game Engine Service
- **Consommateur principal** : Player Service
- **Utilité** : Incrémenter `PlayerProgression.TotalRunsStarted`
- **Données minimales** : eventId, occurredAtUtc, runId, playerId, seed, generatorVersion
- **Statut** : MVP

### RunCompletedIntegrationEvent

- **Producteur** : Game Engine Service
- **Consommateur principal** : Player Service
- **Utilité** : Incrémenter `PlayerProgression.TotalRunsCompleted` et enregistrer les statistiques finales
- **Données minimales** : eventId, occurredAtUtc, runId, playerId, finalDepth, roomsCompleted, combatsWon, rewardsSelected, outcome, generatorVersion
- **Statut** : MVP

### RunFailedIntegrationEvent

- **Producteur** : Game Engine Service
- **Consommateur principal** : Player Service
- **Utilité** : Incrémenter `PlayerProgression.TotalRunsFailed`
- **Données minimales** : eventId, occurredAtUtc, runId, playerId, failureReason, finalDepth
- **Statut** : MVP

### RunAbandonedIntegrationEvent

- **Producteur** : Game Engine Service
- **Consommateur principal** : Player Service
- **Utilité** : Incrémenter `PlayerProgression.TotalRunsAbandoned` (si ce champ est ajouté)
- **Données minimales** : eventId, occurredAtUtc, runId, playerId, finalDepth
- **Statut** : MVP

## Événements futurs possibles

| Événement | Description | Statut |
|---|---|---|
| `RoomCompletedIntegrationEvent` | Room complètement résolue | Futur |
| `BossEncounteredIntegrationEvent` | Boss de salle rencontré | Futur |
| `HimLitEncounteredIntegrationEvent` | Événement narratif spécifique | Futur |
| `RewardSelectedIntegrationEvent` | Reward sélectionnée | Futur |
| `PlayerDeathIntegrationEvent` | Mort du joueur en combat | Futur |
| `PalaceLawAppliedIntegrationEvent` | Loi du palais activée | Futur |

## Table outbox cible

Nom recommandé : `game_engine_outbox_messages`

```sql
-- Structure conceptuelle (pas une migration dans cette PR)
CREATE TABLE game_engine_outbox_messages (
    id              UUID PRIMARY KEY,
    type            VARCHAR(128) NOT NULL,
    payload_json    TEXT NOT NULL,
    occurred_at_utc TIMESTAMPTZ NOT NULL,
    dispatched_at_utc TIMESTAMPTZ NULL,
    retry_count     INT NOT NULL DEFAULT 0,
    last_error      TEXT NULL,
    correlation_id  UUID NULL,
    causation_id    UUID NULL,
    created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT now(),
    event_version VARCHAR(32) NOT NULL,
    destination VARCHAR(128) NULL
);
```

Champs :

| Champ | Type | Description |
|---|---|---|
| `id` | UUID | Identifiant unique du message outbox |
| `type` | VARCHAR(128) | Nom logique de l'événement (ex: `RunCompletedIntegrationEvent`) |
| `payload_json` | TEXT | Payload sérialisé de l'événement |
| `occurred_at_utc` | TIMESTAMPTZ | Date de production métier de l'événement |
| `dispatched_at_utc` | TIMESTAMPTZ | Date à laquelle le message a été traité avec succès |
| `retry_count` | INT | Nombre de tentatives de dispatch |
| `last_error` | TEXT | Dernière erreur observée pendant le dispatch |
| `correlation_id` | UUID | Identifiant de regroupement (même run/requête) |
| `causation_id` | UUID | Identifiant de l'action ayant causé ce message |
| `created_at_utc` | TIMESTAMPTZ | Date d'insertion |

## Consommation côté Player Service

Player Service consomme les événements pour mettre à jour ses projections permanentes.

MVP cible :

| Événement | Effet côté Player |
|---|---|
| `RunStartedIntegrationEvent` | `PlayerProgression.TotalRunsStarted += 1` |
| `RunCompletedIntegrationEvent` | `PlayerProgression.TotalRunsCompleted += 1` + statistiques finales |
| `RunFailedIntegrationEvent` | `PlayerProgression.TotalRunsFailed += 1` |
| `RunAbandonedIntegrationEvent` | `PlayerProgression.TotalRunsAbandoned += 1` |

Player Service ne doit pas recalculer le déroulé de la run. Il reçoit un résumé fiable produit par Game Engine.

## Garanties attendues

- **At-least-once delivery** : un événement peut être livré plusieurs fois.
- **Idempotence côté consommateur** : Player Service doit ignorer les doublons via `eventId` unique.
- **`eventId` unique** : chaque message outbox a un identifiant unique.
- **Versionnement** : les événements doivent être versionnés si leur payload change.
- **Erreurs** : les erreurs ne doivent pas être avalées silencieusement. `last_error` doit être loggé.
- **Observabilité** : les messages en échec doivent être visibles.

## Règles d'architecture

- Game Engine ne met jamais à jour directement la base Player.
- Player ne lit jamais directement la base Game Engine.
- Les projections permanentes Player viennent d'événements d'intégration.
- Les événements sont produits par Game Engine dans sa propre transaction.
- Les appels HTTP synchrones restent réservés aux queries nécessaires au démarrage de run, comme `GET run-snapshot`.

## Alternatives rejetées

### 1. Appel HTTP synchrone direct Game Engine → Player

**Avantage :** Simple à comprendre.

**Inconvénients :**
- Dual-write
- Coupling temporel
- Player indisponible bloque la fin de run
- Retry difficile
- Incohérences possibles

**Conclusion :** Rejeté pour les mises à jour permanentes de progression.

### 2. Event bus immédiat sans outbox

**Avantage :** Découplage apparent.

**Inconvénients :**
- Message publié mais transaction DB échouée
- Transaction DB réussie mais message non publié
- Perte possible
- Pas de garantie locale

**Conclusion :** Rejeté sans outbox.

### 3. Player lit directement la DB Game Engine

**Avantage :** Évite un événement.

**Inconvénients :**
- Couplage fort
- Violation microservices
- Partage de modèle de données
- Migrations dangereuses

**Conclusion :** Rejeté.

### 4. Outbox Game Engine (décision acceptée)

**Avantages :**
- Transaction locale fiable
- Retries
- Découplage
- Compatible RabbitMQ plus tard
- Cohérent avec microservices

**Inconvénients :**
- Plus de complexité
- Nécessite dispatcher
- Nécessite idempotence côté consommateur

**Conclusion :** Accepté.

## Consequences

### Positives

- Pas de dual-write.
- Player permanent mis à jour de façon fiable.
- Game Engine reste propriétaire des runs.
- Player reste propriétaire des statistiques.
- Future intégration RabbitMQ facilitée.
- Pattern robuste et bien connu.

### Négatives

- Nouvelle table à créer.
- Dispatcher à implémenter.
- Gestion des retries.
- Idempotence à prévoir.
- Observabilité nécessaire.

## Implémentation recommandée

La première PR de code après cette ADR :

```text
feat(game-engine): add outbox messages for run outcomes
```

Scope :
- Créer table `game_engine_outbox_messages` dans Game Engine
- Écrire un message outbox quand une run se termine, échoue ou est abandonnée
- Ne pas encore utiliser RabbitMQ
- Dispatcher InProcess ou HostedService simple possible
- Ou même outbox écrite sans dispatcher dans une première PR

Puis une PR suivante :

```text
feat(player): consume run outcome projections
```

## Prochaines étapes

1. Implémenter la table `game_engine_outbox_messages` dans Game Engine.
2. Écrire les messages outbox dans les handlers `UseCombatSkillCommandHandler` (combat complet/échoué) et `AbandonRunCommandHandler`.
3. Implémenter un dispatcher simple (InProcess ou HostedService).
4. Ajouter un endpoint ou un consumer dans Player Service pour traiter les événements.
5. Ajouter l'idempotence côté Player via `eventId`.
6. Ajouter observabilité (logs, métriques, table monitoring).
7. Envisager RabbitMQ pour le dispatch asynchrone quand le volume le justifie.
