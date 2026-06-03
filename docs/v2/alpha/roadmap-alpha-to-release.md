# L’épopée des silences — Roadmap de versioning v2

## 1. Logique générale de versioning

La v2 est découpée en versions progressives afin de distinguer clairement :

```text
alpha-0.0.x
→ fondations techniques et modules backend isolés

alpha-0.1.x
→ première boucle backend jouable de bout en bout

alpha-0.2.x
→ enrichissement gameplay systémique

alpha-0.3.x
→ persistance événementielle, projections et stabilisation runtime

alpha-0.4.x
→ intégration frontend web v2

alpha-0.5.x
→ consolidation microservices et observabilité

beta-0.x
→ version jouable testable avec contenu représentatif

1.0.0
→ première version v2 stable
```

Le versioning ne doit pas seulement refléter le nombre de commits.
Il doit refléter des **jalons fonctionnels et architecturaux réels**.

---

## 2. État actuel

## alpha-0.0.6 — état atteint

Statut : **atteint**

La v2 dispose désormais d’un Game Engine structuré, avec premières fondations de génération, Markov et résolution événementielle.

Contenu validé :

```text
- structure microservice Game Engine ;
- Clean Architecture ;
- CQRS / MediatR ;
- Run / Room / Node domain model ;
- endpoints de run initiaux ;
- shared-building-blocks minimal ;
- Catalog Service amorcé ;
- contrats Game Engine ↔ Catalog ;
- Markov Engine déterministe ;
- RoomType guidé par Markov ;
- NodeEventType guidé par Markov ;
- pipeline typé de résolution de contenu événementiel ;
- stratégie par famille d’événement ;
- tests unitaires et intégration existants maintenus.
```

Cette version correspond à la fin des **fondations de génération événementielle**.

Elle respecte la logique de transition v1→v2 : le Game Engine reste propriétaire du runtime gameplay, tandis que Catalog, Player, Identity, Audit/GDPR et Leaderboard restent des services spécialisés périphériques.

---

# 3. Roadmap alpha-0.0.x

## alpha-0.0.7 — Node Event Resolution Use Case

Commit cible :

```text
feat(game-engine): expose node event resolution use case
```

Objectif :

Créer le premier vrai cas d’usage de résolution d’événement de node.

À faire :

```text
- ResolveNodeEventCommand ;
- ResolveNodeEventCommandHandler ;
- validation run / room / node / event ;
- appel à IEventContentResolver ;
- résultat applicatif NodeEventResolved ;
- transition d’état du NodeEvent si nécessaire ;
- tests unitaires handler ;
- tests d’intégration API si endpoint exposé.
```

Critère de sortie :

```text
Un événement de node peut être résolu côté backend via un use case CQRS.
```

---

## alpha-0.0.8 — Combat Runtime Domain Foundation

Commit cible :

```text
feat(game-engine): introduce combat runtime domain
```

Objectif :

Créer le domaine runtime minimal du combat.

À faire :

```text
- CombatInstance ;
- CombatId ;
- CombatantSnapshot ;
- CombatState ;
- CombatTurn ;
- CombatAction ;
- DamageResolver minimal ;
- règles de victoire / défaite ;
- invariants domaine ;
- tests unitaires domaine.
```

Ne pas encore faire :

```text
- endpoint complet d’action combat ;
- IA avancée ;
- skills complexes ;
- effets de statut ;
- rewards post-combat.
```

Critère de sortie :

```text
Un combat minimal peut exister et être résolu en mémoire au niveau domaine.
```

---

## alpha-0.0.9 — Start Combat from Resolved Event

Commit cible :

```text
feat(game-engine): start combat from resolved event content
```

Objectif :

Relier le pipeline événementiel typé au domaine Combat.

À faire :

```text
- CombatInstanceFactory ;
- transformation ResolvedCombatEventContent → CombatInstance ;
- EnemyTemplateSnapshot → CombatantSnapshot ;
- premier PlayerCombatantSnapshot temporaire ;
- intégration avec ResolveNodeEventCommandHandler ;
- tests de création de combat depuis un event combat.
```

Critère de sortie :

```text
Un événement Combat ou Elite résolu peut créer une CombatInstance serveur-autoritaire.
```

---

## alpha-0.0.10 — Combat Action Flow

Commit cible :

```text
feat(game-engine): expose combat action flow
```

Objectif :

Créer la première boucle d’action de combat.

À faire :

```text
- SubmitCombatActionCommand ;
- SubmitCombatActionCommandHandler ;
- action Attack minimale ;
- calcul dégâts ;
- mise à jour des HP ;
- fin de combat si victoire / défaite ;
- endpoint API si nécessaire ;
- tests d’intégration.
```

Critère de sortie :

```text
Le backend peut démarrer un combat et résoudre au moins une action joueur.
```

---

## alpha-0.0.11 — Reward Offer Foundation

Commit cible :

```text
feat(game-engine): introduce reward offer flow
```

Objectif :

Créer la première structure de récompense runtime.

À faire :

```text
- RewardOffer ;
- RewardChoice ;
- RewardSource ;
- RewardOfferFactory ;
- RewardSelectionCommand ;
- application d’une récompense temporaire de run ;
- tests domaine et application.
```

Critère de sortie :

```text
Une récompense peut être proposée puis sélectionnée après résolution d’un événement.
```

---

## alpha-0.0.12 — Minimal Room Loop Completion

Commit cible :

```text
feat(game-engine): complete minimal room progression loop
```

Objectif :

Permettre une room complète jouable côté backend.

À faire :

```text
- choisir un node ;
- résoudre son événement ;
- gérer combat ou récompense simple ;
- marquer le node comme résolu ;
- générer / débloquer les nodes suivants ;
- atteindre le boss de room ;
- résoudre le boss de room ;
- marquer la room comme terminée.
```

Critère de sortie :

```text
Une room peut être parcourue de bout en bout côté backend.
```

---

# 4. Passage en alpha-0.1.0

## alpha-0.1.0 — First Backend Playable Slice

Tag cible :

```text
alpha-0.1.0
```

Critère principal :

```text
Une boucle backend minimale est jouable de bout en bout.
```

Scénario attendu :

```text
1. Démarrer une run.
2. Générer une room initiale.
3. Choisir un node disponible.
4. Résoudre un événement.
5. Démarrer un combat si nécessaire.
6. Exécuter une action de combat.
7. Terminer le combat.
8. Recevoir une récompense.
9. Appliquer ou sélectionner la récompense.
10. Résoudre le node.
11. Générer la progression suivante.
12. Atteindre le boss de room.
13. Résoudre le boss.
14. Terminer la room.
```

Critère de qualité :

```text
- tests unitaires principaux ;
- tests d’intégration API sur la boucle ;
- aucune dépendance directe entre Game Engine et Catalog Domain ;
- backend serveur-autoritaire ;
- génération déterministe par seed ;
- versioning de génération explicite.
```

---

# 5. Roadmap alpha-0.1.x

## alpha-0.1.1 — Stabilisation Run Loop

Commit cible :

```text
fix/game-engine: stabilize minimal run loop
```

Objectif :

Corriger les incohérences détectées après la première boucle jouable.

À faire :

```text
- durcir les validations ;
- améliorer les messages d’erreur ;
- renforcer les tests d’état ;
- corriger les cas limites de progression ;
- stabiliser les transitions de room.
```

---

## alpha-0.1.2 — Catalog-backed Event Templates

Commit cible :

```text
feat(game-engine): resolve event templates from catalog snapshots
```

Objectif :

Réduire les placeholders et brancher davantage de contenu Catalog.

À faire :

```text
- EventTemplateSnapshot plus riche ;
- sélection template selon NodeEventType ;
- filtrage par RoomType ;
- filtrage par RiskLevel ;
- gestion des versions de template.
```

---

## alpha-0.1.3 — Player Snapshot Foundation

Commit cible :

```text
feat(game-engine): introduce run player snapshot
```

Objectif :

Préparer la séparation Game Engine / Player.

À faire :

```text
- PlayerRunSnapshot ;
- stats de départ ;
- inventaire temporaire de run ;
- skills accessibles ;
- port IPlayerSnapshotGateway ;
- adapter InMemory temporaire.
```

---

## alpha-0.1.4 — Improved Combat MVP

Commit cible :

```text
feat(game-engine): improve combat runtime resolution
```

Objectif :

Rendre le combat minimal plus crédible.

À faire :

```text
- initiative simple ;
- skills basiques ;
- coût d’action ;
- défense ;
- logs de combat ;
- victoire / défaite mieux structurées.
```

---

## alpha-0.1.5 — Reward Application

Commit cible :

```text
feat(game-engine): apply run rewards
```

Objectif :

Appliquer réellement les récompenses au snapshot de run.

À faire :

```text
- item temporaire ;
- bonus stat temporaire ;
- heal ;
- monnaie de run si nécessaire ;
- validation des choix.
```

---

# 6. Roadmap alpha-0.2.x

## alpha-0.2.0 — Palace Laws Runtime

Objectif :

Introduire les Lois du Palais en runtime.

À faire :

```text
- ActivePalaceLaw ;
- PalaceLawRuntimeEffect ;
- application sur génération ;
- application sur combat ;
- application sur rewards ;
- interaction avec Markov ;
- tests de contraintes.
```

Critère de sortie :

```text
Les Lois du Palais influencent réellement le runtime.
```

---

## alpha-0.2.1 — Event Content Selection by Context

Objectif :

Améliorer la sélection des contenus selon le contexte.

À faire :

```text
- RoomType ;
- RiskLevel ;
- RewardProfile ;
- historique de run ;
- lois actives ;
- versions Catalog ;
- Markov spécialisé.
```

---

## alpha-0.2.2 — NPC Event Foundation

Objectif :

Introduire les premiers événements PNJ.

À faire :

```text
- NpcTemplateSnapshot ;
- NpcRuntimeInteraction ;
- dialogue simple ;
- choix joueur ;
- trace d’interaction ;
- préparation attitude Markov.
```

---

## alpha-0.2.3 — NPC Attitude Markov

Objectif :

Brancher Markov sur les attitudes PNJ.

À faire :

```text
- états émotionnels / psychologiques narratifs non médicaux ;
- matrice d’attitude ;
- résolution par interaction ;
- impact sur dialogue ;
- impact sur récompense ou risque.
```

---

# 7. Roadmap alpha-0.3.x

## alpha-0.3.0 — Run Event Store Foundation

Objectif :

Introduire la persistance événementielle des runs.

À faire :

```text
- RunEvent ;
- RunEventStore ;
- événements de génération ;
- événements de choix ;
- événements de résolution ;
- événements de combat ;
- événements de récompense ;
- projections simples.
```

Critère de sortie :

```text
La run peut être reconstruite ou auditée à partir d’événements.
```

---

## alpha-0.3.1 — Audit and Traceability Events

Objectif :

Préparer Audit/GDPR et observabilité.

À faire :

```text
- événements auditables ;
- corrélation runId / playerId ;
- trace des décisions Markov ;
- trace des versions de matrice ;
- trace des récompenses.
```

---

## alpha-0.3.2 — Leaderboard Projection Foundation

Objectif :

Préparer les projections de score.

À faire :

```text
- score de run ;
- projection completed run ;
- season placeholder ;
- port LeaderboardPublisher ;
- adapter InMemory / EventBus plus tard.
```

---

# 8. Roadmap alpha-0.4.x

## alpha-0.4.0 — Web Client Gameplay Skeleton

Objectif :

Commencer le client web v2.

À faire :

```text
- Vue 3 / TypeScript ;
- écran run ;
- affichage room map ;
- choix de node ;
- affichage événement ;
- affichage combat minimal ;
- affichage reward.
```

Critère de sortie :

```text
La première boucle alpha-0.1 est utilisable depuis le client web.
```

---

# 9. Roadmap alpha-0.5.x

## alpha-0.5.0 — Service Integration Hardening

Objectif :

Renforcer l’architecture microservices.

À faire :

```text
- HTTP Catalog adapter ;
- cache éventuel ;
- Player snapshot adapter ;
- préparation RabbitMQ ;
- contracts versionnés ;
- retries / timeouts ;
- observabilité ;
- health checks.
```

---

# 10. Passage en beta

## beta-0.1.0 — First Playable Vertical Slice

Critère :

```text
Le jeu est jouable de manière minimale par un utilisateur test.
```

Contenu attendu :

```text
- client web ;
- run complète ;
- combat minimal ;
- rewards ;
- plusieurs rooms ;
- boss de room ;
- premiers événements narratifs ;
- premiers PNJ ;
- premières lois ;
- sauvegarde / reprise basique ;
- logs et traces suffisants.
```

---

# 11. Passage en 1.0.0

## 1.0.0 — Première version stable v2

Critère :

```text
Le jeu dispose d’une boucle roguelite narrative stable, testée, versionnée et exploitable.
```

Contenu attendu :

```text
- plusieurs types de rooms ;
- plusieurs familles d’événements ;
- combat stable ;
- rewards stables ;
- progression durable ;
- Catalog suffisamment fourni ;
- Player service intégré ;
- audit minimal ;
- leaderboard ou projection de run ;
- frontend web jouable ;
- documentation technique ;
- monitoring ;
- CI/CD ;
- couverture de tests satisfaisante.
```

---

# 12. Règle de versioning à conserver

Une version doit être incrémentée quand l’un des éléments suivants change :

```text
- format de génération ;
- comportement déterministe à seed identique ;
- version de matrice Markov ;
- structure de run ;
- structure de node ;
- structure des événements ;
- contrat interservice ;
- endpoint public ;
- modèle runtime ;
- persistance ;
- projection.
```

Si une modification peut changer le résultat d’une run pour une même seed, elle doit être documentée et versionnée.

---

# 13. Synthèse

État actuel :

```text
alpha-0.0.6
→ fondations génération + Markov + pipeline événementiel typé
```

Prochain grand jalon :

```text
alpha-0.1.0
→ première boucle backend jouable
```

Condition pour alpha-0.1.0 :

```text
Une room complète peut être parcourue côté backend, avec résolution d’événement, combat minimal, reward minimal et boss de room.
```

La priorité immédiate est donc :

```text
alpha-0.0.7
→ ResolveNodeEventCommand

alpha-0.0.8
→ Combat runtime domain

alpha-0.0.9
→ Start combat from resolved event

alpha-0.0.10
→ Combat action flow

alpha-0.0.11
→ Reward offer flow

alpha-0.0.12
→ Minimal room loop completion

alpha-0.1.0
→ First backend playable slice
```
