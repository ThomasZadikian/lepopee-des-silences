# L’épopée des silences — Suivi technique Catalog

## Jalon : structuration des primitives de contenu du Catalog

**Branche cible :** `v2/develop`  
**Service concerné :** `services/catalog`  
**Commit recommandé :** `refactor(catalog): structure domain primitives by responsibility`  
**Contexte :** extraction progressive du backend legacy v1 vers l’architecture microservices v2

---

## 1. Objectif de l’étape

Cette étape a permis de consolider le socle du futur **Catalog Service** avant l’ajout des premiers templates métier (`EnemyTemplate`, `SkillTemplate`, `ItemTemplate`, etc.).

L’objectif n’était pas encore de migrer les entités legacy du backend v1, mais de poser une structure de domaine propre, évolutive et non temporaire pour tous les contenus versionnés du catalogue.

Le Catalog Service est destiné à porter les référentiels de contenu du jeu :

```text
EnemyTemplate
SkillTemplate
ItemTemplate
NpcTemplate
PalaceLawDefinition
EventTemplate
NarrativeFragmentTemplate
RewardTemplate
```

Cette étape prépare donc la suite sans coupler le Game Engine à des entités legacy ou à des modèles EF.

---

## 2. Correction architecturale importante

Une première approche plaçait plusieurs éléments dans un dossier générique :

```text
Domain/Common
```

Cette organisation a été jugée insuffisante, car `Common` risquait de devenir un dossier fourre-tout.

La structure a donc été corrigée pour répartir les responsabilités dans des dossiers explicites :

```text
Leds.Catalog.Domain
├── Abstractions
├── CatalogContent
├── Combat
├── Errors
├── Enemies
├── Items
└── Skills
```

Cette décision évite une dette d’architecture dès le démarrage du service.

---

## 3. Nouvelle organisation du domaine

### 3.1 `Domain/Abstractions`

Ce dossier contient les abstractions métier transversales du Catalog.

Éléments introduits :

```text
ICatalogContent
CatalogContentBase
```

#### `ICatalogContent`

Interface de lecture commune à tous les contenus du Catalog.

Elle expose :

```text
Id
Key
Name
Description
Version
Status
IsActive
IsDraft
IsDeprecated
IsDisabled
```

Elle permettra plus tard de manipuler différents contenus (`EnemyTemplate`, `SkillTemplate`, `ItemTemplate`, etc.) sans dépendre de leur type concret.

#### `CatalogContentBase`

Classe abstraite portant les comportements communs aux contenus versionnés :

```text
Rename
ChangeDescription
ChangeVersion
Activate
Deprecate
Disable
```

Elle implémente `ICatalogContent`.

Cette classe n’est pas un raccourci temporaire : elle centralise uniquement les règles réellement communes aux contenus du catalogue.

---

### 3.2 `Domain/CatalogContent`

Ce dossier contient les primitives métier liées à l’identité et au cycle de vie d’un contenu catalogué.

Éléments introduits :

```text
CatalogContentId
CatalogContentKey
CatalogContentName
CatalogContentDescription
CatalogContentVersion
CatalogContentStatus
```

#### Renommages effectués

Deux renommages ont été faits pour éviter les ambiguïtés futures :

```text
CatalogItemStatus
→ CatalogContentStatus

CatalogVersion
→ CatalogContentVersion
```

Raison :

- `CatalogItemStatus` pouvait être confondu avec les objets de jeu (`ItemTemplate`) ;
- `CatalogVersion` pouvait être confondu avec la version globale du service Catalog ;
- `CatalogContentStatus` et `CatalogContentVersion` expriment mieux que ces concepts concernent tous les contenus versionnés.

---

### 3.3 `Domain/Combat`

Ce dossier porte les concepts de combat partagés par plusieurs futurs templates.

Élément prévu / introduit :

```text
CombatElement
```

Ce concept sera utilisé par les futurs ennemis, compétences, résistances, affinités et effets de Lois.

Il est volontairement séparé de `CatalogContent`, car il ne décrit pas l’identité d’un contenu mais une donnée gameplay commune.

---

### 3.4 `Domain/Errors`

Ce dossier contient les erreurs métier du domaine.

Élément introduit :

```text
DomainException
```

Cette exception est spécifique au Catalog Service. Elle évite de réutiliser celle du Game Engine et maintient l’indépendance des services.

---

## 4. Règles métier introduites

### 4.1 Identité de contenu

Un contenu catalogué doit disposer :

```text
Id
Key
Name
Version
Status
```

La description est volontairement optionnelle afin de permettre la création de contenus en brouillon.

### 4.2 Validation des primitives

Les règles suivantes ont été introduites :

```text
CatalogContentKey
→ obligatoire, trim

CatalogContentName
→ obligatoire, trim

CatalogContentVersion
→ obligatoire, trim

CatalogContentDescription
→ optionnelle, trim si présente
```

### 4.3 Cycle de vie d’un contenu

Les statuts possibles sont :

```text
Draft
Active
Deprecated
Disabled
```

Les transitions métier actuelles sont :

```text
Draft → Active
Active → Deprecated
Any → Disabled
```

Transitions interdites :

```text
Disabled → Active
Draft → Deprecated
Disabled → Deprecated
```

Ces règles permettent de préparer un Catalog administrable et versionné, sans autoriser des états incohérents.

---

## 5. Point de rigueur : aucun artefact de test dans le Domain

Une classe concrète de test avait été envisagée pour tester `CatalogContentBase`.

Cette approche a été rejetée.

Décision retenue :

```text
Le Domain ne contient que du métier réel.
Les classes de test restent dans le projet de tests.
```

La classe concrète `TestCatalogContent` existe donc uniquement comme classe privée dans :

```text
tests/Leds.Catalog.UnitTests/CatalogContent/CatalogContentBaseTests.cs
```

Cette décision évite une dette structurelle et respecte la séparation des responsabilités.

---

## 6. Tests unitaires

Les tests unitaires valident désormais :

```text
CatalogContentKey
CatalogContentName
CatalogContentDescription
CatalogContentVersion
CatalogContentBase
ICatalogContent
```

Points couverts :

- création valide ;
- trim des valeurs ;
- refus des valeurs obligatoires vides ;
- description optionnelle ;
- activation d’un contenu ;
- dépréciation d’un contenu actif ;
- désactivation d’un contenu ;
- transitions interdites ;
- implémentation de `ICatalogContent`.

À l’issue de l’étape, les tests unitaires du Catalog passent.

---

## 7. Pourquoi cette étape est structurante

Cette étape garantit que les futurs contenus du jeu seront construits sur une base stable.

Au lieu de dupliquer les mêmes propriétés dans chaque futur template :

```text
EnemyTemplate
SkillTemplate
ItemTemplate
NpcTemplate
PalaceLawDefinition
EventTemplate
```

chaque contenu pourra hériter de règles communes déjà testées :

```text
Id
Key
Name
Description
Version
Status
Lifecycle
```

Cela rendra le Catalog plus maintenable, plus extensible et plus cohérent.

---

## 8. Impact sur la roadmap

Cette étape prépare directement :

```text
feat(catalog): add enemy skill item template definitions
```

Le prochain jalon pourra ajouter les premiers modèles métier concrets :

```text
EnemyTemplate
SkillTemplate
ItemTemplate
```

avec une architecture déjà propre :

```text
Domain/Enemies
Domain/Skills
Domain/Items
```

Ensuite, le Catalog pourra évoluer vers :

```text
NpcTemplate
PalaceLawDefinition
EventTemplate
RewardTemplate
NarrativeFragmentTemplate
```

avant d’être relié au Game Engine via des contrats applicatifs.

---

## 9. Décision d’architecture à conserver

Le Catalog doit rester un service de **contenu versionné**, pas un runtime gameplay.

Règle cible :

```text
Catalog décrit le contenu.
Game Engine résout le runtime.
Player conserve la progression durable.
```

Cette séparation évite de recoller le monolithe v1 dans le Game Engine.

---

## 10. Commandes de validation

Depuis `services/catalog` :

```bash
dotnet format Leds.Catalog.slnx
dotnet test Leds.Catalog.slnx
```

Puis vérification du Game Engine :

```bash
cd ../game-engine
dotnet test Leds.GameEngine.slnx
```

---

## 11. Commit recommandé

```bash
git add services/catalog
git commit -m "refactor(catalog): structure domain primitives by responsibility"
git push
```

---

## 12. Synthèse

Ce jalon ne produit pas encore de contenu gameplay visible, mais il sécurise la base du Catalog.

Il évite une architecture temporaire et pose les fondations nécessaires à un catalogue évolutif, versionné et compatible avec l’ajout futur de contenus sans refonte profonde.

C’est une étape volontairement structurante avant l’arrivée des premiers templates métier.
