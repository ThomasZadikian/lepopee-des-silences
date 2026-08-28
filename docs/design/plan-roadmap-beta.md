# Roadmap de maintenant jusqu’à la bêta

Référence : LEDS-PLAN-BETA-001  
Version : 1.0  
Statut : Plan directeur  

## 1. Finalité de la roadmap

La roadmap ordonne le développement depuis l’état actuel de la branche T-RPG jusqu’à une bêta jouable et déployable. L’ordre imposé est : fiabiliser CI/CD, créer les comptes utilisateurs liés au domaine Player, terminer une tranche verticale jouable, enrichir le contenu puis durcir la plateforme avant ouverture.

Une bêta n’est pas définie par la présence de tous les contenus SFD. Elle est atteinte lorsqu’un utilisateur externe peut créer son compte, entrer dans le jeu, progresser dans un parcours cohérent, combattre, sauvegarder, reprendre et atteindre une conclusion temporaire sans assistance technique.

### 1.1 Hypothèses de planification

- Estimation exprimée pour un développeur principal, hors disponibilité d’un auteur de contenu ou d’un artiste.
- Les durées sont des fourchettes de travail et doivent être recalibrées après l’inventaire réel des tests rouges.
- La cible bêta utilise des conteneurs Docker et trois bases PostgreSQL distinctes.
- GitHub Actions et GHCR constituent la plateforme de référence.
- L’identité est séparée du profil joueur ; la décision détaillée sera formalisée par SFD, STD et ADR avant développement.
- Les Talents, la totalité des vingt-sept salles et la normalisation exhaustive des 138 compétences ne bloquent pas la première bêta.

## 2. État de départ

| Domaine | Acquis | Dette bloquante ou structurante |
|---|---|---|
| Exploration | Carte, déplacements, PNJ et ennemis mobiles, combat au contact | Connexions de certains événements et contenus incomplets |
| Combat | Système tactique, fuite, ressources persistées, équipement | Renforts génériques et équilibrage incomplets |
| Run | Création, reprise, abandon, persistance révisée | Validation de résilience et récupération à industrialiser |
| Story | Modèles de progression et overlay | Séquences réellement exécutables et événements Hall absents |
| Compte | Profil Player technique et identifiant de démonstration | Aucun compte, authentification ni autorisation |
| Tests | Suites xUnit, Vitest et Playwright présentes | Nombreux échecs, couverture non pilotée |
| Livraison | Compose local pour PostgreSQL | Pas de workflows, Dockerfiles applicatifs ni environnement bêta |

## 3. Vue d’ensemble

[[FIGURE:roadmap-beta.png|Roadmap dépendancielle vers la bêta jouable]]

| Jalon | Résultat | Effort indicatif | Dépendance |
|---|---|---:|---|
| M0 — Baseline qualité | Inventaire fiable des tests et risques | 3 à 5 JH | Aucune |
| M1 — CI verte | Tous les tests requis passent, seuils pilotés | 25 à 50 JH | M0 |
| M2 — CD opérationnel | Artefacts OCI et déploiements reproductibles | 8 à 15 JH | M1 |
| M3 — Comptes utilisateurs | Inscription, connexion et lien Player sécurisés | 20 à 35 JH | M2 |
| M4 — Tranche verticale | Parcours Hall → combat → récompense → reprise | 20 à 35 JH | M3 |
| M5 — Contenu bêta | Parcours suffisamment varié et compréhensible | 20 à 40 JH | M4 |
| M6 — Durcissement | Bêta observable, sauvegardée et réversible | 10 à 20 JH | M5 |

La fourchette globale est de 106 à 200 JH. L’incertitude principale porte sur la remise en état des tests et la production de contenu. M0 doit réduire cette incertitude avant d’engager une date publique.

## 4. Phase M0 — Baseline qualité et gouvernance

### 4.1 Objectif

Obtenir une vision exacte du build, des suites, de leurs échecs et de la couverture actuelle. Cette phase n’améliore pas artificiellement les indicateurs : elle rend la dette mesurable.

### 4.2 Travaux

1. Exécuter séparément Shared, Catalog, Player, Game Engine, Vitest et Playwright.
2. Produire un inventaire des tests par projet, catégorie, durée et statut.
3. Classer chaque échec : défaut produit, test obsolète, dépendance, environnement, flakiness ou donnée incorrecte.
4. Identifier les tests ignorés, dupliqués, sans assertion utile ou dépendants de l’ordre.
5. Mesurer les couvertures initiales sans exclusion nouvelle.
6. Relier les parcours critiques aux tests existants et identifier les trous.
7. Établir le registre de dette et l’ordre de correction par service.

### 4.3 Livrables

- Rapport de baseline des tests et couvertures.
- Registre des échecs avec cause, propriétaire et décision.
- Matrice exigences critiques ↔ tests.
- Commandes locales reproductibles identiques aux runners GitHub.

### 4.4 Gate de sortie

Chaque test existant possède un statut expliqué ; chaque service produit un rapport de couverture exploitable ; aucune catégorie d’échec n’est laissée sous l’étiquette générique « test cassé ».

## 5. Phase M1 — CI verte et couverture à 80 %

### 5.1 Objectif

Mettre en place la CI décrite dans LEDS-STD-CICD-001, réparer les suites et faire de 80 % un seuil réellement bloquant par unité logicielle.

### 5.2 Lot M1-A — Infrastructure CI

- Épingler le SDK .NET 10 et les dépendances.
- Harmoniser `Microsoft.NET.Test.Sdk`, xUnit et Coverlet.
- Configurer Vitest avec provider V8 et rapports LCOV/Cobertura.
- Créer les workflows réutilisables backend et frontend.
- Publier TRX/JUnit, traces Playwright et rapports de couverture.
- Ajouter CodeQL, revue de dépendances et Dependabot.

### 5.3 Lot M1-B — Retour à zéro échec

Ordre recommandé : Shared Building Blocks, Catalog, Player, Game Engine, Game Client, puis Playwright. Cet ordre suit les dépendances et évite de corriger plusieurs fois les mêmes contrats.

Pour chaque test rouge : vérifier d’abord l’exigence propriétaire, corriger le produit si le comportement attendu reste valide, sinon corriger ou remplacer le test. La suppression sans analyse est interdite.

### 5.4 Lot M1-C — Couverture utile

- Couvrir en priorité invariants, erreurs, concurrence et persistance.
- Fusionner unitaires et intégration avant calcul du seuil backend.
- Ajouter des tests de composant pour les interactions Vue complexes.
- Ajouter des E2E pour les parcours critiques sans utiliser l’E2E pour gonfler artificiellement la couverture unitaire.
- Relever les seuils par paliers contrôlés jusqu’à 80 % sur toutes les métriques définies.
- Introduire le mutation testing nocturne sur les domaines critiques.

### 5.5 Gate de sortie M1

- Zéro test requis en échec sur trois exécutions consécutives.
- Game Engine, Player, Catalog, Shared et Game Client atteignent chacun 80 %.
- Les protections de `develop` bloquent toute régression.
- La suite complète peut être exécutée localement avec les mêmes résultats.
- Aucun CD n’a été activé tant que ces conditions ne sont pas réunies.

## 6. Phase M2 — Conteneurisation et CD

### 6.1 Objectif

Construire une release une seule fois, la publier dans GHCR et promouvoir les mêmes digests jusqu’à l’environnement bêta.

### 6.2 Travaux

1. Créer les Dockerfiles multi-stage des trois API et du client.
2. Étendre Docker Compose aux applications, reverse proxy, healthchecks et réseaux privés.
3. Produire les bundles de migration EF de Game Engine, Player et Catalog.
4. Construire, scanner et attester les images avec SBOM et provenance.
5. Créer les environnements GitHub `integration`, `preproduction` et `beta`.
6. Déployer automatiquement `develop` vers l’intégration.
7. Ajouter approbation, sauvegarde, migrations, smoke tests et rollback pour préproduction et bêta.
8. Exécuter un exercice de panne : image invalide, migration échouée et restauration.

### 6.3 Gate de sortie M2

- Une release candidate est déployée sans compilation sur la cible.
- Les versions et digests sont visibles depuis les services.
- Les migrations d’une base vide et d’une base N-1 passent.
- Le rollback applicatif et la restauration des données sont documentés et testés.
- Les logs et alertes permettent de diagnostiquer un déploiement sans accès direct au code.

## 7. Phase M3 — Comptes utilisateurs et identité

### 7.1 Cadrage obligatoire

Avant le code, produire :

- une SFD Account/Identity précisant inscription, connexion, vérification, récupération et suppression ;
- une STD définissant tokens, rotation, stockage des secrets et frontières interservices ;
- un ADR statuant sur le service Identity séparé ou l’intégration au service Player ;
- une matrice de menaces et de tests de sécurité ;
- un plan de migration du `demoPlayerId` et des profils existants.

### 7.2 Modèle cible recommandé

Le compte représente l’identité et les droits. Le `PlayerProfile` conserve la progression et l’équipe. Un compte possède un profil joueur principal ; ce profil possède le personnage initial puis ses compagnons. Cette séparation évite de confondre credential, personne connectée et personnage de jeu.

Le modèle minimal comprend : `UserAccount`, `Credential`, `Session` ou `RefreshToken`, `PlayerProfileLink`, consentements et journal de sécurité. Les contraintes garantissent un compte unique par adresse normalisée et un seul profil principal actif par compte.

### 7.3 Parcours fonctionnels

- Création de compte et validation de l’adresse.
- Création atomique du profil et du personnage initial.
- Connexion et renouvellement sécurisé de session.
- Déconnexion de la session courante et de toutes les sessions.
- Mot de passe oublié et changement de mot de passe.
- Consultation et modification des données autorisées.
- Suspension, suppression et anonymisation selon la politique retenue.
- Reprise d’une run appartenant uniquement au compte connecté.

### 7.4 Sécurité et autorisation

- Hashage mémoire-dur des mots de passe avec paramètres versionnés.
- Access tokens courts et refresh tokens rotatifs, hachés en base.
- Protection contre brute force et credential stuffing.
- Aucun `playerId` fourni par le client ne fait foi : l’identité est dérivée du token.
- Authentification interservice distincte des tokens utilisateurs.
- Devtools désactivés hors développement et protégés par une politique dédiée.
- Journalisation sans mot de passe, token, secret ni donnée sensible.

### 7.5 Tests obligatoires

- Un compte ne peut lire ni modifier le profil ou la run d’un autre compte.
- Une inscription concurrente ne crée pas de doublon.
- Un refresh token réutilisé après rotation est rejeté et la famille de sessions est révoquée.
- La création compte + profil + personnage est idempotente ou compensée.
- Les anciennes routes acceptant un `playerId` arbitraire deviennent internes ou autorisées par ownership.
- Les parcours Playwright couvrent inscription, connexion, reprise, déconnexion et récupération.

### 7.6 Gate de sortie M3

Un utilisateur neuf peut créer son compte, obtenir son personnage initial, se reconnecter sur une nouvelle session et retrouver exclusivement ses propres données. Aucun identifiant de démonstration n’est utilisé dans le parcours normal.

## 8. Phase M4 — Tranche verticale jouable

### 8.1 Objectif

Rendre un segment court mais intégralement connecté : compte → Hall → dialogue ou événement → exploration → combat → récompense → changement de salle → sauvegarde → reprise → conclusion temporaire.

### 8.2 Travaux prioritaires

- Sélectionner le premier segment narratif du Hall et ses checkpoints.
- Connecter les StorySteps et la progression Account réellement persistée.
- Câbler les dialogues Catalog des PNJ indispensables.
- Terminer les conséquences de règles locales utilisées par ce segment.
- Garantir le déclenchement de tous les combats concernés au contact.
- Finaliser récompenses, inventaire, équipement et synchronisation des caractéristiques.
- Ajouter les transitions de salle et une conclusion de tranche clairement présentée.
- Couvrir le parcours complet en E2E et en tests d’intégration.

### 8.3 Gate de sortie M4

Un testeur externe peut terminer ou perdre la tranche sans DevTools, sans manipulation de base et sans état bloqué. Une fermeture du navigateur ou un redémarrage des services ne détruit pas sa progression validée.

## 9. Phase M5 — Contenu et expérience bêta

### 9.1 Périmètre minimum recommandé

- Hall d’entrée finalisé pour le parcours retenu.
- Plusieurs salles reliées et visuellement distinctes.
- Combats standards, au moins une rencontre élite et une conclusion forte.
- Plusieurs PNJ avec dialogues et réactions réellement connectés.
- Un ensemble contrôlé de compétences, équipements, objets et récompenses entièrement authorés.
- Un objectif et une progression visibles depuis l’interface.
- Une durée de jeu suffisante pour observer exploration, combat et progression.

### 9.2 Travaux UX

- Lisibilité de l’initiative, des portées, cibles, coûts et conséquences.
- File d’animations unique empêchant les effets simultanés incohérents.
- Caméra exploration/combat stable et adaptée à l’action.
- Texte systématique à côté des icônes métier.
- Feedback clair sur interaction impossible, sauvegarde, connexion et erreur réseau.
- Navigation clavier, focus et contrastes sur les parcours principaux.

### 9.3 Contenu reportable

Le système de Talents, la totalité des HF, les vingt-sept salles, la difficulté N complète, tous les renforts génériques et la normalisation des 138 compétences sont différés sauf lorsqu’un de ces éléments est indispensable au segment choisi.

### 9.4 Gate de sortie M5

Le contenu ne présente aucun placeholder visible, aucune interaction sans conséquence annoncée et aucun objet non documenté. Les testeurs comprennent l’objectif, les règles de combat et la progression sans explication orale du développeur.

## 10. Phase M6 — Durcissement et ouverture bêta

### 10.1 Qualification finale

- Campagne E2E sur navigateur principal et résolutions supportées.
- Tests de charge ciblés sur inscription, connexion, reprise et commandes de combat.
- Tests de concurrence : double démarrage de run, double récompense et appels idempotents.
- Tests de perte réseau et indisponibilité temporaire d’un service.
- Revue sécurité des endpoints publics et internes.
- Vérification des sauvegardes et restauration chronométrée.
- Revue des données personnelles, rétention et suppression.
- Sessions de jeu exploratoires avec collecte structurée des retours.

### 10.2 Exploitation bêta

- Tableau de bord technique et produit.
- Alertes sur indisponibilité, erreurs, migrations et sauvegardes.
- Formulaire de signalement avec version, run et identifiant de corrélation.
- Procédure d’incident, rollback, restauration et communication.
- Politique de versions bêta et notes de release.
- Procédure de fermeture temporaire des inscriptions.

### 10.3 Gate d’ouverture

La bêta est ouverte uniquement si les critères de la section 12 sont validés et si aucun défaut critique ou élevé sans mitigation n’est accepté.

## 11. Dépendances critiques

| Dépendance | Bloque | Décision |
|---|---|---|
| Tests rouges | Toute activation CD | Résoudre avant M2 |
| Couverture inférieure à 80 % | Protection de `develop` et release | Relever par service sans moyenne globale |
| Cible d’hébergement non choisie | Adaptateur final de déploiement | Docker Compose par défaut bêta |
| Modèle Account non arbitré | M3 | SFD + STD + ADR obligatoires |
| Story Hall non sélectionnée | M4 | Définir une tranche cohérente et finie |
| Contenu Catalog non normalisé | M5 | Publier uniquement le sous-ensemble validé |

## 12. Definition of Ready de la bêta

### 12.1 Qualité et livraison

- Tous les statuts CI requis sont verts.
- Chaque service backend, Shared et le frontend atteint au moins 80 %.
- Aucun test critique n’est ignoré ou instable.
- Les images déployées possèdent SBOM, provenance et digest vérifié.
- Le déploiement et le rollback ont été testés sur préproduction.

### 12.2 Comptes et sécurité

- Inscription, connexion, récupération et déconnexion fonctionnent.
- Chaque compte accède uniquement à son profil, son personnage et ses runs.
- Les identifiants de démonstration et routes publiques par `playerId` sont retirés du parcours normal.
- Aucun secret n’est présent dans le dépôt ou les images.
- Aucun finding critique ou élevé non accepté n’est ouvert.

### 12.3 Jouabilité

- Le joueur comprend son objectif dès le Hall.
- Les PNJ utiles déclenchent un vrai dialogue ou événement.
- Les ennemis poursuivent et déclenchent les combats au contact.
- Les combats permettent victoire, défaite et fuite sans blocage.
- Les récompenses et équipements ont un effet observable.
- La sauvegarde et la reprise restaurent exactement l’état validé.
- Une conclusion temporaire de la tranche est atteignable.

### 12.4 Exploitation

- Les sauvegardes sont automatiques et restaurables.
- Les erreurs sont corrélées et observables.
- Une procédure d’incident et une procédure de rollback existent.
- Les retours testeurs peuvent être reliés à une version précise.

## 13. Risques et réponses

| Risque | Impact | Réponse |
|---|---|---|
| Dette de tests supérieure aux estimations | Décalage de toute la roadmap | Baseline M0, réparation par dépendance, aucun CD prématuré |
| Couverture gonflée par tests faibles | Fausse confiance | Branches à 80 %, mutation testing et revue des assertions |
| Authentification ajoutée trop tard dans les API | Refonte des contrats | Ownership et identité dérivée du token dès M3 |
| Migration EF non réversible | Perte de service ou données | Expand/contract, sauvegarde, bundle et répétition préproduction |
| Contenu trop large avant vertical slice | Bêta jamais stabilisée | Sous-ensemble authoré et gel du périmètre M4/M5 |
| Charge d’un développeur unique | Délais et contexte fragmenté | Jalons fermés, WIP limité et gates explicites |
| Cible cloud changeante | Reprise du CD | Séparer build/promotion de l’adaptateur de déploiement |

## 14. Gouvernance et pilotage

Chaque phase dispose d’un backlog fermé, d’un responsable, d’un gate mesurable et d’une démonstration de sortie. Un chantier de phase suivante peut être préparé, mais ne doit pas consommer la capacité principale tant que le gate précédent est rouge.

Le pilotage hebdomadaire examine : tests rouges restants, couverture par service, durée CI, défauts bloquants, stabilité de l’environnement, progression du jalon et variation du périmètre. Toute nouvelle fonctionnalité doit indiquer le jalon qu’elle sert ou être placée après la bêta.

## 15. Historique

| Version | Date | Évolution | Statut |
|---|---|---|---|
| 1.0 | 25/08/2026 | Roadmap ordonnée CI/CD → Accounts → vertical slice → bêta. | Plan directeur |
