# alpha-0.8.1 Palace Laws And Room Climate

## Delivered

- Palace law event content now lists active Catalog laws and filters out laws already active on the run.
- Palace law selection is deterministic from the event context and does not use unseeded random APIs.
- The law shown by event resolution is the law applied by choice resolution via `accept-law:<lawKey>`.
- Game Engine no longer resolves runtime LAW choices through `StaticPalaceLawCatalog`.
- Catalog palace law DTOs expose `effectSetKey` and public effect definitions for HTTP consumers.
- Catalog seed includes runtime palace laws and room climate laws without an EF migration.
- Room climate is represented as a room-scoped `RunModifierType.RoomClimate`, avoiding changes to `RoomEntity`.
- `RoomDto.activeClimate` exposes the active room climate when present.
- Combat creation applies non-DOT climate effects: `Rain` grants starting guard, `Grey` lowers enemy skill power, `Heatwave` raises enemy skill power.

## Explicit Limit

- `Hail` is selectable and exposed as active room climate, but no DOT runtime was added. The current combat model has no status/DOT lifecycle, and adding one would exceed the bounded refactor target.

## Validation

- `dotnet build services/game-engine/Leds.GameEngine.slnx`
- `dotnet test services/game-engine/Leds.GameEngine.slnx`
- `dotnet build services/catalog/Leds.Catalog.slnx`
- `dotnet test services/catalog/Leds.Catalog.slnx`
