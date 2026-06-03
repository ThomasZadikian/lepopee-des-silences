# L’épopée des silences — Suivi technique alpha-0.0.5

## PR — Select Node Event Types with Markov Matrix

**Branche cible :** `v2/develop`
**Service concerné :** `services/game-engine`
**Version concernée :** `alpha-0.0.5`
**Type de PR :** feature Game Engine / Infrastructure generation / Markov integration
**Commit recommandé :** `feat(game-engine): select node event types with markov matrix`
**Statut final :** tous les tests passent au moment du suivi
**Diffusion :** confidentiel projet

---

## 1. Contexte de la PR

Cette PR poursuit l’intégration progressive du Markov Engine dans la génération du Palais.

La PR précédente avait branché Markov sur la sélection du `RoomType`. Cette nouvelle étape branche Markov sur un deuxième niveau de génération : la sélection du type d’événement d’un node.

L’objectif n’est pas encore de résoudre le contenu précis d’un événement, mais uniquement sa famille fonctionnelle.

Exemples de familles :

```text
Combat
Elite
Item
Npc
Rest
Merchant
Law
Curse
Rare
```

Les événements non planifiables ou réservés restent exclus de cette sélection.

---

## 2. Objectif de la PR

L’objectif principal est de remplacer la sélection pseudo-aléatoire directe du `NodeEventType` par une sélection Markov déterministe, tout en conservant les contraintes métier existantes.

Le nouveau flux est :

```text
RoomType
+ seed
+ roomDepth
+ nodeDepth
+ eventIndex
+ previousEventType
+ matrixVersion
+ allowedEventTypes
→ MarkovNodeEventTypeResolver
→ NodeEventType
```

Le principe reste :

```text
Markov propose.
Les contraintes métier arbitrent.
Le backend reste serveur-autoritaire.
```

---

## 3. Positionnement architectural

La PR respecte la Clean Architecture existante.

Le flux applicatif reste inchangé :

```text
API
→ Controller
→ MediatR / Command Handler
→ IRunGenerator
→ DeterministicRunGenerator
→ RoomPlanGenerator
→ RoomNodeFactory
→ NodeEventGenerator
→ MarkovNodeEventTypeResolver
→ Markov Engine
```

Aucun controller n’a été modifié.

Aucun handler CQRS n’a été contourné.

La logique reste dans la chaîne de génération du Game Engine, côté Infrastructure, en s’appuyant sur le noyau Markov du Domain.

---

## 4. Fichiers ajoutés

```text
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Events/INodeEventTypeMarkovMatrixProvider.cs
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Events/StaticNodeEventTypeMarkovMatrixProvider.cs
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Events/NodeEventTypeSelectionContext.cs
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Events/MarkovNodeEventTypeResolver.cs
```

Tests ajoutés :

```text
tests/Leds.GameEngine.UnitTests/Generation/Rooms/Events/MarkovNodeEventTypeResolverTests.cs
```

---

## 5. Fichiers modifiés

```text
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Events/INodeEventGenerator.cs
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Events/NodeEventGenerator.cs
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Nodes/IRoomNodeFactory.cs
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Nodes/RoomNodeFactory.cs
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Planning/IRoomPlanGenerator.cs
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Planning/RoomPlanGenerator.cs
src/Leds.GameEngine.Infrastructure/Generation/DeterministicRunGenerator.cs
src/Leds.GameEngine.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs
tests/Leds.GameEngine.UnitTests/Generation/Rooms/Events/NodeEventGeneratorTests.cs
tests/Leds.GameEngine.UnitTests/Common/Factories/TestGeneratorFactory.cs
```

---

## 6. Architecture technique

La nouvelle chaîne de décision pour les événements de node est :

```text
NodeEventGenerator
→ NodeEventCandidateResolver
→ MarkovNodeEventTypeResolver
→ StaticNodeEventTypeMarkovMatrixProvider
→ MarkovTransitionResolver
→ DeterministicMarkovSampler
```

Répartition des responsabilités :

```text
NodeEventCandidateResolver
→ calcule les types autorisés selon les contraintes métier

MarkovNodeEventTypeResolver
→ sélectionne un type parmi les candidats autorisés

NodeEventGenerator
→ crée les NodeEvent et enregistre les compteurs dans RoomEventGenerationState
```

Cette séparation est importante : Markov ne contourne jamais les règles métier.

---

## 7. Propagation des paramètres déterministes

Pour permettre une vraie sélection déterministe, plusieurs signatures ont été enrichies.

Les informations suivantes sont maintenant transmises jusqu’au `NodeEventGenerator` :

```text
seed
matrixVersion
roomDepth
nodeDepth
eventIndex
roomType
```

La chaîne mise à jour est :

```text
DeterministicRunGenerator
→ RoomPlanGenerator.Generate(seed, matrixVersion, roomDepth, roomType, random)
→ RoomNodeFactory.CreateLayerNodes(... seed, matrixVersion, roomDepth, nodeDepth ...)
→ NodeEventGenerator.Generate(... seed, matrixVersion, roomDepth, nodeDepth ...)
→ MarkovNodeEventTypeResolver
```

Cette propagation rend la génération reproductible et prépare les futures extensions.

---

## 8. Décisions techniques

### 8.1 Matrice statique temporaire

La matrice est fournie par :

```text
StaticNodeEventTypeMarkovMatrixProvider
```

La version actuelle est :

```text
markov-node-event-type-0.1.0
```

Cette matrice est temporaire et sert à valider l’intégration technique.

Elle ne représente pas encore les poids finaux d’équilibrage.

---

### 8.2 Exclusion des événements non planifiables

La sélection Markov exclut les événements qui ne doivent pas être planifiés dans les nodes standards.

Sont exclus :

```text
Memory
RoomBoss
FinalBoss
```

Les événements de boss restent créés par la génération spécifique des boss nodes.

Les événements Memory restent destinés à apparaître plus tard à la résolution d’un node ou via une logique dédiée, et non comme événement planifié standard.

---

### 8.3 Contraintes métier prioritaires

Le moteur Markov sélectionne uniquement parmi les types autorisés par `NodeEventCandidateResolver`.

Cela préserve les contraintes déjà actées :

```text
Combat : illimité
Elite : maximum 1 par room
Item : maximum 1 par node et maximum nodes/2 par room
Npc : maximum 1 par room
Rest : maximum 1 par room
Merchant : maximum 1 par room
Law : maximum 1 par room
Curse : maximum 1 par room
Rare : maximum 3 par room
Memory : non planifié
Boss : réservé au boss node
```

Si Markov propose un type non autorisé, le resolver applique une stratégie de fallback déterministe.

---

### 8.4 Stratégie de fallback

Lorsque le type échantillonné n’est pas autorisé par les contraintes courantes, le resolver ne relance pas un tirage aléatoire.

Il parcourt la ligne Markov par probabilité décroissante et choisit le premier type encore autorisé.

Cela garantit :

```text
- déterminisme ;
- absence de boucle de retry ;
- respect des contraintes métier ;
- cohérence avec la matrice.
```

Si aucun type proposé par la ligne n’est autorisé, un fallback sécurisé est appliqué selon les types restants.

---

### 8.5 Conservation du Random uniquement pour le volume

`Random` reste utilisé pour déterminer le nombre d’événements dans un node.

Le choix du `NodeEventType`, lui, passe désormais par :

```text
seed + matrixVersion + roomDepth + nodeDepth + eventIndex + Markov
```

Le générateur conserve donc un comportement déterministe à seed identique.

---

## 9. Tests ajoutés ou adaptés

Les tests couvrent notamment :

```text
- sélection du type échantillonné quand il est autorisé ;
- fallback vers le type autorisé le plus probable ;
- utilisation d’un état source dérivé du RoomType si aucun événement précédent n’existe ;
- utilisation du PreviousEventType si disponible ;
- déterminisme pour les mêmes entrées ;
- exclusion de Memory dans les nodes standards ;
- rejet si seuls des types non planifiables sont autorisés ;
- rejet d’une version de matrice inconnue ;
- conservation des garanties du NodeEventGenerator ;
- respect de la sortie du CandidateResolver ;
- génération entre 1 et 4 événements par node ;
- enregistrement des événements générés dans RoomEventGenerationState.
```

---

## 10. Validation effectuée

Commandes exécutées :

```bash
dotnet format services/game-engine/Leds.GameEngine.slnx
dotnet test services/game-engine/Leds.GameEngine.slnx
dotnet test services/catalog/Leds.Catalog.slnx
dotnet test packages/shared-building-blocks/Leds.SharedBuildingBlocks.slnx
```

Résultat :

```text
Tous les tests passent.
```

---

## 11. Vérifications architecture

Cette PR ne doit introduire aucune dépendance interdite.

Points vérifiés ou à vérifier avant push :

```text
- GameEngine.Domain ne dépend pas d’Application, Infrastructure, API ou Catalog ;
- GameEngine.Application n’est pas couplé à Infrastructure ;
- GameEngine.Infrastructure implémente les détails de génération ;
- aucun controller n’est modifié ;
- aucun handler CQRS n’est contourné ;
- aucune référence Leds.Catalog.* n’est ajoutée au Game Engine ;
- aucun Random.Shared n’est utilisé dans les nouveaux resolvers Markov ;
- aucune matrice réelle ou sensible de gameplay n’est exposée.
```

---

## 12. Ce que cette PR valide

Cette PR valide que le Markov Engine est maintenant utilisé sur deux niveaux de génération :

```text
1. RoomType
2. NodeEventType
```

Elle confirme que :

```text
- la génération commence à se construire autour de Markov ;
- les règles métier restent prioritaires ;
- la sélection d’événements est déterministe ;
- les types non planifiables restent exclus ;
- le pipeline est prêt pour des couches de résolution plus fines.
```

---

## 13. Ce que cette PR ne fait pas encore

Cette PR ne résout pas encore le contenu précis d’un événement.

Elle ne choisit pas encore :

```text
- l’ennemi généré ;
- le type d’ennemi ;
- l’item généré ;
- la rareté réelle ;
- la loi exacte ;
- le PNJ exact ;
- la récompense exacte ;
- le fragment narratif ;
- l’attitude PNJ ;
- l’impact sur Him’Lit.
```

Ces choix seront traités par des resolvers spécialisés ultérieurs.

---

## 14. Vision cible conservée

Cette PR prépare un pipeline de génération par couches successives.

Vision cible :

```text
RoomType
→ NodeEventType
→ EventTemplate
→ Runtime payload
→ Enemy / Item / Law / NPC / Reward / Narrative resolver
```

Chaque couche devra pouvoir dépendre de :

```text
- RoomType ;
- NodeEventType ;
- seed ;
- matrixVersion ;
- run context ;
- active laws ;
- constraints ;
- history ;
- Catalog snapshots ;
- Markov.
```

Cette PR ne met en place que la couche `NodeEventType`, mais elle prépare correctement la suite.

---

## 15. Risques maîtrisés

### Risque : Markov remplace les règles métier

Réponse :

```text
Le CandidateResolver filtre les types autorisés avant la sélection finale.
Markov ne choisit que dans l’espace légal.
```

### Risque : perte de déterminisme

Réponse :

```text
Le choix dépend de la seed, de la version de matrice, du scope, de la profondeur et de l’index d’événement.
```

### Risque : fuite de logique sensible

Réponse :

```text
La matrice actuelle est technique et temporaire.
Aucune matrice finale d’équilibrage ou logique narrative sensible n’est ajoutée.
```

### Risque : explosion de responsabilité dans NodeEventGenerator

Réponse :

```text
NodeEventGenerator orchestre uniquement la génération d’événements du node.
La sélection Markov est déléguée à MarkovNodeEventTypeResolver.
Les contraintes restent dans NodeEventCandidateResolver.
```

---

## 16. Suite recommandée

Suite logique possible :

```text
feat(game-engine): prepare event resolution from catalog templates
```

Objectif :

```text
Utiliser les EventTemplateSnapshot pour préparer la résolution réelle des événements.
```

Autre suite possible :

```text
feat(game-engine): introduce event content resolution pipeline
```

Objectif :

```text
Créer la structure des resolvers spécialisés :
- CombatEventContentResolver
- ItemEventContentResolver
- PalaceLawEventContentResolver
- NpcEventContentResolver
- RareEventContentResolver
```

Recommandation actuelle :

```text
Préparer d’abord le pipeline de résolution de contenu, sans encore brancher le combat complet.
```

---

## 17. Commit recommandé

```text
feat(game-engine): select node event types with markov matrix
```

---

## 18. Conclusion

Cette PR constitue une étape importante dans la refonte v2.

La génération ne repose plus uniquement sur des choix pseudo-aléatoires locaux. Les types d’événements de node sont désormais guidés par une matrice Markov déterministe, versionnée et contrainte par les règles métier du domaine.

Le projet continue ainsi à construire la génération autour du Markov Engine, tout en conservant les principes essentiels : Clean Architecture, CQRS, backend serveur-autoritaire, testabilité et déterminisme par seed.
