# L’épopée des silences — Suivi technique alpha0.0.2

## Jalon : Minimal Palace Law Domain

**Branche cible :** `v2/develop`  
**Service concerné :** `services/game-engine`  
**Commit recommandé :** `feat(game-engine): introduce minimal palace law domain`  
**Contexte :** refonte v2 — Game Engine Service serveur-autoritaire

---

## 1. Objectif de la session

Cette session a introduit le premier socle réel du système des **Lois du Palais**.

Jusqu’ici, les événements de type `Law` existaient dans la génération, dans la pipeline de résolution d’événements et dans le choix d’événement courant. Le joueur pouvait rencontrer une Loi et choisir `accept-law` ou `reject-law`, mais ce choix ne modifiait pas encore durablement l’état de la run.

L’objectif était donc de transformer le choix `accept-law` en vraie conséquence métier :

```text
Node Law
→ ResolveCurrentEvent
→ Outcome PalaceLawOffered
→ ChooseCurrentEventOption
→ LawEventChoiceResolver
→ IPalaceLawCatalog
→ Run.ActivatePalaceLaw(...)
→ Run.ActivePalaceLaws
```

---

## 2. Principe architectural retenu

Le principe appliqué reste :

```text
Implémenter petit,
mais dans l’architecture finale.
```

Les Lois du Palais ne doivent pas être codées comme de simples textes narratifs ou comme des effets ponctuels dans un handler. Elles sont un système central de la v2.

À terme, elles devront influencer :

- la génération des rooms et des nodes ;
- les événements proposés ;
- les combats ;
- les récompenses ;
- la narration ;
- Him’Lit et son adaptation finale.

Cette session pose donc une fondation volontairement minimale, mais compatible avec cette cible.

---

## 3. Domaine `PalaceLaws`

Un nouveau sous-domaine a été introduit dans :

```text
src/Leds.GameEngine.Domain/PalaceLaws
```

Éléments créés :

```text
PalaceLawId
PalaceLawDomain
PalaceLaw
ActivePalaceLaw
```

### 3.1 `PalaceLawId`

Identifiant métier fortement typé pour les Lois du Palais.

Objectifs :

- éviter les `Guid` nus dans le domaine ;
- rester cohérent avec `RunId`, `NodeId`, etc. ;
- préparer la persistance, les projections et l’Event Sourcing.

### 3.2 `PalaceLawDomain`

Enumération des domaines impactables par une Loi :

```text
Generation
Events
Combat
Rewards
Narrative
HimLit
```

Cette enum prépare le futur Palace Law Engine.

### 3.3 `PalaceLaw`

Représente une définition de Loi :

```text
Key
Name
Version
Domains
```

Règles métier introduites :

- une Loi doit avoir une clé ;
- une Loi doit avoir un nom ;
- une Loi doit avoir une version ;
- une Loi doit cibler au moins un domaine ;
- les domaines dupliqués sont supprimés.

### 3.4 `ActivePalaceLaw`

Représente une Loi activée sur une run.

Elle contient un snapshot de la Loi au moment de son activation :

```text
LawId
Key
Name
Version
Domains
```

Ce choix est volontaire : une run doit rester reconstructible avec les règles actives lors de son exécution. Si une définition de Loi évolue plus tard dans un catalogue, les anciennes runs ne doivent pas changer implicitement.

---

## 4. Évolution de `Run`

La classe `Run` porte désormais une collection :

```text
ActivePalaceLaws
```

Une méthode métier a été ajoutée :

```text
ActivatePalaceLaw(PalaceLaw law)
```

Règles :

- une Loi peut être activée sur une run ouverte ;
- une Loi ne peut pas être activée sur une run fermée (`Completed`, `Failed`, `Abandoned`) ;
- une même Loi n’est pas dupliquée ;
- l’activation est idempotente.

L’idempotence évite qu’un même effet soit appliqué plusieurs fois en cas de double appel ou de comportement inattendu côté client.

---

## 5. Exposition dans les DTOs

Un DTO a été ajouté :

```text
ActivePalaceLawDto
```

`RunDto` expose désormais :

```text
ActivePalaceLaws
```

Cela permettra au futur frontend Vue 3 d’afficher les Lois actives de la run, par exemple dans le panneau latéral de l’écran principal.

---

## 6. Port applicatif `IPalaceLawCatalog`

Un port applicatif a été créé dans :

```text
src/Leds.GameEngine.Application/PalaceLaws
```

Il expose :

```text
GetDefaultLawFor(CurrentEventChoiceResolutionContext context)
```

### Pourquoi dans Application et non Domain ?

Le domaine ne doit pas dépendre de l’application.

Dépendance correcte :

```text
Application → Domain
```

Dépendance interdite :

```text
Domain → Application
```

Pendant l’implémentation, une erreur de build a confirmé cette règle : `IPalaceLawCatalog` et `StaticPalaceLawCatalog` avaient été placés initialement dans `Domain`, alors qu’ils dépendaient de types applicatifs. Ils ont été déplacés dans `Application`.

---

## 7. `StaticPalaceLawCatalog`

Une implémentation temporaire a été introduite :

```text
StaticPalaceLawCatalog
```

Elle retourne actuellement une Loi fixe :

```text
key: law-silence-v1
name: Loi du Silence
version: 1.0.0
domains:
  - Narrative
  - Generation
```

Cette classe est temporaire et prépare le futur `Catalog Service`.

À terme, les Lois ne devront pas être codées en dur dans le Game Engine. Elles devront venir d’un catalogue versionné.

---

## 8. Évolution de `LawEventChoiceResolver`

Le resolver de choix de Loi a été enrichi.

Avant :

```text
accept-law
→ message narratif uniquement
```

Maintenant :

```text
accept-law
→ récupération d’une Loi via IPalaceLawCatalog
→ Run.ActivatePalaceLaw(law)
→ message narratif
```

`reject-law` reste un choix narratif sans activation de Loi.

Comportement actuel :

```text
accept-law
→ active "Loi du Silence"

reject-law
→ n’active aucune Loi

unknown-choice
→ DomainException
```

---

## 9. Tests ajoutés ou adaptés

### Domaine `PalaceLaw`

Couverture :

- création valide ;
- clé obligatoire ;
- au moins un domaine requis ;
- suppression des domaines dupliqués.

### `Run.ActivatePalaceLaw`

Couverture :

- activation d’une Loi sur une run active ;
- non-duplication d’une Loi déjà active ;
- interdiction d’activer une Loi sur une run fermée.

### `LawEventChoiceResolver`

Couverture :

- `accept-law` active réellement une Loi ;
- `reject-law` n’active aucune Loi ;
- choix inconnu → `DomainException`.

---

## 10. Problèmes rencontrés et corrections

### 10.1 Mauvais projet pour `IPalaceLawCatalog`

Erreur :

```text
Le nom de type ou d'espace de noms 'Events' n'existe pas dans l'espace de noms 'Leds.GameEngine.Application'
```

Cause :

```text
IPalaceLawCatalog.cs
StaticPalaceLawCatalog.cs
```

avaient été placés dans :

```text
src/Leds.GameEngine.Domain/PalaceLaws
```

Correction :

```text
src/Leds.GameEngine.Application/PalaceLaws/IPalaceLawCatalog.cs
src/Leds.GameEngine.Application/PalaceLaws/StaticPalaceLawCatalog.cs
```

### 10.2 Namespace de `ChosenEventOptionResultDto`

Le fichier était dans :

```text
Events/Dtos
```

mais déclarait un namespace incohérent.

Correction :

```text
namespace Leds.GameEngine.Application.Events.Dtos;
```

### 10.3 Référence à `StaticPalaceLawCatalog` dans les tests

Après déplacement vers `Application/PalaceLaws`, les tests doivent importer :

```csharp
using Leds.GameEngine.Application.PalaceLaws;
```

---

## 11. Importance du jalon

Cette étape transforme les Lois du Palais en état réel de run.

Avant :

```text
Law = événement + choix + texte
```

Maintenant :

```text
Law = événement + choix + conséquence + état de run
```

Le système est encore minimal, mais il est aligné avec l’architecture finale :

```text
Event choice
→ Resolver applicatif
→ Catalog abstraction
→ Domain mutation
→ Run state
→ DTO frontend
→ future projection / Event Sourcing
```

---

## 12. Vision future

### 12.1 Mapping backend legacy → services v2

Prochaine étape recommandée :

```text
docs/v2/migration/legacy-backend-service-mapping.md
```

Objectif :

- inventorier les domaines existants du backend v1 ;
- identifier leur service cible v2 ;
- éviter de réimplémenter des concepts déjà existants ;
- préparer l’éclatement progressif du monolithe.

Mapping attendu :

```text
backend v1          service cible v2
------------------------------------
Enemies            Catalog
Skills             Catalog
Items              Catalog
CombatStats        Game Engine / Player à arbitrer
PlayerProfile      Player
PlayerInventory    Player
Bestiary           Catalog + Player
Auth               Identity
GDPR               Audit-GDPR
Leaderboard        Leaderboard
AuditLogs          Audit-GDPR
```

### 12.2 Catalog minimal

Après le mapping, il faudra introduire un socle Catalog minimal.

Concepts futurs :

```text
EnemyTemplate
SkillTemplate
ItemTemplate
RewardTemplate
EventTemplate
PalaceLawDefinition
NarrativeFragmentTemplate
```

### 12.3 Combat MVP

Le combat ne doit pas être codé avant d’avoir clarifié :

- où vivent les ennemis ;
- où vivent les compétences ;
- où vivent les objets ;
- ce qui appartient au runtime `Game Engine` ;
- ce qui appartient au référentiel `Catalog` ;
- ce qui appartient à la progression durable `Player`.

Règle cible :

```text
Catalog décrit le contenu.
Player décrit le joueur durable.
Game Engine résout l’action runtime.
```

---

## 13. Recommandation de commit

Lorsque les tests sont au vert :

```bash
git add .
git commit -m "feat(game-engine): introduce minimal palace law domain"
git push
```

---

## 14. Roadmap après ce jalon

### Socle terminé ou stabilisé

```text
Run lifecycle
Room generation
Node path progression
Room boss convergence
Move to next room
Abandon run
Node event resolver pipeline
Current event choice endpoint
Progression bloquée par choix obligatoire
Minimal Palace Law Domain
```

### À venir

```text
Legacy backend → services v2 mapping
Catalog minimal
Palace Law Engine v1
Narrative fragment resolver
Tome writer
Combat MVP
Reward selection
Player progression
Leaderboard projections
Event Sourcing runtime
```

---

## 15. Synthèse

Ce jalon introduit le premier état durable des Lois du Palais dans la run.

La v2 commence à dépasser le simple moteur procédural de progression : elle porte désormais un premier élément central de l’identité du jeu comme état métier.

Les futures évolutions pourront s’appuyer sur ce socle pour influencer la génération, les événements, la narration, les récompenses, le combat et Him’Lit.
