# ADR-003 — Game Engine Service central

## Statut

Acceptée.

## Contexte

La v2 introduit plusieurs sous-systèmes fortement liés :

- run ;
- génération du Palais ;
- pièces ;
- nœuds ;
- événements ;
- Lois du Palais ;
- récompenses ;
- narration ;
- combat ;
- Tome ;
- Him’Lit.

Une séparation prématurée en microservices risquerait de fragmenter la logique gameplay et de complexifier inutilement le développement.

## Décision

Le cœur gameplay est regroupé dans un Game Engine Service central.

## Modules internes

- Run Module
- Palace Module
- Palace Law Engine
- Event Module
- Combat Module
- Reward Module
- Narrative Module
- Markov Generator
- Tome Writer

## Services périphériques

Les domaines périphériques peuvent rester séparés ou être extraits progressivement :

- Identity Service
- Catalog Service
- Player Service
- Leaderboard Service
- Audit/GDPR Service

## Conséquences

### Positives

- Cohérence métier forte.
- Développement plus rapide.
- Moins de complexité distribuée.
- Tests métier plus simples.
- Extraction future possible si nécessaire.

### Négatives

- Le Game Engine peut devenir volumineux.
- Une discipline stricte de modularisation interne est nécessaire.
- Les frontières entre modules doivent être surveillées.