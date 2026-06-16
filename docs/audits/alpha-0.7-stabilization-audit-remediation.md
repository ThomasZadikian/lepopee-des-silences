# Alpha 0.7 — Stabilization Audit Remediation

- **PR**: `refactor(repo): stabilize alpha 0.7 architecture after audit`
- **Target version**: `alpha-0.7.15-stabilization`
- **Branch**: `refactor/stabilize-alpha-0.7-architecture`
- **Date**: 2026-06-16
- **Scope**: stabilization, cleanup and architecture clarification only. **No gameplay feature.** No Markov / ATB / new rewards / new rooms / new combat logic. No UX change.

## 1. Source of the audit

Static analysis of the repository dated 2026-06-16, recorded in
[`docs/follow-up/analyse-incoherences-2026-06-16.md`](../follow-up/analyse-incoherences-2026-06-16.md).
It reported: backend/frontend dual combat system, dead frontend folder `features/combats`,
combat store collision, duplicated `IClock/SystemClock`, under-used `shared-building-blocks`,
contradictory frontend API ports, Catalog not provisioned in Docker/env, incomplete
docker-compose files, split combat HTTP surface, EF migrations spread across two folders,
heavy legacy tree with broken Git LFS, and structural placeholders.

## 2. Status legend

`Fixed` — corrected and (where possible) validated in this PR.
`Mitigated` — risk materially reduced; a residual remains, tracked below.
`Documented` — intentionally not changed; rationale + remediation path recorded.
`Deferred` — postponed to a follow-up PR with a concrete plan.
`Not reproduced` — claim did not hold on closer inspection.

## 3. Item-by-item remediation

### P0

| # | Item | Status | Detail |
|---|------|--------|--------|
| 1 | Contradictory API ports | **Fixed** | `apps/game-client/.env.example` corrected from `http://localhost:5000` to `http://localhost:5187`. The code fallback (`src/shared/config/environment.ts`) and the root `.env.example` were already on `5187` and are now consistent. Env override (`VITE_GAME_ENGINE_API_URL`) is preserved. |
| 2 | Catalog not provisionable | **Fixed** | Added `catalog-postgres` (5434→5432, db `leds_catalog`) to `docker-compose.dev.yml` and `docker-compose.yml`. Added `CATALOG_DB_*` and `CATALOG_DB_CONNECTION_STRING` to root `.env.example`. `CatalogDbContextFactory` (design-time) now reads `CATALOG_DB_CONNECTION_STRING` with a documented localhost fallback instead of a hardcoded string, so `dotnet ef` targets the provisioned DB. `scripts/dev/apply-migrations.ps1` now applies Catalog migrations; `scripts/dev/start-dev.ps1` lists the Catalog DB. **Note:** `**/appsettings.json` is gitignored repo-wide (line 462) — game-engine's own `appsettings.json` is untracked too — so no Catalog `appsettings.json` is committed. Runtime stays `InMemory` by default; Postgres mode is opt-in via env (`Persistence__Mode=Postgres`, `ConnectionStrings__CatalogDb=…`), which the runtime DI already reads (`configuration.GetConnectionString("CatalogDb")`). |
| 3 | Dual combat system (backend/frontend) | **Mitigated** | Canonical flow declared = `skill-actions` + `item-actions` (`UseCombatSkill` / `UseItemInCombat`), the flow the live client consumes. The legacy `POST .../combats/{combatId}/actions` (`SubmitCombatAction`) endpoint is marked `[Obsolete]` with an XML doc pointing to the canonical endpoints and an alpha-0.8.x removal target; it is kept as a compatibility facade because integration/unit tests still exercise it. The frontend half of the duplication was removed (see #4). **Residual (Deferred):** physically deleting or delegating the legacy handler, and removing any divergence between the two damage paths, is **not** done here — it touches combat business logic (out of scope for a no-gameplay PR) and could not be compile-verified in this environment (no .NET SDK). |
| 4 | Dead frontend `features/combats` | **Fixed** | Verified 0 references, then deleted `apps/game-client/src/features/combats/` entirely. Only `features/combat/` remains; `useCombatStore` now resolves to a single definition. |

### P1

| # | Item | Status | Detail |
|---|------|--------|--------|
| 5 | Non-uniform frontend HTTP access | **Fixed** | `features/combat/api/combatApi.ts` now goes through the shared `gameEngineApi` wrapper (like `runs`/`events`/`inventory`/`rewards`) instead of calling `httpRequest` directly. Routes and DTOs unchanged. Type-checked clean (`vue-tsc`). |
| 6 | Split combat HTTP surface | **Deferred** | `item-actions` still lives in `RunsController` while `GET`/`actions`/`skill-actions` live in `CombatsController`, under the same `api/v2/runs/{runId}/combats/{combatId}` prefix. Moving the action (route-preserving) is mechanically simple but moves handler wiring/usings across controllers and **cannot be compile-verified here** (no .NET SDK). Plan in §5. Public routes unchanged regardless. |
| 7 | `shared-building-blocks` under-used / duplicated `IClock` | **Partially Fixed / Deferred** | **Fixed:** test file typo `SystemCloclTests.cs` → `SystemClockTests.cs` (class was already `SystemClockTests`). **Deferred:** removing the local `Leds.GameEngine.Application.Abstractions.IClock` + `Infrastructure/Clock/SystemClock` in favor of the shared `Leds.SharedBuildingBlocks.Time` versions is a ~16-file cross-project change (7 handlers, DI, ~6 test files, 2 deletions) that cannot be compiled/verified in this environment. Plan in §5. `shared-building-blocks` kept as a minimal technical kernel (`Result`/`Error`/`IClock`/`SystemClock`); no gameplay domain added. Catalog/Player were **not** forced onto the shared package (would be a large refactor; see #9 of the original audit). |
| 8 | Incomplete docker-compose | **Fixed** | `docker-compose.yml` now contains `game-engine-postgres` (5432), `player-postgres` (5433) and `catalog-postgres` (5434) with matching volumes. `docker-compose.dev.yml` now contains all three as well. The two files are consistent and self-sufficient for local DBs. |

### P2 / P3

| # | Item | Status | Detail |
|---|------|--------|--------|
| 9 | EF migrations output-dir incoherent | **Documented** | game-engine has one `GameEngineDbContext` but migrations sit in both `Migrations/` (canonical) and `Migrations/GameEngine/` (one migration). Not moved — relocating applied migrations risks EF model-snapshot drift. Future migrations must target the canonical folder: see §6. |
| 10 | Placeholders / HelloWorld | **Fixed / Documented** | `HelloWorld.vue` deleted (unreferenced). `PalaceMapPlaceholder.vue` **kept** — it is still imported by `RunPage.vue`; only its name is misleading, rename deferred (§5). Empty service/app placeholders documented in §7. |
| 11 | Heavy legacy tree | **Deferred** | `legacy/` ≈ 3160 files / ~37 MB (full `unity-v1`, old `RPG_ESI07` backend, old frontend). Archive already exists: branch `legacy/v1` and tag `v1-final-archive` (both local and origin), plus a dedicated branch `chore/remove-legacy-v1-sources`. Removal is therefore handled by that dedicated PR, not bundled into this stabilization PR (keeps the diff reviewable). Strategy in §6. |
| 12 | Broken Git LFS on legacy | **Documented** | `.gitattributes` declares `*.jpg filter=lfs`, but legacy jpgs are committed as 131-byte LFS pointers while the working tree holds the real ~127 KB images, so they show as permanently modified. `git-lfs` is also absent from the toolchain used here. Becomes moot once `legacy/` is removed (#11); interim steps in §6. Not touched in this PR to avoid mixing binary/LFS churn into a code-stabilization diff. |
| 13 | Migration churn | **Documented** | 17 migrations all dated 2026-06-16 (`Align…`, `Resolve…PendingModelChanges`) indicate a still-moving data model. Process note, not a code change. Recommend a model freeze before alpha-0.8.x. |
| 14 | Service/app placeholders | **Documented** | `apps/admin-portal`, `apps/player-portal`, `services/api-gateway`, `services/audit-gdpr` contain only `.gitkeep`. Kept (on roadmap) but must be labeled "planned", not "implemented". README already marks several as `futur`; see §7. |

### Discovered during validation (not in original audit)

| Item | Status | Detail |
|------|--------|--------|
| Frontend build blocked by `tsconfig.app.json` | **Documented (pre-existing)** | `vue-tsc` fails with `TS5101: Option 'baseUrl' is deprecated` (TypeScript ~6.0.2). This exists independently of this PR (tsconfig untouched) and would already break `npm run build`. Fix is a one-liner (`"ignoreDeprecations": "6.0"` or drop `baseUrl`) but is outside the audit scope; flagged here so it is fixed before relying on CI green. |

## 4. Files touched

**Created**
- `packages/shared-building-blocks/tests/Leds.SharedBuildingBlocks.UnitTests/Clock/SystemClockTests.cs`
- `docs/audits/alpha-0.7-stabilization-audit-remediation.md` (this file)

**Modified**
- `apps/game-client/.env.example` (port 5000 → 5187)
- `apps/game-client/src/features/combat/api/combatApi.ts` (uses `gameEngineApi`)
- `docker-compose.yml` (+ player + catalog DBs)
- `docker-compose.dev.yml` (+ catalog DB)
- `.env.example` (+ Catalog DB vars)
- `services/catalog/src/Leds.Catalog.Infrastructure/Persistence/CatalogDbContextFactory.cs` (env-driven connection string)
- `services/game-engine/src/Leds.GameEngine.Api/Controllers/CombatsController.cs` (legacy `actions` endpoint marked `[Obsolete]` + doc)
- `scripts/dev/apply-migrations.ps1` (+ Catalog step)
- `scripts/dev/start-dev.ps1` (+ Catalog DB line)
- `README.md` (local databases section)

**Deleted**
- `apps/game-client/src/features/combats/` (whole dead feature)
- `apps/game-client/src/shared/components/HelloWorld.vue`
- `packages/shared-building-blocks/tests/Leds.SharedBuildingBlocks.UnitTests/Clock/SystemCloclTests.cs` (renamed)

## 5. Deferred work — concrete plans for alpha-0.8.x

**(7) Deduplicate `IClock` onto the shared kernel**
1. Delete `services/game-engine/src/Leds.GameEngine.Application/Abstractions/IClock.cs`.
2. Delete `services/game-engine/src/Leds.GameEngine.Infrastructure/Clock/SystemClock.cs`.
3. Add `using Leds.SharedBuildingBlocks.Time;` to every file resolving `IClock`: the ~7 handlers (`SubmitCombatAction`, `AbandonRun`, `ExitMidRoom`, `SaveAndExitRun`, `StartRun`, `UseCombatSkill`, `UseItemInCombat`), the Infrastructure DI extension, and the ~6 unit-test files that reference `IClock`.
4. In `InfrastructureServiceCollectionExtensions`, register the shared clock: `services.AddSingleton<IClock, SystemClock>();` now resolving `Leds.SharedBuildingBlocks.Time.*`; remove the now-empty `using Leds.GameEngine.Infrastructure.Clock;`.
5. `dotnet build` + `dotnet test` the game-engine solution to confirm.

**(6) Consolidate combat HTTP surface**
- Move the `UseItemInCombat` action from `RunsController` to `CombatsController` as `[HttpPost("{combatId:guid}/item-actions")]` (controller route already supplies `runId`), moving the `UseItemInCombatRequest` record and the `Leds.GameEngine.Application.Runs.UseItemInCombat` using along with it. Public route stays `api/v2/runs/{runId}/combats/{combatId}/item-actions`. Verify with a build + the existing integration tests.

**(3) Retire the legacy combat path**
- Once tests are migrated to `skill-actions`/`item-actions`, delete `Combats/SubmitCombatAction/*`, the `actions` endpoint, and `SubmitCombatActionRequest`/`Response`. Confirm a single damage/resolution path remains.

**(10) Rename `PalaceMapPlaceholder.vue`**
- Rename to a non-placeholder name (e.g. `PalaceMapPanel.vue`) and update the import in `RunPage.vue`.

## 6. Operational notes

**Canonical EF migrations folder (game-engine)** — generate into the canonical directory:
```
dotnet ef migrations add <Name> \
  --project src/Leds.GameEngine.Infrastructure \
  --startup-project src/Leds.GameEngine.Api \
  --context GameEngineDbContext \
  --output-dir Persistence/Migrations
```
Do not let EF default to `Persistence/Migrations/GameEngine`.

**Legacy removal (#11/#12)** — archive already in place:
- branch `legacy/v1`, tag `v1-final-archive`, working branch `chore/remove-legacy-v1-sources`.
- Final step: `git rm -r legacy/` on the dedicated chore PR (`chore(repo): remove legacy v1 sources`), keeping it out of this stabilization diff. This also removes the broken-LFS jpgs, making the LFS issue moot. If legacy must stay temporarily, install `git-lfs` and re-track the binaries (`git lfs migrate import --include="*.jpg,*.mp3,*.wav,*.fbx"`).

## 7. Microservice boundaries (compliance check)

This PR respects the mandated boundaries. No cross-service foreign key, no direct `CatalogDbContext`/`PlayerDbContext` reference from Game Engine, and no Catalog/Player domain entity used inside Game Engine were introduced or removed. Game Engine keeps consuming Catalog content through its existing `ICatalogContentGateway` port (HTTP/in-memory), not through Catalog persistence. The shared kernel stays technical-only (`Result`/`Error`/`IClock`); no volatile gameplay domain was added to it. Placeholder services (`api-gateway`, `audit-gdpr`, `admin-portal`, `player-portal`) remain empty and should be presented as planned, not implemented.

## 8. Validation performed (this environment)

| Check | Result |
|-------|--------|
| `vue-tsc -b` (frontend typecheck) | Passes for all PR changes. Only failure is the **pre-existing** `tsconfig.app.json` `baseUrl` deprecation (TS5101), unrelated to this PR. No dangling imports from the `combats`/`HelloWorld` deletions; `combatApi.ts` refactor is type-clean. |
| `vitest` (frontend unit) | Could not run — installed `node_modules` lacks the Linux native `rolldown` binary (deps were installed on another OS). Re-run on the dev machine. |
| `dotnet build` / `dotnet test` (all services) | Could not run — no .NET SDK in this environment. **Must be run on the dev machine before merge**, especially the game-engine solution (controller `[Obsolete]` attribute) and catalog solution (factory change + new appsettings). |

### Required validation before merge (dev machine)
```powershell
# Frontend
cd apps/game-client
npm install
npm run build            # fix the pre-existing tsconfig baseUrl error first

# Backend (touched services)
dotnet build services/game-engine/Leds.GameEngine.slnx
dotnet test  services/game-engine/Leds.GameEngine.slnx
dotnet build services/catalog/Leds.Catalog.slnx
dotnet test  services/catalog/Leds.Catalog.slnx
dotnet test  packages/shared-building-blocks/Leds.SharedBuildingBlocks.slnx

# DB provisioning smoke test
docker compose -f docker-compose.dev.yml up -d
./scripts/dev/apply-migrations.ps1
```

## 9. Remaining risks before alpha-0.8.x

- Legacy combat endpoint still live (compat facade). Two resolution paths exist until the deferred retirement.
- `IClock` duplication still present in code (only the test typo fixed); harmless at runtime, to be deduped per §5.
- Frontend `npm run build` is red until the pre-existing `tsconfig.app.json` `baseUrl` error is fixed.
- Backend build/test not executed here; the C# edits (additive: `[Obsolete]`, factory env read, new appsettings) are low-risk but unverified — run the commands in §8.
- Data model still churning (17 same-day migrations); recommend a freeze before alpha-0.8.x.

## 10. Recommendation for the next PR

1. `chore(repo): remove legacy v1 sources` — delete `legacy/`, resolving the heavy tree and the broken LFS at once.
2. A small backend PR applying §5 (IClock dedup + combat HTTP consolidation + legacy combat retirement) behind a working `dotn