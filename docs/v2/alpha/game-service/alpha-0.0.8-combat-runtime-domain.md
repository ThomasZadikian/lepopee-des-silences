# L’épopée des silences — Suivi technique alpha-0.0.8

## PR — Combat Runtime Domain

**Branche cible :** `v2/develop`
**Service concerné :** `services/game-engine`
**Version concernée :** `alpha-0.0.8`
**Type de PR :** feature domaine / combat runtime / fondation gameplay serveur-autoritaire
**Commit recommandé :** `feat(game-engine): introduce combat runtime domain`
**Statut final :** à compléter après validation des tests
**Diffusion :** confidentiel projet

---

## 1. Contexte

Le jalon `alpha-0.0.7` a clarifié la séparation entre les responsabilités de navigation et les responsabilités événementielles :

```text
Domain/Nodes
→ navigation dans la room map

Domain/NodeEvents
→ événements candidats portés par un node
```

Cette clarification permet d’aborder la suite sans transformer `Node` ou `NodeEvent` en objets trop larges.

Le jalon `alpha-0.0.8` introduit maintenant le premier vrai sous-domaine runtime du combat.

L’objectif n’est pas encore de brancher le combat aux événements de node, ni d’exposer une API de combat.
L’objectif est de poser un modèle domaine propre, testable et isolé.

---

## 2. Objectif de la PR

Cette PR introduit le sous-domaine :

```text
Domain/Combats
```

Ce sous-domaine doit représenter le combat runtime serveur-autoritaire.

La PR ajoute les concepts minimaux suivants :

```text
CombatInstance
CombatantSnapshot
CombatAction
DamageResolver
CombatActionResult
DamageResult
CombatState
CombatantSide
CombatActionType
CombatId
CombatantId
```

Le périmètre est volontairement limité au domaine.

Aucun controller n’est ajouté.

Aucun handler CQRS n’est ajouté.

Aucune dépendance à Catalog, Player ou Infrastructure n’est ajoutée.

---

## 3. Décision d’architecture

La décision structurante est de créer un sous-domaine `Combats` dédié, au lieu de placer la logique de combat dans :

```text
Nodes
NodeEvents
Runs
Events
Application
Infrastructure
```

Le combat est un concept runtime autonome.

Il a ses propres règles :

```text
- composition des combattants ;
- différenciation joueur / ennemi ;
- ordre de tour ;
- action de combat ;
- résolution de dégâts ;
- victoire ;
- défaite ;
- état du combat.
```

Il mérite donc son propre espace domaine.

---

## 4. Positionnement Clean Architecture

La PR respecte la Clean Architecture.

Répartition des responsabilités :

```text
Domain/Combats
→ règles métier fondamentales du combat runtime.

Application
→ inchangé dans cette PR.

Infrastructure
→ inchangé dans cette PR.

API
→ inchangé dans cette PR.
```

Le domaine `Combats` ne dépend pas :

```text
- de Catalog ;
- de Player ;
- de l’API ;
- de l’Infrastructure ;
- de MediatR ;
- d’EF Core ;
- d’un client HTTP ;
- d’un service externe.
```

Il est autonome et testable en mémoire.

---

## 5. Pourquoi ne pas brancher Catalog maintenant ?

Le pipeline événementiel existant sait déjà résoudre des contenus typés, notamment des contenus de combat.

Cependant, brancher immédiatement :

```text
ResolvedCombatEventContent
+ EnemyTemplateSnapshot
+ PlayerSnapshot
→ CombatInstance
```

aurait mélangé deux étapes différentes :

```text
1. Créer le domaine Combat.
2. Construire un CombatInstance depuis un événement résolu.
```

La PR `alpha-0.0.8` se limite à la première étape.

Le branchement entre événement résolu et combat runtime sera traité dans le jalon suivant :

```text
alpha-0.0.9
feat(game-engine): start combat from resolved node event
```

---

## 6. Modèle introduit

### 6.1 CombatInstance

`CombatInstance` représente une instance runtime de combat.

Il porte :

```text
- CombatId ;
- liste des combattants ;
- ordre de tour ;
- état du combat ;
- index du tour courant ;
- round courant ;
- résolution d’action.
```

Il protège les invariants suivants :

```text
- un combat doit contenir au moins deux combattants ;
- un combat doit contenir au moins un combattant côté Player ;
- un combat doit contenir au moins un combattant côté Enemy ;
- les identifiants de combattants doivent être uniques ;
- un combat ne peut pas démarrer avec un combattant déjà vaincu ;
- seul le combattant courant peut agir ;
- un combattant ne peut pas cibler un allié ;
- un combat terminé ne peut plus recevoir d’action.
```

---

### 6.2 CombatantSnapshot

`CombatantSnapshot` représente l’état runtime d’un combattant au moment du combat.

Il contient :

```text
- CombatantId ;
- TemplateKey ;
- DisplayName ;
- Side ;
- MaxHealth ;
- CurrentHealth ;
- Attack ;
- Defense ;
- Speed.
```

Le terme `Snapshot` est volontaire.

Il prépare le futur découplage entre :

```text
Catalog
→ templates versionnés d’ennemis, compétences, objets

Player
→ progression durable, statistiques, compétences débloquées

Game Engine
→ snapshot runtime utilisé pendant le combat
```

Un combat ne doit pas manipuler directement les entités Catalog ou Player.

---

### 6.3 CombatAction

`CombatAction` représente une intention d’action de combat.

Dans cette PR, une seule action est introduite :

```text
BasicAttack
```

Ce choix est volontairement minimal pour poser la boucle de base.

Les futures actions pourront inclure :

```text
- Skill ;
- Defend ;
- UseItem ;
- CompanionAction ;
- Flee ;
- SpecialLawAction.
```

Ces actions ne sont pas ajoutées dans cette PR afin de garder un domaine simple et testable.

---

### 6.4 DamageResolver

`DamageResolver` isole la formule de dégâts.

La formule alpha est volontairement simple :

```text
damage = max(1, attacker.Attack - defender.Defense)
```

L’intérêt de l’isoler dès maintenant est d’éviter que `CombatInstance` devienne responsable de la formule de dégâts.

À terme, `DamageResolver` pourra évoluer pour intégrer :

```text
- types ;
- résistances ;
- affinités ;
- esquive ;
- critique ;
- états altérés ;
- effets des Lois du Palais ;
- compétences ;
- buffs et debuffs ;
- scaling par room ou profondeur.
```

---

### 6.5 CombatActionResult

`CombatActionResult` représente le résultat d’une action de combat.

Il contient :

```text
- CombatId ;
- ActorId ;
- TargetId ;
- ActionType ;
- Damage ;
- TargetRemainingHealth ;
- TargetDefeated ;
- CombatState ;
- WinningSide ;
- NextActorId ;
- Round.
```

Ce résultat est pensé pour alimenter plus tard :

```text
- un handler CQRS ;
- une réponse API ;
- un log de combat ;
- une projection ;
- le game-client ;
- l’Event Store.
```

---

## 7. Fichiers ajoutés

```text
services/game-engine/src/Leds.GameEngine.Domain/Combats/CombatId.cs
services/game-engine/src/Leds.GameEngine.Domain/Combats/CombatantId.cs
services/game-engine/src/Leds.GameEngine.Domain/Combats/CombatState.cs
services/game-engine/src/Leds.GameEngine.Domain/Combats/CombatantSide.cs
services/game-engine/src/Leds.GameEngine.Domain/Combats/CombatActionType.cs
services/game-engine/src/Leds.GameEngine.Domain/Combats/CombatAction.cs
services/game-engine/src/Leds.GameEngine.Domain/Combats/CombatantSnapshot.cs
services/game-engine/src/Leds.GameEngine.Domain/Combats/DamageResult.cs
services/game-engine/src/Leds.GameEngine.Domain/Combats/DamageResolver.cs
services/game-engine/src/Leds.GameEngine.Domain/Combats/CombatActionResult.cs
services/game-engine/src/Leds.GameEngine.Domain/Combats/CombatInstance.cs
```

Tests ajoutés :

```text
services/game-engine/tests/Leds.GameEngine.UnitTests/Combats/CombatInstanceTests.cs
services/game-engine/tests/Leds.GameEngine.UnitTests/Combats/CombatantSnapshotTests.cs
```

---

## 8. États du combat

`CombatState` introduit trois états :

```text
Created
InProgress
Completed
```

Dans cette première implémentation, un combat créé par `CombatInstance.Create(...)` démarre directement en `InProgress`.

L’état `Created` est conservé comme état conceptuel utile pour les évolutions futures, par exemple :

```text
- préparation d’un combat ;
- enrichissement depuis Catalog ;
- attente d’un PlayerSnapshot ;
- introduction de cinématiques ou transitions ;
- phase de pré-combat.
```

---

## 9. Sides des combattants

`CombatantSide` introduit deux côtés :

```text
Player
Enemy
```

Cela permet de garder le modèle simple tout en préparant :

```text
- joueur seul ;
- joueur + compagnons ;
- ennemis multiples ;
- boss ;
- invocations futures ;
- mécaniques de camp.
```

La règle actuelle est :

```text
Un combat doit avoir au moins un combattant Player et au moins un combattant Enemy.
```

---

## 10. Ordre de tour

L’ordre de tour est calculé au démarrage du combat.

Règle actuelle :

```text
Speed décroissante
puis CombatantId croissant en cas d’égalité
```

Objectif :

```text
- ordre déterministe ;
- comportement testable ;
- absence de hasard implicite dans le domaine.
```

À terme, l’ordre pourra être enrichi avec :

```text
- initiative ;
- buffs ;
- ralentissements ;
- effets des Lois du Palais ;
- états altérés ;
- actions bonus ;
- ATB éventuel.
```

---

## 11. Résolution d’action

Le flux minimal est :

```text
CombatAction.BasicAttack(actorId, targetId)
→ CombatInstance.SubmitAction(action)
→ validation du tour
→ validation de la cible
→ DamageResolver.ResolveBasicAttack(...)
→ target.ReceiveDamage(...)
→ vérification victoire/défaite
→ avancement du tour si combat non terminé
→ CombatActionResult
```

Cette boucle est volontairement minimale, mais elle valide le socle du combat serveur-autoritaire.

---

## 12. Conditions de fin de combat

Un combat se termine si :

```text
tous les combattants Player sont vaincus
```

ou :

```text
tous les combattants Enemy sont vaincus
```

Le résultat indique alors :

```text
WinningSide = Player
```

ou :

```text
WinningSide = Enemy
```

Le combat passe à l’état :

```text
Completed
```

---

## 13. Ce que cette PR ne fait pas

Cette PR ne crée pas encore :

```text
- CombatRepository ;
- StartCombatCommand ;
- SubmitCombatActionCommand ;
- endpoint API ;
- intégration avec ResolveCurrentEvent ;
- intégration avec Catalog ;
- intégration avec Player ;
- RewardOffer ;
- logs de combat persistés ;
- Event Store ;
- compétences ;
- types ;
- esquive ;
- critique ;
- états altérés ;
- IA ennemie ;
- boss ;
- compagnons.
```

Ces éléments seront ajoutés progressivement.

La PR `alpha-0.0.8` ne cherche pas à faire un combat complet, mais à poser une base domaine propre.

---

## 14. Pourquoi cette PR n’est pas un placeholder

Le modèle introduit n’est pas jetable.

Il pose des concepts stables :

```text
CombatInstance
CombatantSnapshot
CombatAction
DamageResolver
CombatActionResult
```

Ces concepts seront enrichis, mais la structure restera valide.

Les futures évolutions porteront sur :

```text
- nouvelles actions ;
- nouvelles formules ;
- nouveaux effets ;
- nouveaux états ;
- intégration Event Sourcing ;
- intégration avec Catalog et Player.
```

Elles ne nécessiteront pas de déplacer la logique hors du sous-domaine `Combats`.

---

## 15. Tests ajoutés

Les tests `CombatantSnapshotTests` couvrent :

```text
- création d’un combattant avec santé pleine ;
- réception de dégâts ;
- limitation des PV à zéro ;
- rejet d’une santé maximale invalide.
```

Les tests `CombatInstanceTests` couvrent :

```text
- création d’un combat en état InProgress ;
- ordre de tour selon Speed ;
- application d’une attaque basique ;
- avancement du tour ;
- fin de combat si les ennemis sont vaincus ;
- rejet d’une action si ce n’est pas le tour de l’acteur ;
- rejet d’un ciblage allié ;
- rejet d’un combat sans ennemi.
```

Ces tests valident les invariants métier de base du combat runtime.

---

## 16. Risques maîtrisés

### Risque : CombatInstance devient trop gros

Réponse :

```text
La formule de dégâts est isolée dans DamageResolver.
CombatInstance orchestre l’état du combat et le tour, mais ne porte pas toute la logique de calcul.
```

### Risque : couplage prématuré à Catalog

Réponse :

```text
Aucune dépendance à Catalog n’est introduite.
Le combat utilise des CombatantSnapshot.
```

### Risque : couplage prématuré à Player

Réponse :

```text
Aucun PlayerProfile n’est injecté dans le combat.
Le futur PlayerSnapshot sera transformé en CombatantSnapshot.
```

### Risque : action system trop pauvre

Réponse :

```text
BasicAttack est volontairement minimal.
Le modèle CombatActionType prépare l’ajout progressif d’autres actions.
```

### Risque : logique de combat dans Application

Réponse :

```text
La logique de combat runtime est dans Domain/Combats.
Application orchestrera plus tard les cas d’usage.
```

---

## 17. Validation à effectuer

Commandes :

```bash
dotnet format services/game-engine/Leds.GameEngine.slnx
dotnet test services/game-engine/Leds.GameEngine.slnx
dotnet test services/catalog/Leds.Catalog.slnx
dotnet test packages/shared-building-blocks/Leds.SharedBuildingBlocks.slnx
```

---

## 18. Vérifications architecture

Vérifier que le Domain reste pur :

```powershell
Get-ChildItem services/game-engine/src/Leds.GameEngine.Domain -Recurse -Filter *.csproj |
  Select-String "Application|Infrastructure|Api|Catalog"
```

Vérifier que l’Application ne dépend pas d’Infrastructure :

```powershell
Get-ChildItem services/game-engine/src/Leds.GameEngine.Application -Recurse -Filter *.csproj |
  Select-String "Infrastructure"
```

Vérifier l’absence de couplage direct au service Catalog :

```powershell
Get-ChildItem services/game-engine -Recurse -Filter *.csproj |
  Select-String "Leds.Catalog"
```

Résultat attendu :

```text
Aucune dépendance interdite.
```

---

## 19. Critères de sortie alpha-0.0.8

Le jalon `alpha-0.0.8` est validé si :

```text
- Domain/Combats existe ;
- CombatInstance peut être créé ;
- un combat contient au moins un Player et un Enemy ;
- un ordre de tour déterministe existe ;
- une BasicAttack peut être soumise ;
- les dégâts sont résolus par DamageResolver ;
- les PV de la cible sont mis à jour ;
- le combat se termine quand un camp est vaincu ;
- les tests unitaires passent ;
- aucune dépendance interdite n’est ajoutée.
```

---

## 20. Suite recommandée

La suite logique est :

```text
alpha-0.0.9
feat(game-engine): start combat from resolved node event
```

Objectif :

```text
Transformer un événement de node résolu de type Combat ou Elite en CombatInstance.
```

Travail attendu :

```text
- créer un CombatInstanceFactory ;
- consommer ResolvedCombatEventContent ou ResolvedEliteEventContent ;
- créer un PlayerCombatantSnapshot temporaire ;
- créer un EnemyCombatantSnapshot à partir du contenu Catalog ;
- préparer un stockage temporaire des combats ;
- commencer à lier ResolveCurrentEvent et CombatInstance.
```

Puis :

```text
alpha-0.0.10
feat(game-engine): expose combat action flow
```

Objectif :

```text
Créer SubmitCombatActionCommand et exposer la première action de combat côté API.
```

---

## 21. Commit recommandé

```text
feat(game-engine): introduce combat runtime domain
```

---

## 22. Conclusion

Cette PR introduit le premier socle réel du combat runtime dans le Game Engine.

Elle respecte les principes du projet :

```text
- Domain isolé ;
- Clean Architecture ;
- absence de couplage prématuré ;
- serveur-autoritaire ;
- testabilité ;
- séparation entre runtime gameplay et services périphériques.
```

`alpha-0.0.8` prépare directement la prochaine étape : démarrer un combat depuis un événement de node résolu, sans mélanger le combat avec la navigation, les événements, Catalog ou Player.
