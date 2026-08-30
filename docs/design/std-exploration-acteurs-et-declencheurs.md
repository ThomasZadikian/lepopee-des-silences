# STD — Moteur d'acteurs d'exploration et déclencheurs

Statut : **référence d'implémentation**
Référence : **LEDS-STD-EXP-001**
Version : **1.0 — 25/08/2026**

## 1. Portée technique

Cette STD traduit `LEDS-SFD-EXP-001` dans l'architecture existante .NET / EF
Core / Vue. Elle réutilise `RoomNpc` pour les acteurs neutres et `MapNode` pour
la représentation d'exploration des rencontres. Aucun nouveau stockage n'est
nécessaire : les positions des deux agrégats sont déjà persistées.

## 2. Décisions d'architecture

### ADR-EXP-001 — Horloge pilotée par le client, simulation serveur

Le client demande un tick lorsqu'il est en phase `Map`. Le domaine serveur
reste l'unique autorité sur l'éligibilité, les collisions, la destination et le
contact. Le tick ne dépend pas du temps mural côté serveur et reste donc
rejouable et testable.

### ADR-EXP-002 — Rencontre mobile portée par `MapNode`

Un nœud de type combat devient la présence ennemie sur la carte. Sa position
`Lane/Row` est mutable tant que le nœud est `Available`. Son identité, son
niveau de risque et son profil de récompense restent inchangés. Les combats
scénarisés continuent d'être produits par les résolveurs d'événements et les
règles locales, sans `MapNode` mobile supplémentaire.

### ADR-EXP-003 — Commande séparée du déplacement joueur

`MoveParty` ne déplace plus les PNJ. Une commande `AdvanceRoomActors` produit un
pas autonome. Le mode `HostilesOnly` est appelé après l'animation joueur ; le
mode complet est appelé par l'horloge d'inactivité.

### ADR-EXP-004 — Animation par projection, état logique immédiat

Le serveur persiste immédiatement la position finale et renvoie la liste des
transitions. Le client conserve temporairement une position d'affichage
fractionnaire et interpole vers la position logique. Les collisions et les
clics utilisent toujours la position logique finale.

## 3. Contrat de domaine

### 3.1 `Room.AdvanceActors`

Entrée : `ActorAdvanceMode` (`All` ou `HostilesOnly`).

Sortie :

```text
ActorAdvanceResult
  Movements[]: ActorMovement(ActorId, ActorKind, FromX, FromY, ToX, ToY)
  TriggeredNodeId?: Guid
```

Préconditions : `Room.State == Active`. Sinon, aucun acteur ne progresse.

Ordre déterministe :

1. rafraîchir la perception des PNJ ;
2. si `All`, avancer les PNJ neutres par identifiant stable ;
3. avancer les nœuds de combat par identifiant stable ;
4. arrêter immédiatement après sélection d'un contact ennemi.

### 3.2 Collisions

L'ensemble d'occupation comprend : joueur, obstacles, trous, PNJ, nœuds de
combat disponibles et nœuds bloquants. Avant chaque mouvement, l'ancienne case
de l'acteur est libérée ; la destination n'est réservée qu'après validation.
Une destination invalide produit un mouvement nul, jamais une exception.

Pour le pathfinding du joueur uniquement, une case de PNJ neutre est praticable
en transit mais reste interdite comme destination finale. Les PNJ continuent de
se bloquer mutuellement durant leurs propres ticks.

### 3.3 Poursuite ennemie

Pour `d = |node.Lane-partyX| + |node.Row-partyY|` :

- `d <= 1` : sélectionner le nœud, sans superposer les positions ;
- `2 <= d <= 3` : tester d'abord l'axe de plus grand écart, puis l'autre axe ;
- `d > 3` : choisir un voisin orthogonal depuis un ordre pseudo-aléatoire
  déterministe basé sur l'identité et la position courante ;
- après déplacement, si `d <= 1`, sélectionner le nœud.

### 3.4 Contact effectif

`MapNode.TriggersOnContact` vaut vrai pour tout type combat, même si une
sauvegarde historique porte `ContactBehavior.None`. Le DTO expose le
comportement effectif afin que le pathfinder client et le serveur restent
alignés. Les nœuds non-combat conservent leur valeur authored.

## 4. API

### `POST /api/v2/runs/{runId}/rooms/current/actors/advance`

Corps :

```json
{ "mode": "All" }
```

Réponse `200` :

```json
{
  "run": { "...": "RunDto" },
  "movements": [
    {
      "actorId": "uuid",
      "actorKind": "Npc",
      "fromX": 4,
      "fromY": 6,
      "toX": 5,
      "toY": 6
    }
  ],
  "triggeredNodeId": null
}
```

Le mode est strictement validé. La commande est idempotente seulement au sens
transactionnel : deux appels réussis représentent deux ticks distincts.

### Interaction et dialogue d'un PNJ physique

- `POST .../npcs/{roomNpcId}/interact` vérifie l'adjacence, applique les règles
  locales, initialise la relation de run et renvoie le premier
  `NpcDialogueViewDto` issu du Catalogue.
- `POST .../npcs/{roomNpcId}/dialogue/choices` applique un choix au graphe actif
  sans exiger de `MapNode` d'événement, puis renvoie le nœud suivant ou la fin
  de la rencontre.
- Un PNJ sans définition Catalog conserve le retour protocolaire local et ne
  fabrique aucun dialogue artificiel.

## 5. Client Vue

- `runStore.advanceRoomActors(mode)` utilise un verrou `actorsAdvancing` pour
  interdire les appels concurrents.
- `RunPage` demande un tick `All` à intervalle régulier uniquement lorsque
  `gameplayPhase == Map`, que la salle est `Active` et qu'aucune action n'est en
  cours. La cadence nominale est de 1 800 ms.
- `movePartyTo` attend l'animation du joueur puis demande `HostilesOnly` avant
  de résoudre un éventuel contact.
- `TacticalGridMap` interpole les positions signalées par les DTO successifs
  sur 420 ms, avec 70 ms de décalage entre acteurs.
- Un clic sur un PNJ adjacent émet `interactRoomNpc`; un clic plus éloigné ne
  tente jamais une destination occupée.
- Les entrées du plateau sont désactivées pendant `isLoading` ou
  `actorsAdvancing`.

## 6. Persistance et compatibilité

- `RoomNpcEntity.X/Y` persiste les PNJ comme aujourd'hui.
- `MapNodeEntity.Lane/Row` persiste la position courante de l'ennemi.
- Aucune migration n'est attendue.
- Les anciennes runs obtiennent automatiquement le contact effectif pour leurs
  nœuds de combat, même si `ContactBehavior` vaut `None`.
- Le mapping de réhydratation conserve les identifiants et positions sans
  recalcul.

## 7. Observabilité et erreurs

- Un tick refusé pour état de salle non actif retourne une erreur métier
  cohérente avec les autres commandes de run.
- Chaque réponse contient les mouvements réellement appliqués ; un acteur
  bloqué n'apparaît pas dans `movements`.
- Une sélection par contact apparaît dans `triggeredNodeId` et dans
  `run.currentRoom.state == NodeSelected`.

## 8. Stratégie de tests

### Domaine

- le déplacement joueur ne modifie plus la position d'un PNJ ;
- un PNJ à distance deux ne bouge pas ;
- deux acteurs ne se superposent pas ;
- un ennemi à distance trois réduit la distance ;
- un ennemi adjacent sélectionne son nœud ;
- tout type de combat déclenche au contact ;
- un nœud d'événement non-combat conserve son déclencheur authored.

### Application / API

- commande avec run introuvable ;
- mode invalide ;
- réponse et persistance des mouvements ;
- contact renvoyé puis résolu par le flux existant.
- interaction physique renvoyant le dialogue Catalog ;
- choix de dialogue physique résolu sans nœud d'événement.

### Client

- verrouillage des ticks concurrents ;
- phase hostile après animation joueur ;
- clic adjacent sur PNJ ;
- ouverture du dialogue Catalog et choix via l'API du PNJ physique ;
- trajet possible à travers une case de PNJ non choisie comme destination ;
- interpolation ancien → nouveau ;
- aucune horloge pendant dialogue, événement ou combat.

## 9. Matrice de traçabilité

| Exigence | Composants | Tests |
|---|---|---|
| RG-EXP-001 à 003 | `Room.MoveParty`, store, `RunPage` | domaine + store |
| RG-EXP-010 à 014 | `Room.AdvanceActors`, `RoomNpc` | domaine |
| RG-EXP-020 à 026 | `MapNode`, `Room`, resolvers | domaine + API |
| RG-EXP-030 à 032 | règles locales existantes | régression protocoles |
| RG-EXP-040 à 043 | `TacticalGridMap` | composables + manuel |
