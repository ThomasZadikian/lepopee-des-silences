# L’épopée des silences — Suivi technique alpha-0.0.9

## PR — Unification des factories de test du Game Engine

**Branche cible :** `v2/develop`
**Service concerné :** `services/game-engine`
**Version concernée :** `alpha-0.0.9`
**Type de PR :** refactor tests / fiabilisation des invariants / préparation combat runtime integration
**Commit recommandé :** `test(game-engine): centralize valid room and run factories`
**Statut final :** validé si build et tests passent
**Diffusion :** confidentiel projet

---

## 1. Contexte

Dans le cadre de la préparation du jalon `alpha-0.0.9`, plusieurs tests unitaires du Game Engine manipulaient directement des objets complexes du domaine :

```text
Run
Room
Node
NodeEvent
```

Chaque fichier de test reconstruisait localement ses propres méthodes :

```text
CreateRun()
CreateInitialRoom()
CreateRoom()
CreateNode()
```

Cette duplication était devenue problématique depuis le durcissement des invariants du domaine, notamment après :

```text
- la séparation Domain/Nodes et Domain/NodeEvents ;
- l’introduction de NodeEventStatus ;
- la clarification du rôle de Node ;
- la clarification du rôle de NodeEvent ;
- le renforcement des règles de Room.Create(...).
```

Les erreurs récurrentes étaient liées à des rooms de test invalides :

```text
Initial node layer must contain between 1 and 4 nodes.
A room must contain at least one progression layer before the boss.
```

Ces erreurs ne révélaient pas nécessairement des bugs métier, mais des factories de test locales obsolètes.

---

## 2. Problème initial

Les tests reconstruisaient une room à la main dans plusieurs fichiers.

Cela créait plusieurs risques :

```text
- duplication importante de code ;
- divergence entre les factories locales ;
- oubli d’invariants métier ;
- rooms invalides dans des tests qui ne testent pas Room ;
- corrections répétées après chaque évolution du domaine ;
- perte de lisibilité dans les tests applicatifs ;
- bruit inutile dans les PR.
```

Les tests de `Run`, de handlers, de choix d’événement ou de progression n’ont pas vocation à redéfinir la topologie d’une room valide.

Cette responsabilité doit être centralisée.

---

## 3. Objectif de la PR

L’objectif de cette PR est de créer une factory de test unique pour le Game Engine :

```text
TestGameEngineFactory
```

Cette factory fournit des scénarios valides et réutilisables pour les tests unitaires.

Elle permet notamment de créer :

```text
- un Node valide ;
- une Room Threshold valide ;
- une Run valide ;
- une Run avec node cible ;
- une Run avec node sélectionné ;
- une Run avec événement courant résolu ;
- une Run avec room courante complétée.
```

---

## 4. Fichier ajouté

La factory centralisée est placée ici :

```text
services/game-engine/tests/Leds.GameEngine.UnitTests/Common/Factories/TestGameEngineFactory.cs
```

Elle devient le point d’entrée standard pour les tests unitaires qui ont besoin d’un état cohérent de jeu.

---

## 5. Responsabilités de TestGameEngineFactory

La factory centralise uniquement les scénarios transverses et stables.

Elle est responsable de créer des objets cohérents pour les tests :

```text
Node
Room
Run
```

Elle n’est pas responsable de tester les règles métier.

Les tests de domaine bas niveau peuvent toujours créer directement leurs objets si c’est précisément ce qu’ils testent.

---

## 6. Méthodes disponibles

La factory expose les méthodes suivantes :

```text
CreateNode(...)
```

Crée un `Node` valide contenant une collection de `NodeEvent`.

```text
CreateThresholdRoom(...)
```

Crée une room Threshold complète et valide.

```text
CreateThresholdRoomWithTargetInitialNode(...)
```

Crée une room Threshold complète et retourne aussi le node initial cible.

```text
CreateRun(...)
```

Crée une run active contenant une room Threshold valide.

```text
CreateRunWithTargetInitialNode(...)
```

Crée une run active et retourne le node initial cible non sélectionné.

```text
CreateRunWithSelectedTargetNode(...)
```

Crée une run active avec un node cible déjà sélectionné.

```text
CreateRunWithResolvedCurrentEvent(...)
```

Crée une run active avec un événement courant déjà résolu.

```text
CreateRunWithCompletedCurrentRoom(...)
```

Crée une run dont la room courante est complétée, en respectant les transitions métier attendues.

---

## 7. Structure de room garantie

La factory crée une room valide selon la structure suivante :

```text
Depth 0
→ 2 nodes initiaux Available

Depth 1
→ 3 nodes de progression Planned

Depth 2
→ 1 boss node Planned
```

Cette structure garantit les invariants suivants :

```text
- la couche initiale contient entre 1 et 4 nodes ;
- une couche de progression existe avant le boss ;
- les nodes initiaux n’ont pas de parents ;
- les nodes non initiaux ont au moins un parent ;
- le boss node contient un NodeEvent RoomBoss ;
- le boss node est marqué isRoomBossNode ;
- toutes les branches convergent vers le boss.
```

---

## 8. Fichiers migrés

Les tests suivants ont été migrés vers `TestGameEngineFactory` :

```text
Runs/RunTests.cs
Runs/AbandonRun/AbandonRunCommandHandlerTests.cs
Runs/ChooseNode/ChooseNodeCommandHandlerTests.cs
Runs/GetRunById/GetRunByIdQueryHandlerTests.cs
Runs/ResolveCurrentEvent/ResolveCurrentEventCommandHandlerTests.cs
Runs/StartRun/StartRunCommandHandlerTests.cs
Events/ChooseEventOption/CurrentEventChoiceResolverDispatcherTests.cs
Runs/MoveToNextRoom/MoveToNextRoomCommandHandlerTests.cs
Events/ChooseEventOption/ChooseCurrentEventOptionCommandHandlerTests.cs
Events/ChoiceResolvers/LawEventChoiceResolverTests.cs
```

Cette liste peut évoluer si d’autres tests utilisent encore des factories locales.

---

## 9. Anciennes factories supprimées

Les anciennes factories spécialisées devenues redondantes ont été supprimées :

```text
TestCurrentEventChoiceResolutionContextFactory.cs
TestNodeEventResolutionContextFactory.cs
TestNodeFactory.cs
TestRoomFactory.cs
TestRunFactory.cs
```

Leur logique utile a été consolidée dans `TestGameEngineFactory`.

---

## 10. Règle de test adoptée

La règle retenue est la suivante :

```text
Tests de Room
→ peuvent utiliser Room.Create(...) directement.

Tests de Node
→ peuvent utiliser Node.Create(...) directement.

Tests de Run, handlers, choix d’événements, progression et combat
→ utilisent TestGameEngineFactory.
```

Cette règle évite que des tests applicatifs reproduisent manuellement des graphes de room.

---

## 11. Bénéfices obtenus

Cette PR apporte plusieurs bénéfices :

```text
- réduction forte de la duplication ;
- suppression de centaines de lignes de factories locales ;
- création de rooms systématiquement valides ;
- meilleure lisibilité des tests ;
- moindre fragilité lors des évolutions du Domain ;
- correction centralisée si les invariants de Room évoluent ;
- cohérence accrue entre les tests de Run, Events et Handlers.
```

---

## 12. Impact sur la maintenabilité

Avant cette PR, chaque évolution de `Room`, `Node` ou `NodeEvent` nécessitait de corriger plusieurs tests manuellement.

Après cette PR, les tests s’appuient sur une factory unique.

Ainsi, si les invariants changent, la correction sera principalement localisée dans :

```text
TestGameEngineFactory.cs
```

Cette centralisation réduit le risque d’incohérences silencieuses entre tests.

---

## 13. Impact sur alpha-0.0.9

Ce refactor est directement lié à `alpha-0.0.9`.

Le jalon `alpha-0.0.9` introduit le démarrage d’un combat depuis un événement de node résolu.

Pour tester ce flux proprement, il faut pouvoir créer facilement :

```text
- une run valide ;
- un node Combat sélectionné ;
- un node courant non résolu ;
- un événement courant résolu ;
- une room complétée ;
- une progression valide.
```

Sans factory commune, chaque test de combat aurait réintroduit sa propre room de test, avec un risque élevé d’invariants invalides.

---

## 14. Ce que cette PR ne fait pas

Cette PR ne modifie pas le comportement métier du Game Engine.

Elle ne crée pas :

```text
- nouveau endpoint ;
- nouveau handler ;
- nouvelle règle de combat ;
- nouveau resolver ;
- nouveau repository ;
- nouveau DTO applicatif.
```

Elle améliore uniquement la structure des tests.

---

## 15. Risques maîtrisés

### Risque : TestGameEngineFactory devient trop grosse

Réponse :

```text
La factory ne doit contenir que les scénarios transverses.
Les factories spécialisées ne doivent être recréées que si un sous-domaine a des besoins spécifiques durables.
```

### Risque : masquer les invariants du domaine

Réponse :

```text
Les tests de Room et Node continuent de pouvoir manipuler directement Room.Create(...) et Node.Create(...).
La factory est utilisée surtout par les tests qui ne testent pas ces invariants.
```

### Risque : créer des scénarios trop artificiels

Réponse :

```text
La room générée respecte les invariants réels du domaine :
initial layer, progression layer, boss node, parents et convergence.
```

---

## 16. Critères de validation

La PR est validée si :

```text
- TestGameEngineFactory existe ;
- les anciennes factories redondantes sont supprimées ;
- les tests applicatifs utilisent la factory commune ;
- les tests de domaine bas niveau restent explicites ;
- aucune room invalide n’est construite dans les tests migrés ;
- le build passe ;
- tous les tests unitaires et d’intégration passent.
```

---

## 17. Commandes de validation

```bash
dotnet format services/game-engine/Leds.GameEngine.slnx
dotnet test services/game-engine/Leds.GameEngine.slnx
dotnet test services/catalog/Leds.Catalog.slnx
dotnet test packages/shared-building-blocks/Leds.SharedBuildingBlocks.slnx
```

---

## 18. Résultat attendu

Résultat attendu après migration :

```text
Build
→ 0 erreur
→ 0 warning bloquant

Tests
→ tous les tests unitaires passent
→ tous les tests d’intégration passent
```

État constaté après correction :

```text
197 tests unitaires Game Engine
25 tests d’intégration Game Engine
tous passent
```

---

## 19. Commit recommandé

```text
test(game-engine): centralize valid room and run factories
```

Si ce refactor est intégré dans la PR fonctionnelle `alpha-0.0.9`, il doit être conservé comme commit séparé pour garder l’historique lisible.

---

## 20. Suite recommandée

Après cette stabilisation des tests, la suite logique est de finaliser le jalon :

```text
alpha-0.0.9
feat(game-engine): start combat from resolved node event
```

Puis de passer au jalon suivant :

```text
alpha-0.0.10
feat(game-engine): expose combat action flow
```

Objectif du prochain jalon :

```text
- créer SubmitCombatActionCommand ;
- récupérer la CombatInstance active ;
- soumettre une BasicAttack ;
- mettre à jour le combat ;
- terminer le combat si un camp est vaincu ;
- débloquer la résolution du node après victoire ;
- préparer le RewardOffer.
```

---

## 21. Conclusion

Cette PR de test est un investissement de maintenabilité.

Elle réduit fortement la duplication, stabilise les scénarios de tests autour d’une room valide et rend le Game Engine plus robuste face aux évolutions du domaine.

Elle est particulièrement importante après la séparation entre :

```text
Node
→ navigation dans la room

NodeEvent
→ contenu événementiel candidat
```

Grâce à `TestGameEngineFactory`, les tests applicatifs peuvent se concentrer sur leur intention métier sans reconstruire manuellement la topologie du Palais à chaque fois.
