# Player Service Foundation

## Objectif

Créer le bounded context Player.

## Responsabilités

Le Player Service gère :

* profil joueur permanent ;
* personnages persistants ;
* roster ;
* progression permanente ;
* unlocks futurs.

## Non-responsabilités

Le Player Service ne gère pas :

* runs ;
* rooms ;
* map nodes ;
* combats ;
* rewards runtime ;
* lois actives de run.

## Contrat avec Game Engine

Endpoint :

```http
GET /api/v2/players/{playerId}/run-snapshot
```

Ce snapshot sert à initialiser une run.
Le Game Engine en fera une copie runtime.

## Séparation des services

* Permanent = Player Service
* Runtime de run = Game Engine Service
* Définitions = Catalog Service

## État actuel

Cette PR ajoute un repository InMemory.
La persistance PostgreSQL du Player Service sera ajoutée dans une PR ultérieure.

## Prochaines étapes

* brancher Game Engine sur Player Service via gateway ;
* créer `IPlayerRunSnapshotGateway` dans Game Engine ;
* initialiser les runs depuis le snapshot Player ;
* ajouter la persistance PostgreSQL du Player Service ;
* ajouter companions/party runtime plus tard.
