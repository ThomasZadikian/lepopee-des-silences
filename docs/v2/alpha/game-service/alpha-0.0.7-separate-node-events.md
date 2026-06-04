# L’épopée des silences — Suivi technique alpha-0.0.7

## PR — Séparation Node / NodeEvent

**Branche cible :** `v2/develop`
**Service concerné :** `services/game-engine`
**Version concernée :** `alpha-0.0.7`
**Type de PR :** refactor domaine / clarification modèle / gameplay runtime foundation
**Commit recommandé :** `feat(game-engine): separate node events from node navigation`
**Statut final :** à compléter après validation des tests
**Diffusion :** confidentiel projet

---

## 1. Contexte

Avant cette PR, les concepts `Node` et `NodeEvent` étaient regroupés dans le même espace domaine `Domain/Nodes`.

Cette organisation fonctionnait pour les premières étapes de génération, mais elle créait une ambiguïté conceptuelle :

```text
Node
→ élément de navigation dans la room map

NodeEvent
→ contenu événementiel candidat porté par un node
```

Cette PR clarifie cette séparation afin d’éviter que le sous-domaine `Nodes` devienne un fourre-tout mêlant navigation, événements, contenu, résolution, combat et récompenses.

---

## 2. Objectif de la PR

L’objectif est de séparer proprement les responsabilités :

```text
Domain/Nodes
→ navigation, état de carte, sélection, verrouillage, progression

Domain/NodeEvents
→ événements candidats, type événementiel, ordre, statut de résolution
```

La PR ne crée pas de nouveau use case applicatif.

Elle ne crée pas de nouvel endpoint.

Elle stabilise le modèle de domaine existant derrière le flux déjà exposé :

```text
POST /api/v2/runs/{runId}/current-event/resolve
```

---

## 3. Décision d’architecture

La décision principale est de sortir `NodeEvent` et `NodeEventType` du namespace `Domain.Nodes`.

Nouvelle séparation :

```text
Leds.GameEngine.Domain.Nodes
→ Node
→ NodeId
→ NodeState

Leds.GameEngine.Domain.NodeEvents
→ NodeEvent
→ NodeEventType
→ NodeEventStatus
```

Cette séparation rend explicite le fait que le Game Engine manipule deux sous-concepts différents :

```text
Node
→ choix visible dans une room

NodeEvent
→ candidat événementiel contenu dans ce choix
```

---

## 4. Règle métier ajoutée

La règle métier stabilisée est la suivante :

```text
Un node peut contenir 1 à 4 NodeEvents candidats.

Lorsqu’un node sélectionné est résolu :
- un seul NodeEvent est résolu ;
- les autres NodeEvents du même node sont fermés ;
- le Node passe à l’état Resolved.
```

Cette règle prépare les futurs besoins de traçabilité, de replay, de Player Portal, de Tome et d’Event Sourcing.

---

## 5. Modèle avant / après

### Avant

```text
Node
- State
- Events
  - NodeEventType
  - Order
```

La résolution faisait essentiellement :

```text
Node.State = Resolved
```

Les événements internes n’avaient pas de statut propre.

### Après

```text
Node
- State
- Events

NodeEvent
- EventType
- Order
- Status
```

La résolution fait désormais :

```text
NodeEvent primaire = Resolved
NodeEvents alternatifs = Closed
Node = Resolved
```

---

## 6. Fichiers déplacés

Les fichiers suivants ont été déplacés :

```text
services/game-engine/src/Leds.GameEngine.Domain/Nodes/NodeEvent.cs
→ services/game-engine/src/Leds.GameEngine.Domain/NodeEvents/NodeEvent.cs
```

```text
services/game-engine/src/Leds.GameEngine.Domain/Nodes/NodeEventType.cs
→ services/game-engine/src/Leds.GameEngine.Domain/NodeEvents/NodeEventType.cs
```

---

## 7. Fichiers ajoutés

```text
services/game-engine/src/Leds.GameEngine.Domain/NodeEvents/NodeEventStatus.cs
```

```text
services/game-engine/src/Leds.GameEngine.Application/Runs/Dtos/NodeEventDto.cs
```

Tests ajoutés :

```text
services/game-engine/tests/Leds.GameEngine.UnitTests/NodeEvents/NodeEventTests.cs
services/game-engine/tests/Leds.GameEngine.UnitTests/Nodes/NodeEventCandidateResolutionTests.cs
```

---

## 8. Fichiers modifiés

```text
services/game-engine/src/Leds.GameEngine.Domain/Nodes/Node.cs
```

Modifications principales :

```text
- ajout de PrimaryEvent ;
- ajout de ResolvedEvent ;
- ajout de HasResolvedEvent ;
- ajout de ClosedEvents ;
- ajout de ResolvePrimaryEvent() ;
- ajout de ResolveEvent(int eventOrder) ;
- adaptation de Resolve() pour déléguer à ResolvePrimaryEvent().
```

```text
services/game-engine/src/Leds.GameEngine.Application/Runs/Dtos/NodeDto.cs
```

Modifications principales :

```text
- ajout de Events ;
- ajout de ResolvedEventType ;
- conservation de EventTypes et EventCount pour compatibilité.
```

---

## 9. NodeEventStatus

`NodeEventStatus` introduit trois statuts :

```text
Planned
→ événement candidat encore disponible dans le node.

Resolved
→ événement effectivement résolu lors de la résolution du node.

Closed
→ événement candidat fermé parce qu’un autre événement du même node a été résolu.
```

Ce statut ne remplace pas `NodeState`.

`NodeState` continue de piloter la navigation dans la room.

`NodeEventStatus` pilote uniquement le statut interne d’un événement candidat.

---

## 10. Rôle de Node après la PR

`Node` reste responsable de la navigation et de la progression.

Il porte :

```text
- son état dans la carte ;
- sa profondeur ;
- ses parents ;
- son niveau de risque ;
- son profil de récompense ;
- ses événements candidats ;
- la résolution du node sélectionné.
```

Il ne porte pas :

```text
- logique de combat ;
- logique de récompense ;
- logique Catalog ;
- logique Markov ;
- logique narrative avancée.
```

Cette séparation évite que `Node` devienne un objet trop large.

---

## 11. Rôle de NodeEvent après la PR

`NodeEvent` devient un concept autonome du domaine.

Il représente un candidat événementiel porté par un node.

Il porte :

```text
- EventType ;
- Order ;
- Status.
```

Il possède ses propres règles :

```text
- un événement fermé ne peut pas être résolu ;
- un événement résolu ne peut pas être fermé ;
- un événement déjà résolu ne peut pas être résolu à nouveau.
```

---

## 12. Impact sur le flux existant

Le flux applicatif existant reste valide :

```text
ChooseNode
→ ResolveCurrentEvent
→ GenerateNextNodes
```

La PR ne modifie pas le contrat principal du use case.

Elle modifie ce que `Node.Resolve()` fait en interne.

Avant :

```text
Node.Resolve()
→ Node.State = Resolved
```

Après :

```text
Node.Resolve()
→ ResolvePrimaryEvent()
→ NodeEvent primaire = Resolved
→ NodeEvents alternatifs = Closed
→ Node.State = Resolved
```

---

## 13. Impact sur l’API

Aucun nouvel endpoint n’est introduit.

L’endpoint existant reste :

```text
POST /api/v2/runs/{runId}/current-event/resolve
```

Les DTO de run peuvent désormais exposer les événements internes du node avec leur statut.

Cela prépare le futur `game-client` et le futur `player-portal`.

---

## 14. Intérêt pour le frontend v2

Cette PR prépare deux usages frontend distincts.

### Game Client

Le `game-client` pourra visualiser :

```text
- le node résolu ;
- l’événement effectivement résolu ;
- les événements candidats fermés.
```

### Player Portal

Le `player-portal` pourra plus tard afficher dans le détail d’une run :

```text
- les événements proposés ;
- l’événement effectivement résolu ;
- les alternatives fermées ;
- la seed ;
- les choix ;
- la trace de progression.
```

---

## 15. Intérêt pour l’Event Sourcing futur

Cette séparation prépare les futurs événements de run :

```text
NodeSelected
NodeEventResolved
NodeAlternativeEventClosed
NodeResolved
```

Même si l’Event Store n’est pas encore implémenté, le modèle domaine est maintenant compatible avec cette évolution.

---

## 16. Ce que cette PR ne fait pas

Cette PR ne crée pas encore :

```text
- CombatInstance ;
- RewardOffer ;
- ActivePalaceLaw ;
- NpcInteraction ;
- Event Store ;
- Player Portal ;
- Game Client ;
- nouveau resolver applicatif ;
- nouvel endpoint API.
```

Elle clarifie uniquement le modèle de domaine nécessaire avant de poursuivre vers le combat runtime.

---

## 17. Risques maîtrisés

### Risque : sur-complexifier le Domain

Réponse :

```text
La PR sépare les concepts au lieu d’en ajouter dans Node.
NodeEvents devient un sous-domaine dédié.
```

### Risque : casser le flux existant

Réponse :

```text
Node.Resolve() conserve sa signature.
Room.ResolveSelectedNodeEvent() peut continuer à l’appeler.
```

### Risque : casser les DTO existants

Réponse :

```text
NodeDto conserve EventTypes et EventCount.
Les nouveaux champs Events et ResolvedEventType sont ajoutés sans supprimer les anciens champs.
```

### Risque : confusion NodeState / NodeEventStatus

Réponse :

```text
NodeState concerne la navigation.
NodeEventStatus concerne le candidat événementiel.
```

---

## 18. Tests ajoutés

Tests `NodeEvent` :

```text
- création d’un NodeEvent Planned ;
- résolution d’un NodeEvent ;
- fermeture d’un NodeEvent ;
- interdiction de résoudre un NodeEvent fermé ;
- interdiction de fermer un NodeEvent résolu.
```

Tests `Node` :

```text
- résolution de l’événement primaire ;
- fermeture des événements alternatifs ;
- résolution explicite d’un événement par ordre ;
- passage du node en Resolved.
```

---

## 19. Commandes de validation

```bash
dotnet format services/game-engine/Leds.GameEngine.slnx
dotnet test services/game-engine/Leds.GameEngine.slnx
dotnet test services/catalog/Leds.Catalog.slnx
dotnet test packages/shared-building-blocks/Leds.SharedBuildingBlocks.slnx
```

---

## 20. Vérifications architecture

```powershell
Get-ChildItem services/game-engine/src/Leds.GameEngine.Domain -Recurse -Filter *.csproj |
  Select-String "Application|Infrastructure|Api|Catalog"
```

```powershell
Get-ChildItem services/game-engine/src/Leds.GameEngine.Application -Recurse -Filter *.csproj |
  Select-String "Infrastructure"
```

```powershell
Get-ChildItem services/game-engine -Recurse -Filter *.csproj |
  Select-String "Leds.Catalog"
```

Résultat attendu :

```text
Aucune dépendance interdite.
```

---

## 21. Critère de sortie alpha-0.0.7

Le jalon `alpha-0.0.7` est validé si :

```text
- Nodes et NodeEvents sont séparés dans le Domain ;
- NodeEvent porte un statut propre ;
- Node.Resolve() résout un seul événement candidat ;
- les alternatives sont fermées ;
- ResolveCurrentEvent continue de fonctionner ;
- NodeDto expose les événements et leurs statuts ;
- les tests passent ;
- aucune dépendance interdite n’est ajoutée.
```

---

## 22. Suite recommandée

La suite logique est :

```text
alpha-0.0.8
feat(game-engine): introduce combat runtime domain
```

Objectif :

```text
Créer le domaine Combat runtime :
- CombatInstance ;
- CombatantSnapshot ;
- CombatState ;
- CombatAction ;
- DamageResolver minimal ;
- conditions victoire / défaite.
```

Ensuite :

```text
alpha-0.0.9
feat(game-engine): start combat from resolved node event
```

Objectif :

```text
Transformer un ResolvedCombatEventContent ou NodeEvent Combat résolu en CombatInstance.
```

---

## 23. Commit recommandé

```text
feat(game-engine): separate node events from node navigation
```

---

## 24. Conclusion

Cette PR stabilise un point important de la modélisation du Game Engine.

Elle clarifie la différence entre :

```text
Node
→ navigation dans le graphe de room

NodeEvent
→ contenu événementiel candidat du node
```

Cette séparation rend le Domain plus lisible, plus maintenable et mieux préparé aux futures briques :

```text
- combat runtime ;
- récompenses ;
- Event Sourcing ;
- Player Portal ;
- Tome ;
- run replay ;
- statistiques d’événements.
```

La PR constitue donc un socle propre pour poursuivre vers la boucle jouable `alpha-0.1.0`.
