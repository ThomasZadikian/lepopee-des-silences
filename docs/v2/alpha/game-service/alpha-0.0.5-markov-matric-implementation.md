# L’épopée des silences — Suivi technique alpha-0.0.5

## PR — Game Engine Markov Foundation

**Branche cible :** `v2/develop`
**Service concerné :** `services/game-engine`
**Version concernée :** `alpha-0.0.5`
**Type de PR :** feature Game Engine / Domain / Markov Engine / deterministic runtime
**Commit recommandé :** `feat(game-engine): introduce deterministic markov engine`
**Statut final :** tous les tests passent au moment du suivi
**Diffusion :** confidentiel projet

---

## 1. Contexte de la PR

Cette PR introduit le premier socle réel du **Markov Engine** dans le `Game Engine Service`.

La matrice de Markov est considérée comme une primitive centrale de la v2. Elle doit servir progressivement à guider :

```text
- la génération du Palais ;
- les transitions de rooms ;
- les types d’événements ;
- les attitudes PNJ ;
- la narration modulaire ;
- les effets des Lois du Palais ;
- l’adaptation future d’Him’Lit.
```

L’objectif de cette PR n’était pas d’ajouter un simple helper de tirage pondéré, mais de poser une base mathématique exploitable, déterministe, validée et versionnable.

---

## 2. Objectif de la PR

L’objectif principal était d’introduire un vrai moteur Markov dans le domaine du Game Engine.

Le moteur doit permettre deux opérations fondamentales :

```text
currentState + transitionMatrix + deterministicSeed
→ nextState
```

et :

```text
π(t+1) = π(t) × P
```

Cette seconde opération est essentielle : elle garantit que le moteur manipule bien une matrice de transition exploitable, et pas uniquement un tirage probabiliste décoratif.

---

## 3. Positionnement architectural

Le module Markov est placé dans :

```text
services/game-engine/src/Leds.GameEngine.Domain/Markov
```

Ce choix est volontaire.

Le Markov Engine appartient au `Domain` du Game Engine, car il constitue une primitive métier/mathématique du runtime serveur-autoritaire.

Il ne dépend pas de :

```text
- ASP.NET Core ;
- MediatR ;
- EF Core ;
- Infrastructure ;
- Catalog ;
- Player ;
- Identity ;
- Leaderboard ;
- API Gateway.
```

La Clean Architecture reste donc respectée :

```text
Domain
→ aucune dépendance applicative ou infrastructure

Application
→ pourra consommer le domaine plus tard via des use cases

Infrastructure
→ pourra charger des matrices plus tard

API
→ ne connaît pas encore le moteur Markov
```

---

## 4. Fichiers ajoutés

### Domain

```text
src/Leds.GameEngine.Domain/Markov/MarkovMatrixValidationException.cs
src/Leds.GameEngine.Domain/Markov/MarkovState.cs
src/Leds.GameEngine.Domain/Markov/MarkovTransitionRow.cs
src/Leds.GameEngine.Domain/Markov/MarkovStateDistribution.cs
src/Leds.GameEngine.Domain/Markov/MarkovTransitionMatrix.cs
src/Leds.GameEngine.Domain/Markov/DeterministicMarkovSampler.cs
src/Leds.GameEngine.Domain/Markov/MarkovTransitionResolver.cs
```

### Tests

```text
tests/Leds.GameEngine.UnitTests/Markov/MarkovStateTests.cs
tests/Leds.GameEngine.UnitTests/Markov/MarkovStateDistributionTests.cs
tests/Leds.GameEngine.UnitTests/Markov/MarkovTransitionMatrixTests.cs
tests/Leds.GameEngine.UnitTests/Markov/DeterministicMarkovSamplerTests.cs
tests/Leds.GameEngine.UnitTests/Markov/MarkovTransitionResolverTests.cs
```

---

## 5. Modèle introduit

### 5.1 MarkovState

`MarkovState` représente un état fini de la chaîne.

Règles validées :

```text
- valeur obligatoire ;
- trim automatique ;
- égalité insensible à la casse ;
- prévention des doublons logiques dans une matrice.
```

Le moteur reste générique : aucun état de gameplay réel n’est codé en dur.

---

### 5.2 MarkovTransitionRow

`MarkovTransitionRow` représente une ligne de matrice.

Une ligne associe un état source à une distribution complète de probabilités vers les états cibles.

Règles validées par la matrice :

```text
- une source doit appartenir à l’ensemble des états ;
- chaque cible doit être connue ;
- chaque cible attendue doit être présente ;
- chaque probabilité doit être comprise entre 0 et 1 ;
- la somme de la ligne doit être égale à 1.
```

---

### 5.3 MarkovTransitionMatrix

`MarkovTransitionMatrix` représente la matrice de transition complète `P`.

Elle porte :

```text
- Key ;
- Version ;
- States ;
- Rows.
```

Règles validées :

```text
- key obligatoire ;
- version obligatoire ;
- au moins un état ;
- au moins une ligne ;
- aucun état dupliqué ;
- aucune ligne dupliquée ;
- une ligne par état ;
- lignes complètes ;
- lignes mathématiquement valides.
```

La matrice permet également l’opération :

```text
π(t+1) = π(t) × P
```

via la méthode :

```text
Advance(distribution)
```

---

### 5.4 MarkovStateDistribution

`MarkovStateDistribution` représente une distribution de probabilité sur les états.

Règles validées :

```text
- distribution non vide ;
- probabilités entre 0 et 1 ;
- somme égale à 1 ;
- création possible depuis un état unique via une distribution de Dirac.
```

---

### 5.5 DeterministicMarkovSampler

`DeterministicMarkovSampler` produit un échantillon déterministe dans l’intervalle :

```text
[0, 1)
```

Il utilise une entrée stable composée notamment de :

```text
- seed ;
- scope ;
- matrixKey ;
- matrixVersion ;
- currentState ;
- step.
```

Le sampler n’utilise pas :

```text
- Random.Shared ;
- new Random() ;
- Guid.NewGuid() ;
- DateTime.UtcNow ;
- DateTime.Now.
```

Cela garantit que les transitions pourront être rejouées et auditées.

---

### 5.6 MarkovTransitionResolver

`MarkovTransitionResolver` résout un prochain état observable à partir :

```text
- d’une matrice ;
- d’un état courant ;
- d’un sample déterministe ;
```

ou directement à partir :

```text
- d’une matrice ;
- d’un état courant ;
- d’une seed ;
- d’un scope ;
- d’un step.
```

La résolution utilise la distribution cumulée d’une ligne de transition.

---

## 6. Choix techniques importants

### 6.1 Pas de normalisation automatique

Le moteur ne normalise pas silencieusement les matrices invalides.

Si une ligne ne somme pas à `1`, la matrice est rejetée.

Raison :

```text
Une normalisation implicite pourrait masquer une erreur d’équilibrage ou de contenu.
```

Une normalisation contrôlée pourra être ajoutée plus tard, mais uniquement via une opération explicite et testée.

---

### 6.2 CultureInfo.InvariantCulture

Les messages d’erreur contenant des valeurs décimales ont été rendus stables via une représentation culture-invariante.

Objectif :

```text
- éviter les différences 0.8 / 0,8 selon la culture système ;
- stabiliser les tests ;
- conserver des messages déterministes.
```

---

### 6.3 Moteur générique, usages gameplay séparés

Cette PR ne contient aucune logique spécifique à :

```text
- PNJ ;
- rooms ;
- nodes ;
- Him’Lit ;
- narration ;
- combat ;
- reward ;
- Palace Laws.
```

Le moteur expose uniquement la mécanique mathématique.

Les usages gameplay seront ajoutés par-dessus dans des PR dédiées.

---

## 7. Tests ajoutés

Les tests couvrent :

```text
- création d’états ;
- trim des états ;
- égalité insensible à la casse ;
- rejet d’états invalides ;
- rejet de matrices vides ;
- rejet de versions ou clés invalides ;
- rejet des lignes manquantes ;
- rejet des lignes dupliquées ;
- rejet des états dupliqués ;
- rejet des cibles manquantes ;
- rejet des probabilités invalides ;
- rejet des lignes dont la somme n’est pas égale à 1 ;
- création d’une distribution de Dirac ;
- validation des distributions ;
- multiplication matricielle π(t+1) = π(t) × P ;
- transition discrète par distribution cumulée ;
- déterminisme du sampler ;
- variation du sample selon le step ;
- variation du sample selon la version de matrice.
```

---

## 8. Validation effectuée

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

## 9. Ce que cette PR valide

Cette PR valide que le Game Engine dispose désormais d’un noyau Markov capable de :

```text
- représenter une vraie matrice de transition ;
- valider les invariants mathématiques ;
- faire évoluer une distribution complète ;
- résoudre une transition discrète ;
- produire un échantillon déterministe ;
- intégrer la version de matrice dans le déterminisme ;
- rester indépendant de l’infrastructure et du Catalog.
```

---

## 10. Ce que cette PR ne fait pas encore

Cette PR ne branche pas encore le Markov Engine sur :

```text
- RoomGenerator ;
- NodeGenerator ;
- EventTypeSelector ;
- PalaceLawEngine ;
- NPC system ;
- Narrative system ;
- Him’Lit adaptation ;
- Catalog ;
- API ;
- persistance.
```

Ces intégrations seront traitées dans des PR dédiées.

---

## 11. Risques maîtrisés

### Risque : faux Markov / simple tirage pondéré

Réponse :

```text
Le moteur supporte explicitement π(t+1) = π(t) × P.
```

### Risque : non-reproductibilité

Réponse :

```text
Le sampler est déterministe et basé sur seed + scope + matrixKey + matrixVersion + currentState + step.
```

### Risque : couplage prématuré au Catalog

Réponse :

```text
Aucune référence Catalog n’est introduite dans cette PR.
```

### Risque : fuite de logique gameplay sensible

Réponse :

```text
Les tests utilisent des matrices neutres et génériques.
Aucune matrice réelle de gameplay n’est introduite.
```

---

## 12. Suite recommandée

Suite immédiate recommandée :

```text
feat(game-engine): add catalog content contracts
```

Puis :

```text
feat(game-engine): connect room generation to markov matrix
```

Ensuite :

```text
feat(game-engine): select node event types with markov matrix
```

Puis, dans un jalon dédié :

```text
feat(game-engine): prepare markov npc attitude profiles
```

---

## 13. Commit recommandé

```text
feat(game-engine): introduce deterministic markov engine
```

---

## 14. Conclusion

Cette PR pose une fondation majeure de la v2.

Le Markov Engine est désormais introduit comme un vrai moteur de transition mathématique, déterministe et versionnable, compatible avec la vision serveur-autoritaire du Game Engine et avec les futures exigences de génération, narration et adaptation systémique du Palais.
