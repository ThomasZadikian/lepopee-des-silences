# TEST-AUDIT-001 — Baseline CI et couverture

Statut : baseline technique du 26 août 2026  
Branche auditée : `codex/npc-pass-through-dialogue`, issue de `T-RPG`

## 1. Conclusion

Le dépôt possédait déjà un volume de tests conséquent, mais aucun calcul homogène de couverture, aucun seuil bloquant par composant et aucun pipeline actif. La qualité ne pouvait donc pas être démontrée de manière reproductible.

La présente reprise met en place cinq portes indépendantes : Game Engine, Player, Catalog, Shared Building Blocks et Game Client. Chacune doit atteindre 80 % ; une excellente couverture d'un composant ne peut pas compenser la faiblesse d'un autre.

## 2. Inventaire statique

Les nombres ci-dessous sont des déclarations de tests repérées dans les sources. Une `Theory` peut produire plusieurs cas à l'exécution.

| Composant | Fichiers de test | Déclarations de test |
| --- | ---: | ---: |
| Game Engine — unitaires | 217 | 1 724 |
| Game Engine — intégration | 22 | 91 |
| Player — unitaires | 16 | 221 |
| Player — intégration | 4 | 15 |
| Catalog — unitaires | 69 | 357 |
| Catalog — intégration | 17 | 72 |
| Shared Building Blocks | 4 | 28 |
| Game Client — Vitest | 86 | 872 |

Constats complémentaires :

- 2 508 déclarations xUnit et 872 déclarations Vitest sont présentes ;
- aucun marqueur de test ignoré ou focalisé (`Skip`, `.skip`, `.only`, `.todo`) n'a été détecté ;
- les tests d'intégration .NET utilisent PostgreSQL via Testcontainers ;
- le client déclare Playwright, mais ne possède encore ni configuration ni scénario E2E ;
- aucun rapport historique ne permet de confirmer que les 80 % étaient déjà atteints.

## 3. Corrections d'infrastructure appliquées

- SDK .NET verrouillé par `global.json` ;
- versions du SDK de test et de l'adaptateur xUnit harmonisées ;
- Coverlet collecté via un fichier `runsettings` commun ;
- migrations EF, fichiers Designer, sorties `obj` et assemblies de tests exclus du dénominateur ;
- rapports Cobertura, HTML, JSON et texte produits par composant ;
- seuils .NET bloquants : lignes, branches et méthodes à 80 % ;
- seuils Vue/TypeScript bloquants : lignes, branches, fonctions et instructions à 80 % ;
- verrou npm complété avec le fournisseur officiel `@vitest/coverage-v8` ;
- contrôle automatique interdisant les tests désactivés ;
- conservation des TRX et rapports de couverture pendant 14 jours.
- points d'entrée Catalog et Game Engine rendus accessibles à `WebApplicationFactory<Program>` ;
- fixtures API Catalog et Player raccordées à des bases PostgreSQL Testcontainers dédiées ;
- classes d'intégration Game Engine regroupées dans une collection xUnit partageant une seule fixture API, afin d'éviter un conteneur PostgreSQL par classe ;
- tests d'API Catalog et Player raccordés à leurs fixtures applicatives au lieu de démarrer avec une configuration de base absente.
- contrôle EF bloquant lorsqu'un modèle possède des changements sans migration associée.

## 4. Risques et écarts restant à traiter

| Priorité | Écart | Conséquence | Traitement |
| --- | --- | --- | --- |
| P0 | Exécution .NET/Docker indisponible dans l'environnement de travail actuel | Impossible de connaître ici les tests réellement rouges et les pourcentages initiaux | Exécuter le nouveau workflow CI sur GitHub ; corriger à partir des TRX et Cobertura produits |
| P0 | Seuil initial de 80 % non encore mesuré | La première CI peut être rouge malgré le volume de tests | Le seuil reste volontairement bloquant ; aucun `continue-on-error` n'est introduit |
| P1 | Aucun scénario Playwright | Les parcours complets joueur/API ne sont pas couverts | Ajouter en premier : lancement, reprise de run, déplacement, interaction PNJ et déclenchement de combat |
| P1 | Absence de fichiers `packages.lock.json` NuGet | Les dépendances transitives .NET ne sont pas totalement reproductibles | Générer puis versionner les verrous une fois la première restauration .NET validée |
| P1 | Pas de test de déploiement sur hôte réel | Les bundles EF et le rollback applicatif restent à éprouver | Valider d'abord l'environnement `integration` protégé |
| P2 | Pas de test de mutation | Une couverture élevée peut contenir des assertions faibles | Introduire Stryker progressivement sur Domain/Application après stabilisation |

## 5. Règle de lecture du seuil

80 % de couverture ne signifie pas que 80 % des exigences sont validées, ni que tout comportement est correct. La couverture mesure uniquement l'exécution du code par les tests. Les scénarios critiques doivent donc conserver des assertions fonctionnelles explicites, même lorsqu'un composant dépasse déjà le seuil.

## 6. Commandes locales de référence

```bash
dotnet tool restore
./scripts/ci/test-dotnet-component.sh game-engine services/game-engine/Leds.GameEngine.slnx 'Leds.GameEngine.*'
./scripts/ci/test-dotnet-component.sh player services/player/Leds.Player.slnx 'Leds.Player.*'
./scripts/ci/test-dotnet-component.sh catalog services/catalog/Leds.Catalog.slnx 'Leds.Catalog.*'
./scripts/ci/test-dotnet-component.sh shared-building-blocks packages/shared-building-blocks/Leds.SharedBuildingBlocks.slnx 'Leds.SharedBuildingBlocks'

cd apps/game-client
npm ci --ignore-scripts
npm run typecheck
npm run test:coverage
npm run build
```
