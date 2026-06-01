# L'épopée des silences - Mapping backend legacy v1 vers services v2

## 1. Objet du document

Ce document formalise la réflexion de migration du backend legacy `backend/` de **RPG_ESI07 v1.0.0** vers l'architecture cible de **L'épopée des silences v2**.

Il ne s'agit pas d'un plan de suppression du backend v1. Le backend v1 est considéré comme une **source de référence métier** : il contient déjà des entités, contrôleurs, handlers, repositories, services et tests utiles. La stratégie retenue est donc une extraction progressive, documentée et contrôlée.

Objectifs :

- inventorier les domaines existants dans le backend v1 ;
- décider où chaque élément doit vivre dans l'architecture v2 ;
- éviter de recréer des concepts déjà présents ;
- empêcher la création d'un Game Engine trop monolithique ;
- préparer l'extraction vers `Catalog`, `Player`, `Identity`, `Audit/GDPR` et `Leaderboard` ;
- cadrer le futur Combat MVP avant de coder.

---

## 2. Sources analysées

Sources utilisées pour ce mapping :

```text
backend.zip
LEpopee_des_Silences_SFD_V2_complete.pdf
Résumé-conversation-originale.txt
3WA_TZADIKIAN_Jury_RNCP_refondu_v1.docx.pdf
état actuel du service services/game-engine
```

Le backend v1 contient une structure Clean Architecture complète :

```text
backend/
├── RPG_ESI07.API
├── RPG_ESI07.Application
├── RPG_ESI07.Domain
├── RPG_ESI07.Infrastructure
└── RPG_ESI07.Tests
```

Contrôleurs observés :

```text
AuditLogsController
AuthController
BestiaryUnlocksController
CombatStatsController
CompanionController
EnemiesController
GameSavesController
ItemsController
LeaderboardController
NpcInteractionsController
NpcsController
PlayerInventoriesController
PlayerProfileController
PlayerSkillsController
RGPDController
SkillsController
UserConsentsController
UsersController
```

Entités domaine observées :

```text
AuditLog
BestiaryUnlock
CombatStats
CompanionState
Enemy
GameSave
Item
Npc
NpcInteraction
PlayerInventory
PlayerProfile
PlayerSkill
Skill
User
UserConsent
```

---

## 3. Décision structurante

La v2 ne doit pas devenir une micro-architecture fragmentée à l'extrême.

Décision retenue :

```text
Game Engine Service central pour le runtime gameplay fortement couplé.
Services périphériques spécialisés pour les domaines autonomes.
```

Le Game Engine doit conserver la cohérence entre :

```text
Run
Room
Node
Palace Laws
Events
Combat runtime
Rewards runtime
Narrative runtime
```

Les services périphériques portent les référentiels, l'identité, la progression durable, la conformité et les projections.

Architecture cible :

```text
Client Web Vue 3
        |
        v
API Gateway
        |
        v
Game Engine Service  <---->  Catalog Service
        |                      |
        |                      +--> EnemyTemplate / ItemTemplate / SkillTemplate / NpcTemplate
        |
        +----> Player Service
        +----> Leaderboard Service
        +----> Audit/GDPR Service
        +----> Identity Service
        |
        +----> Event Store / RabbitMQ / Redis / PostgreSQL
```

---

## 4. Principes de migration

### 4.1 Ne pas migrer par table

Le backend v1 contient beaucoup de tables et endpoints. La v2 ne doit pas créer un service par table.

Mauvais découpage :

```text
EnemyService
ItemService
SkillService
NpcService
CombatStatsService
GameSaveService
...
```

Bon découpage :

```text
Catalog Service       → contenu de référence
Player Service        → état durable joueur
Game Engine Service   → runtime serveur-autoritaire
Identity Service      → comptes, JWT, MFA, rôles
Audit/GDPR Service    → audit, export, anonymisation, consentements
Leaderboard Service   → score, seed, saison, projections
```

### 4.2 Distinguer contenu, runtime et progression durable

Règle cible :

```text
Catalog décrit le contenu.
Player décrit le joueur durable.
Game Engine résout l'action runtime.
```

Exemple avec un ennemi :

```text
EnemyTemplate           → Catalog
EnemyCombatSnapshot     → Game Engine
EnemyRuntimeState       → Game Engine
BestiaryUnlock          → Player
```

### 4.3 Ne pas copier les entités EF v1 telles quelles

Les entités v1 ont été conçues pour :

```text
Unity + API REST + portail Vue + PostgreSQL
```

La v2 cible :

```text
full web + run serveur-autoritaire + Event Sourcing + Catalog versionné
```

Une migration directe produirait des incohérences. Les entités doivent être transformées en modèles adaptés à leur bounded context.

---

## 5. Mapping synthétique v1 vers v2

| Domaine legacy v1 | Éléments v1 | Service cible v2 | Décision |
|---|---|---|---|
| Authentification | `User`, `AuthController`, JWT, MFA, Argon2id | `Identity Service` | Migrer / adapter |
| Rôles et sécurité compte | `Role`, lockout, MFA secret | `Identity Service` | Migrer |
| Consentements | `UserConsent` | `Audit/GDPR Service` ou `Identity` | Découper selon responsabilité |
| RGPD | export, anonymisation | `Audit/GDPR Service` | Migrer |
| Audit | `AuditLog`, `AuditLogsController` | `Audit/GDPR Service` | Migrer |
| Ennemis | `Enemy`, `GetScaledEnemy` | `Catalog` + `Game Engine` | Découper |
| Objets | `Item`, `PlayerInventory` | `Catalog` + `Player` + `Game Engine` | Découper |
| Compétences | `Skill`, `PlayerSkill` | `Catalog` + `Player` + `Game Engine` | Découper |
| PNJ | `Npc`, `NpcInteraction` | `Catalog` + `Game Engine` + `Player` | Découper |
| Profil joueur | `PlayerProfile` | `Player Service` | Migrer / adapter |
| Inventaire joueur | `PlayerInventory` | `Player Service` | Migrer |
| Compétences joueur | `PlayerSkill` | `Player Service` | Migrer |
| Bestiaire | `BestiaryUnlock` | `Player Service` + `Catalog` | Découper |
| Stats combat | `CombatStats` | `Player Service` | Renommer en statistiques cumulées |
| Compagnon | `CompanionState`, `CompanionMarkovService` | `Player Service` puis module compagnon | Migrer progressivement |
| Sauvegarde | `GameSave` | Event Store / projections v2 | Ne pas migrer tel quel |
| Leaderboard | `LeaderboardController`, queries | `Leaderboard Service` | Migrer plus tard |
| Repositories EF | repositories v1 | par service cible | Réécrire selon bounded context |
| Tests v1 | tests domain/application/infrastructure | référence métier | Réutiliser comme filet de régression |

---

## 6. Analyse détaillée par domaine

### 6.1 Identity Service

Éléments v1 concernés :

```text
User
AuthController
LoginCommand / LoginHandler
RegisterCommand / RegisterHandler
SetupMfaCommand / VerifyMfaCommand
IPasswordHasher
IMfaService
ITokenService
IUserRepository
```

Responsabilités cible :

```text
- création de compte ;
- authentification ;
- MFA TOTP ;
- JWT / claims ;
- rôles ;
- lockout ;
- soft delete identité ;
- publication d'événements d'identité.
```

Décision : **migrer fortement**.

Raison : cette partie v1 est déjà solide et alignée avec les exigences sécurité. Elle doit être extraite proprement, pas réécrite sans justification.

Points d'attention :

```text
- l'email est stocké chiffré en byte[] ;
- MfaSecret est nullable et chiffré ;
- DeletedAt et DeletionReason relèvent de RGPD mais touchent aussi Identity ;
- les événements d'audit doivent sortir vers Audit/GDPR.
```

---

### 6.2 Audit/GDPR Service

Éléments v1 concernés :

```text
AuditLog
UserConsent
RGPDController
AuditLogsController
UserConsentsController
IEncryptionService
RGPD commands / handlers
```

Responsabilités cible :

```text
- export utilisateur ;
- anonymisation ;
- consentements ;
- audit trail ;
- traçabilité des actions sensibles ;
- conformité RGPD ;
- conservation / purge.
```

Décision : **extraire en service spécialisé**.

Ce service doit consommer des événements des autres services plutôt que dépendre de leurs bases directement.

Événements attendus :

```text
UserRegistered
LoginSucceeded
LoginFailed
MfaEnabled
GdprExportRequested
GdprAnonymizationRequested
RunCompleted
RewardSelected
AdminActionPerformed
```

---

### 6.3 Catalog Service

Éléments v1 concernés :

```text
Enemy
Item
Skill
Npc
NpcInteraction partiellement
DatabaseSeeder
GetScaledEnemy partiellement
```

Responsabilités cible :

```text
- référentiel ennemis ;
- référentiel objets ;
- référentiel compétences ;
- référentiel PNJ ;
- futures lois du Palais ;
- futurs EventTemplates ;
- futurs NarrativeFragmentTemplates ;
- versioning des contenus.
```

Décision : **créer rapidement un Catalog minimal**.

Modèles cibles :

```text
EnemyTemplate
EnemyBehaviorTemplate
ItemTemplate
SkillTemplate
NpcTemplate
NpcInteractionTemplate
PalaceLawDefinition
EventTemplate
NarrativeFragmentTemplate
RewardTemplate
```

Important : le Catalog ne résout pas les combats. Il décrit le contenu.

---

### 6.4 Game Engine Service

Éléments v2 déjà présents :

```text
Run
Room
Node
NodeEvent
RoomBossProfile
CurrentEvent resolver pipeline
Current event choice endpoint
Progression serveur-autoritaire
ActivePalaceLaws
```

Éléments v1 à utiliser comme inspiration :

```text
GetScaledEnemy
Enemy combat fields
Skill combat fields
Item effects
Npc / NpcInteraction
Companion Markov logic
```

Responsabilités cible :

```text
- état runtime d'une run ;
- progression dans les rooms ;
- résolution d'événements ;
- combat runtime ;
- reward runtime ;
- application des Lois actives ;
- écriture d'événements de run ;
- orchestration avec Catalog et Player.
```

Ne doivent pas être stockés durablement dans Game Engine :

```text
- catalogue complet des ennemis ;
- inventaire permanent ;
- profil joueur permanent ;
- auth ;
- données RGPD ;
- leaderboard final.
```

---

### 6.5 Player Service

Éléments v1 concernés :

```text
PlayerProfile
PlayerInventory
PlayerSkill
BestiaryUnlock
CombatStats
CompanionState
```

Responsabilités cible :

```text
- profil joueur durable ;
- progression permanente ;
- inventaire permanent ;
- compétences débloquées ;
- compagnons débloqués ;
- statistiques cumulées ;
- bestiaire débloqué ;
- snapshot joueur pour démarrage de run.
```

Décision : **migrer après Catalog minimal ou en parallèle du Combat MVP**.

Point majeur : `CombatStats` doit être renommé conceptuellement.

En v1 :

```text
CombatStats = statistiques cumulées joueur
```

En v2 :

```text
PlayerCombatStatistics = statistiques cumulées
CombatInstance = combat runtime
CombatantSnapshot = combattant en combat
```

---

### 6.6 Leaderboard Service

Éléments v1 concernés :

```text
LeaderboardController
GetLeaderboardQuery
```

Responsabilités cible :

```text
- classement ;
- seed ;
- score ;
- saison ;
- statut de run ;
- projection des événements de run ;
- affichage public contrôlé.
```

Décision : **migrer plus tard par projection événementielle**.

Le leaderboard v2 doit consommer :

```text
RunCompleted
RunFailed
RunAbandoned
ScoreComputed
SeasonClosed
```

---

## 7. Cas particulier : Enemy

L'entité v1 `Enemy` contient :

```text
Name
Type
MaxHP
Strength
Intelligence
Speed
PhysicalResistance
MagicalResistance
ExperienceReward
GoldReward
Description
InfluenceRadius
TransitionMatrix
CombatScripts
MapStates
InitialState
```

Ce modèle mélange :

```text
1. contenu de référence ;
2. récompenses ;
3. comportement / Markov / scripts ;
4. état initial runtime.
```

Découpage v2 recommandé :

```text
Catalog.EnemyTemplate
→ Name, Type, base stats, resistances, description, tags

Catalog.EnemyBehaviorTemplate
→ transition matrix, combat scripts, map states, initial state

GameEngine.EnemyCombatSnapshot
→ stats calculées pour une run et un combat donné

GameEngine.EnemyRuntimeState
→ état temporaire pendant le combat
```

---

## 8. Cas particulier : GetScaledEnemy

`GetScaledEnemyHandler` calcule un multiplicateur selon :

```text
enemy type
player level
equipment bonus
base power
max multiplier
courbe logistique
```

Cette logique est utile, mais ne doit plus être exposée comme simple endpoint isolé.

Découpage v2 recommandé :

```text
Catalog
→ fournit EnemyTemplate

Player
→ fournit PlayerSnapshot

Game Engine
→ calcule EnemyCombatSnapshot
```

Composants cibles possibles :

```text
IEnemyTemplateReader
IPlayerSnapshotReader
IEnemyScalingResolver
EnemyCombatSnapshotFactory
```

---

## 9. Cas particulier : GameSave

`GameSave` contient :

```text
CurrentZone
PositionX
PositionY
InventoryData JSON
QuestFlags JSON
SavedAt
```

Ce modèle correspond à Unity v1.

En v2, il ne doit pas être migré tel quel. La run doit être reconstruite depuis des événements :

```text
RunStarted
RunSeedGenerated
RoomGenerated
NodeSelected
PalaceLawApplied
EventResolved
CombatStarted
CombatActionResolved
CombatEnded
RewardOffered
RewardSelected
RunCompleted
RunFailed
RunAbandoned
```

Décision : **archiver comme référence legacy**.

Remplacement v2 :

```text
Event Store + projections RunState
```

---

## 10. Cas particulier : CompanionState et Markov

La v1 contient déjà :

```text
CompanionState
CompanionMarkovService
CompanionBackgroundService
```

Le service Markov manipule les états :

```text
REPOS
JEU
MANGER
EXCITE
TRISTE
ENDORMI
```

Il applique des boosts selon :

```text
victoires du jour
défaites du jour
temps depuis le dernier combat
```

C'est une excellente base conceptuelle pour Neige et les futurs compagnons.

Découpage v2 recommandé :

```text
Catalog.CompanionTemplate
→ définition de Neige et futurs compagnons

Player.CompanionState
→ état durable du compagnon

GameEngine.CompanionRunSnapshot
→ état temporaire pendant une run

Narrative.CompanionReactionResolver
→ réactions et fragments narratifs
```

---

## 11. Procédure d'extraction recommandée

### Étape 1 - Documentation

Créer et maintenir :

```text
docs/v2/migration/legacy-backend-service-mapping.md
```

### Étape 2 - Catalog minimal

Créer :

```text
services/catalog
```

Structure cible :

```text
services/catalog/
├── src/
│   ├── Leds.Catalog.Api
│   ├── Leds.Catalog.Application
│   ├── Leds.Catalog.Domain
│   └── Leds.Catalog.Infrastructure
└── tests/
    ├── Leds.Catalog.UnitTests
    └── Leds.Catalog.IntegrationTests
```

Premiers concepts :

```text
EnemyTemplate
SkillTemplate
ItemTemplate
NpcTemplate
PalaceLawDefinition
EventTemplate
```

### Étape 3 - Contrats Game Engine vers Catalog

Le Game Engine doit dépendre de contrats applicatifs, pas des entités EF Catalog.

Exemples :

```text
IEnemyTemplateReader
ISkillTemplateReader
IItemTemplateReader
IPalaceLawDefinitionReader
```

### Étape 4 - Combat MVP

Créer dans Game Engine :

```text
CombatInstance
CombatantSnapshot
CombatAction
TurnState
SkillUseResolver
DamageResolver
CombatEnded
RewardOffered
```

### Étape 5 - Player Service

Extraire progressivement :

```text
PlayerProfile
PlayerInventory
PlayerSkill
BestiaryUnlock
CompanionState
PlayerCombatStatistics
```

### Étape 6 - Identity / Audit / Leaderboard

Extraire ensuite :

```text
Identity Service
Audit/GDPR Service
Leaderboard Service
```

---

## 12. Ordre de migration recommandé

| Ordre | Jalon | Type | Pourquoi |
|---:|---|---|---|
| 1 | Mapping legacy → v2 | Documentation | Éviter les doublons |
| 2 | Catalog Service minimal | Architecture | Base nécessaire au combat |
| 3 | Contrats Game Engine ↔ Catalog | Intégration | Découplage propre |
| 4 | Combat MVP | Gameplay | Dépend des ennemis et skills |
| 5 | Reward runtime | Gameplay | Dépend des items et player state |
| 6 | Player Service | Domaine durable | Inventaire, skills, bestiaire |
| 7 | Narrative/Tome | Gameplay narratif | Dépend events et player history |
| 8 | Leaderboard Service | Projection | Dépend RunCompleted / score |
| 9 | Identity extraction | Sécurité | Peut rester legacy temporairement |
| 10 | Audit/GDPR extraction | Conformité | À isoler avant bêta publique |

---

## 13. Risques identifiés

| Risque | Impact | Mitigation |
|---|---|---|
| Copier les entités v1 telles quelles | Modèle v2 incohérent | Transformer en templates/snapshots |
| Créer trop de microservices | Complexité opérationnelle | Services par bounded context uniquement |
| Mettre le Catalog dans Game Engine | Couplage durable | Créer `services/catalog` minimal |
| Migrer `GameSave` tel quel | Incompatibilité Event Sourcing | Remplacer par Event Store |
| Confondre `CombatStats` et combat runtime | Mauvais modèle combat | Renommer en `PlayerCombatStatistics` |
| Implémenter combat avant Catalog | Refonte future probable | Catalog minimal avant Combat MVP |
| Perdre la logique Markov compagnon | Perte de valeur v1 | Documenter et migrer plus tard |
| Extraire Identity trop tôt | Frein sur gameplay | Garder legacy jusqu'au besoin réel |

---

## 14. Décision finale

La migration v2 commence maintenant, mais elle commence par le **mapping** et le **Catalog**, pas par une explosion brutale du monolithe.

Décision :

```text
backend v1 = legacy actif + source de référence métier
services v2 = cible officielle
migration = progressive, testée, documentée
Game Engine = runtime gameplay
Catalog = contenu versionné
Player = progression durable
Identity = sécurité compte
Audit/GDPR = conformité
Leaderboard = projection compétitive
```

---

## 15. Commit recommandé

```bash
git add docs/v2/migration/legacy-backend-service-mapping.md
git commit -m "docs(v2): map legacy backend domains to target services"
git push
```

---

## 16. Prochaine étape proposée

Après ce commit :

```bash
feat(catalog): initialize catalog service skeleton
```

Scope strict du premier jalon Catalog :

```text
- solution .NET propre ;
- Clean Architecture ;
- EnemyTemplate minimal ;
- SkillTemplate minimal ;
- ItemTemplate minimal ;
- endpoint health / lecture interne si nécessaire ;
- tests unitaires domaine ;
- aucune logique combat ;
- aucune administration complète.
```
