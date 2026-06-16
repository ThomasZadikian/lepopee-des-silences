# Alpha 0.7 — Stabilization Audit Remediation

- **PR**: `refactor(repo): stabilize alpha 0.7 architecture after audit`
- **Target version**: `alpha-0.7.15-stabilization`
- **Branch**: `refactor/stabilize-alpha-0.7-architecture`
- **Last updated**: 2026-06-16 (second pass)
- **Scope**: stabilization, cleanup and architecture clarification only. **No gameplay feature.** No Markov / ATB / new rewards / new rooms / new combat logic. No UX change.

## 0. FINAL VERDICT — NOT MERGEABLE (yet)

This branch is **not mergeable into `v2/develop`** from the current pass. Frontend is fully fixed and validated. Backend code changes are complete but **two blocking gates remain**:

1. **Backend build/test not executed.** The working environment has no .NET SDK and the package proxy blocks installing one; the services target `net10.0`. Every backend change below (IClock dedup, `item-actions` move) is code-complete and reasoned-correct but **unverified by compiler/tests**. They MUST pass `dotnet build` + `dotnet test` locally before merge (commands in §8).
2. **Divergent legacy combat resolution path (Blocking before alpha-0.8).** The old `/actions` path and the canonical `skill-actions`/`item-actions` path use two genuinely different resolution engines (see item #3). Reconciling them (delegate or delete+migrate-tests) needs a working build/test loop and was therefore **not done**; it is flagged blocking, not "done".

Merge only once both gates are cleared.

## 1. Source of the audit

Static analysis of 2026-06-16, recorded in
[`docs/follow-up/analyse-incoherences-2026-06-16.md`](../follow-up/analyse-incoherences-2026-06-16.md).

## 2. Status legend

`Fixed` — corrected and validated here. `Fixed (code, unverified)` — code complete but not compiled/tested in this environment. `Blocking before alpha-0.8` — must be resolved before the next milestone. `Deferred (dedicated PR)` — intentionally handled by a separate PR. `Documented` — intentionally unchanged, rationale recorded.

## 3. Item-by-item remediation

### P0

| # | Item | Status | Detail |
|---|------|--------|--------|
| 1 | Contradictory API ports | **Fixed** | `apps/game-client/.env.example` 5000 → 5187; code fallback and root `.env.example` already 5187. Env override preserved. |
| 2 | Catalog not provisionable | **Fixed (config) / build unverified** | `catalog-postgres` (5434) added to both compose files; `CATALOG_DB_*` + `CATALOG_DB_CONNECTION_STRING` in root `.env.example`; `CatalogDbContextFactory` reads `CATALOG_DB_CONNECTION_STRING` with localhost fallback; `apply-migrations.ps1` applies Catalog; `start-dev.ps1` lists Catalog DB. `**/appsettings.json` is gitignored repo-wide (so none committed); runtime default stays InMemory, Postgres is opt-in via env (`Persistence__Mode=Postgres`, `ConnectionStrings__CatalogDb`). `dotnet build/test` for Catalog **could not be run here** — validate locally. |
| 3 | Dual combat system | **Blocking before alpha-0.8** | The frontend half is gone (#4) and all combat HTTP is consolidated (#6), but the two **backend resolution engines still diverge**: old `SubmitCombatActionCommandHandler` resolves through the Domain (`combat.SubmitAction(CombatAction.BasicAttack(...))`, `CombatState`, single-target, BasicAttack only); canonical `UseCombatSkillCommandHandler` resolves through an Application pipeline (`ICombatSkillActionValidator` + `ICombatSkillEffectResolver` + `IEnemyCombatTurnResolver`, `CombatStatus`, metrics, action records, player-state sync, multi-target). These are **two separate damage calculators**, not a thin wrapper. Option A (delegate `/actions`→canonical) and Option B (migrate tests then delete `SubmitCombatAction`) both require iterating against a green build/test loop, which is unavailable here. Per the brief's Option C, this is recorded as **blocking**: see §5. |
| 4 | Dead frontend `features/combats` | **Fixed** | Deleted; `useCombatStore` resolves to a single definition. Verified by `vue-tsc` + `vitest` green. |

### P1

| # | Item | Status | Detail |
|---|------|--------|--------|
| 5 | Non-uniform frontend HTTP access | **Fixed** | `features/combat/api/combatApi.ts` uses `gameEngineApi`. Validated by green build + tests. |
| 6 | Split combat HTTP surface | **Fixed (code, unverified)** | `UseItemInCombat` action + `UseItemInCombatRequest` record moved from `RunsController` to `CombatsController` as `[HttpPost("{combatId:guid}/item-actions")]`. Public route unchanged (`api/v2/runs/{runId}/combats/{combatId}/item-actions`); DTO, command and handler unchanged. Now `GET`, `actions` (legacy), `skill-actions`, `item-actions` all live in `CombatsController`; `RunsController` keeps runs/nodes/rooms/progression and its now-unused `Combats.Actions`/`UseItemInCombat` usings were removed. Needs local `dotnet build`. |
| 7 | Duplicated `IClock` / under-used shared kernel | **Fixed (code, unverified)** | Deleted `Application/Abstractions/IClock.cs` and `Infrastructure/Clock/SystemClock.cs`. Added `using Leds.SharedBuildingBlocks.Time;` to the 7 handlers + 6 unit-test files referencing `IClock`; DI now imports the shared `Time` namespace so `AddSingleton<IClock, SystemClock>()` binds the shared types. No local `IClock`/`SystemClock` remain; no references to the old `Infrastructure.Clock` namespace. Test-file typo `SystemCloclTests.cs` → `SystemClockTests.cs`. Shared kernel kept technical-only (`Result`/`Error`/`IClock`/`SystemClock`); Catalog/Player not forced onto it. Needs local `dotnet build`/`dotnet test`. |
| 8 | Incomplete docker-compose | **Fixed** | `docker-compose.yml` and `docker-compose.dev.yml` both define `game-engine-postgres` (5432), `player-postgres` (5433), `catalog-postgres` (5434) with volumes. |

### P2 / P3

| # | Item | Status | Detail |
|---|------|--------|--------|
| 9 | EF migrations output-dir incoherent | **Documented** | Not relocated (EF snapshot risk). Future migrations must target `Persistence/Migrations` (§6). |
| 10 | Placeholders / HelloWorld | **Fixed / Documented** | `HelloWorld.vue` deleted. `PalaceMapPlaceholder.vue` kept (used by `RunPage.vue`); rename deferred (§5). Empty service/app placeholders documented (§7). |
| 11 | Heavy legacy tree | **Deferred (dedicated PR)** | Archive in place: branch `legacy/v1`, tag `v1-final-archive`, working branch `chore/remove-legacy-v1-sources`. Removal handled by `chore(repo): remove legacy v1 sources`, not bundled here. |
| 12 | Broken Git LFS on legacy | **Documented** | Moot once `legacy/` is removed (#11). `git-lfs` is also absent from the local hook chain. Interim steps in §6. |

### Discovered during validation

| Item | Status | Detail |
|------|--------|--------|
| Frontend build red (`tsconfig.app.json` `baseUrl`) | **Fixed** | Removed deprecated `baseUrl` (TS5101); `paths` rewritten to `@/* → ./src/*` (resolved relative to the config since TS 5+). |
| Frontend unused-var errors | **Fixed** | `noUnusedLocals`/`noUnusedParameters` flagged an unused `i` index in `LawsPopover.vue` and `PalaceLawPanel.vue` (`v-for="(law, i)"`). Changed to `v-for="law"` (key already `law.key`; render identical). |
| `npm run typecheck` / `npm run lint` | **Documented** | These scripts **do not exist** in `package.json`. Available scripts: `dev`, `build` (`vue-tsc -b && vite build` = typecheck + bundle), `preview`, `test` (`vitest run`), `test:watch`. Validation used `build` + `test`. |

## 4. Files touched (second pass cumulative)

**Created**: `packages/shared-building-blocks/tests/.../Clock/SystemClockTests.cs`, `docs/audits/alpha-0.7-stabilization-audit-remediation.md`, `docs/follow-up/analyse-incoherences-2026-06-16.md`.

**Modified**: `.env.example`, `README.md`, `apps/game-client/.env.example`, `apps/game-client/tsconfig.app.json`, `apps/game-client/src/features/combat/api/combatApi.ts`, `apps/game-client/src/features/palace-laws/LawsPopover.vue`, `apps/game-client/src/features/palace-laws/PalaceLawPanel.vue`, `docker-compose.yml`, `docker-compose.dev.yml`, `scripts/dev/apply-migrations.ps1`, `scripts/dev/start-dev.ps1`, `services/catalog/.../CatalogDbContextFactory.cs`, `services/game-engine/.../Controllers/CombatsController.cs`, `services/game-engine/.../Controllers/RunsController.cs`, `services/game-engine/.../Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs`, the 7 game-engine command handlers and 6 unit-test files that reference `IClock`.

**Deleted**: `apps/game-client/src/features/combats/` (whole), `apps/game-client/src/shared/components/HelloWorld.vue`, `services/game-engine/.../Application/Abstractions/IClock.cs`, `services/game-engine/.../Infrastructure/Clock/SystemClock.cs`, `packages/.../Clock/SystemCloclTests.cs` (renamed).

## 5. Blocking issue before alpha-0.8 — legacy combat resolution path

```
Blocking issue before alpha-0.8:
legacy combat resolution path (POST /api/v2/runs/{runId}/combats/{combatId}/actions,
SubmitCombatActionCommandHandler) still exists and DIVERGES from the canonical
skill-actions / item-actions path (UseCombatSkillCommandHandler):
  - legacy: Domain combat.SubmitAction + CombatState, single-target, BasicAttack only.
  - canonical: ICombatSkillEffectResolver pipeline + CombatStatus + metrics + records.
Two separate damage calculators. The endpoint is marked [Obsolete] but not delegated.
```

Resolution (next backend PR, with a working build/test loop):
- **Option A (preferred):** rewrite `SubmitCombatActionCommandHandler` to map `ActionType=BasicAttack` to the canonical basic-attack `SkillKey` and call the same validator/effect-resolver pipeline, adapting the result back to `SubmitCombatActionResponse`. Keep `/actions` as a thin compat facade.
- **Option B:** migrate `ProgressRunEndpointTests` / `RunIntegrationTestBase` and the `SubmitCombatAction` unit tests to `skill-actions`, then delete `Combats/SubmitCombatAction/*`, the `/actions` endpoint and `SubmitCombatActionRequest`/`Response`.

Other deferred cleanups: `IClock` dedup verification (build), `item-actions` move verification (build), rename `PalaceMapPlaceholder.vue` → `PalaceMapPanel.vue` (+ update `RunPage.vue` import).

## 6. Operational notes

Canonical EF migrations dir (game-engine):
```
dotnet ef migrations add <Name> --project src/Leds.GameEngine.Infrastructure \
  --startup-project src/Leds.GameEngine.Api --context GameEngineDbContext \
  --output-dir Persistence/Migrations
```
Legacy removal (#11/#12): on the dedicated chore PR, `git rm -r legacy/` (archive branch `legacy/v1` + tag `v1-final-archive` already exist); this also clears the broken-LFS jpgs. If legacy must stay temporarily, install `git-lfs` and `git lfs migrate import --include="*.jpg,*.mp3,*.wav,*.fbx"`.

## 7. Microservice boundaries (compliance check)

No cross-service FK, no `CatalogDbContext`/`PlayerDbContext` reference from Game Engine, no Catalog/Player domain entity used inside Game Engine — none introduced. Game Engine still consumes Catalog content through its `ICatalogContentGateway` port. Shared kernel stays technical-only (`Result`/`Error`/`IClock`/`SystemClock`). Placeholder services (`api-gateway`, `audit-gdpr`, `admin-portal`, `player-portal`) remain empty `.gitkeep` and should be presented as planned, not implemented.

## 8. Validation

| Check | Result |
|-------|--------|
| `npm install` (game-client) | OK (fetched the Linux native binaries missing from the committed install). |
| `npm run build` (`vue-tsc -b && vite build`) | **PASS** (exit 0; 134 modules; `dist/` produced). |
| `npm run test` (`vitest run`) | **PASS** (13/13, incl. combat store). |
| `dotnet build/test` game-engine, catalog, shared | **NOT RUN** — no .NET SDK in this environment, proxy blocks installing one, targets `net10.0`. **Blocking gate** — must be run locally. |
| `docker compose ... up` + `apply-migrations.ps1` | **NOT RUN** — no Docker in this environment. Run locally to confirm Catalog provisioning. |

### Required before merge (dev machine)
```powershell
cd apps/game-client; npm install; npm run build; npm run test
dotnet build services/game-engine/Leds.GameEngine.slnx
dotnet test  services/game-engine/Leds.GameEngine.slnx
dotnet build services/catalog/Leds.Catalog.slnx
dotnet test  services/catalog/Leds.Catalog.slnx
dotnet test  packages/shared-building-blocks/Leds.SharedBuildingBlocks.slnx
docker compose -f docker-compose.dev.yml up -d
./scripts/dev/apply-migrations.ps1
```

## 9. Recommendation for the next PR

1. Run the §8 backend commands; fix any compile fallout from the IClock dedup / `item-actions` move (mechanical edits, not compiler-verified here).
2. Resolve the blocking combat path via Option A or B (§5).
3. `chore(repo): remove legacy v1 sources` — delete `legacy/`, clearing the heavy tree and broken LFS.
