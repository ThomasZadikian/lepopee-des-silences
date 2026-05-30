# Structure cible du dépôt — v2

## Objectif

La refonte v2 introduit une organisation plus claire entre :

- applications clientes ;
- services backend ;
- packages partagés ;
- infrastructure ;
- tests ;
- legacy v1.

## Structure cible

```text
/
├── apps/
│   ├── web-client/
│   └── admin-portal/
│
├── services/
│   ├── api-gateway/
│   ├── game-engine/
│   ├── identity/
│   ├── catalog/
│   ├── player/
│   ├── leaderboard/
│   └── audit-gdpr/
│
├── packages/
│   ├── shared-contracts/
│   └── shared-kernel/
│
├── infra/
│   ├── docker/
│   └── observability/
│
├── legacy/
│   └── unity-v1/
│
├── tests/
│   ├── backend/
│   ├── integration/
│   └── contract/
│
└── docs/
    └── v2/