# Analyse du repo — incohérences, doublons, problématiques

Date : 2026-06-16. Analyse statique uniquement.
Périmètre réel du code : backend `.NET` (`services/catalog`, `services/game-engine`, `services/player`, `packages/shared-building-blocks`) + frontend Vue/TS (`apps/game-client`). Les autres dossiers d'`apps/` et `services/` sont des placeholders vides.

## 1. Doublons et code mort

### 1.1 `features/combat` vs `features/combats` (frontend) — doublon complet
`apps/game-client/src/features/combats/` (pluriel) est mort : 0 référence. Seul `features/combat/` (singulier) est importé (par `RunPage.vue`). Le dossier mort duplique `combatApi.ts`, `CombatantCard.vue`, `CombatLogPanel.vue`, un store et les types.

### 1.2 Collision de nom de store Pinia
`features/combat/stores/useCombatStore.ts` et `features/combats/stores/combatStore.ts` exportent tous deux `useCombatStore` (ids Pinia `'combatRuntime'` vif, `'combat'` mort).

### 1.3 `IClock` / `SystemClock` dupliqués (backend)
Défini dans `packages/shared-building-blocks/.../Time/` (jamais utilisé hors de son test) et dans `services/game-engine/.../Application/Abstractions/IClock.cs` + `Infrastructure/Clock/SystemClock.cs` (version utilisée).

### 1.4 Deux systèmes de résolution de combat (backend) — migration incomplète
Ancien : `Combats/SubmitCombatAction` → `POST .../combats/{id}/actions` (mono-cible). Nouveau : `Runs/UseCombatSkill` + `Runs/UseItemInCombat` → `skill-actions` / `item-actions` (multi-cibles, consommé par le front).

### 1.5 Scaffolding résiduel
`HelloWorld.vue` non référencé ; `PalaceMapPlaceholder.vue` placeholder (utilisé par RunPage).

## 2. Incohérences d'architecture

- 2.1 `features/combat/api/combatApi.ts` appelle `httpRequest` directement, alors que les autres features passent par `gameEngineApi`.
- 2.2 Surface HTTP combat éclatée : `skill-actions` dans `CombatsController`, `item-actions` dans `RunsController`.
- 2.3 `shared-building-blocks` référencé seulement par game-engine ; catalog/player ne l'utilisent pas.
- 2.4 Migrations EF game-engine réparties entre `Migrations/` et `Migrations/GameEngine/` pour un seul DbContext.

## 3. Problématiques de configuration

- 3.1 Ports contradictoires : front `.env.example` = 5000 ; fallback code = 5187 ; root `.env.example` = 5187.
- 3.2 Catalog : DB Postgres (port 5434, connexion `CatalogDb`) absente des docker-compose et du `.env.example` racine ; port hardcodé dans `CatalogDbContextFactory`.
- 3.3 `docker-compose.yml` ne contient que game-engine ; player seulement dans `.dev` ; catalog nulle part.

## 4. Points mineurs / hygiène

- Typo `SystemCloclTests.cs`.
- `legacy/` ≈ 3160 fichiers / ~37 Mo trackés (unity-v1, RPG_ESI07, ancien frontend).
- Git LFS cassé : jpgs legacy commités comme pointeurs 131 octets vs fichiers réels ~127 Ko.
- Placeholders structurels (`apps/admin-portal`, `apps/player-portal`, `services/api-gateway`, `services/audit-gdpr`) = `.gitkeep` seulement.
- 17 migrations toutes datées du 2026-06-16 (modèle de données instable).

> Remédiation : voir [docs/audits/alpha-0.7-stabilization-audit-remediation.md](../audits/alpha-0.7-stabilization-audit-remediation.md).
