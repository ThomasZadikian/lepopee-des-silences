# L’épopée des silences — Suivi technique alpha0.0.2

## Session — Résolution de choix d’événement et verrouillage de progression

**Branche cible :** `v2/develop`  
**Service concerné :** `services/game-engine`  
**Version fonctionnelle :** `alpha0.0.2`  
**Commit recommandé :** `feat(game-engine): enforce current event choice before progression`  
**État final observé :** 126 tests au vert

---

## 1. Contexte de la session

La session précédente avait introduit la première couche structurante du moteur d’événements : la pipeline `INodeEventResolver`, permettant à `ResolveCurrentEvent` de ne plus simplement marquer un node comme résolu, mais de produire un **outcome métier**.

Cette session a prolongé cette logique en traitant un point essentiel : certains événements ne doivent pas permettre au joueur de progresser immédiatement après résolution. Ils doivent d’abord exiger un **choix explicite du joueur**.

Exemples concernés à ce stade :

- `Npc` : écouter ou partir ;
- `Law` : accepter ou refuser une Loi du Palais ;
- `Merchant` : échanger ou refuser ;
- `Curse` : accepter ou refuser une malédiction.

La règle fonctionnelle posée est donc :

```text
ChooseNode
→ ResolveCurrentEvent
→ si RequiresPlayerChoice = true : ChooseCurrentEventOption obligatoire
→ ProgressRun
```

Cette règle renforce le caractère serveur-autoritaire du Game Engine : le frontend ne peut pas ignorer un choix narratif ou systémique et forcer la progression.

---

## 2. Objectif technique de la session

L’objectif n’était pas encore d’implémenter les effets réels des choix, par exemple appliquer une Loi, modifier les récompenses ou écrire dans le Tome.

L’objectif était de poser l’architecture finale minimale permettant de :

1. recevoir un choix d’événement courant ;
2. router ce choix vers un resolver spécialisé ;
3. retourner un résultat métier lisible ;
4. persister le fait que le choix a été effectué ;
5. empêcher la progression tant que le choix obligatoire n’a pas été traité.

---

## 3. Fonctionnalité ajoutée : choix d’option d’événement courant

Une nouvelle route alpha a été ajoutée :

```http
POST /api/v2/runs/{runId}/current-event/choice
```

Cette route est volontairement centrée sur l’événement courant, car le modèle ne dispose pas encore d’un `eventId` persistant complet. À terme, lorsque les événements seront historisés ou portés par l’Event Sourcing, cette route pourra évoluer vers :

```http
POST /api/v2/runs/{runId}/events/{eventId}/choice
```

Cette décision évite de simuler prématurément un identifiant d’événement qui n’existe pas encore dans le modèle réel.

---

## 4. Architecture applicative ajoutée

### 4.1 Use case `ChooseCurrentEventOption`

Un nouveau use case applicatif a été introduit dans le module Events :

```text
Application
└── Events
    └── ChooseEventOption
        ├── ChooseCurrentEventOptionCommand
        ├── ChooseCurrentEventOptionCommandHandler
        ├── ChooseCurrentEventOptionCommandValidator
        ├── ChooseCurrentEventOptionResponse
        ├── CurrentEventChoiceResolutionContext
        ├── CurrentEventChoiceResolutionResult
        ├── ICurrentEventChoiceResolver
        ├── ICurrentEventChoiceResolverDispatcher
        ├── CurrentEventChoiceResolverDispatcher
        ├── ICurrentEventChoiceRequirementResolver
        └── CurrentEventChoiceRequirementResolver
```

Le handler a pour responsabilité d’orchestrer le cas d’usage :

- récupérer la run ;
- vérifier que la run existe ;
- vérifier qu’un événement courant résolu attend potentiellement un choix ;
- construire un contexte métier ;
- appeler le dispatcher ;
- persister le choix si le résultat est accepté ;
- sauvegarder la run ;
- retourner un DTO applicatif.

Le handler ne contient pas la logique spécifique des événements. Cette logique est portée par des resolvers spécialisés.

---

### 4.2 Dispatcher de choix d’événement

Le dispatcher `CurrentEventChoiceResolverDispatcher` a été ajouté pour sélectionner le resolver adapté au `NodeEventType` primaire du node courant.

Principe :

```text
Node.EventType
→ ICurrentEventChoiceResolver correspondant
→ CurrentEventChoiceResolutionResult
```

Si aucun resolver n’est enregistré pour le type d’événement courant, une exception métier est levée :

```text
Current event type '{EventType}' does not accept player choices.
```

Cette règle évite qu’un choix soit envoyé sur un événement qui n’en attend pas, par exemple un `Combat` ou un `Rest`.

---

### 4.3 Resolvers de choix MVP

Quatre resolvers de choix ont été ajoutés :

```text
Application
└── Events
    └── ChoiceResolvers
        ├── NpcEventChoiceResolver
        ├── LawEventChoiceResolver
        ├── MerchantEventChoiceResolver
        └── CurseEventChoiceResolver
```

Ils correspondent aux événements qui exposent déjà `RequiresPlayerChoice = true` dans la pipeline de résolution.

#### `NpcEventChoiceResolver`

Choix actuellement supportés :

- `listen` ;
- `leave`.

Objectif futur : brancher ce resolver sur le système narratif, Elise, les fragments conditionnels et les conséquences relationnelles.

#### `LawEventChoiceResolver`

Choix actuellement supportés :

- `accept-law` ;
- `reject-law`.

Objectif futur : brancher ce resolver sur le `PalaceLawEngine`, afin d’appliquer réellement une Loi du Palais à la run.

#### `MerchantEventChoiceResolver`

Choix actuellement supportés :

- `trade` ;
- `refuse`.

Objectif futur : connecter ce resolver à un système d’échange, de monnaie temporaire, de coût caché ou d’offre contextuelle.

#### `CurseEventChoiceResolver`

Choix actuellement supportés :

- `accept-curse` ;
- `reject-curse`.

Objectif futur : connecter ce resolver à un système de malédictions, de risques augmentés, de récompenses amplifiées et de conséquences persistantes dans la run.

---

## 5. DTOs et contrats de sortie

Les DTOs publics/réutilisables liés aux événements restent centralisés dans :

```text
Application/Events/Dtos
```

Cela permet d’éviter de disperser les contrats exposés à l’API dans chaque sous-use case.

Le nouveau DTO principal est :

```text
ChosenEventOptionResultDto
```

Il expose :

- `ChoiceId` ;
- `Accepted` ;
- `Message` ;
- `NarrativeFragments`.

Ce DTO représente le résultat serveur-autoritaire du choix effectué.

---

## 6. Évolution du domaine `Node`

Le domaine `Node` a été enrichi afin de porter l’état du choix d’événement courant.

Nouvelles propriétés :

```csharp
public string? ChosenEventOptionId { get; private set; }

public bool HasChosenEventOption =>
    !string.IsNullOrWhiteSpace(ChosenEventOptionId);
```

Nouvelle méthode métier :

```csharp
public void ChooseEventOption(string choiceId)
```

Règles métier associées :

- seul un node `Resolved` peut recevoir un choix ;
- le `choiceId` est obligatoire ;
- un choix ne peut être effectué qu’une seule fois ;
- le `choiceId` est trimé avant persistance.

Messages métier associés :

```text
Only a resolved node can receive an event choice.
Event choice id is required.
Current event choice has already been resolved.
```

Cette évolution est importante : le choix n’est pas seulement une réponse API, il devient un état porté par le modèle de run.

---

## 7. Exposition dans les DTOs de run

`NodeDto` a été enrichi pour exposer l’état du choix côté API.

Nouveaux champs :

```text
ChosenEventOptionId
HasChosenEventOption
```

Cela permettra au futur frontend Vue de savoir si un node résolu a déjà traité son choix d’événement.

---

## 8. Verrouillage de progression

Le point central de la session est le verrouillage de `ProgressRun`.

Avant cette session, le flux suivant restait possible :

```text
ChooseNode
→ ResolveCurrentEvent avec RequiresPlayerChoice = true
→ ProgressRun
```

Ce comportement était incorrect, car il permettait de contourner des choix importants : Loi, malédiction, PNJ, marchand.

La progression vérifie désormais :

```text
si le node courant est résolu
et si son type d’événement nécessite un choix
et si aucun choix n’a été enregistré
alors ProgressRun refuse d’avancer
```

Message métier associé :

```text
Current event requires a player choice before progressing.
```

La logique de détection est portée par :

```text
ICurrentEventChoiceRequirementResolver
CurrentEventChoiceRequirementResolver
```

Cette classe s’appuie actuellement sur les `ICurrentEventChoiceResolver` enregistrés. Si un resolver de choix existe pour un type d’événement, alors ce type est considéré comme nécessitant un choix.

Ce choix est volontairement simple pour `alpha0.0.2`, mais il reste compatible avec l’architecture finale. À terme, la règle pourra être portée par les `EventTemplate` du futur `Catalog Service`.

---

## 9. Injection de dépendances

Les nouveaux services ont été enregistrés explicitement dans l’application :

- `ICurrentEventChoiceResolverDispatcher` ;
- `CurrentEventChoiceResolverDispatcher` ;
- `ICurrentEventChoiceRequirementResolver` ;
- `CurrentEventChoiceRequirementResolver` ;
- `NpcEventChoiceResolver` ;
- `LawEventChoiceResolver` ;
- `MerchantEventChoiceResolver` ;
- `CurseEventChoiceResolver`.

L’enregistrement explicite reste préféré à ce stade : il rend les dépendances lisibles et évite une magie de scan prématurée.

---

## 10. Tests ajoutés ou ajustés

### 10.1 Tests domaine `Node`

Le fichier `NodeTests` était vide. Il a été complété afin de couvrir :

- création d’un node valide ;
- création d’un node enfant planifié ;
- validation du `riskLevel` ;
- validation du `rewardProfile` ;
- validation de la profondeur ;
- règles parent/enfant ;
- `Unlock` ;
- `Select` ;
- `Resolve` ;
- `Lock` ;
- `MarkUnreachable` ;
- `ChooseEventOption` ;
- prévention du double choix ;
- trim du `choiceId`.

Cette couverture est importante, car `Node` devient progressivement un agrégat plus riche.

---

### 10.2 Tests du dispatcher de choix

Ajout de tests pour :

- vérifier que le dispatcher appelle le resolver correspondant au `NodeEventType` ;
- vérifier qu’une exception est levée si aucun resolver n’accepte ce type d’événement.

---

### 10.3 Tests du resolver de besoin de choix

Ajout de tests pour :

- retourner `true` si un resolver de choix existe pour le type d’événement ;
- retourner `false` si aucun resolver n’existe.

---

### 10.4 Tests du handler `ChooseCurrentEventOption`

Ajout de tests pour :

- résoudre un choix lorsque l’événement courant est résolu ;
- retourner `NotFoundException` si la run n’existe pas ;
- refuser un choix si l’événement courant n’est pas encore résolu ;
- refuser un double choix.

---

### 10.5 Tests d’intégration adaptés

Les tests d’intégration existants ont été adaptés au nouveau flux obligatoire.

Tests concernés :

- `ProgressRunEndpointTests` ;
- `MoveToNextRoomEndpointTests` ;
- `RoomBossProgressionEndpointTests`.

Ces tests effectuaient auparavant :

```text
ChooseNode
→ ResolveCurrentEvent
→ ProgressRun
```

Ils utilisent désormais un helper :

```text
ResolveCurrentEventAndChooseOptionIfRequiredAsync
```

Ce helper :

1. appelle `/current-event/resolve` ;
2. lit `Outcome.RequiresPlayerChoice` ;
3. si un choix est requis, sélectionne la première option disponible ;
4. appelle `/current-event/choice` ;
5. permet ensuite à `/progress` de réussir.

---

## 11. Résultat de validation

État final :

```text
126 tests passent
```

Cela valide :

- le domaine `Node` enrichi ;
- la persistance du choix d’événement ;
- le use case `ChooseCurrentEventOption` ;
- le dispatcher de choix ;
- la règle de progression bloquante ;
- les flows d’intégration jusqu’au boss de room ;
- le changement de room après complétion ;
- la compatibilité de la nouvelle règle avec les tests de progression existants.

---

## 12. Pourquoi cette architecture est fidèle à la cible finale

Cette session respecte la règle de conception fixée : implémenter petit, mais selon l’architecture finale.

La solution évite :

- un gros `switch` dans les handlers ;
- une logique de choix codée directement dans les controllers ;
- une progression client-side non contrôlée ;
- un contournement des événements narratifs ou systémiques ;
- une future dette technique autour des Lois, PNJ, malédictions ou marchands.

L’architecture prépare :

```text
EventTemplate
→ Resolver spécialisé
→ ChoiceResolver spécialisé
→ Effets gameplay
→ Fragments narratifs
→ TomeWriter
→ PalaceLawEngine
→ CombatService
→ RewardService
→ Event Sourcing
```

---

## 13. Vision future

### 13.1 Court terme

Prochaine étape recommandée :

```text
feat(game-engine): apply first palace law choice effect
```

Objectif : faire en sorte qu’un choix `accept-law` ne retourne pas seulement un message, mais applique réellement une Loi du Palais à la run.

Il faudra introduire progressivement :

```text
Application/PalaceLaws
├── IPalaceLawEngine
├── PalaceLawContext
├── PalaceLawApplicationResult
└── Laws
    └── SilenceLaw / MemoryLaw / RuptureLaw
```

Une autre option possible est de commencer par les récompenses :

```text
feat(game-engine): offer and select node rewards
```

Mais au vu de la centralité des Lois du Palais dans les SFD, commencer par une Loi simple semble plus structurant.

---

### 13.2 Moyen terme

Les choix d’événements devront progressivement produire des effets réels :

- `Law` : ajouter/modifier une Loi active ;
- `Curse` : ajouter un malus ou augmenter le risque futur ;
- `Merchant` : proposer des offres ;
- `Npc` : révéler des fragments narratifs ou ouvrir une branche scénaristique ;
- `Item` : proposer plusieurs récompenses ;
- `Rare` : déclencher un effet exceptionnel ;
- `RoomBoss` : préparer les récompenses de room et l’influence sur Him’Lit.

---

### 13.3 Long terme

À terme, les événements ne doivent plus être seulement des types C#.

La cible recommandée est un modèle hybride :

```text
NodeEventType = catégorie technique stable
EventTemplate = contenu configurable
Resolver = logique métier serveur
ChoiceResolver = résolution des choix
NarrativeFragment = contenu narratif conditionnel
```

Exemples :

```text
NodeEventType = Law
EventTemplate = law_silence_v1

NodeEventType = Npc
EventTemplate = npc_elise_memory_fragment_001

NodeEventType = Combat
EventTemplate = combat_forest_trauma_pack_003
```

Cela permettra d’ajouter beaucoup de contenu sans créer une classe C# par événement narratif.

---

## 14. Versioning

La session reste cohérente avec `alpha0.0.2`.

Raison : cette étape enrichit fortement le moteur d’événements, mais reste encore dans la consolidation du socle Run/Room/Node/Event.

Proposition de lecture :

```text
alpha0.0.2
→ Run/Room/Node server-authoritative
→ Event outcome pipeline
→ Current event choice endpoint
→ Progression verrouillée par choix obligatoire
```

Un futur passage à `alpha0.0.3` serait pertinent lorsque les choix commenceront à produire des effets persistants réels : Lois actives, récompenses, inventaire, malédictions, fragments du Tome.

---

## 15. Commit recommandé

```bash
git add .
git commit -m "feat(game-engine): enforce current event choice before progression"
git push
```

---

## 16. Synthèse

Cette session transforme les choix d’événements en élément réel du moteur serveur-autoritaire.

Avant :

```text
Un événement pouvait indiquer qu’un choix existait, mais la progression pouvait encore continuer sans ce choix.
```

Après :

```text
Un événement à choix bloque réellement la progression tant que le choix n’a pas été effectué.
```

C’est une étape fondamentale pour le jeu final, car elle garantit que les futurs systèmes centraux — Lois du Palais, scénario, Elise, Tome, malédictions, marchands, récompenses — ne pourront pas être contournés par le client.
