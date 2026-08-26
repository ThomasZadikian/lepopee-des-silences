# Plateforme CI/CD — L’Épopée des Silences

Référence : LEDS-STD-CICD-001  
Version : 1.0  
Statut : Référence de mise en œuvre — cible bêta  

## 1. Objet et décision

Ce document définit la chaîne d’intégration, de qualification, de publication et de déploiement de L’Épopée des Silences. La cible retenue est une plateforme GitHub Actions adaptée au monorepo, publiant des images OCI immuables dans GitHub Container Registry puis promouvant exactement les mêmes digests entre les environnements.

La chaîne doit poursuivre quatre objectifs non négociables : empêcher une régression d’entrer sur `develop`, atteindre et maintenir au moins 80 % de couverture sur chaque unité logicielle, produire des artefacts traçables et déployer avec une stratégie de migration et de retour arrière explicite.

La cible d’hébergement définitive n’étant pas encore arrêtée, le cœur du CD reste indépendant du fournisseur. Le profil de référence pour la bêta est un hôte Linux administré exécutant Docker Compose. Un adaptateur OIDC vers Azure, AWS, GCP ou une plateforme Kubernetes pourra remplacer l’adaptateur de déploiement sans modifier la chaîne de construction ni les artefacts.

### 1.1 Résultat attendu

- Toute pull request exécute les contrôles correspondant aux composants affectés.
- Une exécution nocturne contrôle l’intégralité du monorepo, même lorsqu’aucun chemin détecté ne l’exige.
- Aucun déploiement n’est possible avec un test requis en échec ou un seuil de couverture non atteint.
- Chaque image est identifiée par son digest, accompagnée d’un SBOM et d’une attestation de provenance.
- Les migrations sont générées, vérifiées puis exécutées comme une étape de déploiement contrôlée.
- Les environnements `integration`, `preproduction` et `beta` utilisent des règles de protection distinctes.

## 2. État initial constaté

| Unité | Technologie | Tests présents | État CI/CD initial |
|---|---|---|---|
| Game Engine | .NET 10, EF Core, PostgreSQL | xUnit, intégration Testcontainers | Aucun workflow, aucun Dockerfile |
| Player | .NET 10, EF Core, PostgreSQL | xUnit, intégration Testcontainers | Aucun workflow, aucun Dockerfile |
| Catalog | .NET 10, EF Core, PostgreSQL | xUnit, intégration Testcontainers | Aucun workflow, aucun Dockerfile |
| Shared Building Blocks | .NET 10 | xUnit | Aucun workflow de package |
| Game Client | Vue 3, TypeScript, Vite | Vitest, Playwright | Aucun seuil de couverture configuré |
| Infrastructure locale | Docker Compose, PostgreSQL 16 | Démarrage manuel | Compose limité aux bases de données |

Le dépôt contient des solutions `.slnx` indépendantes par service et un client frontend autonome. Cette séparation doit être conservée dans les matrices de build et de couverture afin qu’un service ne puisse pas compenser la faible couverture d’un autre.

## 3. Principes d’architecture

### 3.1 Décisions structurantes

- **CI par composant, qualification globale périodique.** Les chemins affectés réduisent le temps de retour sur une pull request ; la campagne nocturne neutralise le risque d’une dépendance transversale non détectée.
- **Build once, promote many.** Une image publiée n’est jamais reconstruite entre intégration et bêta. La promotion référence son digest OCI.
- **Pipeline as code.** Les workflows, scripts de qualité, seuils et manifestes de déploiement sont versionnés avec le code.
- **Droits minimaux.** Les workflows démarrent avec `permissions: contents: read`; les droits `packages: write`, `id-token: write` et `attestations: write` sont accordés uniquement aux jobs concernés.
- **Actions immuables.** Les actions tierces sont épinglées par SHA complet et maintenues par Dependabot.
- **Migrations hors processus API.** Les API ne modifient pas leur schéma au démarrage en environnement partagé.
- **Compatibilité progressive des schémas.** Les migrations suivent le modèle expand/migrate/contract afin de permettre le retour à l’image précédente.
- **Aucune dette de test masquée.** Les exclusions, tests ignorés et quarantaines temporaires sont inventoriés, justifiés, associés à une échéance et interdits dans l’état cible.

[[FIGURE:ci-architecture.png|Architecture cible de la chaîne CI/CD et promotion des artefacts]]

### 3.2 Branches et événements

| Événement | Traitement | Déploiement |
|---|---|---|
| Pull request vers `develop` | CI affectée, analyse sécurité, couverture | Aucun |
| Push sur `T-RPG` pendant la transition | CI complète ou affectée selon le lot | Aucun |
| Merge sur `develop` | CI complète, construction et publication OCI | Automatique vers `integration` |
| Tag `vX.Y.Z-rc.N` | Requalification des digests publiés | `preproduction`, approbation requise |
| Release `vX.Y.Z-beta.N` | Vérification des attestations et promotion | `beta`, approbation requise |
| `main` | Réservée à la future production | Hors périmètre bêta initial |

Les règles de protection de `develop` imposent les statuts requis, une branche à jour, au moins une revue et l’interdiction de pousser directement. Les déploiements concurrents sur un même environnement sont sérialisés par `concurrency` ; une exécution plus ancienne est annulée avant le début de la migration.

## 4. Organisation des workflows

### 4.1 Fichiers cibles

| Fichier | Responsabilité |
|---|---|
| `.github/workflows/ci.yml` | Orchestration PR/push, détection des chemins, agrégation des statuts |
| `.github/workflows/ci-nightly.yml` | Qualification exhaustive, mutation testing et dérives de dépendances |
| `.github/workflows/security.yml` | CodeQL C# et JavaScript/TypeScript, dépendances et secrets |
| `.github/workflows/release.yml` | Construction OCI, SBOM, provenance, publication GHCR |
| `.github/workflows/deploy.yml` | Promotion d’un manifeste de digests vers un environnement |
| `.github/workflows/_dotnet-service.yml` | Workflow réutilisable build/test/coverage d’un service .NET |
| `.github/workflows/_frontend.yml` | Workflow réutilisable typecheck/Vitest/build/Playwright |
| `.github/workflows/_container.yml` | Workflow réutilisable BuildKit et attestations |
| `.github/workflows/_deploy-compose.yml` | Adaptateur de déploiement Docker Compose de la bêta |

Les workflows préfixés par `_` sont appelés avec `workflow_call`. Ils exposent des entrées explicites : chemin de solution, nom du service, projets de tests, contexte Docker, Dockerfile, environnement et digest. Aucun workflow réutilisable ne reçoit de secret non déclaré.

[[PAGEBREAK]]

### 4.2 Graphe CI d’une pull request

1. Détecter les composants affectés et les dépendances transversales.
2. Vérifier le format, les fichiers de verrouillage et l’absence de changement EF sans migration.
3. Exécuter en parallèle les matrices .NET et frontend.
4. Démarrer PostgreSQL via Testcontainers pour les suites d’intégration.
5. Fusionner les rapports de couverture par unité logicielle.
6. Appliquer les seuils et publier les résultats TRX/JUnit, Cobertura/LCOV et HTML.
7. Construire les artefacts sans les publier afin de vérifier la reproductibilité.
8. Exécuter les contrôles CodeQL et de dépendances.
9. Produire un unique statut `ci-required` consommé par la protection de branche.

Une modification de `packages/shared-building-blocks` déclenche les trois services .NET. Une modification des contrats HTTP partagés ou des fichiers de composition déclenche également les tests d’intégration et les tests E2E concernés.

## 5. Stratégie de tests

### 5.1 Pyramide de qualification

| Niveau | Finalité | Exécution |
|---|---|---|
| Tests de domaine | Invariants, calculs, transitions d’état, déterminisme | Chaque PR |
| Tests d’application | Commandes, queries, idempotence, ports et erreurs | Chaque PR |
| Tests d’intégration | EF Core, migrations, PostgreSQL, endpoints, sérialisation | Chaque PR affectée et nightly |
| Tests de contrat | Compatibilité OpenAPI et échanges Game Engine/Player/Catalog | Chaque PR de contrat |
| Tests frontend unitaires | Stores, composables, composants et règles d’affichage | Chaque PR frontend |
| Tests E2E | Compte, run, exploration, dialogue, combat, sauvegarde | PR ciblée, intégration et préproduction |
| Smoke tests | Santé, version, connexion DB et parcours minimal | Après chaque déploiement |
| Tests de résilience | reprise, concurrence, indisponibilité interservice | Nightly puis préproduction |
| Tests de performance | latence API et stabilité d’une session | Préproduction et avant release bêta |

Les tests doivent vérifier les comportements publics et les invariants, non reproduire l’implémentation interne. Les snapshots volumineux ne remplacent pas les assertions métier. Un test supprimé doit être remplacé lorsqu’il protégeait encore une exigence valide.

### 5.2 Seuils de couverture

| Unité de qualité | Lignes | Branches | Méthodes / fonctions | Statements |
|---|---:|---:|---:|---:|
| Game Engine | 80 % | 80 % | 80 % | N/A |
| Player | 80 % | 80 % | 80 % | N/A |
| Catalog | 80 % | 80 % | 80 % | N/A |
| Shared Building Blocks | 80 % | 80 % | 80 % | N/A |
| Game Client | 80 % | 80 % | 80 % | 80 % |

Pour .NET, les couvertures unitaires et d’intégration sont collectées en Cobertura puis fusionnées par service avant l’application du seuil. Pour le client, Vitest utilise le provider V8 avec des seuils globaux à 80 %. Un seuil par fichier peut être appliqué aux modules critiques — authentification, cycle de run, résolution de combat et persistance locale — après stabilisation.

Les seules exclusions automatiques admises sont le code généré, les fichiers `*.Designer.cs` et les migrations EF générées. Toute autre exclusion doit apparaître dans un registre versionné avec justification. Les contrôleurs, gateways, mappers, règles métier, stores et composables restent inclus.

### 5.3 Convergence depuis l’état rouge

La CI est installée en deux modes successifs :

1. **Mode récupération.** Toutes les suites sont exécutées et leurs échecs sont publiés ; aucun CD n’est autorisé. Les échecs sont classés entre régression réelle, test obsolète, défaut d’environnement et test instable.
2. **Mode contraignant.** Dès qu’une unité atteint zéro test rouge, son statut devient requis. Le seuil de couverture est ensuite relevé par paliers sans jamais pouvoir redescendre, jusqu’à 80 %.

La cible n’est considérée atteinte que lorsque toutes les unités sont simultanément vertes et au seuil final. Un manifeste temporaire de dette peut documenter les échecs pendant la récupération, mais ne transforme jamais un test rouge en succès et doit être vide avant le premier déploiement bêta.

### 5.4 Qualité des tests

- Aucun `Skip`, `Only`, `Todo` ou exclusion durable sans ticket, propriétaire et date d’expiration.
- Les tests d’intégration créent leurs propres données et n’utilisent aucune base partagée.
- Les tests temporels utilisent une horloge contrôlable ; les tests aléatoires conservent la seed en cas d’échec.
- Les tests E2E conservent trace, vidéo et capture uniquement sur échec.
- Le mutation testing est exécuté la nuit sur les domaines critiques afin de détecter une couverture artificielle sans assertions efficaces.
- Les résultats sont conservés quatorze jours sur les PR et quatre-vingt-dix jours sur les releases.

## 6. Construction et publication des artefacts

### 6.1 Conteneurisation

Chaque API reçoit un Dockerfile multi-stage : SDK .NET 10 pour restore/build/publish, puis image runtime ASP.NET non-root. Le client est construit avec Node puis servi par une image web minimale configurée pour les routes SPA et les en-têtes de sécurité.

Les images produites sont :

- `ghcr.io/<owner>/leds-game-engine` ;
- `ghcr.io/<owner>/leds-player` ;
- `ghcr.io/<owner>/leds-catalog` ;
- `ghcr.io/<owner>/leds-game-client`.

Chaque image porte les labels OCI de source, révision, version et date de création. Les tags humains facilitent la lecture, mais les déploiements utilisent exclusivement les digests `sha256`.

### 6.2 Reproductibilité et dépendances

- Le SDK .NET 10 est épinglé par `global.json`.
- Les versions NuGet sont centralisées et les restores utilisent des fichiers de verrouillage en mode locked.
- Le client utilise `npm ci` et son lockfile versionné.
- BuildKit utilise un cache GitHub Actions ou registry sans réutiliser les couches contenant des secrets.
- Le contexte Docker exclut les résultats de tests, secrets, fichiers locaux et artefacts de développement.
- Une reconstruction du même commit doit produire un contenu fonctionnellement identique et une provenance vérifiable.

### 6.3 Artefacts de release

Une release publie le manifeste de digests, les images OCI, les bundles de migration EF, les rapports de tests, le rapport de couverture, les SBOM SPDX/CycloneDX et les attestations de provenance. Le manifeste est la seule entrée acceptée par le workflow de promotion.

## 7. Sécurité de la chaîne

### 7.1 Contrôles obligatoires

- CodeQL sur C# et JavaScript/TypeScript.
- Revue des dépendances sur pull request et blocage des vulnérabilités critiques ou élevées non acceptées.
- Dependabot pour NuGet, npm, Docker et GitHub Actions.
- Secret scanning et push protection lorsque disponibles sur le dépôt.
- Scan des images avant promotion et blocage des vulnérabilités critiques exploitables.
- SBOM et provenance attachés aux images.
- Vérification des attestations avant préproduction et bêta.
- Actions tierces épinglées sur SHA complet.
- Environnements GitHub avec approbateurs et secrets séparés.

### 7.2 Identités de déploiement

Lorsqu’un fournisseur cloud compatible est retenu, GitHub OIDC fournit des jetons courts sans secret cloud permanent. Pour le profil Docker Compose initial, l’adaptateur SSH utilise un compte dédié sans shell administratif général, une clé limitée au déploiement, un fichier `authorized_keys` contraint et des secrets propres à chaque environnement.

Les pull requests provenant de forks ou de Dependabot n’obtiennent jamais les secrets d’environnement ni le droit de publier un package.

## 8. Environnements et configuration

| Environnement | Déclenchement | Données | Protection |
|---|---|---|---|
| Intégration | Merge sur `develop` | Éphémères ou réinitialisables | Automatique, concurrence sérialisée |
| Préproduction | Tag release candidate | Copie anonymisée ou dataset contrôlé | Approbation, migrations vérifiées |
| Bêta | Release bêta | Persistantes, sauvegardées | Approbation, fenêtre de déploiement, rollback préparé |

La configuration non sensible est injectée par variables d’environnement versionnées dans un manifeste par environnement. Les secrets restent dans GitHub Environments ou dans un coffre externe. Les chaînes de connexion, clés de signature et secrets interservices ne sont jamais placés dans les fichiers Compose versionnés.

Chaque service expose au minimum :

- `/health/live` pour vérifier le processus ;
- `/health/ready` pour vérifier les dépendances nécessaires ;
- `/version` pour exposer version, commit et digest ;
- des logs JSON avec identifiant de corrélation ;
- des métriques et traces OpenTelemetry exportables.

## 9. Déploiement et migrations

[[FIGURE:deployment-sequence.png|Séquence contrôlée de déploiement et de migration]]

### 9.1 Séquence nominale

1. Résoudre et vérifier les digests du manifeste de release.
2. Vérifier signatures, attestations, SBOM et résultats de qualification.
3. Acquérir le verrou exclusif de l’environnement.
4. Vérifier espace disque, santé PostgreSQL et capacité de sauvegarde.
5. Sauvegarder les trois bases et tester la présence des fichiers produits.
6. Exécuter les migrations EF en mode dry-run ou script revu, puis appliquer les bundles.
7. Déployer les nouvelles images avec leurs digests.
8. Attendre les readiness checks et la stabilisation des dépendances.
9. Exécuter les smoke tests et le parcours E2E critique.
10. Enregistrer le manifeste actif et lever le verrou.

### 9.2 Politique EF Core

Un bundle de migration distinct est produit pour Game Engine, Player et Catalog. La CI vérifie qu’un changement de snapshot EF est accompagné d’une migration et qu’une base vide comme une base issue de la version précédente atteignent le même modèle attendu.

Les scripts SQL idempotents sont conservés pour revue et diagnostic. Aucune commande `database update` exécutée depuis un poste développeur ne fait partie du processus de déploiement.

### 9.3 Retour arrière

Le rollback applicatif redéploie les digests du dernier manifeste sain. Les migrations destructrices sont interdites dans la même release que la suppression du code compatible ; elles sont découpées selon expand/contract. Aucun `Down()` ni restauration de base n’est lancé automatiquement.

Si une migration non compatible rend le rollback applicatif impossible, le déploiement s’arrête avant promotion et exige une décision manuelle : correction en avant ou restauration contrôlée des trois bases. Le plan de restauration est testé avant l’ouverture de la bêta.

## 10. Observabilité et exploitation

La réussite d’un job GitHub ne suffit pas à valider un déploiement. La plateforme doit corréler commit, digest, version de migration et événements runtime.

Les tableaux de bord minimaux suivent : disponibilité et latence des API, taux d’erreur HTTP, connexions PostgreSQL, durée et échec des migrations, démarrages de run, reprises de run, erreurs de dialogue/combat et erreurs frontend.

Les alertes bêta doivent couvrir : service indisponible, échec répété de readiness, erreur de migration, espace disque faible, taux d’erreur anormal et sauvegarde absente. Une release dispose d’un journal indiquant l’approbateur, les digests, la durée, les migrations et le résultat des smoke tests.

## 11. Contrats de qualité et protection de branche

| Statut requis | Condition de succès |
|---|---|
| `build-dotnet` | Restore locked et build Release sans erreur |
| `test-game-engine` | Tous tests verts, couverture Game Engine ≥ 80 % |
| `test-player` | Tous tests verts, couverture Player ≥ 80 % |
| `test-catalog` | Tous tests verts, couverture Catalog ≥ 80 % |
| `test-shared` | Tous tests verts, couverture Shared ≥ 80 % |
| `test-client` | Typecheck, Vitest et build verts, couverture ≥ 80 % |
| `test-e2e` | Parcours critiques Playwright verts |
| `security` | Aucun finding bloquant non accepté |
| `migration-check` | Snapshots synchronisés et migrations testées |
| `ci-required` | Agrégation de tous les statuts applicables |

Une modification documentaire seule peut éviter les builds lourds, mais conserve les contrôles de structure et de liens. Une modification d’un workflow exécute systématiquement la CI complète avant fusion.

## 12. Plan de mise en œuvre

### 12.1 Lot A — Mesure et remise à plat

- Inventorier toutes les suites, tests ignorés, durées et causes d’échec.
- Normaliser les versions de runners et collecteurs de couverture.
- Ajouter `global.json`, configuration de couverture et scripts reproductibles locaux.
- Produire la baseline par service sans masquer les échecs.

### 12.2 Lot B — CI contraignante

- Créer les workflows réutilisables et la détection monorepo.
- Réparer les tests jusqu’à zéro échec.
- Compléter les tests et relever progressivement les seuils jusqu’à 80 %.
- Activer les protections de branche et les contrôles de sécurité.

### 12.3 Lot C — Artefacts et CD

- Créer les Dockerfiles multi-stage et le Compose de déploiement.
- Publier les images, SBOM, attestations et bundles EF.
- Configurer intégration, préproduction et bêta.
- Automatiser les sauvegardes, smoke tests et rollback applicatif.
- Réaliser un exercice complet de déploiement et de restauration.

## 13. Critères d’acceptation

- Une pull request modifiant chaque service déclenche sa suite complète et le bon seuil de couverture.
- Une modification de Shared Building Blocks déclenche les trois services consommateurs.
- Toutes les unités atteignent 80 % selon les métriques définies.
- Aucun test requis n’est rouge, ignoré sans justification ou instable sur trois exécutions successives.
- Une release produit quatre images par digest, trois bundles EF, un manifeste, des SBOM et des attestations.
- Le déploiement d’intégration est automatique après merge sur `develop`.
- La préproduction et la bêta exigent une approbation GitHub Environment.
- Un déploiement défectueux peut revenir au manifeste applicatif précédent.
- Une restauration des bases a été testée et chronométrée.
- Le parcours compte → run → exploration → combat → sauvegarde est exécutable en E2E avant l’ouverture de la bêta.

## 14. Sources techniques

- GitHub Docs — Environments et règles de protection : https://docs.github.com/actions/deployment/targeting-different-environments/using-environments-for-deployment
- GitHub Docs — OpenID Connect : https://docs.github.com/actions/concepts/security/openid-connect
- GitHub Docs — Artifact attestations : https://docs.github.com/actions/security-for-github-actions/using-artifact-attestations/using-artifact-attestations-to-establish-provenance-for-builds
- GitHub Docs — Sécurisation des workflows et épinglage SHA : https://docs.github.com/en/actions/reference/security/secure-use
- GitHub Docs — Container Registry : https://docs.github.com/packages/working-with-a-github-packages-registry/working-with-the-container-registry
- Microsoft Learn — Couverture des tests .NET : https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage
- Microsoft Learn — Application des migrations EF Core : https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying
- Vitest — Seuils de couverture : https://vitest.dev/config/coverage
- Playwright — Exécution en CI : https://playwright.dev/docs/ci
- Docker Docs — SBOM et provenance avec GitHub Actions : https://docs.docker.com/build/ci/github-actions/attestations/

[[PAGEBREAK]]

## 15. Historique

| Version | Date | Évolution | Statut |
|---|---|---|---|
| 1.0 | 25/08/2026 | Définition de la plateforme CI/CD cible et de son plan de convergence. | Référence de mise en œuvre |
