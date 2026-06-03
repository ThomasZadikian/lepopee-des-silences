# L’épopée des silences — Suivi technique alpha-0.0.5

## PR — Connect Room Generation to Markov Matrix

**Branche cible :** `v2/develop`
**Service concerné :** `services/game-engine`
**Version concernée :** `alpha-0.0.5`
**Type de PR :** feature Game Engine / Infrastructure generation / Markov integration
**Commit recommandé :** `feat(game-engine): connect room generation to markov matrix`
**Statut final :** tous les tests passent au moment du suivi
**Diffusion :** confidentiel projet

---

## 1. Contexte de la PR

Cette PR branche le Markov Engine sur un premier usage concret du gameplay : la sélection du type de room suivant.

Le Markov Engine avait été introduit précédemment comme un noyau mathématique pur dans le domaine du Game Engine. Il permet déjà :

```text
- la représentation d’une vraie matrice de transition ;
- la validation des lignes de probabilités ;
- l’évolution d’une distribution π(t+1) = π(t) × P ;
- la résolution déterministe d’un état suivant ;
- la prise en compte de la seed, du scope, de la version de matrice et du step.
```

Cette PR ne modifie pas encore toute la génération du Palais. Elle connecte volontairement un premier point d’usage limité, maîtrisé et testable : le choix du `RoomType`.

---

## 2. Objectif de la PR

L’objectif est de remplacer la sélection pseudo-aléatoire du type de room par une transition Markov déterministe.

Avant cette PR, le type de room intermédiaire était choisi par une logique aléatoire classique.

Après cette PR :

```text
seed + currentRoomType + nextRoomDepth + markovMatrixVersion
→ MarkovRoomTypeResolver
→ nextRoomType
```

La matrice Markov guide donc la progression entre les types de rooms, tout en laissant les règles métier prioritaires au générateur.

---

## 3. Positionnement architectural

La PR respecte la Clean Architecture existante.

Le flux reste :

```text
API
→ Controller
→ MediatR / Command Handler
→ Application port IRunGenerator
→ Infrastructure DeterministicRunGenerator
→ MarkovRoomTypeResolver
→ Domain Markov Engine
```

Aucun controller n’a été modifié.

Aucun handler CQRS n’a été contourné.

Le `StartRunCommandHandler` et les autres use cases continuent de dépendre du port applicatif `IRunGenerator`.

L’implémentation concrète reste côté Infrastructure.

---

## 4. Architecture technique

La nouvelle chaîne de génération du type de room est :

```text
DeterministicRunGenerator
→ IRoomTypeResolver
→ MarkovRoomTypeResolver
→ IRoomTypeMarkovMatrixProvider
→ StaticRoomTypeMarkovMatrixProvider
→ MarkovTransitionMatrix
→ MarkovTransitionResolver
→ DeterministicMarkovSampler
```

Répartition par couche :

```text
Domain
- MarkovTransitionMatrix
- MarkovTransitionResolver
- MarkovState
- DeterministicMarkovSampler

Application
- IRunGenerator

Infrastructure
- DeterministicRunGenerator
- IRoomTypeResolver
- MarkovRoomTypeResolver
- IRoomTypeMarkovMatrixProvider
- StaticRoomTypeMarkovMatrixProvider

Tests
- MarkovRoomTypeResolverTests
- DeterministicRunGeneratorTests
```

---

## 5. Fichiers ajoutés

```text
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Types/IRoomTypeMarkovMatrixProvider.cs
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Types/StaticRoomTypeMarkovMatrixProvider.cs
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Types/MarkovRoomTypeResolver.cs
```

Tests ajoutés :

```text
tests/Leds.GameEngine.UnitTests/Generation/Rooms/Types/MarkovRoomTypeResolverTests.cs
```

---

## 6. Fichiers modifiés

```text
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Types/IRoomTypeResolver.cs
src/Leds.GameEngine.Infrastructure/Generation/DeterministicRunGenerator.cs
src/Leds.GameEngine.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs
tests/Leds.GameEngine.UnitTests/Common/Factories/TestGeneratorFactory.cs
tests/Leds.GameEngine.UnitTests/Generation/DeterministicRunGeneratorTests.cs
```

Fichier supprimé :

```text
src/Leds.GameEngine.Infrastructure/Generation/Rooms/Types/RoomTypeResolver.cs
```

L’ancien `RoomTypeResolver` ne correspondait plus au contrat `IRoomTypeResolver`, car il reposait sur une méthode pseudo-aléatoire :

```text
Resolve(int roomDepth, Random random)
```

Il a été remplacé par `MarkovRoomTypeResolver`, qui respecte la nouvelle signature déterministe :

```text
ResolveNextRoomType(string seed, int nextRoomDepth, RoomType currentRoomType, string matrixVersion)
```

---

## 7. Décisions techniques

### 7.1 Markov guide, le domaine arbitre

La matrice Markov ne remplace pas les règles métier.

Elle guide uniquement le choix du prochain type de room sur les profondeurs intermédiaires.

Les règles fixes restent prioritaires :

```text
depth = 0
→ RoomType.Threshold

depth >= 10
→ RoomType.Final
```

Cela garantit que la matrice ne peut pas produire une room finale prématurée ou remplacer le point d’entrée du Palais.

---

### 7.2 Matrice statique temporaire

La matrice est fournie par :

```text
StaticRoomTypeMarkovMatrixProvider
```

Ce provider est temporaire.

Il permet de valider l’intégration du Markov Engine dans le flux de génération sans introduire immédiatement :

```text
- persistance ;
- chargement depuis Catalog ;
- administration des matrices ;
- client HTTP ;
- cache ;
- matrices réelles d’équilibrage.
```

Les poids actuels sont donc des valeurs techniques d’intégration, pas des matrices finales de gameplay.

---

### 7.3 Version de matrice explicite

La version de matrice utilisée pour les types de rooms est :

```text
markov-room-type-0.1.0
```

Le `DeterministicRunGenerator` expose cette version via :

```text
MarkovMatrixVersion
```

Cette version est importante pour la reproductibilité future des runs.

---

### 7.4 Suppression de l’ancien resolver pseudo-aléatoire

L’ancien `RoomTypeResolver` a été supprimé pour éviter de maintenir deux stratégies concurrentes :

```text
- ancienne stratégie pseudo-aléatoire ;
- nouvelle stratégie Markov déterministe.
```

Le choix du type de room doit désormais passer par le Markov Engine.

---

## 8. Règles de déterminisme

Le choix du prochain type de room dépend de :

```text
- seed de run ;
- profondeur de room suivante ;
- type de room courant ;
- matrixKey ;
- matrixVersion ;
- scope de génération.
```

Le scope utilisé est :

```text
room-type-generation
```

À entrées identiques, le résultat doit être identique.

Cela prépare :

```text
- la reproductibilité par seed ;
- le debug ;
- le replay ;
- l’Event Sourcing ;
- l’audit des décisions de génération.
```

---

## 9. Dependency Injection

La DI Infrastructure enregistre désormais les composants nécessaires :

```text
DeterministicMarkovSampler
MarkovTransitionResolver
IRoomTypeMarkovMatrixProvider → StaticRoomTypeMarkovMatrixProvider
IRoomTypeResolver → MarkovRoomTypeResolver
```

Le `DeterministicRunGenerator` reçoit donc un `IRoomTypeResolver` déjà branché sur le moteur Markov.

---

## 10. Tests ajoutés ou ajustés

Les tests couvrent :

```text
- Threshold forcé à la profondeur 0 ;
- Final forcé à la profondeur 10 ;
- déterminisme du type de room pour les mêmes entrées ;
- génération d’un type jouable sur profondeur intermédiaire ;
- erreur si seed vide ;
- erreur si version de matrice inconnue ;
- erreur si le type courant est Final ;
- exposition de la version Markov par DeterministicRunGenerator.
```

Les tests existants du générateur restent valides, ce qui confirme que l’intégration Markov ne casse pas les contraintes de room/nodes déjà stabilisées.

---

## 11. Validation effectuée

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

## 12. Vérifications architecture

Cette PR ne doit introduire aucune dépendance interdite.

Vérifications attendues :

```text
- GameEngine.Domain ne dépend pas d’Application, Infrastructure, API ou Catalog ;
- GameEngine.Application continue de dépendre de ports ;
- GameEngine.Infrastructure implémente les détails de génération ;
- aucun controller n’est modifié ;
- aucun handler CQRS n’est contourné ;
- aucune référence Leds.Catalog.* n’est ajoutée au Game Engine.
```

---

## 13. Ce que cette PR valide

Cette PR valide que le Markov Engine peut maintenant être utilisé par un premier flux réel du Game Engine.

Elle confirme que :

```text
- le moteur Markov n’est plus seulement isolé dans le domaine ;
- une matrice versionnée peut guider une décision de génération ;
- le générateur reste déterministe ;
- les règles métier restent prioritaires ;
- Clean Architecture et CQRS ne sont pas contournés ;
- l’intégration se fait par abstraction et injection.
```

---

## 14. Ce que cette PR ne fait pas encore

Cette PR ne branche pas encore Markov sur :

```text
- génération des nodes ;
- sélection des NodeEventType ;
- sélection d’EventTemplate ;
- PalaceLawEngine ;
- PNJ ;
- narration ;
- Him’Lit ;
- combat ;
- récompenses.
```

Elle ne charge pas encore de matrice depuis :

```text
- Catalog ;
- base de données ;
- fichier JSON ;
- API HTTP ;
- cache Redis.
```

---

## 15. Risques maîtrisés

### Risque : Markov viole les règles métier

Réponse :

```text
Les règles de profondeur restent prioritaires.
Le générateur conserve ses invariants métier.
```

### Risque : couplage avec Catalog

Réponse :

```text
Aucune référence Catalog n’est ajoutée.
La matrice est statique et temporaire côté Infrastructure.
```

### Risque : perte de déterminisme

Réponse :

```text
La résolution dépend de la seed, du scope, de la version et du step.
```

### Risque : coexistence ancien aléatoire / nouveau Markov

Réponse :

```text
L’ancien RoomTypeResolver pseudo-aléatoire a été supprimé.
```

---

## 16. Suite recommandée

Deux suites sont possibles.

### Option A — Continuer l’intégration Markov

```text
feat(game-engine): select node event types with markov matrix
```

Objectif :

```text
Utiliser Markov pour guider les types d’événements des nodes, tout en conservant les contraintes déjà définies :
- Elite max 1 par room ;
- Item max 1 par node et max nodes/2 par room ;
- Npc max 1 par room ;
- Rest max 1 par room ;
- Law max 1 par room ;
- Curse max 1 par room ;
- Rare max 3 par room ;
- Memory non planifié.
```

### Option B — Exploiter les contrats Catalog

```text
feat(game-engine): prepare event resolution from catalog templates
```

Objectif :

```text
Commencer à utiliser les EventTemplateSnapshot côté Game Engine pour préparer la résolution réelle des événements.
```

Recommandation actuelle :

```text
Option A d’abord.
```

Raison :

```text
Le Markov Engine vient d’être introduit comme fondation systémique.
Il est cohérent de poursuivre son intégration dans la génération avant de complexifier la résolution événementielle.
```

---

## 17. Commit recommandé

```text
feat(game-engine): connect room generation to markov matrix
```

---

## 18. Conclusion

Cette PR constitue le premier branchement concret du Markov Engine dans la génération du Palais.

Le type de room suivant est désormais guidé par une matrice Markov déterministe et versionnée, tout en conservant les garanties métier existantes du générateur.

La v2 commence ainsi à construire le gameplay autour de la matrice, au lieu d’ajouter Markov tardivement comme un simple décor probabiliste.
