# Alpha 0.8.1 - Game Client DevTools

## Scope

- Adds a frontend-only debug panel for local development against `/api/dev/v2` game-engine endpoints.
- The panel is available only when `import.meta.env.DEV === true` and `VITE_GAME_CLIENT_DEVTOOLS_ENABLED=true`.
- No devtools token is committed. The token is entered in the UI and stored in local `localStorage` only.

## Safety

- No Markov matrices, weights, probabilities, adaptive scores, or internal selection reasons are displayed.
- The client does not simulate gameplay mutations. It calls the backend endpoint, then reloads run/combat state from the server.
- Production builds do not render the frontend devtools toggle or panel.
- Existing runtime debug rendering on `RunPage` is now restricted to `import.meta.env.DEV`.

## Supported Actions

- Check backend devtools status.
- Advance one or more rooms.
- Force current room `PalaceRoomState`.
- Force current room `RoomClimate`.
- Activate or clear palace laws.
- Activate or clear curses.
- Kill all current enemies or one enemy.
- Set combatant vitality and guard.
- Try applying a status effect; this can return unavailable until backend runtime status exists.

## Local Usage

1. Enable backend devtools locally with `GAME_ENGINE_DEVTOOLS_ENABLED=true` and a local `GAME_ENGINE_DEVTOOLS_TOKEN`.
2. Enable the client panel with `VITE_GAME_CLIENT_DEVTOOLS_ENABLED=true`.
3. Start the game client in dev mode.
4. Open a run and use the `DevTools` floating button.
5. Enter the same local backend token in the panel.
