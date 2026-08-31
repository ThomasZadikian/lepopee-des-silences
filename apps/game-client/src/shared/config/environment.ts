const gameEngineApiUrl = import.meta.env.VITE_GAME_ENGINE_API_URL;
const playerApiUrl = import.meta.env.VITE_PLAYER_API_URL;

if (!gameEngineApiUrl) {
  console.warn(
    '[game-client] VITE_GAME_ENGINE_API_URL is not defined. Falling back to api.',
  );
}

if (!playerApiUrl) {
  console.warn(
    '[game-client] VITE_PLAYER_API_URL is not defined. Falling back to the Player development API.',
  );
}

export const environment = {
  gameEngineApiUrl: gameEngineApiUrl ?? 'http://localhost:5187',
  playerApiUrl: playerApiUrl ?? 'http://localhost:5189',
} as const;
