# SFD — Feature Interlude / Repli du Palais

Projet : **L’épopée des silences**  
Feature : **Interlude entre Rooms / Repli du Palais**  
Version de cadrage : `interlude-feature-sfd-0.1`  
Cible roadmap : préparation `0.2.x`  
Statut : spécification fonctionnelle et technique initiale

---

## 1. Objectif de la feature

La feature **Interlude / Repli du Palais** introduit une phase de transition entre deux Rooms dans le mode infini.

Après avoir vaincu le boss d’une Room et sélectionné la récompense associée, le joueur ne doit pas être envoyé automatiquement dans la Room suivante. Il doit revenir à un état de transition contrôlé, permettant :

- de consulter l’état de la run ;
- de respirer après la Room terminée ;
- de gérer des éléments de run ;
- de parler à Elise ;
- de consulter le sac à dos ;
- de consulter le journal de run ;
- de décider de continuer, sauvegarder ou abandonner.

Cette feature sert de pivot entre :

```text
Room active
→ RoomBoss vaincu
→ Reward boss
→ RoomCleared
→ Interlude / Repli du Palais
→ Next Room
```

Elle prépare la future boucle infinie multi-room et l’arrivée cyclique d’Him’Lit.

---

## 2. Contexte produit

La première release jouable du projet est orientée **mode infini**, et non campagne histoire complète.

Le joueur enchaîne des Rooms successives, accumule des récompenses, des Lois du Palais, des altérations, des fragments et une empreinte de run.

Him’Lit apparaîtra plus tard toutes les X Rooms, avec des caractéristiques influencées par :

- les Rooms traversées ;
- les types de Rooms dominants ;
- les combats normaux / rares / élites ;
- les Lois du Palais ;
- les malédictions ;
- les choix du joueur ;
- les rewards sélectionnées ;
- le niveau de risque accumulé.

L’Interlude sert donc de moment de respiration et de gestion entre deux phases de danger.

---

## 3. Problème à résoudre

Le flow direct suivant n’est pas souhaité :

```text
RoomBoss vaincu
→ reward boss sélectionnée
→ génération immédiate de la Room suivante
```

Ce flow pose plusieurs problèmes :

- enchaînement trop brutal ;
- absence de respiration ;
- impossibilité de sauvegarder proprement entre deux Rooms ;
- impossibilité de revenir au menu sans perdre toute la run ;
- pas de lieu naturel pour consulter le journal ou l’inventaire ;
- pas de support futur pour le campement / menuing ;
- difficulté à introduire Him’Lit comme menace cyclique.

La feature Interlude introduit un point stable entre deux Rooms.

---

## 4. Décision fonctionnelle

Après chaque Room terminée, le joueur passe par une phase de décision.

Flow cible :

```text
RoomBoss vaincu
→ Reward boss générée
→ Reward boss sélectionnée
→ Run passe en état RoomCleared
→ Page de transition de Room
→ Choix joueur :
   1. Continuer la run
   2. Sauvegarder et retourner au menu
   3. Abandonner définitivement la run
→ Si Continuer :
   → Interlude / Repli du Palais
   → Actions de consultation / gestion
   → Entrer dans la Room suivante
```

La Room suivante ne doit jamais être générée automatiquement juste après la sélection de la reward boss.

---

## 5. Vocabulaire métier

### 5.1 Room

Une Room normale est une carte de progression contenant des `MapNode`.

Elle est dangereuse, structurée et orientée choix/progression.

Exemple :

```text
Room
└── MapNode[]
```

Une Room normale contient :

- des combats ;
- des nodes de repos ;
- des marchands ;
- des Lois ;
- des PNJ ;
- des malédictions ;
- un boss final de Room.

### 5.2 RoomCleared

`RoomCleared` représente l’état où la Room vient d’être terminée.

La Room est résolue, mais la run n’a pas encore avancé vers la suite.

C’est un point de décision.

### 5.3 Interlude / Repli du Palais

L’Interlude est une phase non hostile, non combat, centrée sur la gestion et la consultation.

Elle ne doit pas être confondue avec une Room normale.

### 5.4 InterludeNode

Un `InterludeNode` est un point interactif autour du joueur dans l’Interlude.

Il ne s’agit pas d’un `MapNode`.

Il ne porte pas de `riskLevel`, de `rewardProfile` ou de parenté de RoomMap.

---

## 6. Séparation des concepts

La séparation suivante est obligatoire :

```text
RoomMap / MapNode
= progression dangereuse dans une Room

Interlude / InterludeNode
= gestion, consultation, respiration entre deux Rooms
```

À éviter :

```text
Interlude = Room normale avec un seul MapNode
```

Raison :

- cela mélangerait progression de Room et phase de gestion ;
- cela forcerait des concepts inutiles comme riskLevel / rewardProfile ;
- cela recréerait de la confusion entre MapNode et événement de transition ;
- cela compliquerait les futures actions de campement.

---

## 7. Structure fonctionnelle de l’Interlude

### 7.1 Vue générale

L’Interlude est une interface visuelle centrée sur le joueur.

Structure cible :

```text
              Journal

      Elise     Joueur     Sac à dos

        Slot futur     Slot futur
```

Le joueur est représenté au centre.

Autour de lui apparaissent plusieurs nodes d’action.

---

## 8. Nodes de l’Interlude

### 8.1 Node central — Joueur

Rôle :

- ancre visuelle ;
- représentation de l’état actuel du joueur ;
- accès futur à la fiche personnage ;
- accès futur aux statistiques de run ;
- affichage possible de la stabilité / santé / état mental.

Statut initial :

- peut être non cliquable en MVP ;
- ou ouvrir une fiche joueur simple si déjà disponible.

### 8.2 Node Elise

Rôle :

- dialogue ;
- conseils ;
- commentaires sur la Room terminée ;
- commentaires sur la prochaine Room ;
- lecture narrative légère ;
- futures actions non définies.

Statut MVP :

- afficher un dialogue court ;
- ou un placeholder propre indiquant que les actions d’Elise seront enrichies plus tard.

Actions futures possibles :

- conseil stratégique ;
- interprétation des Lois actives ;
- commentaire sur Him’Lit ;
- rappel narratif ;
- interaction avec le Tome.

### 8.3 Node Sac à dos

Rôle :

- consulter les objets obtenus ;
- voir les récompenses de la run ;
- inspecter les fragments ;
- préparer la future gestion d’inventaire.

Statut MVP :

- lecture seule ;
- afficher les rewards obtenues ou un placeholder propre si l’inventaire n’est pas encore implémenté.

Actions futures possibles :

- équiper un objet ;
- déséquiper un objet ;
- comparer les stats ;
- recycler une récompense ;
- améliorer un objet ;
- verrouiller une récompense.

### 8.4 Node Journal

Rôle :

- consulter les détails de la run entière ;
- afficher l’historique de progression ;
- préparer l’affichage de l’empreinte de run.

Contenu cible :

- seed ;
- nombre de Rooms traversées ;
- RoomTypes rencontrés ;
- boss vaincus ;
- combats normaux gagnés ;
- combats rares gagnés ;
- élites gagnés ;
- Lois du Palais actives ;
- malédictions ;
- rewards majeures ;
- prochaine apparition d’Him’Lit plus tard ;
- score provisoire ;
- état global de la run.

Statut MVP :

- afficher les données déjà disponibles ;
- les données non disponibles peuvent être indiquées comme futures.

### 8.5 Slots futurs

L’Interlude doit prévoir au moins deux emplacements extensibles.

Types futurs possibles :

- Tome ;
- Forge ;
- Repos ;
- Compagnon ;
- Autel des Lois ;
- Marchand rare ;
- Mémoire ;
- Préparation Him’Lit ;
- Scellement ;
- Sacrifice.

Statut MVP :

- afficher des placeholders propres ;
- ne pas implémenter de logique métier lourde.

---

## 9. Actions principales de transition

Les décisions majeures ne doivent pas être cachées dans les nodes autour du joueur.

Elles doivent être affichées comme actions principales de la page.

### 9.1 Continuer la run

Action :

```text
Continuer la descente
```

Effet attendu :

- le joueur confirme qu’il souhaite poursuivre ;
- il entre dans l’Interlude si ce n’est pas déjà le cas ;
- depuis l’Interlude, il peut ensuite entrer dans la prochaine Room ;
- la prochaine Room est générée uniquement après confirmation.

### 9.2 Sauvegarder et retourner au menu

Action :

```text
Sauvegarder et retourner au menu
```

Effet attendu :

- la run est conservée dans un état stable ;
- le joueur retourne au menu principal ;
- la run pourra être reprise plus tard au dernier point sûr.

Cette action doit être disponible uniquement sur un état stable :

- RoomCleared ;
- Interlude ;
- avant entrée dans la prochaine Room.

Elle ne doit pas être disponible au milieu :

- d’un combat ;
- d’une reward non sélectionnée ;
- d’un node en résolution.

### 9.3 Abandonner la run

Action :

```text
Abandonner la run
```

Effet attendu :

- confirmation obligatoire ;
- la run est marquée comme abandonnée ;
- la run ne peut plus être reprise ;
- un bilan final peut être affiché plus tard ;
- retour au menu principal.

Statut recommandé :

```text
RunStatus = Abandoned
```

Il est recommandé de ne pas confondre `Abandoned` avec `Completed`.

---

## 10. Quitter une run à tout moment

En dehors de l’Interlude, le joueur doit pouvoir quitter la run via un bouton permanent dans l’interface.

### 10.1 Règle

Le joueur peut quitter n’importe quand, mais il perd la progression de la Room en cours.

Il ne perd pas toute la run.

Principe :

```text
Quitter pendant une Room active
→ progression de la Room actuelle perdue
→ retour au dernier point sûr
→ retour menu principal
```

### 10.2 Confirmation UI

Texte recommandé :

```text
Quitter la run ?
La progression de la Room actuelle sera perdue.
Votre run sera conservée au dernier palier sûr.
```

Actions :

```text
Confirmer le retour au menu
Annuler
```

### 10.3 Différence avec abandon

Quitter la run :

```text
La run reste reprenable.
La Room en cours est perdue.
```

Abandonner la run :

```text
La run est terminée définitivement.
Elle ne peut plus être reprise.
```

---

## 11. Notion de point sûr / checkpoint

La feature nécessite à terme une notion de point sûr.

Un point sûr est un état à partir duquel la run peut être reprise.

Points sûrs possibles :

- début de run avant Room 0 ;
- RoomCleared ;
- Interlude ;
- avant entrée dans une nouvelle Room.

Points non sûrs :

- combat actif ;
- reward pendante ;
- node sélectionné ;
- Room active avec progression partielle ;
- boss en cours ;
- action de résolution en cours.

Règle centrale :

```text
La progression d’une Room active est disposable.
La progression de run au dernier point sûr est persistante.
```

---

## 12. États recommandés

### 12.1 RunStatus

Statuts possibles à terme :

```text
Active
RoomCleared
Interlude
Abandoned
Completed
Failed
```

`Saved` n’est pas nécessairement un statut métier. La sauvegarde peut être représentée par un checkpoint technique.

### 12.2 RoomState

Statuts possibles :

```text
Active
NodeSelected
NodeResolved
CombatActive
RewardPending
RoomResolved
Cleared
Discarded
```

Pour le MVP, ne pas multiplier les statuts inutilement si le modèle actuel suffit.

La distinction minimale nécessaire pour `0.2.0` est :

```text
Room active
Room terminée
Interlude
```

---

## 13. Flow cible détaillé

### 13.1 Fin de Room normale

```text
1. Joueur atteint RoomBoss.
2. Joueur choisit RoomBoss.
3. ResolveCurrentEvent démarre le combat boss.
4. Combat boss terminé.
5. Reward boss générée.
6. Joueur sélectionne reward boss.
7. Run passe en RoomCleared.
8. Front affiche page de transition.
9. Joueur choisit :
   - Continuer ;
   - Sauvegarder et retourner menu ;
   - Abandonner.
```

### 13.2 Continuer

```text
RoomCleared
→ Continuer
→ Interlude / Repli du Palais
→ consultation Elise / sac / journal
→ Entrer dans la prochaine Room
→ génération Room suivante
→ Room active
```

### 13.3 Sauvegarder et retourner menu

```text
RoomCleared ou Interlude
→ Sauvegarder et retourner menu
→ checkpoint stable conservé
→ retour menu principal
```

### 13.4 Abandonner

```text
RoomCleared ou Interlude
→ Abandonner
→ confirmation
→ RunStatus Abandoned
→ retour menu principal
```

### 13.5 Quitter pendant Room active

```text
Room active
→ bouton Quitter
→ confirmation
→ Room en cours perdue
→ rollback au dernier checkpoint
→ retour menu principal
```

---

## 14. Contrats API envisagés

Les endpoints exacts sont à définir lors de la PR d’implémentation.

Proposition :

### 14.1 Récupérer l’état d’interlude

```http
GET /api/v2/runs/{runId}/interlude
```

Retourne :

- runId ;
- currentRoomIndex ;
- lastClearedRoom ;
- availableInterludeNodes ;
- run summary ;
- actions principales disponibles.

### 14.2 Continuer vers l’interlude

```http
POST /api/v2/runs/{runId}/interlude/enter
```

Transition :

```text
RoomCleared → Interlude
```

### 14.3 Entrer dans la prochaine Room

```http
POST /api/v2/runs/{runId}/rooms/next
```

Transition :

```text
Interlude → nouvelle Room active
```

### 14.4 Sauvegarder et retourner menu

```http
POST /api/v2/runs/{runId}/save-and-exit
```

### 14.5 Abandonner la run

```http
POST /api/v2/runs/{runId}/abandon
```

### 14.6 Quitter la Room active

```http
POST /api/v2/runs/{runId}/quit-room
```

Nom alternatif :

```http
POST /api/v2/runs/{runId}/return-to-checkpoint
```

À décider selon le modèle final.

---

## 15. DTOs envisagés

### 15.1 InterludeDto

```text
InterludeDto
- RunId
- CurrentRoomIndex
- DisplayRoomNumber
- LastClearedRoom
- Nodes
- AvailableActions
- RunSummary
```

### 15.2 InterludeNodeDto

```text
InterludeNodeDto
- Id
- Type
- Label
- Description
- Position
- IsEnabled
- ActionKey
```

Types possibles :

```text
Player
Elise
Inventory
Journal
Placeholder
```

### 15.3 InterludeActionDto

```text
InterludeActionDto
- Key
- Label
- Description
- RequiresConfirmation
- IsDangerous
- IsEnabled
```

Actions possibles :

```text
ContinueRun
SaveAndExit
AbandonRun
EnterNextRoom
```

### 15.4 RunSummaryDto

```text
RunSummaryDto
- Seed
- CurrentRoomIndex
- CompletedRoomCount
- ClearedRoomTypes
- BossesDefeated
- ActivePalaceLaws
- Curses
- MajorRewards
- CombatCount
- RareCombatCount
- EliteCombatCount
- CurrentScore
```

Tous ces champs ne sont pas obligatoires en MVP.

---

## 16. UI cible

### 16.1 Page RoomCleared

Objectif :

- présenter la Room terminée ;
- afficher le boss vaincu ;
- afficher la récompense obtenue ;
- proposer les trois décisions.

Contenu :

```text
Room terminée
Boss vaincu
Récompense obtenue
Résumé court
```

Actions :

```text
Continuer
Sauvegarder et retourner au menu
Abandonner la run
```

### 16.2 Page Interlude / Repli du Palais

Objectif :

- représenter le joueur au centre ;
- proposer des nodes de consultation ;
- permettre l’entrée dans la prochaine Room.

Structure :

```text
              Journal

      Elise     Joueur     Sac à dos

        Slot futur     Slot futur
```

Actions principales :

```text
Entrer dans la prochaine Room
Sauvegarder et retourner menu
Abandonner la run
```

### 16.3 Bouton global Quitter

Présent pendant une Room active.

Ne doit pas signifier abandonner.

Il signifie :

```text
Retourner au menu en perdant uniquement la Room en cours.
```

---

## 17. Critères d’acceptation MVP

La feature sera considérée comme valide en MVP si :

- la victoire contre un RoomBoss ne déclenche pas automatiquement la Room suivante ;
- après reward boss, la run passe dans un état de transition ;
- le joueur voit une page de transition ;
- le joueur peut choisir de continuer ;
- le joueur peut sauvegarder et retourner au menu ;
- le joueur peut abandonner définitivement ;
- l’Interlude affiche au minimum :
  - joueur central ;
  - node Elise ;
  - node Sac à dos ;
  - node Journal ;
  - deux slots futurs ;
- les nodes d’interlude sont séparés des MapNodes ;
- aucune logique de risk/reward n’est appliquée aux InterludeNodes ;
- la prochaine Room n’est générée qu’après confirmation explicite ;
- quitter pendant une Room active ne supprime pas toute la run ;
- abandonner rend la run définitivement terminée.

---

## 18. Non-objectifs MVP

Ne pas implémenter immédiatement :

- vrai inventaire complet ;
- équipement / déséquipement ;
- forge ;
- vrai dialogue complexe avec Elise ;
- sauvegarde persistante finale en base si la persistance n’existe pas encore ;
- Him’Lit ;
- RunImprint complet ;
- campement complet ;
- économie ;
- amélioration d’items ;
- actions définitives des placeholders.

---

## 19. Roadmap recommandée

### 0.2.0 — Multi-room progression with RoomCleared transition

- RoomBoss ne génère plus automatiquement la Room suivante ;
- RoomCleared introduit ;
- choix Continuer / Sauvegarder-menu / Abandonner ;
- génération prochaine Room uniquement après confirmation.

### 0.2.1 — Interlude / Repli du Palais MVP

- page Interlude ;
- joueur central ;
- Elise ;
- Sac à dos ;
- Journal ;
- slots futurs ;
- bouton entrer prochaine Room.

### 0.2.2 — Checkpoint / quitter Room active

- bouton quitter n’importe quand ;
- perte de progression de Room en cours ;
- retour au dernier checkpoint ;
- sauvegarde/menu plus fiable.

### 0.2.3 — Infinite HUD / Run summary

- affichage complet de la descente ;
- Rooms traversées ;
- score ;
- dangers ;
- préfiguration Him’Lit.

### 0.2.4+ — Him’Lit cycle / RunImprint

- apparition cyclique ;
- adaptation selon run ;
- boss de cycle.

---

## 20. Décision finale

La feature Interlude / Repli du Palais devient le point de respiration structurel du mode infini.

Elle doit garantir que :

```text
Room terminée ≠ Room suivante automatique
```

La transition entre Rooms devient un choix joueur.

Le joueur doit toujours pouvoir :

```text
Continuer
Sauvegarder et retourner menu
Abandonner définitivement
```

Et, pendant une Room active :

```text
Quitter vers le menu en perdant uniquement la progression de la Room actuelle.
```

Cette feature prépare la run infinie, la sauvegarde, le menuing léger, le journal de run et l’arrivée future d’Him’Lit.
