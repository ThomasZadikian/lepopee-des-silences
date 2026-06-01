# L’épopée des silences — Suivi technique alpha-0.0.4

## PR 1 — Catalog Foundation : primitives de domaine et premiers templates gameplay

**Branche cible :** `v2/develop`  
**Service concerné :** `services/catalog`  
**Version concernée :** `alpha-0.0.4`  
**Type de jalon :** fondation Catalog Service / extraction progressive du backend legacy  
**Commit recommandé :** `feat(catalog): add domain primitives and gameplay templates`

---

## 1. Contexte de la PR

Cette PR constitue la première PR officielle de la version `alpha-0.0.4`.

Elle marque le début concret du **Catalog Service** dans l’architecture v2 de *L’épopée des silences*.

Jusqu’ici, le projet disposait principalement :

```text
backend/
→ backend legacy v1 complet, riche en entités métier

services/game-engine/
→ moteur serveur-autoritaire des runs, rooms, nodes, events, choices, Palace Laws runtime

docs/v2/
→ SFD v2, mapping legacy, document de transition V1 → V2
```

Avec cette PR, le service suivant commence réellement à prendre forme :

```text
services/catalog/
→ référentiel versionné du contenu de jeu
```

Le rôle du Catalog est de porter les définitions de contenu stables et versionnées :

```text
EnemyTemplate
SkillTemplate
ItemTemplate
NpcTemplate
PalaceLawDefinition
EventTemplate
RewardTemplate
NarrativeFragmentTemplate
```

Le Game Engine ne doit pas devenir propriétaire de ces référentiels. Il doit uniquement consommer le contenu nécessaire pour résoudre le runtime.

---

## 2. Objectif de la PR

Cette PR ne se limite pas à un refactor de dossiers.

Elle couvre deux volets :

```text
1. Structuration propre des primitives de domaine du Catalog
2. Ajout des premiers templates gameplay : Enemy, Skill, Item
```

Objectif global :

```text
Préparer un Catalog Service évolutif, versionné et non temporaire,
capable d’accueillir progressivement les contenus issus du backend v1,
sans coupler le Game Engine aux anciennes entités legacy.
```

---

## 3. Décision architecturale majeure

Une première organisation du domaine reposait sur un dossier générique :

```text
Domain/Common
```

Cette approche a été rejetée, car elle risquait de devenir rapidement un fourre-tout.

La structure retenue sépare désormais les responsabilités :

```text
Leds.Catalog.Domain
├── Abstractions
├── CatalogContent
├── Combat
├── Enemies
├── Errors
├── Items
└── Skills
```

Cette organisation n’est pas temporaire. Elle constitue la base sur laquelle les prochains domaines du Catalog seront ajoutés.

---

## 4. Organisation du domaine

### 4.1 `Domain/Abstractions`

Ce dossier contient les abstractions métier transversales du Catalog.

Éléments introduits :

```text
ICatalogContent
CatalogContentBase
```

#### `ICatalogContent`

Interface commune de lecture pour tous les contenus du Catalog.

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

Cette interface permettra de manipuler de façon homogène plusieurs types de contenus :

```text
EnemyTemplate
SkillTemplate
ItemTemplate
NpcTemplate
PalaceLawDefinition
EventTemplate
```

Elle est volontairement limitée à un contrat de lecture et d’état. Elle ne porte pas de comportements métier complexes.

#### `CatalogContentBase`

Classe abstraite portant les comportements communs à tous les contenus catalogués :

```text
Rename
ChangeDescription
ChangeVersion
Activate
Deprecate
Disable
```

Elle implémente `ICatalogContent`.

Son rôle est de centraliser uniquement les invariants réellement communs :

```text
identité
clé
nom
description
version
statut
cycle de vie
```

Elle ne doit pas devenir un objet générique incontrôlé. Les règles spécifiques restent dans les templates concernés.

---

### 4.2 `Domain/CatalogContent`

Ce dossier contient les primitives métier décrivant l’identité et le cycle de vie d’un contenu versionné.

Éléments introduits :

```text
CatalogContentId
CatalogContentKey
CatalogContentName
CatalogContentDescription
CatalogContentVersion
CatalogContentStatus
```

#### `CatalogContentId`

Identifiant fortement typé des contenus du Catalog.

Objectif :

```text
éviter les Guid nus
préparer la persistance future
préparer les contrats interservices
```

#### `CatalogContentKey`

Clé fonctionnelle stable du contenu.

Exemples futurs :

```text
enemy-shadow-wolf
skill-shadow-bite
item-memory-potion
law-silence-v1
event-npc-elise-threshold
```

Règle :

```text
la clé est obligatoire et trimée
```

#### `CatalogContentName`

Nom affichable du contenu.

Règle :

```text
le nom est obligatoire et trimé
```

#### `CatalogContentDescription`

Description optionnelle.

Règle :

```text
la description peut être vide
si elle existe, elle est trimée
```

Cette souplesse est utile pour créer des contenus en brouillon.

#### `CatalogContentVersion`

Version propre à un contenu.

Le choix de nommage est volontaire :

```text
CatalogContentVersion
```

plutôt que :

```text
CatalogVersion
```

afin d’éviter la confusion avec la version globale du service Catalog.

#### `CatalogContentStatus`

Statuts possibles :

```text
Draft
Active
Deprecated
Disabled
```

Le choix de nommage est volontaire :

```text
CatalogContentStatus
```

plutôt que :

```text
CatalogItemStatus
```

afin d’éviter la confusion avec les futurs `ItemTemplate`.

---

### 4.3 `Domain/Combat`

Ce dossier contient les concepts de combat partagés par plusieurs templates.

Élément introduit :

```text
CombatElement
```

`CombatElement` prépare les futurs systèmes de types, affinités, résistances et effets élémentaires.

Il est utilisé par :

```text
EnemyTemplate
SkillTemplate
```

et sera probablement réutilisé par :

```text
PalaceLawDefinition
StatusEffectTemplate
DamageResolver
CombatantSnapshot
```

---

### 4.4 `Domain/Errors`

Ce dossier contient les erreurs métier propres au Catalog.

Élément introduit :

```text
DomainException
```

Le Catalog Service possède sa propre exception de domaine. Il ne dépend pas de celle du Game Engine.

Cette séparation confirme l’indépendance des services.

---

## 5. Templates gameplay introduits

Cette PR ajoute aussi les trois premiers modèles métier concrets nécessaires à la préparation du Combat MVP :

```text
EnemyTemplate
SkillTemplate
ItemTemplate
```

Ils constituent la première étape de transformation des entités legacy v1 vers un modèle Catalog v2 propre.

---

## 6. Domaine `Enemies`

### Éléments introduits

```text
EnemyArchetype
EnemyTemplate
```

### 6.1 `EnemyArchetype`

Catégorise le type fonctionnel d’un ennemi.

Exemples :

```text
Trauma
Memory
Shadow
Guardian
Elite
Boss
```

Cette catégorisation prépare :

```text
la génération
le scaling
le combat
les récompenses
les fragments narratifs
les boss de room
```

### 6.2 `EnemyTemplate`

`EnemyTemplate` représente une définition de référence d’un ennemi.

Il ne représente pas un ennemi vivant dans une run.

La séparation est importante :

```text
EnemyTemplate
→ contenu versionné dans Catalog

CombatantSnapshot
→ état runtime dans Game Engine
```

Attributs principaux :

```text
Archetype
Element
MaxHealth
Strength
Intelligence
Speed
PhysicalResistance
MagicalResistance
ExperienceReward
GoldReward
```

Règles métier introduites :

```text
EnemyArchetype.Unknown interdit
MaxHealth > 0 obligatoire
Strength entre 0 et 999
Intelligence entre 0 et 999
Speed entre 0 et 999
PhysicalResistance entre 0 et 100
MagicalResistance entre 0 et 100
ExperienceReward non négatif
GoldReward non négatif
```

Cette structure reprend l’intention du backend v1, mais sans copier l’entité legacy telle quelle.

---

## 7. Domaine `Skills`

### Éléments introduits

```text
SkillEffectType
SkillTargetType
SkillTemplate
```

### 7.1 `SkillEffectType`

Décrit la nature principale d’une compétence :

```text
Damage
Heal
Buff
Debuff
Status
Utility
```

### 7.2 `SkillTargetType`

Décrit la cible fonctionnelle d’une compétence :

```text
Self
SingleAlly
AllAllies
SingleEnemy
AllEnemies
AnySingle
```

### 7.3 `SkillTemplate`

`SkillTemplate` représente une définition de compétence.

Elle ne représente pas encore une compétence débloquée par un joueur, ni une action de combat en cours.

Séparation cible :

```text
SkillTemplate
→ Catalog

UnlockedSkill / PlayerSkill
→ Player Service

CombatSkillSnapshot
→ Game Engine runtime

CombatAction
→ intention joueur validée par le backend
```

Attributs principaux :

```text
Element
EffectType
TargetType
ManaCost
ChargeCost
BasePower
HealPower
```

Règles métier introduites :

```text
ManaCost non négatif
ChargeCost non négatif
BasePower non négatif
HealPower non négatif
Damage skill → BasePower > 0
Heal skill → HealPower > 0
```

Cette structure prépare le futur système de combat à quatre compétences actives, PP/charges, mana, types, affinités et résolution serveur-autoritaire.

---

## 8. Domaine `Items`

### Éléments introduits

```text
ItemCategory
ItemRarity
ItemDuration
ItemTemplate
```

### 8.1 `ItemCategory`

Catégories prévues :

```text
Consumable
Equipment
Relic
Key
Currency
Material
```

### 8.2 `ItemRarity`

Raretés prévues :

```text
Common
Uncommon
Rare
Epic
Legendary
Unique
```

### 8.3 `ItemDuration`

Durées prévues :

```text
RunOnly
Permanent
```

Cette distinction est essentielle pour la v2, qui sépare :

```text
progression temporaire de run
progression permanente du joueur
```

### 8.4 `ItemTemplate`

`ItemTemplate` représente une définition de référence d’objet.

Il ne représente pas encore :

```text
un objet possédé par un joueur
une récompense proposée
un objet consommé pendant une run
```

Séparation cible :

```text
ItemTemplate
→ Catalog

PlayerInventoryItem
→ Player Service

RewardOffer
→ Game Engine runtime

RunInventoryItem
→ Game Engine runtime
```

Attributs principaux :

```text
Category
Rarity
Duration
EffectValue
Price
```

Règles métier introduites :

```text
EffectValue non négatif
Price non négatif
```

---

## 9. Décisions de migration V1 → V2

Cette PR respecte la stratégie de transition décidée précédemment :

```text
Le backend v1 est une source de migration métier,
mais les entités ne doivent pas être recopiées telles quelles.
```

Mapping concerné :

```text
Enemy
→ EnemyTemplate côté Catalog
→ futur CombatantSnapshot côté Game Engine

Skill
→ SkillTemplate côté Catalog
→ futur PlayerSkill côté Player
→ futur CombatSkillSnapshot côté Game Engine

Item
→ ItemTemplate côté Catalog
→ futur PlayerInventory côté Player
→ futur RewardOffer côté Game Engine
```

Cela évite de recréer un monolithe dans le Game Engine.

---

## 10. Règles de séparation confirmées

La PR confirme la règle cible :

```text
Catalog décrit le contenu.
Game Engine résout le runtime.
Player conserve la progression durable.
```

Conséquences :

```text
EnemyTemplate n’a pas d’état de combat courant.
SkillTemplate ne sait pas si le joueur possède la compétence.
ItemTemplate ne sait pas si un joueur possède l’objet.
CatalogContentBase ne connaît pas EF Core.
Le Game Engine ne dépend pas d’entités legacy.
```

---

## 11. Tests unitaires

Les tests couvrent désormais deux niveaux.

### 11.1 Primitives Catalog

Tests sur :

```text
CatalogContentKey
CatalogContentName
CatalogContentDescription
CatalogContentVersion
CatalogContentBase
ICatalogContent
```

Points validés :

```text
création valide
trim des valeurs
valeurs obligatoires
description optionnelle
statuts Draft / Active / Deprecated / Disabled
transitions autorisées
transitions interdites
implémentation de ICatalogContent
```

### 11.2 Templates gameplay

Tests attendus ou intégrés autour de :

```text
EnemyTemplate
SkillTemplate
ItemTemplate
```

Points validés :

```text
création valide
invariants de stats ennemi
invariants de résistances
coûts de compétence non négatifs
puissance obligatoire pour les dégâts
soin obligatoire pour les compétences de soin
valeur/prix d’objet non négatifs
```

---

## 12. Pourquoi cette PR est importante

Cette PR transforme le Catalog d’un simple squelette de service en véritable socle métier.

Avant cette PR :

```text
Catalog Service
→ solution Clean Architecture vide ou quasi vide
```

Après cette PR :

```text
Catalog Service
→ primitives versionnées
→ contrat commun de contenu
→ cycle de vie des contenus
→ premiers templates gameplay
→ base directe pour Combat MVP
```

Elle permet d’avancer vers le combat sans enfermer le Game Engine dans les modèles legacy.

---

## 13. Impact sur alpha-0.0.4

Cette PR devient donc :

```text
alpha-0.0.4 — PR 1
Catalog Foundation — Domain primitives and core gameplay templates
```

Elle remplace la vision initiale plus limitée :

```text
PR 1 — Catalog domain primitives
PR 2 — Enemy / Skill / Item templates
```

par une PR plus complète :

```text
PR 1 — Catalog domain primitives + Enemy / Skill / Item templates
```

---

## 14. Roadmap alpha-0.0.4 mise à jour

Roadmap ajustée :

```text
PR 1 — Catalog domain primitives + Enemy / Skill / Item templates
PR 2 — PalaceLawDefinition + EventTemplate foundations
PR 3 — Catalog application read contracts
PR 4 — InMemory / Static catalog provider
PR 5 — Game Engine catalog contracts preparation
```

La prochaine étape logique n’est donc plus d’ajouter `EnemyTemplate`, `SkillTemplate` et `ItemTemplate`, puisqu’ils sont déjà intégrés.

La prochaine étape recommandée devient :

```text
feat(catalog): add palace law and event template definitions
```

ou, si une consolidation est nécessaire :

```text
test(catalog): complete gameplay template unit coverage
```

---

## 15. Commandes de validation

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

## 16. Commit recommandé

Depuis la racine du repo :

```bash
git add services/catalog docs/v2/follow-up/SUIVI_TECHNIQUE_ALPHA_0_0_4_CATALOG_FOUNDATION.md
git commit -m "feat(catalog): add domain primitives and gameplay templates"
git push
```

---

## 17. Synthèse

Cette PR est structurante.

Elle ne se contente pas d’organiser le code : elle pose la base du modèle de contenu versionné de la v2.

Elle prépare directement :

```text
le Combat MVP
les futures récompenses
les futurs objets de run
les compétences joueur/compagnons
les ennemis scalés
les contrats Game Engine ↔ Catalog
la migration progressive du backend v1
```

Elle respecte la règle d’architecture centrale :

```text
ne pas jeter le legacy,
ne pas le copier tel quel,
mais le transformer dans les bons bounded contexts.
```

C’est donc bien la première PR officielle de `alpha-0.0.4`.
