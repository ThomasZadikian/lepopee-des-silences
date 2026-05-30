# L’épopée des silences — Cadrage technique v2

## Statut

Branche de refonte : `v2/develop`

La v2 est une refonte majeure de `RPG_ESI07`.

Elle introduit une nouvelle direction produit et technique : le projet devient un RPG roguelite narratif full web centré sur l’exploration du Palais mental du joueur.

## Décisions structurantes

- Le client principal devient une application web Vue 3 / TypeScript.
- Unity est conservé comme client legacy v1, mais ne pilote plus la roadmap v2.
- Le backend devient serveur-autoritaire pour toutes les données critiques.
- Le frontend n’envoie que des intentions utilisateur.
- Le cœur gameplay est regroupé dans un Game Engine Service central.
- Les runs sont générées par seed, version de générateur et matrice de Markov versionnée.
- Les choix de run sont visibles, cliquables et irréversibles.
- Les événements de run sont historisés via Event Sourcing ciblé sur les runs.
- Les Lois du Palais sont modulaires, versionnées et extensibles.
- Les services périphériques restent séparés : Identity, Catalog, Player, Leaderboard, Audit/GDPR.
- Le code peut être open source.
- L’univers narratif, les livres, personnages, textes, scénarios, noms, logos et assets restent protégés par droit d’auteur.

## Objectif du premier jalon

Le premier jalon v2 vise à poser un socle technique minimal mais propre :

- branche `v2/develop` initialisée ;
- documentation v2 créée ;
- ADR principales rédigées ;
- structure cible définie ;
- Unity isolé comme legacy ;
- socle backend v2 préparé ;
- futur Game Engine Service cadré ;
- règles serveur-autoritaire documentées.

## Roadmap technique immédiate

1. Initialiser la branche `v2/develop`.
2. Ajouter les ADR de cadrage.
3. Ajouter la notice de propriété intellectuelle.
4. Isoler la v1 Unity en legacy.
5. Préparer la structure cible du repo.
6. Initialiser le socle Game Engine.
7. Créer le modèle Run / Room / Node.
8. Ajouter un Event Store minimal.
9. Implémenter `StartRun`.
10. Générer les 4 choix initiaux.