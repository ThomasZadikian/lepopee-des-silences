# ADR-001 — Passage au client full web

## Statut

Acceptée.

## Contexte

La version v1 de RPG_ESI07 repose sur un client Unity, une API ASP.NET Core et un portail Vue 3.

La v2, renommée « L’épopée des silences », change de direction produit. Le jeu devient un RPG roguelite narratif jouable depuis navigateur. Le joueur explore son Palais mental au travers de runs procédurales, de choix visibles et irréversibles, de fragments narratifs et de Lois du Palais.

## Décision

Le client principal de la v2 sera une application web Vue 3 / TypeScript.

Unity est conservé comme client legacy v1, mais ne pilote plus la roadmap principale de la v2.

## Conséquences

### Positives

- Accès immédiat depuis navigateur.
- Déploiement plus simple.
- Itération UI plus rapide.
- Meilleure intégration avec le Tome, le leaderboard, les compagnons et les écrans de progression.
- Moins de friction pour les tests utilisateurs.

### Négatives

- Le client web ne peut pas être considéré comme fiable.
- Toute logique critique doit rester côté backend.
- Les performances graphiques devront être maîtrisées dans le navigateur.

## Impacts techniques

- Création progressive d’un client Vue 3 / TypeScript.
- Maintien temporaire du dossier Unity comme legacy.
- Renforcement du modèle serveur-autoritaire.
- Création d’API `/api/v2`.